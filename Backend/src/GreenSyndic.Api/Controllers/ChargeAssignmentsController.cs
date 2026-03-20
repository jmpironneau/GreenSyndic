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
public class ChargeAssignmentsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public ChargeAssignmentsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChargeAssignmentDto>>> GetAll(
        [FromQuery] Guid? unitId,
        [FromQuery] Guid? chargeDefinitionId,
        [FromQuery] int? year,
        [FromQuery] int? quarter,
        [FromQuery] bool? isPaid)
    {
        var query = _db.ChargeAssignments
            .Include(ca => ca.ChargeDefinition)
            .Include(ca => ca.Unit)
            .AsQueryable();

        if (unitId.HasValue)
            query = query.Where(ca => ca.UnitId == unitId.Value);

        if (chargeDefinitionId.HasValue)
            query = query.Where(ca => ca.ChargeDefinitionId == chargeDefinitionId.Value);

        if (year.HasValue)
            query = query.Where(ca => ca.Year == year.Value);

        if (quarter.HasValue)
            query = query.Where(ca => ca.Quarter == quarter.Value);

        if (isPaid.HasValue)
            query = query.Where(ca => ca.IsPaid == isPaid.Value);

        var items = await query.Select(ca => new ChargeAssignmentDto
        {
            Id = ca.Id,
            ChargeDefinitionId = ca.ChargeDefinitionId,
            ChargeDefinitionName = ca.ChargeDefinition.Name,
            UnitId = ca.UnitId,
            UnitReference = ca.Unit.Reference,
            Year = ca.Year,
            Quarter = ca.Quarter,
            Amount = ca.Amount,
            PaidAmount = ca.PaidAmount,
            IsPaid = ca.IsPaid,
            DueDate = ca.DueDate
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChargeAssignmentDto>> GetById(Guid id)
    {
        var ca = await _db.ChargeAssignments
            .Include(x => x.ChargeDefinition)
            .Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ca == null) return NotFound();

        return Ok(new ChargeAssignmentDto
        {
            Id = ca.Id,
            ChargeDefinitionId = ca.ChargeDefinitionId,
            ChargeDefinitionName = ca.ChargeDefinition.Name,
            UnitId = ca.UnitId,
            UnitReference = ca.Unit.Reference,
            Year = ca.Year,
            Quarter = ca.Quarter,
            Amount = ca.Amount,
            PaidAmount = ca.PaidAmount,
            IsPaid = ca.IsPaid,
            DueDate = ca.DueDate
        });
    }

    [HttpPost]
    public async Task<ActionResult<ChargeAssignmentDto>> Create([FromBody] CreateChargeAssignmentRequest request)
    {
        var entity = new ChargeAssignment
        {
            Id = Guid.NewGuid(),
            ChargeDefinitionId = request.ChargeDefinitionId,
            UnitId = request.UnitId,
            Year = request.Year,
            Quarter = request.Quarter,
            Amount = request.Amount,
            DueDate = request.DueDate
        };

        _db.ChargeAssignments.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new ChargeAssignmentDto
        {
            Id = entity.Id,
            ChargeDefinitionId = entity.ChargeDefinitionId,
            UnitId = entity.UnitId,
            Year = entity.Year,
            Quarter = entity.Quarter,
            Amount = entity.Amount,
            DueDate = entity.DueDate
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateChargeAssignmentRequest request)
    {
        var entity = await _db.ChargeAssignments.FindAsync(id);
        if (entity == null) return NotFound();

        entity.ChargeDefinitionId = request.ChargeDefinitionId;
        entity.UnitId = request.UnitId;
        entity.Year = request.Year;
        entity.Quarter = request.Quarter;
        entity.Amount = request.Amount;
        entity.DueDate = request.DueDate;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.ChargeAssignments.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
