using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Appel de loyer mensuel (quittancement).
/// Généré automatiquement ou manuellement pour chaque bail actif.
/// </summary>
public class RentCall : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = default!;

    public string Reference { get; set; } = default!;         // AL-2026-03-001
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal RentAmount { get; set; }                     // Loyer nu
    public decimal ChargesAmount { get; set; }                  // Provisions pour charges
    public decimal TotalAmount { get; set; }                    // Loyer + charges
    public decimal PaidAmount { get; set; }                     // Montant déjà réglé
    public decimal RemainingAmount { get; set; }                // Reste à payer

    public RentCallStatus Status { get; set; } = RentCallStatus.Draft;

    public DateTime DueDate { get; set; }                       // Date d'échéance
    public DateTime? SentAt { get; set; }                       // Date d'envoi
    public DateTime? PaidAt { get; set; }                       // Date de paiement complet

    // Commercial-specific
    public decimal? TurnoverRentAmount { get; set; }            // % CA
    public decimal? MarketingContribution { get; set; }         // Fonds marketing

    public string? Notes { get; set; }

    // Navigation
    public ICollection<RentReceipt> Receipts { get; set; } = [];
}
