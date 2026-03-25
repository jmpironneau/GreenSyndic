using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Quittance de loyer (preuve de paiement).
/// Générée automatiquement après paiement complet ou partiel d'un appel de loyer.
/// Envoi dématérialisé conformément au CCH.
/// </summary>
public class RentReceipt : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid RentCallId { get; set; }
    public RentCall RentCall { get; set; } = default!;

    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = default!;

    public string Reference { get; set; } = default!;         // QT-2026-03-001

    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal RentAmount { get; set; }                     // Loyer nu payé
    public decimal ChargesAmount { get; set; }                  // Charges payées
    public decimal TotalAmount { get; set; }                    // Total quittancé

    public RentReceiptStatus Status { get; set; } = RentReceiptStatus.Draft;

    public DateTime? IssuedAt { get; set; }
    public DateTime? SentAt { get; set; }

    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public string? Notes { get; set; }
}
