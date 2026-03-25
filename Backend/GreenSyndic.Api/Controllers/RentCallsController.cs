using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Services.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RentCallsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public RentCallsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<RentCallDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? leaseId,
        [FromQuery] RentCallStatus? status,
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var query = _db.RentCalls
            .Include(r => r.Lease).ThenInclude(l => l.LeaseTenant)
            .Include(r => r.Lease).ThenInclude(l => l.Unit)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(r => r.OrganizationId == organizationId.Value);
        if (leaseId.HasValue)
            query = query.Where(r => r.LeaseId == leaseId.Value);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        if (year.HasValue)
            query = query.Where(r => r.Year == year.Value);
        if (month.HasValue)
            query = query.Where(r => r.Month == month.Value);

        var items = await query.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
            .Select(r => MapToDto(r)).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RentCallDto>> GetById(Guid id)
    {
        var r = await _db.RentCalls
            .Include(x => x.Lease).ThenInclude(l => l.LeaseTenant)
            .Include(x => x.Lease).ThenInclude(l => l.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();
        return Ok(MapToDto(r));
    }

    [HttpPost]
    public async Task<ActionResult<RentCallDto>> Create([FromBody] CreateRentCallRequest request)
    {
        var lease = await _db.Leases
            .Include(l => l.LeaseTenant)
            .Include(l => l.Unit)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId);

        if (lease == null) return BadRequest("Lease not found.");

        // Check duplicate
        var exists = await _db.RentCalls
            .AnyAsync(r => r.LeaseId == request.LeaseId && r.Year == request.Year && r.Month == request.Month);
        if (exists) return Conflict($"Rent call already exists for {request.Year}-{request.Month:D2}.");

        var periodStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var entity = new RentCall
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            LeaseId = request.LeaseId,
            Reference = $"AL-{request.Year}-{request.Month:D2}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Year = request.Year,
            Month = request.Month,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RentAmount = lease.MonthlyRent,
            ChargesAmount = lease.Charges ?? 0,
            TotalAmount = lease.MonthlyRent + (lease.Charges ?? 0)
                + (request.TurnoverRentAmount ?? 0)
                + (request.MarketingContribution ?? 0),
            PaidAmount = 0,
            RemainingAmount = lease.MonthlyRent + (lease.Charges ?? 0)
                + (request.TurnoverRentAmount ?? 0)
                + (request.MarketingContribution ?? 0),
            Status = RentCallStatus.Draft,
            DueDate = request.DueDate,
            TurnoverRentAmount = request.TurnoverRentAmount,
            MarketingContribution = request.MarketingContribution,
            Notes = request.Notes
        };

        _db.RentCalls.Add(entity);
        await _db.SaveChangesAsync();

        entity.Lease = lease;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Generate rent calls for all active leases of an organization for a given month.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateRentCallsResultDto>> Generate([FromBody] GenerateRentCallsRequest request)
    {
        var query = _db.Leases
            .Include(l => l.LeaseTenant)
            .Include(l => l.Unit)
            .Where(l => l.OrganizationId == request.OrganizationId && l.Status == LeaseStatus.Active);

        if (request.CoOwnershipId.HasValue)
            query = query.Where(l => l.Unit.CoOwnershipId == request.CoOwnershipId.Value);

        var leases = await query.ToListAsync();

        var result = new GenerateRentCallsResultDto();

        foreach (var lease in leases)
        {
            var exists = await _db.RentCalls
                .AnyAsync(r => r.LeaseId == lease.Id && r.Year == request.Year && r.Month == request.Month);

            if (exists)
            {
                result.SkippedCount++;
                result.SkippedReasons.Add($"{lease.Reference}: already exists for {request.Year}-{request.Month:D2}");
                continue;
            }

            var periodStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            var total = lease.MonthlyRent + (lease.Charges ?? 0)
                + (lease.TurnoverRentPercent.HasValue ? lease.MonthlyRent * lease.TurnoverRentPercent.Value / 100m : 0)
                + (lease.MarketingContribution ?? 0);

            var entity = new RentCall
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                LeaseId = lease.Id,
                Reference = $"AL-{request.Year}-{request.Month:D2}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
                Year = request.Year,
                Month = request.Month,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                RentAmount = lease.MonthlyRent,
                ChargesAmount = lease.Charges ?? 0,
                TotalAmount = total,
                PaidAmount = 0,
                RemainingAmount = total,
                Status = RentCallStatus.Draft,
                DueDate = request.DueDate,
                TurnoverRentAmount = lease.TurnoverRentPercent.HasValue
                    ? lease.MonthlyRent * lease.TurnoverRentPercent.Value / 100m : null,
                MarketingContribution = lease.MarketingContribution
            };

            _db.RentCalls.Add(entity);
            entity.Lease = lease;
            result.GeneratedCalls.Add(MapToDto(entity));
            result.GeneratedCount++;
        }

        await _db.SaveChangesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Mark a rent call as sent (envoi au locataire).
    /// </summary>
    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id)
    {
        var entity = await _db.RentCalls.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RentCallStatus.Draft)
            return BadRequest("Only draft rent calls can be sent.");

        entity.Status = RentCallStatus.Sent;
        entity.SentAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Cancel a rent call.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var entity = await _db.RentCalls.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RentCallStatus.Paid)
            return BadRequest("Cannot cancel a paid rent call.");

        entity.Status = RentCallStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Mark overdue rent calls (past due date, not fully paid).
    /// </summary>
    [HttpPost("mark-overdue")]
    public async Task<ActionResult> MarkOverdue([FromQuery] Guid organizationId)
    {
        var now = DateTime.UtcNow;
        var overdue = await _db.RentCalls
            .Where(r => r.OrganizationId == organizationId
                && r.DueDate < now
                && r.Status == RentCallStatus.Sent)
            .ToListAsync();

        foreach (var r in overdue)
        {
            r.Status = RentCallStatus.Overdue;
            r.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { markedCount = overdue.Count });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.RentCalls.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RentCallStatus.Paid)
            return BadRequest("Cannot delete a paid rent call.");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static RentCallDto MapToDto(RentCall r) => new()
    {
        Id = r.Id,
        OrganizationId = r.OrganizationId,
        LeaseId = r.LeaseId,
        LeaseReference = r.Lease?.Reference,
        TenantName = r.Lease?.LeaseTenant != null
            ? r.Lease.LeaseTenant.FirstName + " " + r.Lease.LeaseTenant.LastName : null,
        UnitReference = r.Lease?.Unit?.Reference,
        Reference = r.Reference,
        Year = r.Year,
        Month = r.Month,
        PeriodStart = r.PeriodStart,
        PeriodEnd = r.PeriodEnd,
        RentAmount = r.RentAmount,
        ChargesAmount = r.ChargesAmount,
        TotalAmount = r.TotalAmount,
        PaidAmount = r.PaidAmount,
        RemainingAmount = r.RemainingAmount,
        Status = r.Status,
        DueDate = r.DueDate,
        SentAt = r.SentAt,
        PaidAt = r.PaidAt,
        TurnoverRentAmount = r.TurnoverRentAmount,
        MarketingContribution = r.MarketingContribution,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt
    };
}
