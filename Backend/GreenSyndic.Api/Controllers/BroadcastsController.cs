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
public class BroadcastsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public BroadcastsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<BroadcastDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] BroadcastStatus? status,
        [FromQuery] MessageChannel? channel)
    {
        var query = _db.Broadcasts.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(b => b.OrganizationId == organizationId.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        if (channel.HasValue)
            query = query.Where(b => b.Channel == channel.Value);

        var items = await query.OrderByDescending(b => b.CreatedAt)
            .Select(b => new BroadcastDto
            {
                Id = b.Id,
                OrganizationId = b.OrganizationId,
                Name = b.Name,
                Channel = b.Channel,
                Status = b.Status,
                TemplateId = b.TemplateId,
                Subject = b.Subject,
                CoOwnershipId = b.CoOwnershipId,
                TargetRole = b.TargetRole,
                ScheduledAt = b.ScheduledAt,
                StartedAt = b.StartedAt,
                CompletedAt = b.CompletedAt,
                TotalRecipients = b.TotalRecipients,
                SentCount = b.SentCount,
                FailedCount = b.FailedCount
            }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BroadcastDto>> GetById(Guid id)
    {
        var b = await _db.Broadcasts.FindAsync(id);
        if (b == null) return NotFound();

        return Ok(new BroadcastDto
        {
            Id = b.Id,
            OrganizationId = b.OrganizationId,
            Name = b.Name,
            Channel = b.Channel,
            Status = b.Status,
            TemplateId = b.TemplateId,
            Subject = b.Subject,
            CoOwnershipId = b.CoOwnershipId,
            TargetRole = b.TargetRole,
            ScheduledAt = b.ScheduledAt,
            StartedAt = b.StartedAt,
            CompletedAt = b.CompletedAt,
            TotalRecipients = b.TotalRecipients,
            SentCount = b.SentCount,
            FailedCount = b.FailedCount
        });
    }

    [HttpPost]
    public async Task<ActionResult<BroadcastDto>> Create([FromBody] CreateBroadcastRequest request)
    {
        var entity = new Broadcast
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Channel = request.Channel,
            TemplateId = request.TemplateId,
            Subject = request.Subject,
            Body = request.Body,
            CoOwnershipId = request.CoOwnershipId,
            TargetRole = request.TargetRole,
            ScheduledAt = request.ScheduledAt,
            Status = request.ScheduledAt.HasValue ? BroadcastStatus.Scheduled : BroadcastStatus.Draft
        };

        _db.Broadcasts.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new BroadcastDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Name = entity.Name,
            Channel = entity.Channel,
            Status = entity.Status,
            TemplateId = entity.TemplateId,
            Subject = entity.Subject,
            CoOwnershipId = entity.CoOwnershipId,
            TargetRole = entity.TargetRole,
            ScheduledAt = entity.ScheduledAt,
            TotalRecipients = 0,
            SentCount = 0,
            FailedCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateBroadcastRequest request)
    {
        var entity = await _db.Broadcasts.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != BroadcastStatus.Draft && entity.Status != BroadcastStatus.Scheduled)
            return BadRequest("Cannot modify a broadcast that has already started sending");

        entity.Name = request.Name;
        entity.Channel = request.Channel;
        entity.TemplateId = request.TemplateId;
        entity.Subject = request.Subject;
        entity.Body = request.Body;
        entity.CoOwnershipId = request.CoOwnershipId;
        entity.TargetRole = request.TargetRole;
        entity.ScheduledAt = request.ScheduledAt;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Get recipients for a broadcast.
    /// </summary>
    [HttpGet("{id:guid}/recipients")]
    public async Task<ActionResult<List<BroadcastRecipientDto>>> GetRecipients(Guid id)
    {
        var items = await _db.BroadcastRecipients
            .Where(r => r.BroadcastId == id)
            .Include(r => r.Message)
            .Select(r => new BroadcastRecipientDto
            {
                Id = r.Id,
                BroadcastId = r.BroadcastId,
                UserId = r.UserId,
                Name = r.Name,
                Email = r.Email,
                Phone = r.Phone,
                MessageId = r.MessageId,
                MessageStatus = r.Message != null ? r.Message.Status : null
            }).ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Add a recipient manually to a broadcast.
    /// </summary>
    [HttpPost("{id:guid}/recipients")]
    public async Task<ActionResult<BroadcastRecipientDto>> AddRecipient(Guid id,
        [FromBody] AddBroadcastRecipientRequest request)
    {
        var broadcast = await _db.Broadcasts.FindAsync(id);
        if (broadcast == null) return NotFound("Broadcast not found");

        var entity = new BroadcastRecipient
        {
            Id = Guid.NewGuid(),
            BroadcastId = id,
            UserId = request.UserId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            MergeData = request.MergeData
        };

        _db.BroadcastRecipients.Add(entity);
        broadcast.TotalRecipients++;
        await _db.SaveChangesAsync();

        return Ok(new BroadcastRecipientDto
        {
            Id = entity.Id,
            BroadcastId = entity.BroadcastId,
            UserId = entity.UserId,
            Name = entity.Name,
            Email = entity.Email,
            Phone = entity.Phone
        });
    }

    /// <summary>
    /// Auto-populate recipients from co-ownership owners / target role.
    /// </summary>
    [HttpPost("{id:guid}/populate-recipients")]
    public async Task<ActionResult<int>> PopulateRecipients(Guid id)
    {
        var broadcast = await _db.Broadcasts
            .Include(b => b.Recipients)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (broadcast == null) return NotFound();

        if (broadcast.Status != BroadcastStatus.Draft)
            return BadRequest("Can only populate recipients for draft broadcasts");

        var existingUserIds = broadcast.Recipients
            .Where(r => r.UserId != null)
            .Select(r => r.UserId)
            .ToHashSet();

        // Get owners based on filters
        var ownersQuery = _db.Owners.AsQueryable();

        if (broadcast.CoOwnershipId.HasValue)
        {
            var ownerIds = await _db.Units
                .Where(u => u.Building.CoOwnershipId == broadcast.CoOwnershipId.Value && u.OwnerId != null)
                .Select(u => u.OwnerId!.Value)
                .Distinct()
                .ToListAsync();
            ownersQuery = ownersQuery.Where(o => ownerIds.Contains(o.Id));
        }

        var owners = await ownersQuery.ToListAsync();

        var newRecipients = new List<BroadcastRecipient>();
        foreach (var owner in owners)
        {
            if (owner.UserId != null && existingUserIds.Contains(owner.UserId))
                continue;

            newRecipients.Add(new BroadcastRecipient
            {
                Id = Guid.NewGuid(),
                BroadcastId = id,
                UserId = owner.UserId,
                Name = $"{owner.FirstName} {owner.LastName}",
                Email = owner.Email,
                Phone = owner.Phone
            });
        }

        _db.BroadcastRecipients.AddRange(newRecipients);
        broadcast.TotalRecipients += newRecipients.Count;
        await _db.SaveChangesAsync();

        return Ok(newRecipients.Count);
    }

    /// <summary>
    /// Send all messages for the broadcast.
    /// Creates individual CommunicationMessage per recipient and dispatches.
    /// </summary>
    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<object>> Send(Guid id)
    {
        var broadcast = await _db.Broadcasts
            .Include(b => b.Recipients)
            .Include(b => b.Template)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (broadcast == null) return NotFound();

        if (broadcast.Status != BroadcastStatus.Draft && broadcast.Status != BroadcastStatus.Scheduled)
            return BadRequest($"Cannot send broadcast in {broadcast.Status} status");

        if (!broadcast.Recipients.Any())
            return BadRequest("No recipients defined for this broadcast");

        broadcast.Status = BroadcastStatus.Sending;
        broadcast.StartedAt = DateTime.UtcNow;

        var subject = broadcast.Subject ?? broadcast.Template?.Subject ?? "";
        var body = broadcast.Body ?? broadcast.Template?.Body ?? "";
        var sentCount = 0;
        var failedCount = 0;

        foreach (var recipient in broadcast.Recipients)
        {
            // Apply merge variables if any
            var mergedSubject = subject;
            var mergedBody = body;
            if (!string.IsNullOrEmpty(recipient.MergeData))
            {
                try
                {
                    var mergeVars = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, string>>(recipient.MergeData);
                    if (mergeVars != null)
                    {
                        foreach (var kvp in mergeVars)
                        {
                            var placeholder = "{{" + kvp.Key + "}}";
                            mergedSubject = mergedSubject.Replace(placeholder, kvp.Value);
                            mergedBody = mergedBody.Replace(placeholder, kvp.Value);
                        }
                    }
                }
                catch
                {
                    // If merge data is invalid JSON, use raw body
                }
            }

            // Replace common variables
            mergedBody = mergedBody
                .Replace("{{recipientName}}", recipient.Name ?? "")
                .Replace("{{recipientEmail}}", recipient.Email ?? "");
            mergedSubject = mergedSubject
                .Replace("{{recipientName}}", recipient.Name ?? "");

            var message = new CommunicationMessage
            {
                Id = Guid.NewGuid(),
                OrganizationId = broadcast.OrganizationId,
                Channel = broadcast.Channel,
                Status = MessageStatus.Sent,
                RecipientUserId = recipient.UserId,
                RecipientEmail = recipient.Email,
                RecipientPhone = recipient.Phone,
                RecipientName = recipient.Name,
                RecipientAddress = recipient.Address,
                Subject = mergedSubject,
                Body = mergedBody,
                TemplateId = broadcast.TemplateId,
                BroadcastId = broadcast.Id,
                SentAt = DateTime.UtcNow
            };

            _db.CommunicationMessages.Add(message);
            recipient.MessageId = message.Id;

            // In production: dispatch to actual provider here
            sentCount++;
        }

        broadcast.SentCount = sentCount;
        broadcast.FailedCount = failedCount;
        broadcast.Status = failedCount > 0 ? BroadcastStatus.PartiallyCompleted : BroadcastStatus.Completed;
        broadcast.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            broadcast.Id,
            broadcast.Status,
            SentCount = sentCount,
            FailedCount = failedCount,
            TotalRecipients = broadcast.TotalRecipients
        });
    }

    /// <summary>
    /// Cancel a draft or scheduled broadcast.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var entity = await _db.Broadcasts.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != BroadcastStatus.Draft && entity.Status != BroadcastStatus.Scheduled)
            return BadRequest($"Cannot cancel broadcast in {entity.Status} status");

        entity.Status = BroadcastStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Broadcasts.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == BroadcastStatus.Completed || entity.Status == BroadcastStatus.PartiallyCompleted)
            return BadRequest("Cannot delete a completed broadcast");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
