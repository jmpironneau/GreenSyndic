using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Payment record (charges, rent, or any financial transaction).
/// Supports mobile money (Orange Money, MTN, Wave, CinetPay, PayDunya).
/// </summary>
public class Payment : BaseEntity
{
    public Guid AppTenantId { get; set; }

    public string Reference { get; set; } = default!;         // Numéro de paiement
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "XOF";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaymentDate { get; set; }
    public string? TransactionId { get; set; }                 // ID transaction mobile money
    public string? Description { get; set; }

    // Can be linked to owner charge or lease payment
    public Guid? OwnerId { get; set; }
    public Owner? Owner { get; set; }

    public Guid? LocataireId { get; set; }
    public Locataire? Locataire { get; set; }

    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }

    public Guid? ChargeAssignmentId { get; set; }
    public ChargeAssignment? ChargeAssignment { get; set; }
}
