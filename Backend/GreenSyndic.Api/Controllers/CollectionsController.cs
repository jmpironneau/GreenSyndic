using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

/// <summary>
/// Encaissements — le syndic enregistre les paiements reçus
/// (virement bancaire, espèces, chèque, mobile money)
/// et les rapproche avec les appels de fonds / appels de loyer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public CollectionsController(GreenSyndicDbContext db) => _db = db;

    /// <summary>
    /// Get all pending items awaiting payment (rent calls + charge assignments).
    /// The syndic uses this to see what's outstanding before recording a collection.
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult> GetPendingItems([FromQuery] Guid? organizationId)
    {
        // Pending rent calls
        var rentCallsQuery = _db.RentCalls
            .Include(r => r.Lease).ThenInclude(l => l.LeaseTenant)
            .Where(r => r.Status != RentCallStatus.Paid && r.Status != RentCallStatus.Cancelled);

        if (organizationId.HasValue)
            rentCallsQuery = rentCallsQuery.Where(r => r.OrganizationId == organizationId.Value);

        var rentCalls = await rentCallsQuery
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
            .Select(r => new
            {
                type = "RentCall",
                id = r.Id,
                reference = r.Reference,
                label = "Loyer " + r.Month.ToString("D2") + "/" + r.Year,
                debtor = r.Lease != null && r.Lease.LeaseTenant != null
                    ? r.Lease.LeaseTenant.FirstName + " " + r.Lease.LeaseTenant.LastName
                    : null,
                totalAmount = r.TotalAmount,
                paidAmount = r.PaidAmount,
                remainingAmount = r.RemainingAmount,
                dueDate = r.DueDate,
                status = r.Status.ToString(),
                isOverdue = r.Status == RentCallStatus.Overdue
            })
            .ToListAsync();

        // Pending charge assignments (unpaid)
        var chargesQuery = _db.ChargeAssignments
            .Include(ca => ca.Unit)
            .Include(ca => ca.ChargeDefinition)
            .Where(ca => !ca.IsPaid);

        if (organizationId.HasValue)
            chargesQuery = chargesQuery.Where(ca => ca.Unit.OrganizationId == organizationId.Value);

        var charges = await chargesQuery
            .OrderByDescending(ca => ca.CreatedAt)
            .Select(ca => new
            {
                type = "ChargeAssignment",
                id = ca.Id,
                reference = "AF-" + ca.Year + "-Q" + ca.Quarter,
                label = ca.ChargeDefinition != null ? ca.ChargeDefinition.Name : "Appel de fonds",
                debtor = ca.Unit != null ? ca.Unit.Reference : null,
                totalAmount = ca.Amount,
                paidAmount = ca.PaidAmount,
                remainingAmount = ca.Amount - ca.PaidAmount,
                dueDate = ca.DueDate,
                status = ca.IsPaid ? "Paid" : "Pending",
                isOverdue = ca.DueDate < DateTime.UtcNow && !ca.IsPaid
            })
            .ToListAsync();

        // Combine and sort by urgency (overdue first, then by due date)
        var all = rentCalls.Cast<object>().Concat(charges.Cast<object>()).ToList();

        return Ok(new
        {
            totalPending = rentCalls.Count + charges.Count,
            totalOverdue = rentCalls.Count(r => r.isOverdue) + charges.Count(c => c.isOverdue),
            rentCalls,
            charges,
            all
        });
    }

    /// <summary>
    /// Record a collection (encaissement) — the syndic received money.
    /// Supports: bank transfer, cash, check, mobile money.
    /// Optionally links to a rent call or charge assignment for auto-reconciliation.
    /// </summary>
    [HttpPost("record")]
    public async Task<ActionResult> RecordCollection([FromBody] RecordCollectionRequest request)
    {
        var reference = $"ENC-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        Guid organizationId = Guid.Empty;

        // Determine organization from the linked item
        if (request.RentCallId.HasValue)
        {
            var rc = await _db.RentCalls.FindAsync(request.RentCallId.Value);
            if (rc != null) organizationId = rc.OrganizationId;
        }
        else if (request.ChargeAssignmentId.HasValue)
        {
            var ca = await _db.ChargeAssignments.Include(x => x.Unit).FirstOrDefaultAsync(x => x.Id == request.ChargeAssignmentId.Value);
            if (ca?.Unit != null)
                organizationId = ca.Unit.OrganizationId;
        }
        else if (request.OwnerId.HasValue)
        {
            var owner = await _db.Owners.FindAsync(request.OwnerId.Value);
            if (owner != null) organizationId = owner.OrganizationId;
        }

        // Create the payment
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Reference = reference,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Completed, // Manual collection = already received
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            TransactionId = request.BankReference,
            Description = request.Description ?? $"Encaissement {request.Method}",
            OwnerId = request.OwnerId,
            LeaseTenantId = request.LeaseTenantId,
            ChargeAssignmentId = request.ChargeAssignmentId
        };

        _db.Payments.Add(payment);

        // Auto-reconciliation with rent call
        RentReceipt? receipt = null;
        if (request.RentCallId.HasValue)
        {
            var rentCall = await _db.RentCalls.FindAsync(request.RentCallId.Value);
            if (rentCall != null && rentCall.Status != RentCallStatus.Cancelled)
            {
                payment.LeaseId = rentCall.LeaseId;
                rentCall.PaidAmount += request.Amount;
                rentCall.RemainingAmount = rentCall.TotalAmount - rentCall.PaidAmount;

                if (rentCall.RemainingAmount <= 0)
                {
                    rentCall.RemainingAmount = 0;
                    rentCall.Status = RentCallStatus.Paid;
                    rentCall.PaidAt = DateTime.UtcNow;

                    // Auto-generate receipt
                    receipt = new RentReceipt
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
                        Notes = $"Quittance auto — {request.Method} ref {request.BankReference ?? reference}"
                    };
                    _db.RentReceipts.Add(receipt);
                }
                else if (rentCall.PaidAmount > 0)
                {
                    rentCall.Status = RentCallStatus.PartiallyPaid;
                }
                rentCall.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Auto-reconciliation with charge assignment
        if (request.ChargeAssignmentId.HasValue)
        {
            var ca = await _db.ChargeAssignments.FindAsync(request.ChargeAssignmentId.Value);
            if (ca != null)
            {
                ca.PaidAmount += request.Amount;
                if (ca.PaidAmount >= ca.Amount)
                    ca.IsPaid = true;
                ca.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            paymentId = payment.Id,
            reference,
            amount = payment.Amount,
            method = payment.Method.ToString(),
            status = payment.Status.ToString(),
            reconciled = request.RentCallId.HasValue || request.ChargeAssignmentId.HasValue,
            receiptGenerated = receipt != null,
            receiptReference = receipt?.Reference
        });
    }

    /// <summary>
    /// Get collection summary for a period (daily/monthly view).
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult> GetSummary(
        [FromQuery] Guid? organizationId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromDate = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = to ?? DateTime.UtcNow;

        var query = _db.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate);

        if (organizationId.HasValue)
            query = query.Where(p => p.OrganizationId == organizationId.Value);

        var payments = await query.ToListAsync();

        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new { method = g.Key.ToString(), count = g.Count(), total = g.Sum(p => p.Amount) })
            .OrderByDescending(x => x.total)
            .ToList();

        return Ok(new
        {
            period = new { from = fromDate, to = toDate },
            totalCollected = payments.Sum(p => p.Amount),
            totalCount = payments.Count,
            byMethod
        });
    }
}

public record RecordCollectionRequest
{
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public string? Description { get; init; }
    public string? BankReference { get; init; }    // Ref virement / n° chèque / n° transaction mobile
    public string? PayerName { get; init; }        // Nom du payeur (libre)
    public DateTime? PaymentDate { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? LeaseTenantId { get; init; }
    public Guid? RentCallId { get; init; }         // Auto-reconciliation loyer
    public Guid? ChargeAssignmentId { get; init; } // Auto-reconciliation charges
}
