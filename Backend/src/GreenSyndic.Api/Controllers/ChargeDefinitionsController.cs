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
public class ChargeDefinitionsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public ChargeDefinitionsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChargeDefinitionDto>>> GetAll(
        [FromQuery] Guid? coOwnershipId,
        [FromQuery] ChargeType? type,
        [FromQuery] int? fiscalYear)
    {
        var query = _db.ChargeDefinitions
            .Include(cd => cd.CoOwnership)
            .AsQueryable();

        if (coOwnershipId.HasValue)
            query = query.Where(cd => cd.CoOwnershipId == coOwnershipId.Value);

        if (type.HasValue)
            query = query.Where(cd => cd.Type == type.Value);

        if (fiscalYear.HasValue)
            query = query.Where(cd => cd.FiscalYear == fiscalYear.Value);

        var items = await query.Select(cd => new ChargeDefinitionDto
        {
            Id = cd.Id,
            CoOwnershipId = cd.CoOwnershipId,
            CoOwnershipName = cd.CoOwnership.Name,
            Name = cd.Name,
            Type = cd.Type,
            AnnualAmount = cd.AnnualAmount,
            DistributionKey = cd.DistributionKey,
            IsRecoverable = cd.IsRecoverable,
            Description = cd.Description,
            FiscalYear = cd.FiscalYear
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChargeDefinitionDto>> GetById(Guid id)
    {
        var cd = await _db.ChargeDefinitions
            .Include(x => x.CoOwnership)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cd == null) return NotFound();

        return Ok(new ChargeDefinitionDto
        {
            Id = cd.Id,
            CoOwnershipId = cd.CoOwnershipId,
            CoOwnershipName = cd.CoOwnership.Name,
            Name = cd.Name,
            Type = cd.Type,
            AnnualAmount = cd.AnnualAmount,
            DistributionKey = cd.DistributionKey,
            IsRecoverable = cd.IsRecoverable,
            Description = cd.Description,
            FiscalYear = cd.FiscalYear
        });
    }

    [HttpPost]
    public async Task<ActionResult<ChargeDefinitionDto>> Create([FromBody] CreateChargeDefinitionRequest request)
    {
        var entity = new ChargeDefinition
        {
            Id = Guid.NewGuid(),
            CoOwnershipId = request.CoOwnershipId,
            Name = request.Name,
            Type = request.Type,
            AnnualAmount = request.AnnualAmount,
            DistributionKey = request.DistributionKey,
            IsRecoverable = request.IsRecoverable,
            Description = request.Description,
            FiscalYear = request.FiscalYear
        };

        _db.ChargeDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new ChargeDefinitionDto
        {
            Id = entity.Id,
            CoOwnershipId = entity.CoOwnershipId,
            Name = entity.Name,
            Type = entity.Type,
            AnnualAmount = entity.AnnualAmount,
            FiscalYear = entity.FiscalYear
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateChargeDefinitionRequest request)
    {
        var entity = await _db.ChargeDefinitions.FindAsync(id);
        if (entity == null) return NotFound();

        entity.CoOwnershipId = request.CoOwnershipId;
        entity.Name = request.Name;
        entity.Type = request.Type;
        entity.AnnualAmount = request.AnnualAmount;
        entity.DistributionKey = request.DistributionKey;
        entity.IsRecoverable = request.IsRecoverable;
        entity.Description = request.Description;
        entity.FiscalYear = request.FiscalYear;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.ChargeDefinitions.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
