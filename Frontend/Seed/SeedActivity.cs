using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Seed;

/// <summary>
/// Generates 26 months of realistic activity for Green City Bassam (Jan 2024 → Mar 2026).
/// Called after SeedGreenCity.SeedAsync() creates the structural data.
/// </summary>
public static class SeedActivity
{
    private static readonly Random Rng = new(42); // Reproducible
    private static DateTime Utc(int y, int m, int d, int h = 0) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StartDate = Utc(2024, 1, 1);
    private static readonly DateTime EndDate = Utc(2026, 3, 23);

    public static async Task SeedAsync(GreenSyndicDbContext db, UserManager<ApplicationUser> userManager, Func<string, Task>? progress = null)
    {
        async Task log(string msg) { if (progress != null) await progress(msg); }
        // Skip if already seeded
        if (await db.Leases.IgnoreQueryFilters().AnyAsync()) return;

        var org = await db.Organizations.IgnoreQueryFilters().FirstAsync();
        var orgId = org.Id;
        var buildings = await db.Buildings.IgnoreQueryFilters().Where(b => b.OrganizationId == orgId).ToListAsync();
        var units = await db.Units.IgnoreQueryFilters().Where(u => u.OrganizationId == orgId).ToListAsync();
        var owners = await db.Owners.IgnoreQueryFilters().Where(o => o.OrganizationId == orgId).ToListAsync();
        var suppliers = await db.Suppliers.IgnoreQueryFilters().Where(s => s.OrganizationId == orgId).ToListAsync();
        var chargeDefs = await db.ChargeDefinitions.IgnoreQueryFilters().ToListAsync();
        var coOwnerships = await db.CoOwnerships.IgnoreQueryFilters().Where(c => c.OrganizationId == orgId).ToListAsync();

        var apartmentBuildings = buildings.Where(b => b.Name.Contains("Acajou") || b.Name.Contains("Baobab") || b.Name.Contains("Cèdre") || b.Name.Contains("Dattier") || b.Name.Contains("Ébène")).ToList();
        var cosmosUnits = units.Where(u => u.Reference.StartsWith("COS-")).ToList();
        var apartments = units.Where(u => u.Reference.StartsWith("A-")).ToList();
        var adminUser = await userManager.FindByEmailAsync("admin@greensyndic.ci");
        var adminUserId = adminUser?.Id ?? "";

        // ═══════════════════════════════════════════
        // 1. CREATE IDENTITY USERS FOR DEMO
        // ═══════════════════════════════════════════
        await log("1/17 — Creation des comptes utilisateurs (10 proprios, 2 employes)...");
        var demoPassword = "Demo2026!";

        // Helper: create user then assign role via raw SQL to avoid EF batch ordering issue
        async Task<string?> CreateUserWithRole(string email, string firstName, string lastName, string role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) return existing.Id;
            var user = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                FirstName = firstName, LastName = lastName, OrganizationId = orgId
            };
            var result = await userManager.CreateAsync(user, demoPassword);
            if (!result.Succeeded) return null;
            // Raw SQL to avoid EF batching UserRoles before Users
            var roleId = await db.Database.SqlQueryRaw<string>(
                $"SELECT \"Id\" AS \"Value\" FROM \"AspNetRoles\" WHERE \"NormalizedName\" = '{role.ToUpperInvariant()}'")
                .FirstOrDefaultAsync();
            if (roleId != null)
                await db.Database.ExecuteSqlRawAsync(
                    $"INSERT INTO \"AspNetUserRoles\" (\"UserId\", \"RoleId\") VALUES ('{user.Id}', '{roleId}')");
            return user.Id;
        }

        // 10 owners with Identity accounts (first 10)
        for (int i = 0; i < Math.Min(10, owners.Count); i++)
        {
            var o = owners[i];
            var email = $"{o.FirstName.ToLower()}.{o.LastName.ToLower().Replace("'", "")}@greencity.ci";
            o.Email = email;
            var role = o.IsCouncilMember ? "CouncilMember" : "Owner";
            var userId = await CreateUserWithRole(email, o.FirstName, o.LastName, role);
            if (userId != null) o.UserId = userId;
        }

        // 1 technician + 1 accountant
        await CreateUserWithRole("technicien@greencity.ci", "Moussa", "Koné", "SyndicTechnician");
        await CreateUserWithRole("compta@greencity.ci", "Awa", "Traoré", "SyndicAccountant");

        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 2. LEASE TENANTS (10)
        // ═══════════════════════════════════════════
        await log("2/17 — Creation des locataires (5 residentiels, 5 commerciaux)...");
        var tenantNames = new[]
        {
            ("Amadou", "Diallo", null as string, "Résident"),
            ("Fatou", "Konaté", null, "Résident"),
            ("Ibrahim", "Sylla", null, "Résident"),
            ("Mariame", "Bamba", null, "Résident"),
            ("Sekou", "Traoré", null, "Résident"),
            ("KFC CI", "SARL", "KFC Côte d'Ivoire", "Commercial"),
            ("SGBCI", "SA", "Société Générale", "Commercial"),
            ("Clinique", "Bassam", "Clinique Internationale de Bassam", "Commercial"),
            ("Pathé", "Gaumont", "Cinéma Pathé Grand-Bassam", "Commercial"),
            ("Groupe", "Scolaire", "École Internationale Green City", "Commercial"),
        };

        var tenants = new List<LeaseTenant>();
        foreach (var (fn, ln, company, type) in tenantNames)
        {
            var email = company != null ? $"contact@{fn.ToLower().Replace(" ", "")}.ci" : $"{fn.ToLower()}.{ln.ToLower()}@email.ci";
            var tenant = new LeaseTenant
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                FirstName = fn, LastName = ln, CompanyName = company,
                Email = email, Phone = $"+225 07{Rng.Next(10000000, 99999999)}"
            };
            tenants.Add(tenant);

            // Create Identity user via raw SQL role assignment
            var role = company != null ? "CommercialTenant" : "Tenant";
            var tUserId = await CreateUserWithRole(email, fn, ln, role);
            if (tUserId != null) {
                tenant.UserId = tUserId;
            }
        }
        db.LeaseTenants.AddRange(tenants);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 3. LEASES (10)
        // ═══════════════════════════════════════════
        await log("3/17 — Creation des baux (10 contrats)...");
        var resApts = apartments.OrderBy(_ => Rng.Next()).Take(5).ToList();
        var comUnits = cosmosUnits.OrderBy(_ => Rng.Next()).Take(5).ToList();
        decimal[] resRents = [150_000, 180_000, 200_000, 250_000, 175_000];
        decimal[] comRents = [1_500_000, 2_000_000, 1_800_000, 1_200_000, 800_000];

        var leases = new List<Lease>();
        for (int i = 0; i < 5; i++)
        {
            var startMonth = 1 + i; // Jan-May 2024
            leases.Add(new Lease
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                UnitId = resApts[i].Id, LeaseTenantId = tenants[i].Id,
                Reference = $"BAIL-{(i + 1):D3}", Type = LeaseType.Residential,
                Status = i == 4 ? LeaseStatus.Terminated : LeaseStatus.Active,
                StartDate = Utc(2024, startMonth, 1), EndDate = i == 4 ? Utc(2025, 12, 31) : (DateTime?)null,
                DurationMonths = 12, MonthlyRent = resRents[i], Charges = 25_000,
                SecurityDeposit = resRents[i] * 2
            });
            resApts[i].Status = i == 4 ? UnitStatus.Available : UnitStatus.Occupied;
        }
        for (int i = 0; i < 5; i++)
        {
            leases.Add(new Lease
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                UnitId = comUnits[i].Id, LeaseTenantId = tenants[5 + i].Id,
                Reference = $"BAIL-{(6 + i):D3}", Type = LeaseType.Commercial,
                Status = LeaseStatus.Active,
                StartDate = Utc(2024, 1, 1), DurationMonths = 36,
                MonthlyRent = comRents[i], Charges = comRents[i] * 0.15m,
                SecurityDeposit = comRents[i] * 3
            });
            comUnits[i].Status = UnitStatus.Occupied;
        }
        db.Leases.AddRange(leases);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 4. RENT CALLS + RECEIPTS + PAYMENTS (monthly)
        // ═══════════════════════════════════════════
        await log("4/17 — Appels de loyers, quittances et paiements (26 mois)... c'est le plus long");
        var rentCalls = new List<RentCall>();
        var rentReceipts = new List<RentReceipt>();
        var payments = new List<Payment>();
        var paymentMethods = new[] { PaymentMethod.OrangeMoney, PaymentMethod.OrangeMoney, PaymentMethod.MtnMoney, PaymentMethod.MtnMoney, PaymentMethod.BankTransfer, PaymentMethod.BankTransfer, PaymentMethod.Wave, PaymentMethod.Cash };
        int payRef = 1, rcRef = 1, rrRef = 1;

        foreach (var lease in leases)
        {
            var leaseStart = lease.StartDate;
            var leaseEnd = lease.Status == LeaseStatus.Terminated ? lease.EndDate!.Value : EndDate;
            var current = new DateTime(leaseStart.Year, leaseStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (current <= leaseEnd)
            {
                var year = current.Year;
                var month = current.Month;
                var dueDate = Utc(year, month, 5);
                var total = lease.MonthlyRent + (lease.Charges ?? 0);
                var isPastMonth = current < Utc(2026, 3, 1);

                // Determine status
                RentCallStatus rcStatus;
                decimal paidAmt = 0;
                if (!isPastMonth)
                {
                    rcStatus = RentCallStatus.Sent;
                }
                else
                {
                    var roll = Rng.NextDouble();
                    if (roll < 0.85) { rcStatus = RentCallStatus.Paid; paidAmt = total; }
                    else if (roll < 0.95) { rcStatus = RentCallStatus.Overdue; paidAmt = 0; }
                    else { rcStatus = RentCallStatus.PartiallyPaid; paidAmt = Math.Round(total * 0.5m, 0); }
                }

                var rc = new RentCall
                {
                    Id = Guid.NewGuid(), OrganizationId = orgId, LeaseId = lease.Id,
                    Reference = $"AL-{rcRef++:D4}", Year = year, Month = month,
                    PeriodStart = Utc(year, month, 1), PeriodEnd = Utc(year, month, DateTime.DaysInMonth(year, month)),
                    RentAmount = lease.MonthlyRent, ChargesAmount = lease.Charges ?? 0,
                    TotalAmount = total, PaidAmount = paidAmt, RemainingAmount = total - paidAmt,
                    Status = rcStatus, DueDate = dueDate,
                    SentAt = dueDate.AddDays(-3), PaidAt = paidAmt > 0 ? dueDate.AddDays(Rng.Next(0, 15)) : null
                };
                rentCalls.Add(rc);

                // Payment + Receipt for paid/partial
                if (paidAmt > 0)
                {
                    var payDate = dueDate.AddDays(Rng.Next(0, 15));
                    var pay = new Payment
                    {
                        Id = Guid.NewGuid(), OrganizationId = orgId,
                        Reference = $"PAY-{payRef++:D4}", Amount = paidAmt, Currency = "XOF",
                        Method = paymentMethods[Rng.Next(paymentMethods.Length)],
                        Status = PaymentStatus.Completed, PaymentDate = payDate,
                        LeaseTenantId = lease.LeaseTenantId, LeaseId = lease.Id,
                        Description = $"Loyer {month:D2}/{year} — {lease.Reference}"
                    };
                    payments.Add(pay);

                    if (rcStatus == RentCallStatus.Paid)
                    {
                        rentReceipts.Add(new RentReceipt
                        {
                            Id = Guid.NewGuid(), OrganizationId = orgId,
                            RentCallId = rc.Id, LeaseId = lease.Id,
                            Reference = $"QT-{rrRef++:D4}", Year = year, Month = month,
                            PeriodStart = rc.PeriodStart, PeriodEnd = rc.PeriodEnd,
                            RentAmount = lease.MonthlyRent, ChargesAmount = lease.Charges ?? 0,
                            TotalAmount = total, Status = RentReceiptStatus.Issued,
                            IssuedAt = payDate.AddDays(1), PaymentId = pay.Id
                        });
                    }
                }

                current = current.AddMonths(1);
            }
        }

        db.RentCalls.AddRange(rentCalls);
        db.RentReceipts.AddRange(rentReceipts);
        db.Payments.AddRange(payments);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 5. CHARGE ASSIGNMENTS (quarterly for owners)
        // ═══════════════════════════════════════════
        await log("5/17 — Appels de charges trimestriels (80 proprios x 9 trimestres)...");
        var chargeAssignments = new List<ChargeAssignment>();
        var chargePayments = new List<Payment>();

        // Only units with owners
        var ownedUnits = units.Where(u => u.OwnerId.HasValue).ToList();

        for (int year = 2024; year <= 2026; year++)
        {
            int maxQ = year == 2026 ? 1 : 4;
            for (int quarter = 1; quarter <= maxQ; quarter++)
            {
                foreach (var unit in ownedUnits)
                {
                    var unitChargeDefs = chargeDefs.Where(cd =>
                        cd.CoOwnershipId == unit.CoOwnershipId ||
                        coOwnerships.Any(c => c.Id == cd.CoOwnershipId && c.Level == CoOwnershipLevel.Horizontal)
                    ).ToList();

                    foreach (var cd in unitChargeDefs)
                    {
                        var ratio = (unit.ShareRatio ?? 1m) / 1000m;
                        var amount = Math.Round(cd.AnnualAmount / 4m * ratio, 0);
                        if (amount < 1000) continue; // Skip trivial

                        var dueDate = Utc(year, (quarter - 1) * 3 + 1, 15);
                        var isPast = dueDate < Utc(2026, 3, 1);
                        var isPaid = isPast && Rng.NextDouble() < 0.85;

                        var ca = new ChargeAssignment
                        {
                            Id = Guid.NewGuid(), ChargeDefinitionId = cd.Id, UnitId = unit.Id,
                            Year = year, Quarter = quarter, Amount = amount,
                            PaidAmount = isPaid ? amount : 0, IsPaid = isPaid, DueDate = dueDate
                        };
                        chargeAssignments.Add(ca);

                        if (isPaid)
                        {
                            chargePayments.Add(new Payment
                            {
                                Id = Guid.NewGuid(), OrganizationId = orgId,
                                Reference = $"PAY-{payRef++:D4}", Amount = amount, Currency = "XOF",
                                Method = paymentMethods[Rng.Next(paymentMethods.Length)],
                                Status = PaymentStatus.Completed,
                                PaymentDate = dueDate.AddDays(Rng.Next(0, 20)),
                                OwnerId = unit.OwnerId, ChargeAssignmentId = ca.Id,
                                Description = $"Charges Q{quarter}/{year} — {unit.Reference}"
                            });
                        }
                    }
                }
            }
        }

        db.ChargeAssignments.AddRange(chargeAssignments);
        db.Payments.AddRange(chargePayments);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 6. INCIDENTS (60)
        // ═══════════════════════════════════════════
        await log("6/17 — Incidents (60 signalements)...");
        var incidentTitles = new (string title, IncidentCategory cat, IncidentPriority prio)[]
        {
            ("Fuite d'eau cuisine", IncidentCategory.Plumbing, IncidentPriority.High),
            ("Fuite d'eau salle de bain", IncidentCategory.Plumbing, IncidentPriority.Medium),
            ("WC bouché", IncidentCategory.Plumbing, IncidentPriority.Medium),
            ("Robinet cassé", IncidentCategory.Plumbing, IncidentPriority.Low),
            ("Canalisation bouchée parking", IncidentCategory.Plumbing, IncidentPriority.High),
            ("Panne ascenseur", IncidentCategory.Elevator, IncidentPriority.Critical),
            ("Ascenseur bloqué entre étages", IncidentCategory.Elevator, IncidentPriority.Critical),
            ("Bouton ascenseur HS", IncidentCategory.Elevator, IncidentPriority.Low),
            ("Coupure électrique étage 3", IncidentCategory.Electrical, IncidentPriority.High),
            ("Éclairage couloir HS", IncidentCategory.Electrical, IncidentPriority.Low),
            ("Disjoncteur saute", IncidentCategory.Electrical, IncidentPriority.Medium),
            ("Prise électrique grillée", IncidentCategory.Electrical, IncidentPriority.Low),
            ("Groupe électrogène en panne", IncidentCategory.Electrical, IncidentPriority.Critical),
            ("Pelouse non entretenue secteur B", IncidentCategory.GreenSpaces, IncidentPriority.Low),
            ("Arbre tombé après orage", IncidentCategory.GreenSpaces, IncidentPriority.High),
            ("Haies à tailler allée principale", IncidentCategory.GreenSpaces, IncidentPriority.Low),
            ("Porte entrée immeuble bloquée", IncidentCategory.CommonAreas, IncidentPriority.Medium),
            ("Vitre cassée hall d'entrée", IncidentCategory.CommonAreas, IncidentPriority.Medium),
            ("Interphone HS", IncidentCategory.CommonAreas, IncidentPriority.Medium),
            ("Infiltration toiture terrasse", IncidentCategory.Structural, IncidentPriority.High),
            ("Fissure mur porteur", IncidentCategory.Structural, IncidentPriority.Critical),
            ("Caméra surveillance HS", IncidentCategory.Security, IncidentPriority.High),
            ("Barrière parking en panne", IncidentCategory.Security, IncidentPriority.Medium),
            ("Vigile absent poste nuit", IncidentCategory.Security, IncidentPriority.High),
            ("STEP — alarme niveau haut", IncidentCategory.WaterTreatment, IncidentPriority.Critical),
            ("Odeurs STEP secteur villas", IncidentCategory.WaterTreatment, IncidentPriority.Medium),
            ("Climatisation HS salle commune", IncidentCategory.AirConditioning, IncidentPriority.Medium),
            ("Split mural fuite eau", IncidentCategory.AirConditioning, IncidentPriority.Low),
            ("Nuisances sonores chantier", IncidentCategory.Noise, IncidentPriority.Low),
            ("Musique forte restaurant", IncidentCategory.Noise, IncidentPriority.Medium),
            ("Cafards parking sous-sol", IncidentCategory.Pest, IncidentPriority.Medium),
            ("Nid de guêpes balcon", IncidentCategory.Pest, IncidentPriority.High),
            ("Tag graffiti mur extérieur", IncidentCategory.Other, IncidentPriority.Low),
            ("Poubelles débordantes zone B", IncidentCategory.Cleaning, IncidentPriority.Medium),
            ("Sols glissants hall après pluie", IncidentCategory.Cleaning, IncidentPriority.High),
        };

        var incidents = new List<Incident>();
        var statuses = new[] { IncidentStatus.Closed, IncidentStatus.Closed, IncidentStatus.Closed, IncidentStatus.Resolved, IncidentStatus.Resolved, IncidentStatus.InProgress, IncidentStatus.InProgress, IncidentStatus.Reported, IncidentStatus.Rejected };

        for (int i = 0; i < 60; i++)
        {
            var template = incidentTitles[i % incidentTitles.Length];
            var monthOffset = Rng.Next(0, 26);
            var day = Rng.Next(1, 28);
            var date = Utc(2024, 1, 1).AddMonths(monthOffset).AddDays(day - 1);
            if (date > EndDate) date = EndDate.AddDays(-Rng.Next(1, 30));

            var targetUnit = units[Rng.Next(units.Count)];
            var status = statuses[Rng.Next(statuses.Length)];

            incidents.Add(new Incident
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                UnitId = targetUnit.Id, BuildingId = targetUnit.BuildingId,
                Title = $"{template.title} — {targetUnit.Reference}",
                Description = $"Signalement : {template.title} au lot {targetUnit.Reference}.",
                Priority = template.prio, Status = status, Category = template.cat.ToString(),
                ReportedByUserId = adminUserId,
                CreatedAt = date,
                ResolvedAt = status >= IncidentStatus.Resolved ? date.AddDays(Rng.Next(1, 14)) : null,
                ResolutionNotes = status >= IncidentStatus.Resolved ? "Intervention effectuée, problème résolu." : null
            });
        }

        db.Incidents.AddRange(incidents);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 7. WORK ORDERS (40)
        // ═══════════════════════════════════════════
        await log("7/17 — Ordres de travaux (40 bons de commande)...");
        var woStatuses = new[] { WorkOrderStatus.Paid, WorkOrderStatus.Paid, WorkOrderStatus.Paid, WorkOrderStatus.Completed, WorkOrderStatus.Completed, WorkOrderStatus.InProgress, WorkOrderStatus.Invoiced, WorkOrderStatus.Draft, WorkOrderStatus.Cancelled };
        var workOrders = new List<WorkOrder>();
        int woRef = 1;

        var highIncidents = incidents.Where(i => i.Priority >= IncidentPriority.High).Take(25).ToList();
        for (int i = 0; i < 40; i++)
        {
            var incident = i < highIncidents.Count ? highIncidents[i] : null;
            var supplier = suppliers[Rng.Next(suppliers.Count)];
            var woStatus = woStatuses[Rng.Next(woStatuses.Length)];
            var estCost = (decimal)(Rng.Next(5, 500) * 10_000); // 50k-5M XOF
            var date = incident?.CreatedAt.AddDays(Rng.Next(1, 5)) ?? Utc(2024, 1, 1).AddDays(Rng.Next(0, 800));
            if (date > EndDate) date = EndDate.AddDays(-5);

            workOrders.Add(new WorkOrder
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                Reference = $"OT-{woRef++:D3}",
                Title = incident != null ? $"Réparation — {incident.Title}" : $"Travaux planifiés #{woRef}",
                Description = incident != null ? $"Suite incident {incident.Title}" : "Travaux d'entretien planifiés",
                Status = woStatus, SupplierId = supplier.Id,
                IncidentId = incident?.Id, BuildingId = incident?.BuildingId,
                EstimatedCost = estCost,
                ActualCost = woStatus >= WorkOrderStatus.Completed ? estCost * (decimal)(0.8 + Rng.NextDouble() * 0.4) : null,
                ScheduledDate = date, CompletedDate = woStatus >= WorkOrderStatus.Completed ? date.AddDays(Rng.Next(1, 21)) : null,
                CreatedAt = date
            });
        }

        db.WorkOrders.AddRange(workOrders);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 8. MEETINGS + AGENDA + RESOLUTIONS + VOTES + ATTENDEES
        // ═══════════════════════════════════════════
        await log("8/17 — Assemblees generales (5 AG + resolutions + votes)...");
        var meetingDefs = new[]
        {
            ("AG Ordinaire 2024", MeetingType.OrdinaryGeneral, MeetingStatus.Completed, Utc(2024, 6, 15, 10)),
            ("Conseil Syndical Mars 2024", MeetingType.CouncilMeeting, MeetingStatus.Completed, Utc(2024, 3, 20, 14)),
            ("AG Extraordinaire — Travaux Piscine", MeetingType.ExtraordinaryGeneral, MeetingStatus.Completed, Utc(2024, 11, 8, 10)),
            ("AG Ordinaire 2025", MeetingType.OrdinaryGeneral, MeetingStatus.Completed, Utc(2025, 6, 14, 10)),
            ("Conseil Syndical Mars 2025", MeetingType.CouncilMeeting, MeetingStatus.Completed, Utc(2025, 3, 18, 14)),
        };

        var resolutionDefs = new[]
        {
            ("Approbation des comptes de l'exercice", ResolutionMajority.Simple),
            ("Quitus au syndic", ResolutionMajority.Simple),
            ("Approbation du budget prévisionnel", ResolutionMajority.Simple),
            ("Travaux de réfection de la piscine", ResolutionMajority.Absolute),
            ("Remplacement des ascenseurs Bât. Acajou", ResolutionMajority.DoubleMajority),
            ("Mise en place vidéosurveillance", ResolutionMajority.Simple),
            ("Élection nouveau membre du conseil", ResolutionMajority.Simple),
            ("Changement de fournisseur espaces verts", ResolutionMajority.Simple),
            ("Augmentation du fonds de réserve", ResolutionMajority.Absolute),
            ("Mise en conformité STEP", ResolutionMajority.Simple),
        };

        var agendaItemDefs = new[]
        {
            ("Appel, vérification du quorum", AgendaItemType.Information),
            ("Rapport de gestion du syndic", AgendaItemType.Information),
            ("Présentation des comptes", AgendaItemType.Information),
            ("Questions diverses", AgendaItemType.Questions),
        };

        var coH = coOwnerships.First(c => c.Level == CoOwnershipLevel.Horizontal);

        foreach (var (title, type, status, date) in meetingDefs)
        {
            var meeting = new Meeting
            {
                Id = Guid.NewGuid(), OrganizationId = orgId, CoOwnershipId = coH.Id,
                Title = title, Type = type, Status = status,
                ScheduledDate = date, ActualDate = date,
                Location = "Club House Green City Bassam",
                Quorum = 60, AttendeesCount = Rng.Next(45, 65),
                CreatedAt = date.AddDays(-30)
            };
            db.Meetings.Add(meeting);

            // Agenda items
            int order = 1;
            foreach (var (aTitle, aType) in agendaItemDefs)
            {
                db.Set<MeetingAgendaItem>().Add(new MeetingAgendaItem
                {
                    Id = Guid.NewGuid(), MeetingId = meeting.Id,
                    OrderNumber = order++, Title = aTitle, Type = aType,
                    EstimatedDurationMinutes = 15
                });
            }

            // Resolutions (3-5 per meeting)
            var resCount = Rng.Next(3, 6);
            var selectedRes = resolutionDefs.OrderBy(_ => Rng.Next()).Take(resCount).ToArray();
            int resOrder = 1;
            foreach (var (rTitle, rMajority) in selectedRes)
            {
                var votesFor = Rng.Next(35, 60);
                var votesAgainst = Rng.Next(2, 15);
                var votesAbstain = Rng.Next(0, 8);
                var resolution = new Resolution
                {
                    Id = Guid.NewGuid(), MeetingId = meeting.Id,
                    OrderNumber = resOrder++, Title = rTitle,
                    RequiredMajority = rMajority,
                    VotesFor = votesFor, VotesAgainst = votesAgainst, VotesAbstain = votesAbstain,
                    SharesFor = votesFor * 15, SharesAgainst = votesAgainst * 15,
                    IsApproved = votesFor > votesAgainst + votesAbstain
                };
                db.Resolutions.Add(resolution);

                // Add agenda item for resolution
                db.Set<MeetingAgendaItem>().Add(new MeetingAgendaItem
                {
                    Id = Guid.NewGuid(), MeetingId = meeting.Id,
                    OrderNumber = order++, Title = $"Vote — {rTitle}",
                    Type = AgendaItemType.Resolution, ResolutionId = resolution.Id,
                    EstimatedDurationMinutes = 10
                });

                // Individual votes (from present owners)
                var presentOwners = owners.OrderBy(_ => Rng.Next()).Take(meeting.AttendeesCount ?? 50).ToList();
                foreach (var owner in presentOwners)
                {
                    var ownerUnit = units.FirstOrDefault(u => u.OwnerId == owner.Id);
                    db.Votes.Add(new Vote
                    {
                        Id = Guid.NewGuid(), ResolutionId = resolution.Id,
                        OwnerId = owner.Id, UnitId = ownerUnit?.Id,
                        Result = Rng.NextDouble() < 0.75 ? VoteResult.For : Rng.NextDouble() < 0.5 ? VoteResult.Against : VoteResult.Abstain,
                        ShareWeight = ownerUnit?.ShareRatio ?? 10
                    });
                }
            }

            // Attendees (feuille de présence)
            var attendees = owners.OrderBy(_ => Rng.Next()).Take(meeting.AttendeesCount ?? 50);
            foreach (var owner in attendees)
            {
                db.Set<MeetingAttendee>().Add(new MeetingAttendee
                {
                    Id = Guid.NewGuid(), MeetingId = meeting.Id, OwnerId = owner.Id,
                    Status = Rng.NextDouble() < 0.85 ? AttendanceStatus.PresentInPerson : AttendanceStatus.RepresentedByProxy,
                    SharesRepresented = owners.First(o => o.Id == owner.Id).Balance == 0 ? 10 : 15,
                    HasSigned = true, SignedAt = date.AddHours(1)
                });
            }
        }
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 9. ACCOUNTING ENTRIES (SYSCOHADA)
        // ═══════════════════════════════════════════
        await log("9/17 — Ecritures comptables SYSCOHADA...");
        var entries = new List<AccountingEntry>();
        int entryNum = 1;

        // VE journal — rent receipts
        foreach (var rc in rentCalls.Where(r => r.PaidAmount > 0))
        {
            entries.Add(new AccountingEntry
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                EntryNumber = $"EC-{entryNum++:D4}",
                EntryDate = rc.PaidAt ?? rc.DueDate,
                JournalCode = "VE", AccountCode = "706", AccountLabel = "Revenus locatifs",
                Description = $"Loyer {rc.Month:D2}/{rc.Year} — {rc.Reference}",
                Credit = rc.PaidAmount, Debit = 0,
                FiscalYear = rc.Year, Period = rc.Month, IsValidated = true
            });
            entries.Add(new AccountingEntry
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                EntryNumber = $"EC-{entryNum++:D4}",
                EntryDate = rc.PaidAt ?? rc.DueDate,
                JournalCode = "BQ", AccountCode = "512", AccountLabel = "Banque",
                Description = $"Encaissement loyer {rc.Month:D2}/{rc.Year}",
                Debit = rc.PaidAmount, Credit = 0,
                FiscalYear = rc.Year, Period = rc.Month, IsValidated = true
            });
        }

        // AC journal — work orders (completed/paid)
        foreach (var wo in workOrders.Where(w => w.ActualCost.HasValue))
        {
            var woDate = wo.CompletedDate ?? wo.ScheduledDate ?? Utc(2025, 1, 1);
            entries.Add(new AccountingEntry
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                EntryNumber = $"EC-{entryNum++:D4}",
                EntryDate = woDate,
                JournalCode = "AC", AccountCode = "613", AccountLabel = "Charges d'entretien",
                Description = $"Facture — {wo.Title}",
                Debit = wo.ActualCost!.Value, Credit = 0,
                FiscalYear = woDate.Year, Period = woDate.Month, IsValidated = true
            });
        }

        // OD journal — charge calls (quarterly)
        foreach (var year in new[] { 2024, 2025 })
        {
            for (int q = 1; q <= 4; q++)
            {
                var totalCharges = chargeAssignments.Where(c => c.Year == year && c.Quarter == q).Sum(c => c.Amount);
                if (totalCharges > 0)
                {
                    entries.Add(new AccountingEntry
                    {
                        Id = Guid.NewGuid(), OrganizationId = orgId,
                        EntryNumber = $"EC-{entryNum++:D4}",
                        EntryDate = Utc(year, (q - 1) * 3 + 1, 1),
                        JournalCode = "OD", AccountCode = "411", AccountLabel = "Copropriétaires — Appels de fonds",
                        Description = $"Appel de charges Q{q}/{year}",
                        Debit = totalCharges, Credit = 0,
                        FiscalYear = year, Period = (q - 1) * 3 + 1, IsValidated = true
                    });
                }
            }
        }

        db.AccountingEntries.AddRange(entries);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 10. DOCUMENTS (80)
        // ═══════════════════════════════════════════
        await log("10/17 — Documents GED (80 fichiers)...");
        var documents = new List<Document>();
        var docCategories = new[]
        {
            (DocumentCategory.Meeting, "PV AG", 5),
            (DocumentCategory.Invoice, "Facture fournisseur", 30),
            (DocumentCategory.Lease, "Contrat de bail", 10),
            (DocumentCategory.LegalNotice, "Mise en demeure", 5),
            (DocumentCategory.WorkOrder, "Devis travaux", 10),
            (DocumentCategory.Financial, "Relevé bancaire", 10),
            (DocumentCategory.Other, "Photo incident", 10),
        };

        foreach (var (cat, prefix, count) in docCategories)
        {
            for (int i = 0; i < count; i++)
            {
                var date = Utc(2024, 1, 1).AddDays(Rng.Next(0, 800));
                if (date > EndDate) date = EndDate.AddDays(-1);
                documents.Add(new Document
                {
                    Id = Guid.NewGuid(), OrganizationId = orgId,
                    FileName = $"{prefix.Replace(" ", "_")}_{i + 1:D2}.pdf",
                    DisplayName = $"{prefix} #{i + 1}",
                    ContentType = "application/pdf", SizeBytes = Rng.Next(50_000, 5_000_000),
                    StoragePath = $"/documents/{cat}/{Guid.NewGuid()}.pdf",
                    Category = cat,
                    Description = $"{prefix} généré pour la démo",
                    CreatedAt = date
                });
            }
        }

        db.Documents.AddRange(documents);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 11. NOTIFICATIONS (150)
        // ═══════════════════════════════════════════
        await log("11/17 — Notifications (150)...");
        var notifTemplates = new[]
        {
            "Votre loyer de {0} est dû le 5 du mois.",
            "Incident signalé : {0}",
            "Convocation à l'AG du {0}",
            "Votre quittance de {0} est disponible.",
            "Rappel : charges Q{0} en attente de paiement.",
            "Résolution votée : {0}",
            "Travaux planifiés : {0}",
            "Nouveau document disponible : {0}",
        };

        var notifications = new List<Notification>();
        var allUsers = await userManager.Users.Where(u => u.OrganizationId == orgId).ToListAsync();

        for (int i = 0; i < 150; i++)
        {
            var user = allUsers[Rng.Next(allUsers.Count)];
            var template = notifTemplates[Rng.Next(notifTemplates.Length)];
            var date = Utc(2024, 1, 1).AddDays(Rng.Next(0, 800));
            if (date > EndDate) date = EndDate.AddDays(-1);
            var isRead = Rng.NextDouble() < 0.7;

            notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                UserId = user.Id,
                Title = template.Contains("{0}") ? string.Format(template, $"mars {date.Year}") : template,
                Message = $"Notification automatique — {date:dd/MM/yyyy}",
                IsRead = isRead, ReadAt = isRead ? date.AddHours(Rng.Next(1, 48)) : null,
                CreatedAt = date
            });
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 12. LEASE REVISIONS (5 commercial)
        // ═══════════════════════════════════════════
        await log("12/17 — Revisions de baux commerciaux...");
        var commercialLeases = leases.Where(l => l.Type == LeaseType.Commercial).ToList();
        int revRef = 1;
        foreach (var cl in commercialLeases)
        {
            var pct = 2 + (decimal)(Rng.NextDouble() * 3); // 2-5%
            var newRent = Math.Round(cl.MonthlyRent * (1 + pct / 100), 0);
            db.LeaseRevisions.Add(new LeaseRevision
            {
                Id = Guid.NewGuid(), OrganizationId = orgId, LeaseId = cl.Id,
                Reference = $"REV-{revRef++:D3}", Type = RevisionType.Indexation,
                Status = RevisionStatus.Applied,
                EffectiveDate = Utc(2025, 1, 1), NotificationDate = Utc(2024, 11, 1),
                PreviousRent = cl.MonthlyRent, NewRent = newRent,
                VariationPercent = pct,
                LegalBasis = "AUDCG Art. 116",
                CreatedAt = Utc(2024, 11, 1)
            });
        }
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 13. CHARGE REGULARIZATIONS (10)
        // ═══════════════════════════════════════════
        await log("13/17 — Regularisations de charges...");
        int regRef = 1;
        foreach (var lease in leases)
        {
            foreach (var year in new[] { 2024, 2025 })
            {
                if (regRef > 10) break;
                var provisioned = (lease.Charges ?? 25_000) * 12;
                var actual = provisioned * (decimal)(0.8 + Rng.NextDouble() * 0.4);
                var balance = provisioned - actual;

                db.ChargeRegularizations.Add(new ChargeRegularization
                {
                    Id = Guid.NewGuid(), OrganizationId = orgId, LeaseId = lease.Id,
                    Reference = $"REG-{regRef++:D3}", Type = RegularizationType.Annual,
                    Status = year == 2024 ? RegularizationStatus.Settled : RegularizationStatus.Notified,
                    PeriodStart = Utc(year, 1, 1), PeriodEnd = Utc(year, 12, 31),
                    TotalProvisioned = provisioned, TotalActual = Math.Round(actual, 0),
                    Balance = Math.Round(balance, 0),
                    NotifiedAt = Utc(year + 1, 2, 1),
                    AcceptedAt = year == 2024 ? Utc(year + 1, 3, 1) : null,
                    SettledAt = year == 2024 ? Utc(year + 1, 3, 15) : null,
                    CreatedAt = Utc(year + 1, 1, 15)
                });
            }
        }
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 14. TENANT APPLICATIONS (8)
        // ═══════════════════════════════════════════
        await log("14/17 — Candidatures locataires (8)...");
        var appNames = new[] { ("Kofi", "Mensah"), ("Aya", "N'Goran"), ("Marcel", "Tano"), ("Justine", "Gbéhi"), ("Hervé", "Koffi"), ("Rachida", "Cissé"), ("Claude", "Assi"), ("Brigitte", "Yao") };
        var appStatuses = new[] { ApplicationStatus.LeaseCreated, ApplicationStatus.LeaseCreated, ApplicationStatus.LeaseCreated, ApplicationStatus.LeaseCreated, ApplicationStatus.LeaseCreated, ApplicationStatus.Rejected, ApplicationStatus.Rejected, ApplicationStatus.Submitted };
        int appRef = 1;

        for (int i = 0; i < 8; i++)
        {
            var (fn, ln) = appNames[i];
            var targetUnit = apartments.OrderBy(_ => Rng.Next()).First();
            var score = 40 + Rng.Next(0, 56);
            db.TenantApplications.Add(new TenantApplication
            {
                Id = Guid.NewGuid(), OrganizationId = orgId, UnitId = targetUnit.Id,
                Reference = $"CAND-{appRef++:D3}", Status = appStatuses[i],
                FirstName = fn, LastName = ln,
                Email = $"{fn.ToLower()}.{ln.ToLower()}@email.ci",
                Phone = $"+225 05{Rng.Next(10000000, 99999999)}",
                MonthlyIncome = 300_000 + Rng.Next(0, 700_000),
                DesiredRent = 150_000 + Rng.Next(0, 100_000),
                Score = score,
                ScoreLevel = score >= 80 ? ApplicationScoreLevel.Excellent : score >= 60 ? ApplicationScoreLevel.Good : score >= 40 ? ApplicationScoreLevel.Average : ApplicationScoreLevel.Poor,
                CreatedAt = Utc(2024, 1, 1).AddDays(Rng.Next(0, 700))
            });
        }
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 15. MESSAGE TEMPLATES (5)
        // ═══════════════════════════════════════════
        await log("15/17 — Modeles de messages...");
        db.MessageTemplates.AddRange(
            new MessageTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "TPL-LOYER", Name = "Rappel de loyer", Channel = MessageChannel.Email, Subject = "Rappel — Loyer du mois", Body = "Cher(e) {{nom}}, nous vous rappelons que votre loyer de {{montant}} XOF est dû le {{date}}.", IsActive = true },
            new MessageTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "TPL-CONVOC", Name = "Convocation AG", Channel = MessageChannel.Email, Subject = "Convocation — Assemblée Générale", Body = "Cher(e) copropriétaire, vous êtes convoqué(e) à l'AG du {{date}} à {{lieu}}.", IsActive = true },
            new MessageTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "TPL-QUITTANCE", Name = "Quittance disponible", Channel = MessageChannel.Email, Subject = "Votre quittance est disponible", Body = "Votre quittance pour la période {{periode}} est disponible dans votre espace.", IsActive = true },
            new MessageTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "TPL-RELANCE", Name = "Relance impayé", Channel = MessageChannel.Sms, Body = "Rappel : votre loyer de {{montant}} XOF est en retard. Merci de régulariser.", IsActive = true },
            new MessageTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "TPL-BIENVENUE", Name = "Bienvenue nouveau locataire", Channel = MessageChannel.Email, Subject = "Bienvenue à Green City Bassam", Body = "Cher(e) {{nom}}, bienvenue dans votre nouveau logement ! Voici les informations pratiques...", IsActive = true }
        );
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 16. RESOLUTION TEMPLATES (5)
        // ═══════════════════════════════════════════
        await log("16/17 — Modeles de resolutions...");
        db.ResolutionTemplates.AddRange(
            new ResolutionTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "RES-COMPTES", Title = "Approbation des comptes", DefaultMajority = ResolutionMajority.Simple, Category = "Comptabilité", LegalReference = "CCH Art. 388", IsActive = true },
            new ResolutionTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "RES-QUITUS", Title = "Quitus au syndic", DefaultMajority = ResolutionMajority.Simple, Category = "Gouvernance", LegalReference = "CCH Art. 388", IsActive = true },
            new ResolutionTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "RES-BUDGET", Title = "Budget prévisionnel", DefaultMajority = ResolutionMajority.Simple, Category = "Comptabilité", LegalReference = "CCH Art. 396", IsActive = true },
            new ResolutionTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "RES-TRAVAUX", Title = "Travaux exceptionnels", DefaultMajority = ResolutionMajority.Absolute, Category = "Travaux", LegalReference = "CCH Art. 396", IsActive = true },
            new ResolutionTemplate { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "RES-ELECTION", Title = "Élection membre du conseil", DefaultMajority = ResolutionMajority.Simple, Category = "Gouvernance", LegalReference = "CCH Art. 388", IsActive = true }
        );
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════
        // 17. ORGANIZATION SETTINGS
        // ═══════════════════════════════════════════
        await log("17/17 — Parametres de l'organisation...");
        if (!await db.Set<OrganizationSettings>().IgnoreQueryFilters().AnyAsync(s => s.OrganizationId == orgId))
        {
            db.Set<OrganizationSettings>().Add(new OrganizationSettings
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                Currency = "XOF", FiscalYearStartMonth = 1,
                DefaultVatRate = 18, PaymentDueDays = 30,
                RentDueDay = 5, AutoGenerateRentCalls = true,
                AutoSendReminders = true, ReminderDaysBefore = 5,
                OverdueDaysThreshold = 15,
                ApplyLateFees = true, LateFeePercent = 5,
                Timezone = "Africa/Abidjan", Locale = "fr-CI",
                MaxDocumentSizeBytes = 10_485_760,
                AllowedFileExtensions = ".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx"
            });
        }
        await db.SaveChangesAsync();
    }
}
