using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Révision de loyer (triennale CCH art. 423-424 résidentiel, AUDCG art. 116 commercial).
/// Calcule le nouveau loyer et trace notification + acceptation.
/// </summary>
public class LeaseRevision : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = default!;

    public string Reference { get; set; } = default!;         // REV-2026-001

    public RevisionType Type { get; set; }
    public RevisionStatus Status { get; set; } = RevisionStatus.Pending;

    public DateTime EffectiveDate { get; set; }                 // Date d'effet
    public DateTime? NotificationDate { get; set; }             // Date de notification au locataire

    public decimal PreviousRent { get; set; }                   // Loyer avant révision
    public decimal NewRent { get; set; }                        // Loyer après révision
    public decimal VariationPercent { get; set; }               // % de variation

    // Index-based revision
    public string? IndexName { get; set; }                      // Nom de l'indice (IRL, ICC, ILAT)
    public decimal? IndexValueOld { get; set; }
    public decimal? IndexValueNew { get; set; }

    // Legal reference
    public string? LegalBasis { get; set; }                     // "CCH art. 423" / "AUDCG art. 116"
    public string? Justification { get; set; }                  // Motivation détaillée

    public DateTime? AcceptedAt { get; set; }
    public DateTime? ContestedAt { get; set; }
    public string? ContestationReason { get; set; }

    public string? Notes { get; set; }
}
