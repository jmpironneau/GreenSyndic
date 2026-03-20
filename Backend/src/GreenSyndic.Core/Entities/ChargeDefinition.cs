using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Definition of a recurring charge for a copropriété.
/// </summary>
public class ChargeDefinition : BaseEntity
{
    public Guid CoproprieteId { get; set; }
    public Copropriete Copropriete { get; set; } = default!;

    public string Name { get; set; } = default!;               // "Sécurité 24h", "STEP", "Ascenseur"
    public ChargeType Type { get; set; }
    public decimal AnnualAmount { get; set; }                  // Montant annuel total
    public string? DistributionKey { get; set; }               // "tantieme", "equal", "surface"
    public bool IsRecoverable { get; set; }                    // Charges récupérables sur locataire (art. 417)
    public string? Description { get; set; }
    public int FiscalYear { get; set; }
}
