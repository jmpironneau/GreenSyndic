using GreenSyndic.Core.Entities;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Services.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountingEntriesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public AccountingEntriesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    private Guid GetOrgIdFromClaim()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountingEntryDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? coOwnershipId,
        [FromQuery] int? fiscalYear,
        [FromQuery] int? period,
        [FromQuery] string? journalCode,
        [FromQuery] string? accountCode,
        [FromQuery] bool? isValidated)
    {
        var query = _db.AccountingEntries.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(e => e.OrganizationId == organizationId.Value);

        if (coOwnershipId.HasValue)
            query = query.Where(e => e.CoOwnershipId == coOwnershipId.Value);

        if (fiscalYear.HasValue)
            query = query.Where(e => e.FiscalYear == fiscalYear.Value);

        if (period.HasValue)
            query = query.Where(e => e.Period == period.Value);

        if (!string.IsNullOrEmpty(journalCode))
            query = query.Where(e => e.JournalCode == journalCode);

        if (!string.IsNullOrEmpty(accountCode))
            query = query.Where(e => e.AccountCode.StartsWith(accountCode));

        if (isValidated.HasValue)
            query = query.Where(e => e.IsValidated == isValidated.Value);

        var items = await query.OrderByDescending(e => e.EntryDate)
            .ThenBy(e => e.EntryNumber)
            .Select(e => new AccountingEntryDto
            {
                Id = e.Id,
                OrganizationId = e.OrganizationId,
                EntryNumber = e.EntryNumber,
                EntryDate = e.EntryDate,
                JournalCode = e.JournalCode,
                AccountCode = e.AccountCode,
                AccountLabel = e.AccountLabel,
                Description = e.Description,
                Debit = e.Debit,
                Credit = e.Credit,
                FiscalYear = e.FiscalYear,
                Period = e.Period,
                IsValidated = e.IsValidated,
                PaymentId = e.PaymentId,
                ChargeAssignmentId = e.ChargeAssignmentId,
                LeaseId = e.LeaseId,
                CoOwnershipId = e.CoOwnershipId
            }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountingEntryDto>> GetById(Guid id)
    {
        var e = await _db.AccountingEntries.FirstOrDefaultAsync(x => x.Id == id);

        if (e == null) return NotFound();

        string? coOwnershipName = null;
        if (e.CoOwnershipId.HasValue)
        {
            var co = await _db.CoOwnerships.FindAsync(e.CoOwnershipId.Value);
            coOwnershipName = co?.Name;
        }

        return Ok(new AccountingEntryDto
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            EntryNumber = e.EntryNumber,
            EntryDate = e.EntryDate,
            JournalCode = e.JournalCode,
            AccountCode = e.AccountCode,
            AccountLabel = e.AccountLabel,
            Description = e.Description,
            Debit = e.Debit,
            Credit = e.Credit,
            FiscalYear = e.FiscalYear,
            Period = e.Period,
            IsValidated = e.IsValidated,
            PaymentId = e.PaymentId,
            ChargeAssignmentId = e.ChargeAssignmentId,
            LeaseId = e.LeaseId,
            CoOwnershipId = e.CoOwnershipId,
            CoOwnershipName = coOwnershipName
        });
    }

    [HttpPost]
    public async Task<ActionResult<AccountingEntryDto>> Create([FromBody] CreateAccountingEntryRequest request)
    {
        var entity = new AccountingEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = GetOrgIdFromClaim(),
            EntryNumber = request.EntryNumber,
            EntryDate = DateTime.SpecifyKind(request.EntryDate, DateTimeKind.Utc),
            JournalCode = request.JournalCode,
            AccountCode = request.AccountCode,
            AccountLabel = request.AccountLabel,
            Description = request.Description,
            Debit = request.Debit,
            Credit = request.Credit,
            FiscalYear = request.FiscalYear,
            Period = request.Period,
            PaymentId = request.PaymentId,
            ChargeAssignmentId = request.ChargeAssignmentId,
            LeaseId = request.LeaseId,
            CoOwnershipId = request.CoOwnershipId
        };

        _db.AccountingEntries.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new AccountingEntryDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            EntryNumber = entity.EntryNumber,
            EntryDate = entity.EntryDate,
            JournalCode = entity.JournalCode,
            AccountCode = entity.AccountCode,
            AccountLabel = entity.AccountLabel,
            Description = entity.Description,
            Debit = entity.Debit,
            Credit = entity.Credit,
            FiscalYear = entity.FiscalYear,
            Period = entity.Period,
            IsValidated = false,
            PaymentId = entity.PaymentId,
            ChargeAssignmentId = entity.ChargeAssignmentId,
            LeaseId = entity.LeaseId,
            CoOwnershipId = entity.CoOwnershipId
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateAccountingEntryRequest request)
    {
        var entity = await _db.AccountingEntries.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.IsValidated)
            return BadRequest(new { message = "Cannot modify a validated accounting entry." });

        entity.EntryNumber = request.EntryNumber;
        entity.EntryDate = request.EntryDate;
        entity.JournalCode = request.JournalCode;
        entity.AccountCode = request.AccountCode;
        entity.AccountLabel = request.AccountLabel;
        entity.Description = request.Description;
        entity.Debit = request.Debit;
        entity.Credit = request.Credit;
        entity.FiscalYear = request.FiscalYear;
        entity.Period = request.Period;
        entity.PaymentId = request.PaymentId;
        entity.ChargeAssignmentId = request.ChargeAssignmentId;
        entity.LeaseId = request.LeaseId;
        entity.CoOwnershipId = request.CoOwnershipId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/validate")]
    public async Task<IActionResult> Validate(Guid id)
    {
        var entity = await _db.AccountingEntries.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsValidated = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.AccountingEntries.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.IsValidated)
            return BadRequest(new { message = "Cannot delete a validated accounting entry." });

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("balance")]
    public async Task<ActionResult<object>> GetTrialBalance(
        [FromQuery] Guid organizationId,
        [FromQuery] int fiscalYear)
    {
        var entries = await _db.AccountingEntries
            .Where(e => e.OrganizationId == organizationId && e.FiscalYear == fiscalYear)
            .GroupBy(e => new { e.AccountCode, e.AccountLabel })
            .Select(g => new
            {
                g.Key.AccountCode,
                g.Key.AccountLabel,
                TotalDebit = g.Sum(e => e.Debit),
                TotalCredit = g.Sum(e => e.Credit),
                Balance = g.Sum(e => e.Debit) - g.Sum(e => e.Credit)
            })
            .OrderBy(x => x.AccountCode)
            .ToListAsync();

        return Ok(new
        {
            OrganizationId = organizationId,
            FiscalYear = fiscalYear,
            Accounts = entries,
            TotalDebit = entries.Sum(e => e.TotalDebit),
            TotalCredit = entries.Sum(e => e.TotalCredit)
        });
    }

    [HttpGet("journal")]
    public async Task<ActionResult<object>> GetJournal(
        [FromQuery] Guid organizationId,
        [FromQuery] int fiscalYear,
        [FromQuery] string journalCode)
    {
        var entries = await _db.AccountingEntries
            .Where(e => e.OrganizationId == organizationId
                && e.FiscalYear == fiscalYear
                && e.JournalCode == journalCode)
            .OrderBy(e => e.EntryDate)
            .ThenBy(e => e.EntryNumber)
            .Select(e => new AccountingEntryDto
            {
                Id = e.Id,
                OrganizationId = e.OrganizationId,
                EntryNumber = e.EntryNumber,
                EntryDate = e.EntryDate,
                JournalCode = e.JournalCode,
                AccountCode = e.AccountCode,
                AccountLabel = e.AccountLabel,
                Description = e.Description,
                Debit = e.Debit,
                Credit = e.Credit,
                FiscalYear = e.FiscalYear,
                Period = e.Period,
                IsValidated = e.IsValidated
            }).ToListAsync();

        return Ok(new
        {
            OrganizationId = organizationId,
            FiscalYear = fiscalYear,
            JournalCode = journalCode,
            Entries = entries,
            TotalDebit = entries.Sum(e => e.Debit),
            TotalCredit = entries.Sum(e => e.Credit)
        });
    }
}
