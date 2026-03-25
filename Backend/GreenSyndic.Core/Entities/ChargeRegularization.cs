using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Régularisation des charges locatives.
/// Comparaison entre provisions versées et charges réelles sur la période.
/// Régularisation annuelle ou de sortie.
/// </summary>
public class ChargeRegularization : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = default!;

    public string Reference { get; set; } = default!;         // REG-2026-001

    public RegularizationType Type { get; set; }
    public RegularizationStatus Status { get; set; } = RegularizationStatus.Draft;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal TotalProvisioned { get; set; }               // Total des provisions versées
    public decimal TotalActual { get; set; }                    // Total des charges réelles
    public decimal Balance { get; set; }                        // Solde (>0 = trop perçu, <0 = complément dû)

    // Detailed breakdown (JSON for flexibility)
    public string? BreakdownJson { get; set; }                  // Détail par type de charge

    public DateTime? NotifiedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ContestedAt { get; set; }
    public string? ContestationReason { get; set; }
    public DateTime? SettledAt { get; set; }

    public Guid? PaymentId { get; set; }                        // Paiement de régularisation
    public Payment? Payment { get; set; }

    public string? Notes { get; set; }
}
