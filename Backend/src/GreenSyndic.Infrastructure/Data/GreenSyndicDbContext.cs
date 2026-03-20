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

    private static void ApplySoftDeleteFilter<T>(ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }
}
