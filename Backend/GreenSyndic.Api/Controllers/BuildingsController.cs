using GreenSyndic.Core.Entities;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Services.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BuildingsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public BuildingsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<BuildingDto>>> GetAll([FromQuery] Guid? organizationId)
    {
        var query = _db.Buildings.Include(b => b.Units).AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(b => b.OrganizationId == organizationId.Value);

        var items = await query.Select(b => new BuildingDto
        {
            Id = b.Id,
            OrganizationId = b.OrganizationId,
            CoOwnershipId = b.CoOwnershipId,
            Name = b.Name,
            Code = b.Code,
            PrimaryType = b.PrimaryType,
            Address = b.Address,
            NumberOfFloors = b.NumberOfFloors,
            TotalAreaSqm = b.TotalAreaSqm,
            CommonAreaSqm = b.CommonAreaSqm,
            HasElevator = b.HasElevator,
            HasGenerator = b.HasGenerator,
            HasParking = b.HasParking,
            Description = b.Description,
            UnitCount = b.Units.Count
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BuildingDto>> GetById(Guid id)
    {
        var b = await _db.Buildings.Include(x => x.Units).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();

        return Ok(new BuildingDto
        {
            Id = b.Id,
            OrganizationId = b.OrganizationId,
            CoOwnershipId = b.CoOwnershipId,
            Name = b.Name,
            Code = b.Code,
            PrimaryType = b.PrimaryType,
            Address = b.Address,
            NumberOfFloors = b.NumberOfFloors,
            TotalAreaSqm = b.TotalAreaSqm,
            CommonAreaSqm = b.CommonAreaSqm,
            HasElevator = b.HasElevator,
            HasGenerator = b.HasGenerator,
            HasParking = b.HasParking,
            Description = b.Description,
            UnitCount = b.Units.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<BuildingDto>> Create([FromBody] CreateBuildingRequest request)
    {
        var entity = new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            CoOwnershipId = request.CoOwnershipId,
            Name = request.Name,
            Code = request.Code,
            PrimaryType = request.PrimaryType,
            Address = request.Address,
            NumberOfFloors = request.NumberOfFloors,
            TotalAreaSqm = request.TotalAreaSqm,
            CommonAreaSqm = request.CommonAreaSqm,
            HasElevator = request.HasElevator,
            HasGenerator = request.HasGenerator,
            HasParking = request.HasParking,
            Description = request.Description
        };

        _db.Buildings.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new BuildingDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Name = entity.Name,
            Code = entity.Code,
            PrimaryType = entity.PrimaryType,
            UnitCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateBuildingRequest request)
    {
        var entity = await _db.Buildings.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.PrimaryType = request.PrimaryType;
        entity.CoOwnershipId = request.CoOwnershipId;
        entity.Address = request.Address;
        entity.NumberOfFloors = request.NumberOfFloors;
        entity.TotalAreaSqm = request.TotalAreaSqm;
        entity.CommonAreaSqm = request.CommonAreaSqm;
        entity.HasElevator = request.HasElevator;
        entity.HasGenerator = request.HasGenerator;
        entity.HasParking = request.HasParking;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Buildings.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
