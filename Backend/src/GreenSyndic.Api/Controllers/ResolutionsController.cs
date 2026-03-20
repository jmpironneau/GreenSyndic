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
public class ResolutionsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public ResolutionsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResolutionDto>>> GetAll(
        [FromQuery] Guid? meetingId,
        [FromQuery] bool? isApproved)
    {
        var query = _db.Resolutions
            .Include(r => r.Meeting)
            .Include(r => r.Votes)
            .AsQueryable();

        if (meetingId.HasValue)
            query = query.Where(r => r.MeetingId == meetingId.Value);

        if (isApproved.HasValue)
            query = query.Where(r => r.IsApproved == isApproved.Value);

        var items = await query.OrderBy(r => r.OrderNumber).Select(r => new ResolutionDto
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

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResolutionDto>> GetById(Guid id)
    {
        var r = await _db.Resolutions
            .Include(x => x.Meeting)
            .Include(x => x.Votes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();

        return Ok(new ResolutionDto
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
        });
    }

    [HttpPost]
    public async Task<ActionResult<ResolutionDto>> Create([FromBody] CreateResolutionRequest request)
    {
        var entity = new Resolution
        {
            Id = Guid.NewGuid(),
            MeetingId = request.MeetingId,
            OrderNumber = request.OrderNumber,
            Title = request.Title,
            Description = request.Description,
            RequiredMajority = request.RequiredMajority,
            Notes = request.Notes
        };

        _db.Resolutions.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new ResolutionDto
        {
            Id = entity.Id,
            MeetingId = entity.MeetingId,
            OrderNumber = entity.OrderNumber,
            Title = entity.Title,
            Description = entity.Description,
            RequiredMajority = entity.RequiredMajority,
            Notes = entity.Notes,
            VoteCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateResolutionRequest request)
    {
        var entity = await _db.Resolutions.FindAsync(id);
        if (entity == null) return NotFound();

        entity.OrderNumber = request.OrderNumber;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.RequiredMajority = request.RequiredMajority;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/tally")]
    public async Task<IActionResult> Tally(Guid id)
    {
        var resolution = await _db.Resolutions
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resolution == null) return NotFound();

        resolution.VotesFor = resolution.Votes.Count(v => v.Result == VoteResult.For);
        resolution.VotesAgainst = resolution.Votes.Count(v => v.Result == VoteResult.Against);
        resolution.VotesAbstain = resolution.Votes.Count(v => v.Result == VoteResult.Abstain);
        resolution.SharesFor = resolution.Votes.Where(v => v.Result == VoteResult.For).Sum(v => v.ShareWeight);
        resolution.SharesAgainst = resolution.Votes.Where(v => v.Result == VoteResult.Against).Sum(v => v.ShareWeight);

        var totalShares = resolution.SharesFor + resolution.SharesAgainst;
        resolution.IsApproved = resolution.RequiredMajority switch
        {
            ResolutionMajority.Simple => resolution.SharesFor > resolution.SharesAgainst,
            ResolutionMajority.Absolute => totalShares > 0 && resolution.SharesFor > (totalShares / 2),
            ResolutionMajority.DoubleMajority => resolution.VotesFor > (resolution.VotesFor + resolution.VotesAgainst) / 2
                && resolution.SharesFor > totalShares * 2 / 3,
            ResolutionMajority.Unanimity => resolution.VotesAgainst == 0 && resolution.VotesFor > 0,
            _ => false
        };

        resolution.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new ResolutionDto
        {
            Id = resolution.Id,
            MeetingId = resolution.MeetingId,
            OrderNumber = resolution.OrderNumber,
            Title = resolution.Title,
            RequiredMajority = resolution.RequiredMajority,
            VotesFor = resolution.VotesFor,
            VotesAgainst = resolution.VotesAgainst,
            VotesAbstain = resolution.VotesAbstain,
            SharesFor = resolution.SharesFor,
            SharesAgainst = resolution.SharesAgainst,
            IsApproved = resolution.IsApproved
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Resolutions.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/votes")]
    public async Task<ActionResult<List<VoteDto>>> GetVotes(Guid id)
    {
        var votes = await _db.Votes
            .Where(v => v.ResolutionId == id)
            .Include(v => v.Owner)
            .Include(v => v.Resolution)
            .Select(v => new VoteDto
            {
                Id = v.Id,
                ResolutionId = v.ResolutionId,
                ResolutionTitle = v.Resolution.Title,
                OwnerId = v.OwnerId,
                OwnerName = v.Owner.FirstName + " " + v.Owner.LastName,
                UnitId = v.UnitId,
                Result = v.Result,
                ShareWeight = v.ShareWeight,
                IsProxy = v.IsProxy,
                ProxyOwnerId = v.ProxyOwnerId
            }).ToListAsync();

        return Ok(votes);
    }
}
