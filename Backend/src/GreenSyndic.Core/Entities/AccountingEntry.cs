namespace GreenSyndic.Core.Entities;

/// <summary>
/// SYSCOHADA-compliant accounting entry (double-entry bookkeeping).
/// </summary>
public class AccountingEntry : BaseEntity
{
    public Guid AppTenantId { get; set; }

    public string EntryNumber { get; set; } = default!;        // Numéro d'écriture
    public DateTime EntryDate { get; set; }
    public string JournalCode { get; set; } = default!;        // "VE" (ventes), "AC" (achats), "BQ" (banque), "OD" (opérations diverses)
    public string AccountCode { get; set; } = default!;        // Plan comptable SYSCOHADA
    public string? AccountLabel { get; set; }
    public string Description { get; set; } = default!;        // Libellé de l'écriture
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int FiscalYear { get; set; }
    public int Period { get; set; }                            // Month 1-12
    public bool IsValidated { get; set; }

    // Link to source document
    public Guid? PaymentId { get; set; }
    public Guid? ChargeAssignmentId { get; set; }
    public Guid? LeaseId { get; set; }

    public Guid? CoproprieteId { get; set; }
}
