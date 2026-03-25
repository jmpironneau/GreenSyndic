using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Seed;

/// <summary>
/// Phase 7 — Realistic seed data for Green City Bassam.
/// 51 villas, 200 apartments (5 buildings R+5), COSMOS commercial complex.
/// ~80 owners, 15 suppliers, reference charges, legal references.
/// </summary>
public static class SeedGreenCity
{
    public static async Task SeedAsync(GreenSyndicDbContext db)
    {
        // Skip if already seeded
        if (await db.Buildings.IgnoreQueryFilters().AnyAsync())
            return;

        // Get or create the organization
        var org = await db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync();
        if (org == null)
        {
            org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Green City Bassam",
                LegalName = "COFIPRI — Compagnie Financiere de Promotion Immobiliere",
                Address = "Boulevard de la Corniche, Grand-Bassam",
                Country = "Cote d'Ivoire",
                Currency = "XOF"
            };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
        }
        var orgId = org.Id;

        // ══════════════════════════════════════════════
        //  1. COPROPRIÉTÉS (dual-level)
        // ══════════════════════════════════════════════

        var coproH = new CoOwnership
        {
            Id = Guid.NewGuid(), OrganizationId = orgId,
            Name = "Green City Bassam — Copropriété Horizontale",
            Level = CoOwnershipLevel.Horizontal,
            Description = "Copropriété horizontale couvrant l'ensemble du domaine Green City Bassam : voiries, espaces verts, STEP, sécurité 24/24, éclairage public.",
            RegulationReference = "RC-GCB-2024-001",
            AnnualBudget = 450_000_000, // 450M XOF
            ReserveFund = 90_000_000,
            SyndicFeePercent = 8
        };

        var coproVillas = new CoOwnership
        {
            Id = Guid.NewGuid(), OrganizationId = orgId,
            Name = "Zone Villas — Copropriété Verticale",
            Level = CoOwnershipLevel.Vertical,
            ParentCoOwnershipId = coproH.Id,
            Description = "51 villas duplex F4/F5/F6 avec jardin privatif.",
            AnnualBudget = 120_000_000,
            ReserveFund = 25_000_000,
            SyndicFeePercent = 6
        };

        var coproAcajou = NewVerticalCopro(orgId, coproH.Id, "Immeuble Acajou", 85_000_000);
        var coproBaobab = NewVerticalCopro(orgId, coproH.Id, "Immeuble Baobab", 85_000_000);
        var coproCedre = NewVerticalCopro(orgId, coproH.Id, "Immeuble Cèdre", 85_000_000);
        var coproDattier = NewVerticalCopro(orgId, coproH.Id, "Immeuble Dattier", 85_000_000);
        var coproEbene = NewVerticalCopro(orgId, coproH.Id, "Immeuble Ébène", 85_000_000);
        var coproCosmos = NewVerticalCopro(orgId, coproH.Id, "COSMOS Commercial", 150_000_000);

        db.CoOwnerships.AddRange(coproH, coproVillas, coproAcajou, coproBaobab, coproCedre, coproDattier, coproEbene, coproCosmos);
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════
        //  2. IMMEUBLES (Buildings)
        // ══════════════════════════════════════════════

