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
public class RentReceiptsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public RentReceiptsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<RentReceiptDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? leaseId,
        [FromQuery] RentReceiptStatus? status,
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var query = _db.RentReceipts
            .Include(r => r.Lease).ThenInclude(l => l.LeaseTenant)
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
    public async Task<ActionResult<RentReceiptDto>> GetById(Guid id)
    {
        var r = await _db.RentReceipts
            .Include(x => x.Lease).ThenInclude(l => l.LeaseTenant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();
        return Ok(MapToDto(r));
    }

    [HttpPost]
    public async Task<ActionResult<RentReceiptDto>> Create([FromBody] CreateRentReceiptRequest request)
    {
        var rentCall = await _db.RentCalls
            .Include(r => r.Lease).ThenInclude(l => l.LeaseTenant)
            .FirstOrDefaultAsync(r => r.Id == request.RentCallId);

        if (rentCall == null) return BadRequest("Rent call not found.");

        var entity = new RentReceipt
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            RentCallId = request.RentCallId,
            LeaseId = rentCall.LeaseId,
            Reference = $"QT-{rentCall.Year}-{rentCall.Month:D2}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Year = rentCall.Year,
            Month = rentCall.Month,
            PeriodStart = rentCall.PeriodStart,
            PeriodEnd = rentCall.PeriodEnd,
            RentAmount = request.RentAmount,
            ChargesAmount = request.ChargesAmount,
            TotalAmount = request.RentAmount + request.ChargesAmount,
            Status = RentReceiptStatus.Draft,
            PaymentId = request.PaymentId,
            Notes = request.Notes
        };

        _db.RentReceipts.Add(entity);
        await _db.SaveChangesAsync();

        entity.Lease = rentCall.Lease;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Auto-generate receipt after full payment of a rent call.
    /// </summary>
    [HttpPost("auto-generate/{rentCallId:guid}")]
    public async Task<ActionResult<RentReceiptDto>> AutoGenerate(Guid rentCallId, [FromQuery] Guid? paymentId)
    {
        var rentCall = await _db.RentCalls
            .Include(r => r.Lease).ThenInclude(l => l.LeaseTenant)
            .FirstOrDefaultAsync(r => r.Id == rentCallId);

        if (rentCall == null) return BadRequest("Rent call not found.");

        if (rentCall.Status != RentCallStatus.Paid)
            return BadRequest("Receipt can only be auto-generated for paid rent calls.");

        // Check if receipt already exists
        var existingReceipt = await _db.RentReceipts
            .AnyAsync(r => r.RentCallId == rentCallId);
        if (existingReceipt) return Conflict("Receipt already exists for this rent call.");

        var entity = new RentReceipt
        {
            Id = Guid.NewGuid(),
            OrganizationId = rentCall.OrganizationId,
            RentCallId = rentCallId,
            LeaseId = rentCall.LeaseId,
            Reference = $"QT-{rentCall.Year}-{rentCall.Month:D2}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Year = rentCall.Year,
            Month = rentCall.Month,
            PeriodStart = rentCall.PeriodStart,
            PeriodEnd = rentCall.PeriodEnd,
            RentAmount = rentCall.RentAmount,
            ChargesAmount = rentCall.ChargesAmount,
            TotalAmount = rentCall.TotalAmount,
            Status = RentReceiptStatus.Issued,
            IssuedAt = DateTime.UtcNow,
            PaymentId = paymentId,
            Notes = "Quittance générée automatiquement"
        };

        _db.RentReceipts.Add(entity);
        await _db.SaveChangesAsync();

        entity.Lease = rentCall.Lease;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Issue a draft receipt.
    /// </summary>
    [HttpPost("{id:guid}/issue")]
    public async Task<IActionResult> Issue(Guid id)
    {
        var entity = await _db.RentReceipts.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RentReceiptStatus.Draft)
            return BadRequest("Only draft receipts can be issued.");

        entity.Status = RentReceiptStatus.Issued;
        entity.IssuedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Mark receipt as sent (envoi dématérialisé).
    /// </summary>
    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id)
    {
        var entity = await _db.RentReceipts.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RentReceiptStatus.Issued)
            return BadRequest("Only issued receipts can be sent.");

        entity.Status = RentReceiptStatus.Sent;
        entity.SentAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var entity = await _db.RentReceipts.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RentReceiptStatus.Sent)
            return BadRequest("Cannot cancel a sent receipt.");

        entity.Status = RentReceiptStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.RentReceipts.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RentReceiptStatus.Sent)
            return BadRequest("Cannot delete a sent receipt.");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static RentReceiptDto MapToDto(RentReceipt r) => new()
    {
        Id = r.Id,
        OrganizationId = r.OrganizationId,
        RentCallId = r.RentCallId,
        LeaseId = r.LeaseId,
        LeaseReference = r.Lease?.Reference,
        TenantName = r.Lease?.LeaseTenant != null
            ? r.Lease.LeaseTenant.FirstName + " " + r.Lease.LeaseTenant.LastName : null,
        Reference = r.Reference,
        Year = r.Year,
        Month = r.Month,
        PeriodStart = r.PeriodStart,
        PeriodEnd = r.PeriodEnd,
        RentAmount = r.RentAmount,
        ChargesAmount = r.ChargesAmount,
        TotalAmount = r.TotalAmount,
        Status = r.Status,
        IssuedAt = r.IssuedAt,
        SentAt = r.SentAt,
        PaymentId = r.PaymentId,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt
    };
}
