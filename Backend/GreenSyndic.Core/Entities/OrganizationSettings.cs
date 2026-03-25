namespace GreenSyndic.Core.Entities;

/// <summary>
/// Per-organization settings and configuration.
/// One-to-one with Organization.
/// </summary>
public class OrganizationSettings : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    // Financial settings
    public string Currency { get; set; } = "XOF";
    public int FiscalYearStartMonth { get; set; } = 1;      // January by default
    public decimal DefaultVatRate { get; set; } = 18;        // 18% TVA Côte d'Ivoire
    public int PaymentDueDays { get; set; } = 30;            // Délai de paiement

    // Rent settings
    public int RentDueDay { get; set; } = 5;                 // Jour d'échéance loyer
    public bool AutoGenerateRentCalls { get; set; } = false;
    public bool AutoSendReminders { get; set; } = false;
    public int ReminderDaysBefore { get; set; } = 5;
    public int OverdueDaysThreshold { get; set; } = 15;      // Jours avant marquage impayé

    // Late fees (pénalités de retard)
    public bool ApplyLateFees { get; set; } = false;
    public decimal LateFeePercent { get; set; } = 5;         // % par mois de retard

    // Communication
    public string? DefaultEmailFrom { get; set; }
    public string? DefaultSmsFrom { get; set; }
    public string? CompanySignature { get; set; }

    // Document settings
    public long MaxDocumentSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public string AllowedFileExtensions { get; set; } = ".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx";

    // Mobile money
    public string? CinetPayApiKey { get; set; }
    public string? CinetPaySiteId { get; set; }

    // Misc
    public string Timezone { get; set; } = "Africa/Abidjan";
    public string Locale { get; set; } = "fr-CI";
}
