using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Co-ownership entity. Supports dual-level: Horizontal (entire estate) + Vertical (per building).
/// </summary>
public class CoOwnership : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public string Name { get; set; } = default!;              // "Green City Bassam" or "Immeuble Acajou"
    public CoOwnershipLevel Level { get; set; }
    public string? Description { get; set; }
    public string? RegulationReference { get; set; }           // Ref. règlement de copropriété
    public decimal AnnualBudget { get; set; }                  // Budget annuel en XOF
    public decimal ReserveFund { get; set; }                   // Fonds de réserve
    public decimal SyndicFeePercent { get; set; }              // Max 30% (art. 397)

    // For vertical co-ownership, link to parent horizontal
    public Guid? ParentCoOwnershipId { get; set; }
    public CoOwnership? ParentCoOwnership { get; set; }

    // Navigation
    public ICollection<CoOwnership> ChildCoOwnerships { get; set; } = [];
    public ICollection<Building> Buildings { get; set; } = [];
    public ICollection<Unit> Units { get; set; } = [];
    public ICollection<ChargeDefinition> ChargeDefinitions { get; set; } = [];
    public ICollection<Meeting> Meetings { get; set; } = [];
}
