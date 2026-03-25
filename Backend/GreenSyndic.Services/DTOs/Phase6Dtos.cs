using GreenSyndic.Core.Enums;

namespace GreenSyndic.Services.DTOs;

// ── Organization Settings ──

public class OrganizationSettingsDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Currency { get; set; } = "XOF";
    public int FiscalYearStartMonth { get; set; }
    public decimal DefaultVatRate { get; set; }
    public int PaymentDueDays { get; set; }
    public int RentDueDay { get; set; }
    public bool AutoGenerateRentCalls { get; set; }
    public bool AutoSendReminders { get; set; }
    public int ReminderDaysBefore { get; set; }
    public int OverdueDaysThreshold { get; set; }
    public bool ApplyLateFees { get; set; }
    public decimal LateFeePercent { get; set; }
    public string? DefaultEmailFrom { get; set; }
    public string? DefaultSmsFrom { get; set; }
    public string? CompanySignature { get; set; }
    public long MaxDocumentSizeBytes { get; set; }
    public string AllowedFileExtensions { get; set; } = default!;
    public string? CinetPayApiKey { get; set; }
    public string? CinetPaySiteId { get; set; }
    public string Timezone { get; set; } = default!;
    public string Locale { get; set; } = default!;
}

public class UpdateOrganizationSettingsRequest
{
    public string? Currency { get; set; }
    public int? FiscalYearStartMonth { get; set; }
    public decimal? DefaultVatRate { get; set; }
    public int? PaymentDueDays { get; set; }
    public int? RentDueDay { get; set; }
    public bool? AutoGenerateRentCalls { get; set; }
    public bool? AutoSendReminders { get; set; }
    public int? ReminderDaysBefore { get; set; }
    public int? OverdueDaysThreshold { get; set; }
    public bool? ApplyLateFees { get; set; }
    public decimal? LateFeePercent { get; set; }
    public string? DefaultEmailFrom { get; set; }
    public string? DefaultSmsFrom { get; set; }
    public string? CompanySignature { get; set; }
    public long? MaxDocumentSizeBytes { get; set; }
    public string? AllowedFileExtensions { get; set; }
    public string? CinetPayApiKey { get; set; }
    public string? CinetPaySiteId { get; set; }
    public string? Timezone { get; set; }
    public string? Locale { get; set; }
}

// ── Legal Reference (Veille Juridique) ──

public class LegalReferenceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public LegalDomain Domain { get; set; }
    public string? Source { get; set; }
    public string? Url { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public bool IsActive { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLegalReferenceRequest
{
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public LegalDomain Domain { get; set; }
    public string? Source { get; set; }
    public string? Url { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
}

// ── Export ──

public class ExportResultDto
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public int RowCount { get; set; }
}
