using GreenSyndic.Core.Entities;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Services.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

/// <summary>
/// Paramétrage par organisation — configuration financière, loyers,
/// communication, documents, mobile money.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId:guid}/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public SettingsController(GreenSyndicDbContext db) => _db = db;

    /// <summary>
    /// Get settings for an organization.
    /// Creates default settings if none exist yet.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<OrganizationSettingsDto>> Get(Guid organizationId)
    {
        var org = await _db.Organizations.FindAsync(organizationId);
        if (org == null) return NotFound("Organization not found");

        var settings = await _db.OrganizationSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            // Auto-create default settings
            settings = new OrganizationSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId
            };
            _db.OrganizationSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return Ok(MapToDto(settings));
    }

    /// <summary>
    /// Update settings (partial update — only provided fields are changed).
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<OrganizationSettingsDto>> Update(
        Guid organizationId,
        [FromBody] UpdateOrganizationSettingsRequest request)
    {
        var org = await _db.Organizations.FindAsync(organizationId);
        if (org == null) return NotFound("Organization not found");

        var settings = await _db.OrganizationSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            settings = new OrganizationSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId
            };
            _db.OrganizationSettings.Add(settings);
        }

        // Partial update — only set non-null values
        if (request.Currency != null) settings.Currency = request.Currency;
        if (request.FiscalYearStartMonth.HasValue) settings.FiscalYearStartMonth = request.FiscalYearStartMonth.Value;
        if (request.DefaultVatRate.HasValue) settings.DefaultVatRate = request.DefaultVatRate.Value;
        if (request.PaymentDueDays.HasValue) settings.PaymentDueDays = request.PaymentDueDays.Value;
        if (request.RentDueDay.HasValue) settings.RentDueDay = request.RentDueDay.Value;
        if (request.AutoGenerateRentCalls.HasValue) settings.AutoGenerateRentCalls = request.AutoGenerateRentCalls.Value;
        if (request.AutoSendReminders.HasValue) settings.AutoSendReminders = request.AutoSendReminders.Value;
        if (request.ReminderDaysBefore.HasValue) settings.ReminderDaysBefore = request.ReminderDaysBefore.Value;
        if (request.OverdueDaysThreshold.HasValue) settings.OverdueDaysThreshold = request.OverdueDaysThreshold.Value;
        if (request.ApplyLateFees.HasValue) settings.ApplyLateFees = request.ApplyLateFees.Value;
        if (request.LateFeePercent.HasValue) settings.LateFeePercent = request.LateFeePercent.Value;
        if (request.DefaultEmailFrom != null) settings.DefaultEmailFrom = request.DefaultEmailFrom;
        if (request.DefaultSmsFrom != null) settings.DefaultSmsFrom = request.DefaultSmsFrom;
        if (request.CompanySignature != null) settings.CompanySignature = request.CompanySignature;
        if (request.MaxDocumentSizeBytes.HasValue) settings.MaxDocumentSizeBytes = request.MaxDocumentSizeBytes.Value;
        if (request.AllowedFileExtensions != null) settings.AllowedFileExtensions = request.AllowedFileExtensions;
        if (request.CinetPayApiKey != null) settings.CinetPayApiKey = request.CinetPayApiKey;
        if (request.CinetPaySiteId != null) settings.CinetPaySiteId = request.CinetPaySiteId;
        if (request.Timezone != null) settings.Timezone = request.Timezone;
        if (request.Locale != null) settings.Locale = request.Locale;

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapToDto(settings));
    }

    private static OrganizationSettingsDto MapToDto(OrganizationSettings s) => new()
    {
        Id = s.Id,
        OrganizationId = s.OrganizationId,
        Currency = s.Currency,
        FiscalYearStartMonth = s.FiscalYearStartMonth,
        DefaultVatRate = s.DefaultVatRate,
        PaymentDueDays = s.PaymentDueDays,
        RentDueDay = s.RentDueDay,
        AutoGenerateRentCalls = s.AutoGenerateRentCalls,
        AutoSendReminders = s.AutoSendReminders,
        ReminderDaysBefore = s.ReminderDaysBefore,
        OverdueDaysThreshold = s.OverdueDaysThreshold,
        ApplyLateFees = s.ApplyLateFees,
        LateFeePercent = s.LateFeePercent,
        DefaultEmailFrom = s.DefaultEmailFrom,
        DefaultSmsFrom = s.DefaultSmsFrom,
        CompanySignature = s.CompanySignature,
        MaxDocumentSizeBytes = s.MaxDocumentSizeBytes,
        AllowedFileExtensions = s.AllowedFileExtensions,
        CinetPayApiKey = s.CinetPayApiKey,
        CinetPaySiteId = s.CinetPaySiteId,
        Timezone = s.Timezone,
        Locale = s.Locale
    };
}
