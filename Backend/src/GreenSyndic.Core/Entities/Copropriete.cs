using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Co-ownership entity. Supports dual-level: Horizontal (entire estate) + Vertical (per building).
/// </summary>
public class Copropriete : BaseEntity
{
    public Guid AppTenantId { get; set; }
    public AppTenant AppTenant { get; set; } = default!;

    public string Name { get; set; } = default!;              // "Green City Bassam" or "Immeuble Acajou"
    public CoproprieteLevelType Level { get; set; }
    public string? Description { get; set; }
    public string? RegulationReference { get; set; }           // Ref. règlement de copropriété
    public decimal AnnualBudget { get; set; }                  // Budget annuel en XOF
    public decimal ReserveFund { get; set; }                   // Fonds de réserve
    public decimal SyndicFeePercent { get; set; }              // Max 30% (art. 397)

    // For vertical copropriete, link to parent horizontal
    public Guid? ParentCoproprieteId { get; set; }
    public Copropriete? ParentCopropriete { get; set; }

    // Navigation
    public ICollection<Copropriete> ChildCoproprietes { get; set; } = [];
    public ICollection<Building> Buildings { get; set; } = [];
    public ICollection<Unit> Units { get; set; } = [];
    public ICollection<ChargeDefinition> ChargeDefinitions { get; set; } = [];
    public ICollection<Meeting> Meetings { get; set; } = [];
}
