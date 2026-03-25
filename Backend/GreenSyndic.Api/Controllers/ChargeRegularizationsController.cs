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
public class ChargeRegularizationsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public ChargeRegularizationsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChargeRegularizationDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? leaseId,
        [FromQuery] RegularizationStatus? status,
        [FromQuery] RegularizationType? type)
    {
        var query = _db.ChargeRegularizations
            .Include(r => r.Lease).ThenInclude(l => l.LeaseTenant)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(r => r.OrganizationId == organizationId.Value);
        if (leaseId.HasValue)
            query = query.Where(r => r.LeaseId == leaseId.Value);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        var items = await query.OrderByDescending(r => r.PeriodEnd)
            .Select(r => MapToDto(r)).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChargeRegularizationDto>> GetById(Guid id)
    {
        var r = await _db.ChargeRegularizations
            .Include(x => x.Lease).ThenInclude(l => l.LeaseTenant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();
        return Ok(MapToDto(r));
    }

    [HttpPost]
    public async Task<ActionResult<ChargeRegularizationDto>> Create([FromBody] CreateChargeRegularizationRequest request)
    {
        var lease = await _db.Leases
            .Include(l => l.LeaseTenant)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId);

        if (lease == null) return BadRequest("Lease not found.");

        var balance = request.TotalProvisioned - request.TotalActual;

        var entity = new ChargeRegularization
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            LeaseId = request.LeaseId,
            Reference = $"REG-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Type = request.Type,
            Status = RegularizationStatus.Calculated,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            TotalProvisioned = request.TotalProvisioned,
            TotalActual = request.TotalActual,
            Balance = balance,
            BreakdownJson = request.BreakdownJson,
            Notes = request.Notes
        };

        _db.ChargeRegularizations.Add(entity);
        await _db.SaveChangesAsync();

        entity.Lease = lease;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Auto-calculate regularization from rent calls for a lease over a period.
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<ChargeRegularizationDto>> Calculate(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid leaseId,
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd)
    {
        var lease = await _db.Leases
            .Include(l => l.LeaseTenant)
            .FirstOrDefaultAsync(l => l.Id == leaseId);

        if (lease == null) return BadRequest("Lease not found.");

        // Sum of charge provisions from rent calls in the period
        var totalProvisioned = await _db.RentCalls
            .Where(r => r.LeaseId == leaseId
                && r.PeriodStart >= periodStart
                && r.PeriodEnd <= periodEnd
                && r.Status != RentCallStatus.Cancelled)
            .SumAsync(r => r.ChargesAmount);

        // Sum of actual charges (from charge assignments linked to the unit)
        var unitId = lease.UnitId;
        var totalActual = await _db.ChargeAssignments
            .Where(ca => ca.UnitId == unitId
                && ca.CreatedAt >= periodStart
                && ca.CreatedAt <= periodEnd)
            .SumAsync(ca => ca.Amount);

        var balance = totalProvisioned - totalActual;

        var entity = new ChargeRegularization
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LeaseId = leaseId,
            Reference = $"REG-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Type = RegularizationType.Annual,
            Status = RegularizationStatus.Calculated,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalProvisioned = totalProvisioned,
            TotalActual = totalActual,
            Balance = balance
        };

        _db.ChargeRegularizations.Add(entity);
        await _db.SaveChangesAsync();

        entity.Lease = lease;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Notify tenant of regularization.
    /// </summary>
    [HttpPost("{id:guid}/notify")]
    public async Task<IActionResult> Notify(Guid id)
    {
        var entity = await _db.ChargeRegularizations.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RegularizationStatus.Calculated)
            return BadRequest("Only calculated regularizations can be notified.");

        entity.Status = RegularizationStatus.Notified;
        entity.NotifiedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Tenant responds (accept or contest).
    /// </summary>
    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondRegularizationRequest request)
    {
        var entity = await _db.ChargeRegularizations.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RegularizationStatus.Notified)
            return BadRequest("Only notified regularizations can be responded to.");

        if (request.Accepted)
        {
            entity.Status = RegularizationStatus.Accepted;
            entity.AcceptedAt = DateTime.UtcNow;
        }
        else
        {
            entity.Status = RegularizationStatus.Contested;
            entity.ContestedAt = DateTime.UtcNow;
            entity.ContestationReason = request.ContestationReason;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Mark regularization as settled (paid or credited).
    /// </summary>
    [HttpPost("{id:guid}/settle")]
    public async Task<IActionResult> Settle(Guid id, [FromQuery] Guid? paymentId)
    {
        var entity = await _db.ChargeRegularizations.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RegularizationStatus.Accepted)
            return BadRequest("Only accepted regularizations can be settled.");

        entity.Status = RegularizationStatus.Settled;
        entity.SettledAt = DateTime.UtcNow;
        entity.PaymentId = paymentId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var entity = await _db.ChargeRegularizations.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RegularizationStatus.Settled)
            return BadRequest("Cannot cancel a settled regularization.");

        entity.Status = RegularizationStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.ChargeRegularizations.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RegularizationStatus.Settled)
            return BadRequest("Cannot delete a settled regularization.");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ChargeRegularizationDto MapToDto(ChargeRegularization r) => new()
    {
        Id = r.Id,
        OrganizationId = r.OrganizationId,
        LeaseId = r.LeaseId,
        LeaseReference = r.Lease?.Reference,
        TenantName = r.Lease?.LeaseTenant != null
            ? r.Lease.LeaseTenant.FirstName + " " + r.Lease.LeaseTenant.LastName : null,
        Reference = r.Reference,
        Type = r.Type,
        Status = r.Status,
        PeriodStart = r.PeriodStart,
        PeriodEnd = r.PeriodEnd,
        TotalProvisioned = r.TotalProvisioned,
        TotalActual = r.TotalActual,
        Balance = r.Balance,
        BreakdownJson = r.BreakdownJson,
        NotifiedAt = r.NotifiedAt,
        AcceptedAt = r.AcceptedAt,
        ContestedAt = r.ContestedAt,
        ContestationReason = r.ContestationReason,
        SettledAt = r.SettledAt,
        PaymentId = r.PaymentId,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt
    };
}
