using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Services.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

[ApiController]
[Route("api/meetings/{meetingId:guid}/agenda-items")]
[Authorize]
public class MeetingAgendaItemsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public MeetingAgendaItemsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MeetingAgendaItemDto>>> GetAll(Guid meetingId)
    {
        var items = await _db.MeetingAgendaItems
            .Where(a => a.MeetingId == meetingId)
            .OrderBy(a => a.OrderNumber)
            .Select(a => new MeetingAgendaItemDto
            {
                Id = a.Id,
                MeetingId = a.MeetingId,
                OrderNumber = a.OrderNumber,
                Title = a.Title,
                Description = a.Description,
                Type = a.Type,
                EstimatedDurationMinutes = a.EstimatedDurationMinutes,
                ResolutionId = a.ResolutionId,
                AttachmentUrls = a.AttachmentUrls
            }).ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<MeetingAgendaItemDto>> Create(Guid meetingId,
        [FromBody] CreateMeetingAgendaItemRequest request)
    {
        var meeting = await _db.Meetings.FindAsync(meetingId);
        if (meeting == null) return NotFound("Meeting not found");

        var entity = new MeetingAgendaItem
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            OrderNumber = request.OrderNumber,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            ResolutionId = request.ResolutionId,
            AttachmentUrls = request.AttachmentUrls
        };

        _db.MeetingAgendaItems.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { meetingId }, new MeetingAgendaItemDto
        {
            Id = entity.Id,
            MeetingId = entity.MeetingId,
            OrderNumber = entity.OrderNumber,
            Title = entity.Title,
            Description = entity.Description,
            Type = entity.Type,
            EstimatedDurationMinutes = entity.EstimatedDurationMinutes,
            ResolutionId = entity.ResolutionId,
            AttachmentUrls = entity.AttachmentUrls
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid meetingId, Guid id,
        [FromBody] CreateMeetingAgendaItemRequest request)
    {
        var entity = await _db.MeetingAgendaItems
            .FirstOrDefaultAsync(a => a.Id == id && a.MeetingId == meetingId);
        if (entity == null) return NotFound();

        entity.OrderNumber = request.OrderNumber;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Type = request.Type;
        entity.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        entity.ResolutionId = request.ResolutionId;
        entity.AttachmentUrls = request.AttachmentUrls;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Reorder agenda items (batch update of order numbers).
    /// </summary>
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(Guid meetingId,
        [FromBody] List<ReorderItem> items)
    {
        var agendaItems = await _db.MeetingAgendaItems
            .Where(a => a.MeetingId == meetingId)
            .ToListAsync();

        foreach (var item in items)
        {
            var entity = agendaItems.FirstOrDefault(a => a.Id == item.Id);
            if (entity != null)
            {
                entity.OrderNumber = item.OrderNumber;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid meetingId, Guid id)
    {
        var entity = await _db.MeetingAgendaItems
            .FirstOrDefaultAsync(a => a.Id == id && a.MeetingId == meetingId);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class ReorderItem
{
    public Guid Id { get; set; }
    public int OrderNumber { get; set; }
}
