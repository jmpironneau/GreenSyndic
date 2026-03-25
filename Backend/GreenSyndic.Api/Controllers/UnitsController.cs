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
public class UnitsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public UnitsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UnitDto>>> GetAll(
        [FromQuery] Guid? buildingId,
        [FromQuery] Guid? ownerId,
        [FromQuery] UnitStatus? status)
    {
        var query = _db.Units
            .Include(u => u.Building)
            .Include(u => u.Owner)
            .AsQueryable();

        if (buildingId.HasValue)
            query = query.Where(u => u.BuildingId == buildingId.Value);
        if (ownerId.HasValue)
            query = query.Where(u => u.OwnerId == ownerId.Value);
        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        var items = await query.Select(u => new UnitDto
        {
            Id = u.Id,
            BuildingId = u.BuildingId,
            BuildingName = u.Building.Name,
            CoOwnershipId = u.CoOwnershipId,
            Reference = u.Reference,
            Name = u.Name,
            Type = u.Type,
            Status = u.Status,
            Floor = u.Floor,
            AreaSqm = u.AreaSqm,
            ShareRatio = u.ShareRatio,
            HorizontalShareRatio = u.HorizontalShareRatio,
            NumberOfRooms = u.NumberOfRooms,
            MarketValue = u.MarketValue,
            OwnerId = u.OwnerId,
            OwnerName = u.Owner != null ? u.Owner.FirstName + " " + u.Owner.LastName : null
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDto>> GetById(Guid id)
    {
        var u = await _db.Units
            .Include(x => x.Building)
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();

        return Ok(new UnitDto
        {
            Id = u.Id,
            BuildingId = u.BuildingId,
            BuildingName = u.Building.Name,
            CoOwnershipId = u.CoOwnershipId,
            Reference = u.Reference,
            Name = u.Name,
            Type = u.Type,
            Status = u.Status,
            Floor = u.Floor,
            AreaSqm = u.AreaSqm,
            ShareRatio = u.ShareRatio,
            HorizontalShareRatio = u.HorizontalShareRatio,
            NumberOfRooms = u.NumberOfRooms,
            MarketValue = u.MarketValue,
            OwnerId = u.OwnerId,
            OwnerName = u.Owner != null ? u.Owner.FirstName + " " + u.Owner.LastName : null
        });
    }

    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create([FromBody] CreateUnitRequest request)
    {
        var building = await _db.Buildings.FindAsync(request.BuildingId);
        if (building == null) return BadRequest(new { error = "Building introuvable." });

        var entity = new Unit
        {
            Id = Guid.NewGuid(),
            OrganizationId = building.OrganizationId,
            BuildingId = request.BuildingId,
            CoOwnershipId = request.CoOwnershipId,
            Reference = request.Reference,
            Name = request.Name,
            Type = request.Type,
            Status = UnitStatus.Available,
            Floor = request.Floor,
            AreaSqm = request.AreaSqm,
            ShareRatio = request.ShareRatio,
            HorizontalShareRatio = request.HorizontalShareRatio,
            NumberOfRooms = request.NumberOfRooms,
            MarketValue = request.MarketValue,
            OwnerId = request.OwnerId
        };

        _db.Units.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new UnitDto
        {
            Id = entity.Id,
            BuildingId = entity.BuildingId,
            BuildingName = building.Name,
            Reference = entity.Reference,
            Type = entity.Type,
            Status = entity.Status,
            AreaSqm = entity.AreaSqm
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateUnitRequest request)
    {
        var entity = await _db.Units.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Reference = request.Reference;
        entity.Name = request.Name;
        entity.Type = request.Type;
        entity.Floor = request.Floor;
        entity.AreaSqm = request.AreaSqm;
        entity.ShareRatio = request.ShareRatio;
        entity.HorizontalShareRatio = request.HorizontalShareRatio;
        entity.NumberOfRooms = request.NumberOfRooms;
        entity.MarketValue = request.MarketValue;
        entity.OwnerId = request.OwnerId;
        entity.CoOwnershipId = request.CoOwnershipId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Units.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
