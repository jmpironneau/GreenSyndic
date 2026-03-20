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
public class VotesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public VotesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<VoteDto>>> GetAll(
        [FromQuery] Guid? resolutionId,
        [FromQuery] Guid? ownerId)
    {
        var query = _db.Votes
            .Include(v => v.Resolution)
            .Include(v => v.Owner)
            .AsQueryable();

        if (resolutionId.HasValue)
            query = query.Where(v => v.ResolutionId == resolutionId.Value);

        if (ownerId.HasValue)
            query = query.Where(v => v.OwnerId == ownerId.Value);

        var items = await query.Select(v => new VoteDto
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

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VoteDto>> GetById(Guid id)
    {
        var v = await _db.Votes
            .Include(x => x.Resolution)
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (v == null) return NotFound();

        // Resolve proxy owner name if applicable
        string? proxyOwnerName = null;
        if (v.ProxyOwnerId.HasValue)
        {
            var proxy = await _db.Owners.FindAsync(v.ProxyOwnerId.Value);
            proxyOwnerName = proxy != null ? $"{proxy.FirstName} {proxy.LastName}" : null;
        }

        return Ok(new VoteDto
        {
            Id = v.Id,
            ResolutionId = v.ResolutionId,
            ResolutionTitle = v.Resolution.Title,
            OwnerId = v.OwnerId,
            OwnerName = $"{v.Owner.FirstName} {v.Owner.LastName}",
            UnitId = v.UnitId,
            Result = v.Result,
            ShareWeight = v.ShareWeight,
            IsProxy = v.IsProxy,
            ProxyOwnerId = v.ProxyOwnerId,
            ProxyOwnerName = proxyOwnerName
        });
    }

    [HttpPost]
    public async Task<ActionResult<VoteDto>> Create([FromBody] CreateVoteRequest request)
    {
        // Prevent duplicate votes: same owner on same resolution
        var existing = await _db.Votes
            .AnyAsync(v => v.ResolutionId == request.ResolutionId && v.OwnerId == request.OwnerId);

        if (existing)
            return Conflict(new { message = "This owner has already voted on this resolution." });

        var entity = new Vote
        {
            Id = Guid.NewGuid(),
            ResolutionId = request.ResolutionId,
            OwnerId = request.OwnerId,
            UnitId = request.UnitId,
            Result = request.Result,
            ShareWeight = request.ShareWeight,
            IsProxy = request.IsProxy,
            ProxyOwnerId = request.ProxyOwnerId
        };

        _db.Votes.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new VoteDto
        {
            Id = entity.Id,
            ResolutionId = entity.ResolutionId,
            OwnerId = entity.OwnerId,
            UnitId = entity.UnitId,
            Result = entity.Result,
            ShareWeight = entity.ShareWeight,
            IsProxy = entity.IsProxy,
            ProxyOwnerId = entity.ProxyOwnerId
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateVoteRequest request)
    {
        var entity = await _db.Votes.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Result = request.Result;
        entity.ShareWeight = request.ShareWeight;
        entity.IsProxy = request.IsProxy;
        entity.ProxyOwnerId = request.ProxyOwnerId;
        entity.UnitId = request.UnitId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Votes.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
