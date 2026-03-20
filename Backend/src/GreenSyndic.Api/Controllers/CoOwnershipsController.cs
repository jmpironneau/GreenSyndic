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
public class CoOwnershipsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public CoOwnershipsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CoOwnershipDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? parentCoOwnershipId,
        [FromQuery] CoOwnershipLevel? level)
    {
        var query = _db.CoOwnerships
            .Include(c => c.ParentCoOwnership)
            .Include(c => c.ChildCoOwnerships)
            .Include(c => c.Buildings)
            .Include(c => c.Units)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(c => c.OrganizationId == organizationId.Value);

        if (parentCoOwnershipId.HasValue)
            query = query.Where(c => c.ParentCoOwnershipId == parentCoOwnershipId.Value);

        if (level.HasValue)
            query = query.Where(c => c.Level == level.Value);

        var items = await query.Select(c => new CoOwnershipDto
        {
            Id = c.Id,
            OrganizationId = c.OrganizationId,
            Name = c.Name,
            Level = c.Level,
            Description = c.Description,
            RegulationReference = c.RegulationReference,
            AnnualBudget = c.AnnualBudget,
            ReserveFund = c.ReserveFund,
            SyndicFeePercent = c.SyndicFeePercent,
            ParentCoOwnershipId = c.ParentCoOwnershipId,
            ParentCoOwnershipName = c.ParentCoOwnership != null ? c.ParentCoOwnership.Name : null,
            ChildCount = c.ChildCoOwnerships.Count,
            BuildingCount = c.Buildings.Count,
            UnitCount = c.Units.Count
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CoOwnershipDto>> GetById(Guid id)
    {
        var c = await _db.CoOwnerships
            .Include(x => x.ParentCoOwnership)
            .Include(x => x.ChildCoOwnerships)
            .Include(x => x.Buildings)
            .Include(x => x.Units)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return NotFound();

        return Ok(new CoOwnershipDto
        {
            Id = c.Id,
            OrganizationId = c.OrganizationId,
            Name = c.Name,
            Level = c.Level,
            Description = c.Description,
            RegulationReference = c.RegulationReference,
            AnnualBudget = c.AnnualBudget,
            ReserveFund = c.ReserveFund,
            SyndicFeePercent = c.SyndicFeePercent,
            ParentCoOwnershipId = c.ParentCoOwnershipId,
            ParentCoOwnershipName = c.ParentCoOwnership?.Name,
            ChildCount = c.ChildCoOwnerships.Count,
            BuildingCount = c.Buildings.Count,
            UnitCount = c.Units.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<CoOwnershipDto>> Create([FromBody] CreateCoOwnershipRequest request)
    {
        var entity = new CoOwnership
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Level = request.Level,
            Description = request.Description,
            RegulationReference = request.RegulationReference,
            AnnualBudget = request.AnnualBudget,
            ReserveFund = request.ReserveFund,
            SyndicFeePercent = request.SyndicFeePercent,
            ParentCoOwnershipId = request.ParentCoOwnershipId
        };

        _db.CoOwnerships.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new CoOwnershipDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Name = entity.Name,
            Level = entity.Level,
            ChildCount = 0,
            BuildingCount = 0,
            UnitCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCoOwnershipRequest request)
    {
        var entity = await _db.CoOwnerships.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = request.Name;
        entity.Level = request.Level;
        entity.Description = request.Description;
        entity.RegulationReference = request.RegulationReference;
        entity.AnnualBudget = request.AnnualBudget;
        entity.ReserveFund = request.ReserveFund;
        entity.SyndicFeePercent = request.SyndicFeePercent;
        entity.ParentCoOwnershipId = request.ParentCoOwnershipId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.CoOwnerships.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<List<CoOwnershipDto>>> GetChildren(Guid id)
    {
        var children = await _db.CoOwnerships
            .Where(c => c.ParentCoOwnershipId == id)
            .Include(c => c.ChildCoOwnerships)
            .Include(c => c.Buildings)
            .Include(c => c.Units)
            .Select(c => new CoOwnershipDto
            {
                Id = c.Id,
                OrganizationId = c.OrganizationId,
                Name = c.Name,
                Level = c.Level,
                Description = c.Description,
                RegulationReference = c.RegulationReference,
                AnnualBudget = c.AnnualBudget,
                ReserveFund = c.ReserveFund,
                SyndicFeePercent = c.SyndicFeePercent,
                ParentCoOwnershipId = c.ParentCoOwnershipId,
                ChildCount = c.ChildCoOwnerships.Count,
                BuildingCount = c.Buildings.Count,
                UnitCount = c.Units.Count
            }).ToListAsync();

        return Ok(children);
    }
}