        var bldVillas = new Building
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CoOwnershipId = coproVillas.Id,
            Name = "Zone Villas", Code = "VIL",
            PrimaryType = PropertyType.VillaDuplex,
            Address = "Zone résidentielle, Green City Bassam",
            TotalAreaSqm = 51 * 250, // ~250m² par villa
            Description = "51 villas duplex avec jardin privatif, piscine commune, aires de jeux."
        };

        var bldAcajou = NewApartmentBuilding(orgId, coproAcajou.Id, "Acajou", "AC", "Bloc A, allée des Acajous");
        var bldBaobab = NewApartmentBuilding(orgId, coproBaobab.Id, "Baobab", "BB", "Bloc B, allée des Baobabs");
        var bldCedre = NewApartmentBuilding(orgId, coproCedre.Id, "Cèdre", "CD", "Bloc C, allée des Cèdres");
        var bldDattier = NewApartmentBuilding(orgId, coproDattier.Id, "Dattier", "DT", "Bloc D, allée des Dattiers");
        var bldEbene = NewApartmentBuilding(orgId, coproEbene.Id, "Ébène", "EB", "Bloc E, allée des Ébènes");

        var bldCosmos = new Building
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CoOwnershipId = coproCosmos.Id,
            Name = "COSMOS — Centre Commercial", Code = "COS",
            PrimaryType = PropertyType.CommercialUnit,
            Address = "Entrée principale, Green City Bassam",
            NumberOfFloors = 3, TotalAreaSqm = 8500, CommonAreaSqm = 2000,
            HasElevator = true, HasGenerator = true, HasParking = true,
            Description = "Centre commercial mixte : cinéma, food court, banque, clinique, fast food, station-service, commerces."
        };

        // Infrastructure buildings
        var bldStep = new Building
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CoOwnershipId = coproH.Id,
            Name = "Station d'Épuration (STEP)", Code = "STEP",
            PrimaryType = PropertyType.WaterTreatment,
            Description = "Station de traitement des eaux usées du domaine."
        };

        var bldGuard = new Building
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CoOwnershipId = coproH.Id,
            Name = "Poste de Garde Principal", Code = "GRD",
            PrimaryType = PropertyType.GuardHouse,
            Description = "Poste de garde entrée principale, sécurité 24/24."
        };

        var bldClub = new Building
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CoOwnershipId = coproH.Id,
            Name = "Club House & Piscine", Code = "CLB",
            PrimaryType = PropertyType.ClubHouse,
            TotalAreaSqm = 800,
            Description = "Club house avec piscine, salle de sport, espace événementiel."
        };

        db.Buildings.AddRange(bldVillas, bldAcajou, bldBaobab, bldCedre, bldDattier, bldEbene,
            bldCosmos, bldStep, bldGuard, bldClub);
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════
        //  3. PROPRIÉTAIRES (80 owners)
        // ══════════════════════════════════════════════

        var owners = CreateOwners(orgId);
        db.Owners.AddRange(owners);
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════
        //  4. LOTS — 51 Villas
        // ══════════════════════════════════════════════

        var villaTypes = new[] { ("F4", 3, 200m, 45_000_000m), ("F5", 4, 250m, 55_000_000m), ("F6", 5, 300m, 70_000_000m) };
        var villaUnits = new List<Unit>();
        for (int i = 1; i <= 51; i++)
        {
            var vt = villaTypes[(i - 1) % 3];
            var status = i <= 40 ? UnitStatus.Occupied : (i <= 48 ? UnitStatus.Vacant : UnitStatus.Reserved);
            var ownerIdx = i <= 40 ? (i - 1) % owners.Count : (int?)null;

            villaUnits.Add(new Unit
            {
                Id = Guid.NewGuid(), OrganizationId = orgId, BuildingId = bldVillas.Id,
                CoOwnershipId = coproVillas.Id,
                Reference = $"V-{i:D3}",
                Name = $"Villa {vt.Item1} n°{i}",
                Type = PropertyType.VillaDuplex,
                Status = status,
                AreaSqm = vt.Item3,
                NumberOfRooms = vt.Item2,
                ShareRatio = 20m, // millièmes copro verticale
                HorizontalShareRatio = 4m, // millièmes copro horizontale
                MarketValue = vt.Item4,
                OwnerId = ownerIdx.HasValue ? owners[ownerIdx.Value].Id : null
            });
        }
        db.Units.AddRange(villaUnits);

        // ══════════════════════════════════════════════
        //  5. LOTS — 200 Appartements (5 immeubles × 40)
        // ══════════════════════════════════════════════

        var aptBuildings = new[] {
            (bldAcajou, coproAcajou), (bldBaobab, coproBaobab),
            (bldCedre, coproCedre), (bldDattier, coproDattier),
            (bldEbene, coproEbene)
        };
        var aptTypes = new[] { ("F2", 2, 55m, 18_000_000m), ("F3", 3, 72m, 25_000_000m), ("F4", 4, 95m, 35_000_000m) };
        int ownerCursor = 0;

        foreach (var (bld, copro) in aptBuildings)
        {
            for (int floor = 1; floor <= 5; floor++)
            {
                for (int door = 1; door <= 8; door++)
                {
                    var at = aptTypes[(floor + door) % 3];
                    var aptRef = $"A-{bld.Code}-{floor}{door:D2}";
                    var status = (floor <= 4) ? UnitStatus.Occupied : UnitStatus.Vacant;

                    db.Units.Add(new Unit
                    {
                        Id = Guid.NewGuid(), OrganizationId = orgId,
                        BuildingId = bld.Id, CoOwnershipId = copro.Id,
                        Reference = aptRef,
                        Name = $"{at.Item1} Étage {floor} Porte {door}",
                        Type = PropertyType.Apartment,
                        Status = status, Floor = floor,
                        AreaSqm = at.Item3,
                        NumberOfRooms = at.Item2,
                        ShareRatio = 25m,
                        HorizontalShareRatio = 2m,
                        MarketValue = at.Item4,
                        OwnerId = status == UnitStatus.Occupied ? owners[ownerCursor++ % owners.Count].Id : null
                    });
                }
            }
        }

        // ══════════════════════════════════════════════
        //  6. LOTS COSMOS — Commercial (18 lots)
        // ══════════════════════════════════════════════

        var cosmosLots = new (string Ref, string Name, PropertyType Type, decimal Area, decimal Value)[]
        {
            ("COS-CIN", "Cinéma Pathé", PropertyType.Cinema, 600, 250_000_000),
            ("COS-FC1", "Food Court — Zone A", PropertyType.FoodCourt, 400, 120_000_000),
            ("COS-FC2", "Food Court — Zone B", PropertyType.FoodCourt, 350, 100_000_000),
            ("COS-FF1", "KFC Grand Bassam", PropertyType.FastFood, 120, 45_000_000),
            ("COS-FF2", "Pizza Hut", PropertyType.FastFood, 100, 40_000_000),
            ("COS-RST", "Restaurant Le Bassam", PropertyType.Restaurant, 200, 80_000_000),
            ("COS-BNK", "Afrika Banque", PropertyType.Bank, 250, 150_000_000),
            ("COS-CLN", "Clinique Novamed", PropertyType.Clinic, 500, 200_000_000),
            ("COS-GAS", "Station Oryx", PropertyType.GasStation, 800, 180_000_000),
            ("COS-ECO", "Groupe Scolaire Excelia", PropertyType.School, 1200, 300_000_000),
            ("COS-SPT", "Salle de Sport FitLife", PropertyType.SportsClub, 350, 90_000_000),
            ("COS-R01", "Boutique 1 — Prêt-à-porter", PropertyType.RetailUnit, 80, 30_000_000),
            ("COS-R02", "Boutique 2 — Électronique", PropertyType.RetailUnit, 80, 30_000_000),
            ("COS-R03", "Boutique 3 — Cosmétiques", PropertyType.RetailUnit, 60, 25_000_000),
            ("COS-R04", "Pharmacie du Domaine", PropertyType.RetailUnit, 100, 40_000_000),
            ("COS-OF1", "Bureau 1 — Étage 2", PropertyType.Office, 120, 35_000_000),
            ("COS-OF2", "Bureau 2 — Étage 2", PropertyType.Office, 120, 35_000_000),
            ("COS-PKG", "Parking COSMOS", PropertyType.Parking, 1500, 50_000_000),
        };

        foreach (var lot in cosmosLots)
        {
            db.Units.Add(new Unit
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                BuildingId = bldCosmos.Id, CoOwnershipId = coproCosmos.Id,
                Reference = lot.Ref, Name = lot.Name,
                Type = lot.Type, Status = UnitStatus.Occupied,
                AreaSqm = lot.Area, MarketValue = lot.Value,
                ShareRatio = lot.Area / 85m, // proportionnel à la surface
                HorizontalShareRatio = lot.Area / 200m,
                OwnerId = owners[ownerCursor++ % owners.Count].Id
            });
        }

        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════
        //  7. FOURNISSEURS / PRESTATAIRES (15)
        // ══════════════════════════════════════════════

        db.Suppliers.AddRange(CreateSuppliers(orgId));
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════
        //  8. CHARGES — Définitions copro horizontale
        // ══════════════════════════════════════════════

        db.ChargeDefinitions.AddRange(
            NewCharge(coproH.Id, "Sécurité 24/24", ChargeType.Security, 96_000_000),
            NewCharge(coproH.Id, "Espaces verts & voiries", ChargeType.GreenSpaces, 72_000_000),
            NewCharge(coproH.Id, "STEP — Traitement des eaux", ChargeType.WaterTreatment, 48_000_000),
            NewCharge(coproH.Id, "Éclairage public", ChargeType.PublicLighting, 36_000_000),
            NewCharge(coproH.Id, "Assurance multirisque", ChargeType.Insurance, 24_000_000),
            NewCharge(coproH.Id, "Frais de gestion syndic", ChargeType.ManagementFee, 36_000_000),
            NewCharge(coproH.Id, "Fonds de réserve (art. 396 CCH)", ChargeType.SinkingFund, 45_000_000),
            NewCharge(coproH.Id, "Groupe électrogène", ChargeType.Generator, 30_000_000)
        );

        // Charges copro verticale immeubles
        foreach (var copro in new[] { coproAcajou, coproBaobab, coproCedre, coproDattier, coproEbene })
        {
            db.ChargeDefinitions.AddRange(
                NewCharge(copro.Id, "Entretien parties communes", ChargeType.CommonAreaMaintenance, 18_000_000),
                NewCharge(copro.Id, "Ascenseur", ChargeType.Elevator, 12_000_000),
                NewCharge(copro.Id, "Eau parties communes", ChargeType.Water, 6_000_000),
                NewCharge(copro.Id, "Électricité parties communes", ChargeType.Electricity, 8_000_000)
            );
        }

        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════
        //  9. VEILLE JURIDIQUE — Références légales
        // ══════════════════════════════════════════════

        db.LegalReferences.AddRange(CreateLegalReferences());
        await db.SaveChangesAsync();
    }

    // ── Helper methods ──

    private static CoOwnership NewVerticalCopro(Guid orgId, Guid parentId, string name, decimal budget) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = orgId,
        Name = name + " — Copropriété Verticale",
        Level = CoOwnershipLevel.Vertical,
        ParentCoOwnershipId = parentId,
        AnnualBudget = budget, ReserveFund = budget * 0.2m, SyndicFeePercent = 6
    };

    private static Building NewApartmentBuilding(Guid orgId, Guid coproId, string name, string code, string address) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = orgId, CoOwnershipId = coproId,
        Name = $"Immeuble {name}", Code = code,
        PrimaryType = PropertyType.Apartment,
        Address = address,
        NumberOfFloors = 5, TotalAreaSqm = 40 * 75, CommonAreaSqm = 400,
        HasElevator = true, HasGenerator = true, HasParking = true,
        Description = $"Immeuble R+5, 40 appartements F2/F3/F4."
    };

    private static ChargeDefinition NewCharge(Guid coproId, string name, ChargeType type, decimal annual) => new()
    {
        Id = Guid.NewGuid(), CoOwnershipId = coproId,
        Name = name, Type = type, AnnualAmount = annual,
        DistributionKey = "tantieme", IsRecoverable = type != ChargeType.SinkingFund
    };

    private static List<Owner> CreateOwners(Guid orgId)
    {
        var names = new (string First, string Last, string? Company, string City, bool Council)[]
        {
            ("Amadou", "Koné", null, "Abidjan", true),
            ("Fatou", "Ouattara", null, "Abidjan", false),
            ("Moussa", "Coulibaly", null, "Bouaké", false),
            ("Awa", "Diallo", null, "Abidjan", true),
            ("Ibrahim", "Touré", null, "Yamoussoukro", false),
            ("Mariam", "Bamba", null, "Abidjan", false),
            ("Sékou", "Konaté", null, "Daloa", false),
            ("Aminata", "Traoré", null, "Abidjan", true),
            ("Drissa", "Sangaré", null, "San Pedro", false),
            ("Kadiatou", "Cissé", null, "Abidjan", false),
            ("Mamadou", "Dembélé", null, "Abidjan", false),
            ("Rokia", "Fofana", null, "Korhogo", false),
            ("Bakary", "Sylla", null, "Abidjan", false),
            ("Djénéba", "Doumbia", null, "Man", false),
            ("Souleymane", "Diabaté", null, "Abidjan", false),
            ("Nassira", "Kaboré", null, "Abidjan", false),
            ("Ousmane", "Sawadogo", null, "Ouagadougou", false),
            ("Hawa", "Bah", null, "Abidjan", false),
            ("Youssouf", "Camara", null, "Grand Bassam", true),
            ("Safiatou", "Keita", null, "Abidjan", false),
            ("Abdoulaye", "Diaby", null, "Abidjan", false),
            ("Fatoumata", "Sidibé", null, "Abidjan", false),
            ("Seydou", "Diarrassouba", null, "Bouaké", false),
            ("Adama", "N'Guessan", null, "Abidjan", false),
            ("Korotoum", "Soro", null, "Ferkessédougou", false),
            ("Lassina", "Kouyaté", null, "Abidjan", false),
            ("Bintou", "Diarra", null, "Abidjan", false),
            ("Tiémoko", "Ouédraogo", null, "Abidjan", false),
            ("Karidja", "Dagnogo", null, "Odienné", false),
            ("Issouf", "Zié", null, "Abidjan", false),
            ("Naminata", "Yéo", null, "Abidjan", false),
            ("Siaka", "Konaté", null, "Daloa", false),
            ("Fanta", "Sissoko", null, "Abidjan", false),
            ("Gnéri", "Dofini", null, "Abidjan", false),
            ("Massira", "Koné", null, "Abidjan", false),
            ("Lacina", "Ouattara", "COFIPRI SA", "Abidjan", false),
            ("Tenin", "Coulibaly", null, "Abidjan", false),
            ("Daouda", "Fanny", null, "Abidjan", false),
            ("Salimata", "Sanogo", null, "Bouaké", false),
            ("Valy", "Touré", null, "Abidjan", false),
            ("Adjoua", "Koffi", null, "Abidjan", false),
            ("Kouamé", "Assi", null, "Grand Bassam", false),
            ("Affoué", "Aka", null, "Abidjan", false),
            ("Yao", "Kouadio", null, "Abidjan", false),
            ("Akissi", "Brou", null, "Aboisso", false),
            ("Konan", "Ahou", null, "Abidjan", false),
            ("Amoin", "Ehui", null, "Abidjan", false),
            ("Tanoh", "N'Dri", null, "Divo", false),
            ("Loukou", "Gnamba", null, "Abidjan", false),
            ("Yawa", "Mensah", null, "Lomé", false),
            ("Jean-Marc", "Dupont", "SCI Bassam Invest", "Paris", false),
            ("Ali", "Hassan", "Gulf Properties", "Dubai", false),
            ("Pierre", "Lefèvre", null, "Abidjan", false),
            ("Nathalie", "Konan-Martin", null, "Abidjan", false),
            ("Mohamed", "Al-Rashid", "Sahara Holdings", "Abidjan", false),
            ("Christelle", "Ehui-Briand", null, "Abidjan", false),
            ("Patrick", "Kacou", null, "Abidjan", false),
            ("Marie-Claire", "Gnagbo", null, "Grand Bassam", false),
            ("Charles", "Assoumou", null, "Abidjan", false),
            ("Eugénie", "Kra", null, "Abidjan", false),
            ("Bernard", "Yapi", null, "Abidjan", false),
            ("Colette", "Tanoh", null, "Yamoussoukro", false),
            ("Gustave", "Bédié", null, "Daoukro", false),
            ("Solange", "Gbagbo", null, "Mama", false),
            ("Raymond", "Mahi", null, "Abidjan", false),
            ("Théodore", "Yohou", null, "Gagnoa", false),
            ("Pascaline", "Koffi-Aké", null, "Abidjan", false),
            ("Jules", "Kobenan", null, "Abidjan", false),
            ("Hortense", "N'Cho", null, "Abidjan", false),
            ("Victor", "Amon", null, "Abidjan", false),
            ("Rose", "Dacoury", null, "Abidjan", false),
            ("Didier", "Edi", null, "Abidjan", false),
            ("Brigitte", "Kouyaté", null, "Abidjan", false),
            ("Samuel", "Ouraga", null, "Abidjan", false),
            ("Clarisse", "Koudou", null, "Grand Bassam", false),
            ("Emmanuel", "Esso", null, "Abidjan", false),
            ("Gisèle", "Tapé", null, "Abidjan", false),
            ("François", "Loba", null, "Abidjan", false),
            ("Henriette", "Adon", null, "Abidjan", false),
            ("Georges", "Bile", null, "Abidjan", false),
        };

        var list = new List<Owner>();
        int i = 0;
        foreach (var (first, last, company, city, council) in names)
        {
            i++;
            list.Add(new Owner
            {
                Id = Guid.NewGuid(), OrganizationId = orgId,
                FirstName = first, LastName = last,
                CompanyName = company,
                Email = $"{first.ToLower().Replace(" ", "").Replace("-", "")}.{last.ToLower().Replace(" ", "").Replace("'", "")}@{(company != null ? "corp" : "mail")}.ci",
                Phone = $"+22507{i:D8}",
                City = city, Country = "CI",
                IsCouncilMember = council,
                IsCouncilPresident = first == "Amadou" && last == "Koné"
            });
        }
        return list;
    }

    private static List<Supplier> CreateSuppliers(Guid orgId) =>
    [
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Plomberie Express Bassam", ContactPerson = "Koné Sékou", Phone = "+22507101010", Specialty = "Plomberie", Email = "contact@plomberie-express.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "ElectroPro CI", ContactPerson = "Diallo Ali", Phone = "+22507202020", Specialty = "Électricité", Email = "info@electropro.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "JardiVert Services", ContactPerson = "Touré Mamadou", Phone = "+22507303030", Specialty = "Espaces verts", Email = "contact@jardivert.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "STEP Ingénierie", ContactPerson = "Bamba Lamine", Phone = "+22507404040", Specialty = "Traitement des eaux", Email = "info@step-ing.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Sécurité Plus Abidjan", ContactPerson = "Camara Ousmane", Phone = "+22507505050", Specialty = "Sécurité", Email = "contact@securiteplus.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Ascenseurs CI (OTIS)", ContactPerson = "Martin Pierre", Phone = "+22507606060", Specialty = "Ascenseurs", Email = "ci@otis.com", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Peinture & Ravalement Pro", ContactPerson = "Soro Yves", Phone = "+22507707070", Specialty = "Peinture", Email = "contact@ravalement.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "ClimFroid Solutions", ContactPerson = "N'Guessan Koffi", Phone = "+22507808080", Specialty = "Climatisation", Email = "service@climfroid.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Menuiserie Alu Bassam", ContactPerson = "Kouyaté Baba", Phone = "+22507909090", Specialty = "Menuiserie aluminium", Email = "contact@menualu.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "NetPropre CI", ContactPerson = "Diarra Fanta", Phone = "+22507111111", Specialty = "Nettoyage", Email = "contact@netpropre.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "DésinsectPlus", ContactPerson = "Cissé Boubacar", Phone = "+22507121212", Specialty = "Désinsectisation", Email = "info@desinsectplus.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Assurances NSIA", ContactPerson = "Yao Ange", Phone = "+22507131313", Specialty = "Assurance", Email = "contact@nsia.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Cabinet Juridique Koné & Associés", ContactPerson = "Koné Mariam", Phone = "+22507141414", Specialty = "Juridique", Email = "cabinet@kone-avocats.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Géomètre Assi & Partners", ContactPerson = "Assi Bertrand", Phone = "+22507151515", Specialty = "Géomètre-expert", Email = "contact@assi-geo.ci", IsActive = true },
        new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Groupe Électrogène CI", ContactPerson = "Bédié Thomas", Phone = "+22507161616", Specialty = "Groupe électrogène", Email = "service@genset-ci.ci", IsActive = true },
    ];

    private static List<LegalReference> CreateLegalReferences() =>
    [
        // Copropriété
        new() { Id = Guid.NewGuid(), Code = "CCH-381", Title = "Statut de la copropriété", Content = "Les immeubles bâtis ou groupes d'immeubles bâtis dont la propriété est répartie entre plusieurs personnes par lots...", Domain = LegalDomain.Copropriete, Source = "CCH", IsActive = true, Tags = "[\"copropriété\",\"définition\",\"lot\"]" },
        new() { Id = Guid.NewGuid(), Code = "CCH-388", Title = "Convocation AG — Délai 15 jours", Content = "La convocation de l'assemblée générale est faite au moins quinze jours avant la date de la réunion...", Domain = LegalDomain.Copropriete, Source = "CCH", IsActive = true, Tags = "[\"AG\",\"convocation\",\"délai\"]" },
        new() { Id = Guid.NewGuid(), Code = "CCH-396", Title = "Fonds de réserve obligatoire", Content = "Le syndic est tenu de constituer un fonds de réserve pour faire face aux dépenses imprévues...", Domain = LegalDomain.Copropriete, Source = "CCH", IsActive = true, Tags = "[\"réserve\",\"fonds\",\"syndic\"]" },
        new() { Id = Guid.NewGuid(), Code = "CCH-397", Title = "Honoraires du syndic — Max 30%", Content = "Les honoraires du syndic ne peuvent excéder trente pour cent du budget prévisionnel...", Domain = LegalDomain.Copropriete, Source = "CCH", IsActive = true, Tags = "[\"syndic\",\"honoraires\",\"plafond\"]" },

        // Bail résidentiel
        new() { Id = Guid.NewGuid(), Code = "CCH-414", Title = "Bail d'habitation — Définition", Content = "Le bail à usage d'habitation est le contrat par lequel une personne met un immeuble à la disposition d'une autre...", Domain = LegalDomain.BailResidentiel, Source = "CCH", IsActive = true, Tags = "[\"bail\",\"habitation\",\"définition\"]" },
        new() { Id = Guid.NewGuid(), Code = "CCH-423", Title = "Révision triennale du loyer", Content = "Le loyer peut être révisé tous les trois ans à la demande de l'une des parties...", Domain = LegalDomain.BailResidentiel, Source = "CCH", IsActive = true, Tags = "[\"loyer\",\"révision\",\"triennale\"]" },
        new() { Id = Guid.NewGuid(), Code = "CCH-450", Title = "Congé — Préavis 3 mois", Content = "Le congé est donné par lettre recommandée avec accusé de réception, au moins trois mois avant...", Domain = LegalDomain.BailResidentiel, Source = "CCH", IsActive = true, Tags = "[\"congé\",\"préavis\",\"résiliation\"]" },

        // Bail commercial OHADA
        new() { Id = Guid.NewGuid(), Code = "AUDCG-101", Title = "Bail commercial — Champ d'application OHADA", Content = "Les dispositions du présent titre s'appliquent aux baux portant sur des immeubles ou locaux à usage commercial...", Domain = LegalDomain.BailCommercial, Source = "AUDCG", IsActive = true, Tags = "[\"bail\",\"commercial\",\"OHADA\"]" },
        new() { Id = Guid.NewGuid(), Code = "AUDCG-116", Title = "Révision du loyer commercial", Content = "La révision du loyer peut être demandée par chacune des parties tous les trois ans...", Domain = LegalDomain.BailCommercial, Source = "AUDCG", IsActive = true, Tags = "[\"loyer\",\"révision\",\"commercial\"]" },
        new() { Id = Guid.NewGuid(), Code = "AUDCG-123", Title = "Droit au renouvellement", Content = "Le preneur qui justifie d'une exploitation effective du fonds pendant au moins deux ans a droit au renouvellement...", Domain = LegalDomain.BailCommercial, Source = "AUDCG", IsActive = true, Tags = "[\"renouvellement\",\"droit\",\"fonds\"]" },

        // Fiscalité
        new() { Id = Guid.NewGuid(), Code = "CGI-155", Title = "Impôt foncier — Assiette", Content = "L'impôt sur le patrimoine foncier des propriétés bâties est assis sur la valeur locative des immeubles...", Domain = LegalDomain.Fiscalite, Source = "CGI", IsActive = true, Tags = "[\"impôt\",\"foncier\",\"valeur locative\"]" },
        new() { Id = Guid.NewGuid(), Code = "CGI-TVA-18", Title = "TVA 18% — Prestations immobilières", Content = "Le taux normal de la taxe sur la valeur ajoutée est fixé à dix-huit pour cent...", Domain = LegalDomain.Fiscalite, Source = "CGI", IsActive = true, Tags = "[\"TVA\",\"18%\",\"taxe\"]" },
    ];
}
