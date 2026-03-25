using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// A lot/unit: villa, apartment, commercial space, etc.
/// </summary>
public class Unit : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = default!;

    public Guid? CoOwnershipId { get; set; }
    public CoOwnership? CoOwnership { get; set; }

    public string Reference { get; set; } = default!;         // "V-MN-001", "A-AC-301", "COSMOS-B12"
    public string? Name { get; set; }                          // Nom convivial
    public PropertyType Type { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Available;
    public int? Floor { get; set; }
    public decimal AreaSqm { get; set; }                       // Surface m²
    public decimal? ShareRatio { get; set; }                    // Quote-part (millièmes)
    public decimal? HorizontalShareRatio { get; set; }         // Quote-part copro horizontale
    public int? NumberOfRooms { get; set; }                    // F3, F4, F5, F6
    public decimal? MarketValue { get; set; }                  // Valeur marchande FCFA
    public string? Description { get; set; }

    // Navigation
    public Guid? OwnerId { get; set; }
    public Owner? Owner { get; set; }
    public ICollection<Lease> Leases { get; set; } = [];
    public ICollection<ChargeAssignment> ChargeAssignments { get; set; } = [];
    public ICollection<Incident> Incidents { get; set; } = [];
}
