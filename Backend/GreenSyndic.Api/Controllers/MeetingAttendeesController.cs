using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Services.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

[ApiController]
[Route("api/meetings/{meetingId:guid}/attendees")]
[Authorize]
public class MeetingAttendeesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public MeetingAttendeesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MeetingAttendeeDto>>> GetAll(Guid meetingId,
        [FromQuery] AttendanceStatus? status)
    {
        var query = _db.MeetingAttendees
            .Where(a => a.MeetingId == meetingId)
            .Include(a => a.Owner)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var items = await query.Select(a => new MeetingAttendeeDto
        {
            Id = a.Id,
            MeetingId = a.MeetingId,
            OwnerId = a.OwnerId,
            OwnerName = a.Owner.FirstName + " " + a.Owner.LastName,
            Status = a.Status,
            SharesRepresented = a.SharesRepresented,
            ProxyHolderId = a.ProxyHolderId,
            ConvocationMethod = a.ConvocationMethod,
            ConvocationSentAt = a.ConvocationSentAt,
            ConvocationReceivedAt = a.ConvocationReceivedAt,
            HasSigned = a.HasSigned,
            SignedAt = a.SignedAt
        }).ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<MeetingAttendeeDto>> Create(Guid meetingId,
        [FromBody] CreateMeetingAttendeeRequest request)
    {
        // Verify meeting exists
        var meeting = await _db.Meetings.FindAsync(meetingId);
        if (meeting == null) return NotFound("Meeting not found");

        // Check duplicate
        var exists = await _db.MeetingAttendees
            .AnyAsync(a => a.MeetingId == meetingId && a.OwnerId == request.OwnerId);
        if (exists) return Conflict("Owner already registered for this meeting");

        var entity = new MeetingAttendee
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            OwnerId = request.OwnerId,
            Status = request.Status,
            SharesRepresented = request.SharesRepresented,
            ProxyHolderId = request.ProxyHolderId,
            ConvocationMethod = request.ConvocationMethod
        };

        _db.MeetingAttendees.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { meetingId }, new MeetingAttendeeDto
        {
            Id = entity.Id,
            MeetingId = entity.MeetingId,
            OwnerId = entity.OwnerId,
            Status = entity.Status,
            SharesRepresented = entity.SharesRepresented,
            ProxyHolderId = entity.ProxyHolderId,
            ConvocationMethod = entity.ConvocationMethod,
            HasSigned = entity.HasSigned
        });
    }

    /// <summary>
    /// Bulk add: register all owners of a co-ownership for the meeting.
    /// </summary>
    [HttpPost("bulk-register")]
    public async Task<ActionResult<int>> BulkRegister(Guid meetingId)
    {
        var meeting = await _db.Meetings.FindAsync(meetingId);
        if (meeting == null) return NotFound("Meeting not found");

        // Get all owners with units in this co-ownership
        var owners = await _db.Units
            .Where(u => u.Building.CoOwnershipId == meeting.CoOwnershipId && u.OwnerId != null)
            .Select(u => new { u.OwnerId, u.ShareRatio })
            .Distinct()
            .ToListAsync();

        var existingOwnerIds = await _db.MeetingAttendees
            .Where(a => a.MeetingId == meetingId)
            .Select(a => a.OwnerId)
            .ToListAsync();

        var newAttendees = owners
            .Where(o => o.OwnerId.HasValue && !existingOwnerIds.Contains(o.OwnerId.Value))
            .Select(o => new MeetingAttendee
            {
                Id = Guid.NewGuid(),
                MeetingId = meetingId,
                OwnerId = o.OwnerId!.Value,
                Status = AttendanceStatus.Expected,
                SharesRepresented = o.ShareRatio ?? 0
            })
            .ToList();

        _db.MeetingAttendees.AddRange(newAttendees);
        await _db.SaveChangesAsync();

        return Ok(newAttendees.Count);
    }

    /// <summary>
    /// Update attendance status (check-in, proxy, signature).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid meetingId, Guid id,
        [FromBody] UpdateAttendanceStatusRequest request)
    {
        var entity = await _db.MeetingAttendees
            .FirstOrDefaultAsync(a => a.Id == id && a.MeetingId == meetingId);
        if (entity == null) return NotFound();

        entity.Status = request.Status;
        entity.ProxyHolderId = request.ProxyHolderId;
        entity.HasSigned = request.HasSigned;
        if (request.HasSigned && entity.SignedAt == null)
            entity.SignedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Mark convocation as sent for an attendee.
    /// </summary>
    [HttpPut("{id:guid}/convocation-sent")]
    public async Task<IActionResult> MarkConvocationSent(Guid meetingId, Guid id,
        [FromBody] ConvocationMethod method)
    {
        var entity = await _db.MeetingAttendees
            .FirstOrDefaultAsync(a => a.Id == id && a.MeetingId == meetingId);
        if (entity == null) return NotFound();

        entity.ConvocationMethod = method;
        entity.ConvocationSentAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid meetingId, Guid id)
    {
        var entity = await _db.MeetingAttendees
            .FirstOrDefaultAsync(a => a.Id == id && a.MeetingId == meetingId);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
