using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CinetPayController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;
    private readonly IConfiguration _config;

    public CinetPayController(GreenSyndicDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Initialize a CinetPay mobile money disbursement (syndic outgoing payment).
    /// Used for: paying suppliers, refunding deposits, paying for works.
    /// Creates a pending payment and returns the CinetPay transfer parameters.
    /// </summary>
    [HttpPost("initialize")]
    [Authorize]
    public async Task<ActionResult> Initialize([FromBody] CinetPayInitRequest request)
    {
        var reference = $"CP-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

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

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Reference = reference,
            Amount = request.Amount,
            Method = PaymentMethod.CinetPay,
            PaymentDate = DateTime.UtcNow,
            Description = request.Description ?? "Décaissement mobile money",
            OwnerId = request.OwnerId,
            LeaseTenantId = request.LeaseTenantId,
            ChargeAssignmentId = request.ChargeAssignmentId,
            TransactionId = reference // will be updated by webhook
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // CinetPay checkout config
        var apiKey = _config["CinetPay:ApiKey"] ?? "DEMO_API_KEY";
        var siteId = _config["CinetPay:SiteId"] ?? "DEMO_SITE_ID";

        return Ok(new
        {
            paymentId = payment.Id,
            reference,
            cinetpay = new
            {
                apikey = apiKey,
                site_id = siteId,
                transaction_id = reference,
                amount = (int)request.Amount,
                currency = "XOF",
                description = payment.Description,
                notify_url = $"{Request.Scheme}://{Request.Host}/api/cinetpay/notify",
                return_url = $"{Request.Scheme}://{Request.Host}/app/payments",
                channels = "ALL",
                lang = "FR",
                metadata = payment.Id.ToString()
            }
        });
    }

    /// <summary>
    /// CinetPay payment notification webhook (IPN).
    /// Called by CinetPay servers after payment processing.
    /// </summary>
    [HttpPost("notify")]
    [AllowAnonymous]
    public async Task<IActionResult> Notify([FromForm] CinetPayNotification notification)
    {
        if (string.IsNullOrEmpty(notification.cpm_trans_id))
            return BadRequest("Missing transaction ID");

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Reference == notification.cpm_trans_id);

        if (payment == null) return NotFound();

        // In production: verify with CinetPay API using cpm_trans_id
        // For now, trust the notification status
        if (notification.cpm_result == "00") // Success
        {
            payment.Status = PaymentStatus.Completed;
            payment.TransactionId = notification.cpm_trans_id;
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
        }

        payment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { status = "OK" });
    }

    /// <summary>
    /// Check the status of a CinetPay payment.
    /// </summary>
    [HttpGet("status/{paymentId:guid}")]
    [Authorize]
    public async Task<ActionResult> CheckStatus(Guid paymentId)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment == null) return NotFound();

        return Ok(new
        {
            paymentId = payment.Id,
            reference = payment.Reference,
            amount = payment.Amount,
            status = payment.Status.ToString(),
            method = payment.Method.ToString(),
            transactionId = payment.TransactionId,
            paymentDate = payment.PaymentDate
        });
    }
}

// DTOs for CinetPay
public record CinetPayInitRequest
{
    public decimal Amount { get; init; }
    public string? Description { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? OwnerId { get; init; }       // For deposit refunds
    public Guid? LeaseTenantId { get; init; }  // For deposit refunds
    public Guid? ChargeAssignmentId { get; init; }
}

public record CinetPayNotification
{
    public string? cpm_trans_id { get; init; }
    public string? cpm_result { get; init; }
    public string? cpm_amount { get; init; }
    public string? cpm_currency { get; init; }
    public string? cpm_site_id { get; init; }
    public string? cpm_trans_date { get; init; }
    public string? cpm_payment_method { get; init; }
    public string? cpm_phone_prefixe { get; init; }
    public string? cpm_cel_phone_num { get; init; }
}
