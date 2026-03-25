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
public class PaymentsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public PaymentsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetAll(
        [FromQuery] Guid? ownerId,
        [FromQuery] Guid? leaseTenantId,
        [FromQuery] Guid? leaseId,
        [FromQuery] PaymentStatus? status,
        [FromQuery] PaymentMethod? method)
    {
        var query = _db.Payments
            .Include(p => p.Owner)
            .Include(p => p.LeaseTenant)
            .AsQueryable();

        if (ownerId.HasValue)
            query = query.Where(p => p.OwnerId == ownerId.Value);

        if (leaseTenantId.HasValue)
            query = query.Where(p => p.LeaseTenantId == leaseTenantId.Value);

        if (leaseId.HasValue)
            query = query.Where(p => p.LeaseId == leaseId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (method.HasValue)
            query = query.Where(p => p.Method == method.Value);

        var items = await query.Select(p => new PaymentDto
        {
            Id = p.Id,
            Reference = p.Reference,
            Amount = p.Amount,
            Currency = p.Currency,
            Method = p.Method,
            Status = p.Status,
            PaymentDate = p.PaymentDate,
            TransactionId = p.TransactionId,
            Description = p.Description,
            OwnerId = p.OwnerId,
            OwnerName = p.Owner != null ? p.Owner.FirstName + " " + p.Owner.LastName : null,
            LeaseTenantId = p.LeaseTenantId,
            TenantName = p.LeaseTenant != null ? p.LeaseTenant.FirstName + " " + p.LeaseTenant.LastName : null,
            LeaseId = p.LeaseId,
            ChargeAssignmentId = p.ChargeAssignmentId
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id)
    {
        var p = await _db.Payments
            .Include(x => x.Owner)
            .Include(x => x.LeaseTenant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound();

        return Ok(new PaymentDto
        {
            Id = p.Id,
            Reference = p.Reference,
            Amount = p.Amount,
            Currency = p.Currency,
            Method = p.Method,
            Status = p.Status,
            PaymentDate = p.PaymentDate,
            TransactionId = p.TransactionId,
            Description = p.Description,
            OwnerId = p.OwnerId,
            OwnerName = p.Owner != null ? p.Owner.FirstName + " " + p.Owner.LastName : null,
            LeaseTenantId = p.LeaseTenantId,
            TenantName = p.LeaseTenant != null ? p.LeaseTenant.FirstName + " " + p.LeaseTenant.LastName : null,
            LeaseId = p.LeaseId,
            ChargeAssignmentId = p.ChargeAssignmentId
        });
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentRequest request)
    {
        var reference = $"PAY-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}";

        Guid organizationId = Guid.Empty;

        if (request.OwnerId.HasValue)
        {
            var owner = await _db.Owners.FindAsync(request.OwnerId.Value);
            if (owner != null) organizationId = owner.OrganizationId;
        }
        else if (request.LeaseTenantId.HasValue)
        {
            var tenant = await _db.LeaseTenants.FindAsync(request.LeaseTenantId.Value);
            if (tenant != null) organizationId = tenant.OrganizationId;
        }

        var entity = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Reference = reference,
            Amount = request.Amount,
            Method = request.Method,
            PaymentDate = request.PaymentDate,
            TransactionId = request.TransactionId,
            Description = request.Description,
            OwnerId = request.OwnerId,
            LeaseTenantId = request.LeaseTenantId,
            LeaseId = request.LeaseId,
            ChargeAssignmentId = request.ChargeAssignmentId
        };

        _db.Payments.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new PaymentDto
        {
            Id = entity.Id,
            Reference = entity.Reference,
            Amount = entity.Amount,
            Method = entity.Method,
            Status = entity.Status,
            PaymentDate = entity.PaymentDate
        });
    }

    [HttpPut("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var entity = await _db.Payments.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = PaymentStatus.Completed;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Reconcile a payment with a rent call.
    /// Updates the rent call's PaidAmount, RemainingAmount, and Status accordingly.
    /// Auto-generates a receipt if fully paid.
    /// </summary>
    [HttpPost("{id:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(Guid id, [FromBody] ReconcilePaymentRequest request)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment == null) return NotFound();

        if (payment.Status != PaymentStatus.Completed)
            return BadRequest("Only completed payments can be reconciled.");

        var rentCall = await _db.RentCalls.FindAsync(request.RentCallId);
        if (rentCall == null) return BadRequest("Rent call not found.");

        if (rentCall.Status == RentCallStatus.Cancelled)
            return BadRequest("Cannot reconcile with a cancelled rent call.");

        // Link payment to lease
        payment.LeaseId = rentCall.LeaseId;
        payment.UpdatedAt = DateTime.UtcNow;

        // Update rent call amounts
        rentCall.PaidAmount += payment.Amount;
        rentCall.RemainingAmount = rentCall.TotalAmount - rentCall.PaidAmount;

        if (rentCall.RemainingAmount <= 0)
        {
            rentCall.RemainingAmount = 0;
            rentCall.Status = RentCallStatus.Paid;
            rentCall.PaidAt = DateTime.UtcNow;

            // Auto-generate receipt
            var receipt = new RentReceipt
            {
                Id = Guid.NewGuid(),
                OrganizationId = rentCall.OrganizationId,
                RentCallId = rentCall.Id,
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
                PaymentId = payment.Id,
                Notes = "Quittance générée automatiquement après réconciliation"
            };
            _db.RentReceipts.Add(receipt);
        }
        else if (rentCall.PaidAmount > 0)
        {
            rentCall.Status = RentCallStatus.PartiallyPaid;
        }

        rentCall.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            rentCallId = rentCall.Id,
            paidAmount = rentCall.PaidAmount,
            remainingAmount = rentCall.RemainingAmount,
            status = rentCall.Status.ToString(),
            receiptGenerated = rentCall.Status == RentCallStatus.Paid
        });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var entity = await _db.Payments.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = PaymentStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Payments.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
