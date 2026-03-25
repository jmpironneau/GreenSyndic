using GreenSyndic.Core.Entities;
using GreenSyndic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Infrastructure.Data;

public class GreenSyndicDbContext : IdentityDbContext<ApplicationUser>
{
    public GreenSyndicDbContext(DbContextOptions<GreenSyndicDbContext> options) : base(options) { }

    // Domain entities
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<CoOwnership> CoOwnerships => Set<CoOwnership>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<LeaseTenant> LeaseTenants => Set<LeaseTenant>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<ChargeDefinition> ChargeDefinitions => Set<ChargeDefinition>();
    public DbSet<ChargeAssignment> ChargeAssignments => Set<ChargeAssignment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<Resolution> Resolutions => Set<Resolution>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();

    // Phase 3A: Assemblées Générales
    public DbSet<MeetingAttendee> MeetingAttendees => Set<MeetingAttendee>();
    public DbSet<MeetingAgendaItem> MeetingAgendaItems => Set<MeetingAgendaItem>();
    public DbSet<ResolutionTemplate> ResolutionTemplates => Set<ResolutionTemplate>();

    // Phase 3B: Communication
    public DbSet<CommunicationMessage> CommunicationMessages => Set<CommunicationMessage>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<Broadcast> Broadcasts => Set<Broadcast>();
    public DbSet<BroadcastRecipient> BroadcastRecipients => Set<BroadcastRecipient>();
    public DbSet<MessageDeliveryLog> MessageDeliveryLogs => Set<MessageDeliveryLog>();

    // Phase 4: Gestion Locative
    public DbSet<RentCall> RentCalls => Set<RentCall>();
    public DbSet<RentReceipt> RentReceipts => Set<RentReceipt>();
    public DbSet<LeaseRevision> LeaseRevisions => Set<LeaseRevision>();
    public DbSet<ChargeRegularization> ChargeRegularizations => Set<ChargeRegularization>();
    public DbSet<TenantApplication> TenantApplications => Set<TenantApplication>();

    // Phase 6: Modules secondaires
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<LegalReference> LegalReferences => Set<LegalReference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Global query filter for soft delete
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(GreenSyndicDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, [builder]);
            }
        }

        // CoOwnership self-referencing (horizontal -> vertical)
        builder.Entity<CoOwnership>(e =>
        {
            e.HasOne(c => c.ParentCoOwnership)
                .WithMany(c => c.ChildCoOwnerships)
                .HasForeignKey(c => c.ParentCoOwnershipId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Unit -> Owner
        builder.Entity<Unit>(e =>
        {
            e.HasOne(u => u.Owner)
                .WithMany(o => o.Units)
                .HasForeignKey(u => u.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(u => u.Reference).IsUnique();
        });

        // Lease
        builder.Entity<Lease>(e =>
        {
            e.HasIndex(l => l.Reference).IsUnique();
        });

        // Payment
        builder.Entity<Payment>(e =>
        {
            e.HasIndex(p => p.Reference).IsUnique();
        });

        // AccountingEntry
        builder.Entity<AccountingEntry>(e =>
        {
            e.HasIndex(a => a.EntryNumber).IsUnique();
            e.Property(a => a.Debit).HasPrecision(18, 2);
            e.Property(a => a.Credit).HasPrecision(18, 2);
        });

        // MeetingAttendee: one attendance record per owner per meeting
        builder.Entity<MeetingAttendee>(e =>
        {
            e.HasIndex(a => new { a.MeetingId, a.OwnerId }).IsUnique();
        });

        // MeetingAgendaItem: one order number per meeting
        builder.Entity<MeetingAgendaItem>(e =>
        {
            e.HasIndex(a => new { a.MeetingId, a.OrderNumber }).IsUnique();
        });

        // ResolutionTemplate
        builder.Entity<ResolutionTemplate>(e =>
        {
            e.HasIndex(t => new { t.OrganizationId, t.Code }).IsUnique();
        });

        // MessageTemplate
        builder.Entity<MessageTemplate>(e =>
        {
            e.HasIndex(t => new { t.OrganizationId, t.Code }).IsUnique();
        });

        // Phase 4: Gestion Locative

        // RentCall: one per lease per month
        builder.Entity<RentCall>(e =>
        {
            e.HasIndex(r => r.Reference).IsUnique();
            e.HasIndex(r => new { r.LeaseId, r.Year, r.Month }).IsUnique();
        });

        // RentReceipt
        builder.Entity<RentReceipt>(e =>
        {
            e.HasIndex(r => r.Reference).IsUnique();
        });

        // LeaseRevision
        builder.Entity<LeaseRevision>(e =>
        {
            e.HasIndex(r => r.Reference).IsUnique();
        });

        // ChargeRegularization
        builder.Entity<ChargeRegularization>(e =>
        {
            e.HasIndex(r => r.Reference).IsUnique();
        });

        // TenantApplication
        builder.Entity<TenantApplication>(e =>
        {
            e.HasIndex(a => a.Reference).IsUnique();
        });

        // Phase 6: OrganizationSettings — one per org
        builder.Entity<OrganizationSettings>(e =>
        {
            e.HasIndex(s => s.OrganizationId).IsUnique();
            e.HasOne(s => s.Organization).WithOne().HasForeignKey<OrganizationSettings>(s => s.OrganizationId);
        });

        // Phase 6: LegalReference
        builder.Entity<LegalReference>(e =>
        {
            e.HasIndex(l => l.Code).IsUnique();
        });

        // Decimal precision for monetary values
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetPrecision() == null)
                property.SetPrecision(18);
            if (property.GetScale() == null)
                property.SetScale(2);
        }
    }

    public override int SaveChanges()
    {
        NormalizeDateTimesToUtc();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Npgsql 10 requires DateTimeKind.Utc for timestamptz columns.
    /// This normalizes all DateTime properties on tracked entities before save.
    /// </summary>
    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
                    {
                        prop.CurrentValue = dt.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                            : dt.ToUniversalTime();
                    }
                }
            }
        }
    }

    private static void ApplySoftDeleteFilter<T>(ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }
}
