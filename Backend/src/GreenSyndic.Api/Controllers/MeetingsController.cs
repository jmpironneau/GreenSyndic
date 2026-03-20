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
public class MeetingsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public MeetingsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MeetingDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? coOwnershipId,
        [FromQuery] MeetingType? type,
        [FromQuery] MeetingStatus? status)
    {
        var query = _db.Meetings
            .Include(m => m.CoOwnership)
            .Include(m => m.Resolutions)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(m => m.OrganizationId == organizationId.Value);

        if (coOwnershipId.HasValue)
            query = query.Where(m => m.CoOwnershipId == coOwnershipId.Value);

        if (type.HasValue)
            query = query.Where(m => m.Type == type.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        var items = await query.OrderByDescending(m => m.ScheduledDate).Select(m => new MeetingDto
        {
            Id = m.Id,
            OrganizationId = m.OrganizationId,
            CoOwnershipId = m.CoOwnershipId,
            CoOwnershipName = m.CoOwnership.Name,
            Title = m.Title,
            Type = m.Type,
            Status = m.Status,
            ScheduledDate = m.ScheduledDate,
            ActualDate = m.ActualDate,
            Location = m.Location,
            ConvocationDocUrl = m.ConvocationDocUrl,
            MinutesDocUrl = m.MinutesDocUrl,
            Quorum = m.Quorum,
            AttendeesCount = m.AttendeesCount,
            Notes = m.Notes,
            ResolutionCount = m.Resolutions.Count
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MeetingDto>> GetById(Guid id)
    {
        var m = await _db.Meetings
            .Include(x => x.CoOwnership)
            .Include(x => x.Resolutions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (m == null) return NotFound();

        return Ok(new MeetingDto
        {
            Id = m.Id,
            OrganizationId = m.OrganizationId,
            CoOwnershipId = m.CoOwnershipId,
            CoOwnershipName = m.CoOwnership.Name,
            Title = m.Title,
            Type = m.Type,
            Status = m.Status,
            ScheduledDate = m.ScheduledDate,
            ActualDate = m.ActualDate,
            Location = m.Location,
            ConvocationDocUrl = m.ConvocationDocUrl,
            MinutesDocUrl = m.MinutesDocUrl,
            Quorum = m.Quorum,
            AttendeesCount = m.AttendeesCount,
            Notes = m.Notes,
            ResolutionCount = m.Resolutions.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<MeetingDto>> Create([FromBody] CreateMeetingRequest request)
    {
        var entity = new Meeting
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            CoOwnershipId = request.CoOwnershipId,
            Title = request.Title,
            Type = request.Type,
            ScheduledDate = request.ScheduledDate,
            Location = request.Location,
            Notes = request.Notes
        };

        _db.Meetings.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new MeetingDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            CoOwnershipId = entity.CoOwnershipId,
            Title = entity.Title,
            Type = entity.Type,
            Status = entity.Status,
            ScheduledDate = entity.ScheduledDate,
            Location = entity.Location,
            Notes = entity.Notes,
            ResolutionCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMeetingRequest request)
    {
        var entity = await _db.Meetings.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Title = request.Title;
        entity.Type = request.Type;
        entity.ScheduledDate = request.ScheduledDate;
        entity.Location = request.Location;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateMeetingStatusRequest request)
    {
        var entity = await _db.Meetings.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = request.Status;
        if (request.Status == MeetingStatus.Completed)
            entity.ActualDate = DateTime.UtcNow;
        entity.AttendeesCount = request.AttendeesCount;
        entity.ConvocationDocUrl = request.ConvocationDocUrl ?? entity.ConvocationDocUrl;
        entity.MinutesDocUrl = request.MinutesDocUrl ?? entity.MinutesDocUrl;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Meetings.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/resolutions")]
    public async Task<ActionResult<List<ResolutionDto>>> GetResolutions(Guid id)
    {
        var resolutions = await _db.Resolutions
            .Where(r => r.MeetingId == id)
            .Include(r => r.Meeting)
            .Include(r => r.Votes)
            .OrderBy(r => r.OrderNumber)
            .Select(r => new ResolutionDto
            {
                Id = r.Id,
                MeetingId = r.MeetingId,
                MeetingTitle = r.Meeting.Title,
                OrderNumber = r.OrderNumber,
                Title = r.Title,
                Description = r.Description,
                RequiredMajority = r.RequiredMajority,
                VotesFor = r.VotesFor,
                VotesAgainst = r.VotesAgainst,
                VotesAbstain = r.VotesAbstain,
                SharesFor = r.SharesFor,
                SharesAgainst = r.SharesAgainst,
                IsApproved = r.IsApproved,
                Notes = r.Notes,
                VoteCount = r.Votes.Count
            }).ToListAsync();

        return Ok(resolutions);
    }
}

public class UpdateMeetingStatusRequest
{
    public MeetingStatus Status { get; set; }
    public int? AttendeesCount { get; set; }
    public string? ConvocationDocUrl { get; set; }
    public string? MinutesDocUrl { get; set; }
}
