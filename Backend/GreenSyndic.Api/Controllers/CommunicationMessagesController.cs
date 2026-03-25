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
public class CommunicationMessagesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public CommunicationMessagesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommunicationMessageDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] MessageChannel? channel,
        [FromQuery] MessageStatus? status,
        [FromQuery] string? recipientUserId)
    {
        var query = _db.CommunicationMessages
            .Include(m => m.DeliveryLogs)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(m => m.OrganizationId == organizationId.Value);

        if (channel.HasValue)
            query = query.Where(m => m.Channel == channel.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (!string.IsNullOrEmpty(recipientUserId))
            query = query.Where(m => m.RecipientUserId == recipientUserId);

        var items = await query.OrderByDescending(m => m.CreatedAt)
            .Take(100) // Limit for performance
            .Select(m => new CommunicationMessageDto
            {
                Id = m.Id,
                OrganizationId = m.OrganizationId,
                Channel = m.Channel,
                Status = m.Status,
                RecipientUserId = m.RecipientUserId,
                RecipientEmail = m.RecipientEmail,
                RecipientPhone = m.RecipientPhone,
                RecipientName = m.RecipientName,
                Subject = m.Subject,
                Body = m.Body,
                TemplateId = m.TemplateId,
                BroadcastId = m.BroadcastId,
                ScheduledAt = m.ScheduledAt,
                SentAt = m.SentAt,
                ErrorMessage = m.ErrorMessage,
                DeliveryLogCount = m.DeliveryLogs.Count
            }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommunicationMessageDto>> GetById(Guid id)
    {
        var m = await _db.CommunicationMessages
            .Include(x => x.DeliveryLogs)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();

        return Ok(new CommunicationMessageDto
        {
            Id = m.Id,
            OrganizationId = m.OrganizationId,
            Channel = m.Channel,
            Status = m.Status,
            RecipientUserId = m.RecipientUserId,
            RecipientEmail = m.RecipientEmail,
            RecipientPhone = m.RecipientPhone,
            RecipientName = m.RecipientName,
            Subject = m.Subject,
            Body = m.Body,
            TemplateId = m.TemplateId,
            BroadcastId = m.BroadcastId,
            ScheduledAt = m.ScheduledAt,
            SentAt = m.SentAt,
            ErrorMessage = m.ErrorMessage,
            DeliveryLogCount = m.DeliveryLogs.Count
        });
    }

    /// <summary>
    /// Create and optionally send a message.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CommunicationMessageDto>> Create(
        [FromBody] CreateMessageRequest request)
    {
        var entity = new CommunicationMessage
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Channel = request.Channel,
            RecipientUserId = request.RecipientUserId,
            RecipientEmail = request.RecipientEmail,
            RecipientPhone = request.RecipientPhone,
            RecipientName = request.RecipientName,
            RecipientAddress = request.RecipientAddress,
            Subject = request.Subject,
            Body = request.Body,
            TemplateId = request.TemplateId,
            ScheduledAt = request.ScheduledAt,
            Status = request.ScheduledAt.HasValue ? MessageStatus.Scheduled : MessageStatus.Draft
        };

        _db.CommunicationMessages.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new CommunicationMessageDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Channel = entity.Channel,
            Status = entity.Status,
            RecipientUserId = entity.RecipientUserId,
            RecipientEmail = entity.RecipientEmail,
            RecipientPhone = entity.RecipientPhone,
            RecipientName = entity.RecipientName,
            Subject = entity.Subject,
            Body = entity.Body,
            TemplateId = entity.TemplateId,
            ScheduledAt = entity.ScheduledAt,
            DeliveryLogCount = 0
        });
    }

    /// <summary>
    /// Send a draft or scheduled message immediately.
    /// In production, this would dispatch to the actual email/SMS provider.
    /// </summary>
    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id)
    {
        var entity = await _db.CommunicationMessages.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != MessageStatus.Draft && entity.Status != MessageStatus.Scheduled)
            return BadRequest($"Cannot send message in {entity.Status} status");

        entity.Status = MessageStatus.Sent;
        entity.SentAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        // Log the delivery
        _db.MessageDeliveryLogs.Add(new MessageDeliveryLog
        {
            Id = Guid.NewGuid(),
            MessageId = entity.Id,
            Status = DeliveryStatus.Sent,
            OccurredAt = DateTime.UtcNow,
            Details = $"Message sent via {entity.Channel}"
        });

        await _db.SaveChangesAsync();
        return Ok(new { entity.Id, entity.Status, entity.SentAt });
    }

    /// <summary>
    /// Cancel a scheduled or draft message.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var entity = await _db.CommunicationMessages.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != MessageStatus.Draft && entity.Status != MessageStatus.Scheduled)
            return BadRequest($"Cannot cancel message in {entity.Status} status");

        entity.Status = MessageStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get delivery logs for a message.
    /// </summary>
    [HttpGet("{id:guid}/delivery-logs")]
    public async Task<ActionResult<List<MessageDeliveryLogDto>>> GetDeliveryLogs(Guid id)
    {
        var logs = await _db.MessageDeliveryLogs
            .Where(l => l.MessageId == id)
            .OrderByDescending(l => l.OccurredAt)
            .Select(l => new MessageDeliveryLogDto
            {
                Id = l.Id,
                MessageId = l.MessageId,
                Status = l.Status,
                OccurredAt = l.OccurredAt,
                Details = l.Details
            }).ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// Webhook endpoint for delivery status updates from external providers.
    /// </summary>
    [HttpPost("{id:guid}/delivery-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> DeliveryWebhook(Guid id,
        [FromBody] DeliveryWebhookPayload payload)
    {
        var entity = await _db.CommunicationMessages.FindAsync(id);
        if (entity == null) return NotFound();

        _db.MessageDeliveryLogs.Add(new MessageDeliveryLog
        {
            Id = Guid.NewGuid(),
            MessageId = id,
            Status = payload.Status,
            OccurredAt = DateTime.UtcNow,
            Details = payload.Details,
            ExternalEventId = payload.ExternalEventId
        });

        // Update message status based on delivery
        if (payload.Status == DeliveryStatus.Bounced || payload.Status == DeliveryStatus.Failed)
        {
            entity.Status = MessageStatus.Failed;
            entity.ErrorMessage = payload.Details;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.CommunicationMessages.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == MessageStatus.Sent)
            return BadRequest("Cannot delete a sent message");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class DeliveryWebhookPayload
{
    public DeliveryStatus Status { get; set; }
    public string? Details { get; set; }
    public string? ExternalEventId { get; set; }
}
