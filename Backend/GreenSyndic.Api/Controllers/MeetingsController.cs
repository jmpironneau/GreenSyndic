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

    private Guid GetOrgIdFromClaim()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
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
            OrganizationId = GetOrgIdFromClaim(),
            CoOwnershipId = request.CoOwnershipId != Guid.Empty ? request.CoOwnershipId : null,
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

    /// <summary>
    /// Calculate quorum for a meeting based on attendees.
    /// </summary>
    [HttpGet("{id:guid}/quorum")]
    public async Task<ActionResult<QuorumResultDto>> GetQuorum(Guid id)
    {
        var meeting = await _db.Meetings
            .Include(m => m.Attendees)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (meeting == null) return NotFound();

        // Total owners in the co-ownership (with units)
        var totalOwners = await _db.Units
            .Where(u => u.Building.CoOwnershipId == meeting.CoOwnershipId && u.OwnerId != null)
            .Select(u => u.OwnerId)
            .Distinct()
            .CountAsync();

        var totalShares = await _db.Units
            .Where(u => u.Building.CoOwnershipId == meeting.CoOwnershipId && u.OwnerId != null)
            .SumAsync(u => u.ShareRatio ?? 0);

        var presentStatuses = new[]
        {
            AttendanceStatus.PresentInPerson,
            AttendanceStatus.PresentRemote,
            AttendanceStatus.RepresentedByProxy
        };

        var presentAttendees = meeting.Attendees
            .Where(a => presentStatuses.Contains(a.Status))
            .ToList();

        var representedShares = presentAttendees.Sum(a => a.SharesRepresented);
        var quorumPct = totalShares > 0 ? (representedShares / totalShares) * 100 : 0;

        // AG ordinaire : quorum = majorité des tantièmes (>50%)
        // AG extraordinaire : quorum = 2/3 des tantièmes
        var requiredPct = meeting.Type == MeetingType.ExtraordinaryGeneral ? 66.67m : 50m;
        var hasQuorum = quorumPct >= requiredPct;

        // Update meeting quorum info
        meeting.Quorum = (int)Math.Round(quorumPct);
        meeting.AttendeesCount = presentAttendees.Count;
        await _db.SaveChangesAsync();

        return Ok(new QuorumResultDto
        {
            MeetingId = id,
            TotalOwners = totalOwners,
            PresentOrRepresented = presentAttendees.Count,
            TotalShares = totalShares,
            RepresentedShares = representedShares,
            QuorumPercentage = Math.Round(quorumPct, 2),
            HasQuorum = hasQuorum
        });
    }

    /// <summary>
    /// Send convocations to all attendees of a meeting.
    /// Updates meeting status to ConvocationSent.
    /// </summary>
    [HttpPost("{id:guid}/send-convocations")]
    public async Task<IActionResult> SendConvocations(Guid id,
        [FromBody] SendConvocationsRequest request)
    {
        var meeting = await _db.Meetings
            .Include(m => m.Attendees)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (meeting == null) return NotFound();

        if (meeting.Status != MeetingStatus.Planned)
            return BadRequest("Convocations can only be sent for meetings in Planned status");

        var now = DateTime.UtcNow;

        // Mark all attendees as convoked
        foreach (var attendee in meeting.Attendees)
        {
            attendee.ConvocationMethod = request.Method;
            attendee.ConvocationSentAt = now;
            attendee.UpdatedAt = now;
        }

        // Update meeting status
        meeting.Status = MeetingStatus.ConvocationSent;
        meeting.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return Ok(new { ConvocationsSent = meeting.Attendees.Count, Method = request.Method });
    }

    /// <summary>
    /// Get attendance sheet (feuille de présence) for a meeting.
    /// </summary>
    [HttpGet("{id:guid}/attendance-sheet")]
    public async Task<ActionResult<object>> GetAttendanceSheet(Guid id)
    {
        var meeting = await _db.Meetings
            .Include(m => m.CoOwnership)
            .Include(m => m.Attendees)
                .ThenInclude(a => a.Owner)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (meeting == null) return NotFound();

        var sheet = new
        {
            meeting.Id,
            meeting.Title,
            meeting.Type,
            meeting.ScheduledDate,
            meeting.Location,
            CoOwnershipName = meeting.CoOwnership.Name,
            Attendees = meeting.Attendees.OrderBy(a => a.Owner.LastName).Select(a => new
            {
                OwnerName = a.Owner.FirstName + " " + a.Owner.LastName,
                a.Status,
                a.SharesRepresented,
                IsProxy = a.Status == AttendanceStatus.RepresentedByProxy,
                a.ProxyHolderId,
                a.HasSigned,
                a.SignedAt,
                a.ConvocationMethod,
                a.ConvocationSentAt
            }),
            Summary = new
            {
                Total = meeting.Attendees.Count,
                PresentInPerson = meeting.Attendees.Count(a => a.Status == AttendanceStatus.PresentInPerson),
                PresentRemote = meeting.Attendees.Count(a => a.Status == AttendanceStatus.PresentRemote),
                RepresentedByProxy = meeting.Attendees.Count(a => a.Status == AttendanceStatus.RepresentedByProxy),
                Absent = meeting.Attendees.Count(a => a.Status == AttendanceStatus.Absent),
                TotalSharesRepresented = meeting.Attendees
                    .Where(a => a.Status is AttendanceStatus.PresentInPerson
                        or AttendanceStatus.PresentRemote
                        or AttendanceStatus.RepresentedByProxy)
                    .Sum(a => a.SharesRepresented)
            }
        };

        return Ok(sheet);
    }

    /// <summary>
    /// Generate PV (procès-verbal) data for a completed meeting.
    /// </summary>
    [HttpGet("{id:guid}/pv")]
    public async Task<ActionResult<object>> GetProcesVerbal(Guid id)
    {
        var meeting = await _db.Meetings
            .Include(m => m.CoOwnership)
            .Include(m => m.Attendees).ThenInclude(a => a.Owner)
            .Include(m => m.Resolutions).ThenInclude(r => r.Votes).ThenInclude(v => v.Owner)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (meeting == null) return NotFound();

        var agendaItems = await _db.MeetingAgendaItems
            .Where(a => a.MeetingId == id)
            .OrderBy(a => a.OrderNumber)
            .ToListAsync();

        var presentStatuses = new[]
        {
            AttendanceStatus.PresentInPerson,
            AttendanceStatus.PresentRemote,
            AttendanceStatus.RepresentedByProxy
        };

        var pv = new
        {
            Header = new
            {
                meeting.Title,
                meeting.Type,
                meeting.ScheduledDate,
                ActualDate = meeting.ActualDate ?? meeting.ScheduledDate,
                meeting.Location,
                CoOwnershipName = meeting.CoOwnership.Name,
                meeting.Quorum,
                meeting.AttendeesCount
            },
            Attendance = new
            {
                Present = meeting.Attendees
                    .Where(a => presentStatuses.Contains(a.Status))
                    .Select(a => new
                    {
                        Name = a.Owner.FirstName + " " + a.Owner.LastName,
                        a.Status,
                        a.SharesRepresented
                    }),
                Absent = meeting.Attendees
                    .Where(a => a.Status == AttendanceStatus.Absent)
                    .Select(a => new { Name = a.Owner.FirstName + " " + a.Owner.LastName })
            },
            AgendaItems = agendaItems.Select(a => new
            {
                a.OrderNumber,
                a.Title,
                a.Description,
                a.Type
            }),
            Resolutions = meeting.Resolutions.OrderBy(r => r.OrderNumber).Select(r => new
            {
                r.OrderNumber,
                r.Title,
                r.Description,
                r.RequiredMajority,
                r.VotesFor,
                r.VotesAgainst,
                r.VotesAbstain,
                r.SharesFor,
                r.SharesAgainst,
                r.IsApproved,
                r.Notes
            })
        };

        return Ok(pv);
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
