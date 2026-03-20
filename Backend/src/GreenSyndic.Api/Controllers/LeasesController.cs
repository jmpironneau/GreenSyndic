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
public class LeasesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public LeasesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaseDto>>> GetAll(
        [FromQuery] Guid? unitId,
        [FromQuery] Guid? leaseTenantId,
        [FromQuery] LeaseStatus? status,
        [FromQuery] LeaseType? type)
    {
        var query = _db.Leases
            .Include(l => l.Unit)
            .Include(l => l.LeaseTenant)
            .AsQueryable();

        if (unitId.HasValue)
            query = query.Where(l => l.UnitId == unitId.Value);

        if (leaseTenantId.HasValue)
            query = query.Where(l => l.LeaseTenantId == leaseTenantId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (type.HasValue)
            query = query.Where(l => l.Type == type.Value);

        var items = await query.Select(l => new LeaseDto
        {
            Id = l.Id,
            Reference = l.Reference,
            UnitId = l.UnitId,
            UnitReference = l.Unit.Reference,
            LeaseTenantId = l.LeaseTenantId,
            TenantName = l.LeaseTenant.FirstName + " " + l.LeaseTenant.LastName,
            Type = l.Type,
            Status = l.Status,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            DurationMonths = l.DurationMonths,
            MonthlyRent = l.MonthlyRent,
            Charges = l.Charges,
            SecurityDeposit = l.SecurityDeposit,
            NextRevisionDate = l.NextRevisionDate,
            TurnoverRentPercent = l.TurnoverRentPercent,
            MarketingContribution = l.MarketingContribution
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaseDto>> GetById(Guid id)
    {
        var l = await _db.Leases
            .Include(x => x.Unit)
            .Include(x => x.LeaseTenant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (l == null) return NotFound();

        return Ok(new LeaseDto
        {
            Id = l.Id,
            Reference = l.Reference,
            UnitId = l.UnitId,
            UnitReference = l.Unit.Reference,
            LeaseTenantId = l.LeaseTenantId,
            TenantName = l.LeaseTenant.FirstName + " " + l.LeaseTenant.LastName,
            Type = l.Type,
            Status = l.Status,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            DurationMonths = l.DurationMonths,
            MonthlyRent = l.MonthlyRent,
            Charges = l.Charges,
            SecurityDeposit = l.SecurityDeposit,
            NextRevisionDate = l.NextRevisionDate,
            TurnoverRentPercent = l.TurnoverRentPercent,
            MarketingContribution = l.MarketingContribution
        });
    }

    [HttpPost]
    public async Task<ActionResult<LeaseDto>> Create([FromBody] CreateLeaseRequest request)
    {
        var unit = await _db.Units.FindAsync(request.UnitId);
        if (unit == null) return BadRequest("Unit not found.");

        var reference = $"BAIL-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}";

        var entity = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = unit.OrganizationId,
            UnitId = request.UnitId,
            LeaseTenantId = request.LeaseTenantId,
            Reference = reference,
            Type = request.Type,
            Status = LeaseStatus.Draft,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DurationMonths = request.DurationMonths,
            MonthlyRent = request.MonthlyRent,
            Charges = request.Charges,
            SecurityDeposit = request.SecurityDeposit,
            AgencyFee = request.AgencyFee,
            NextRevisionDate = request.NextRevisionDate,
            RevisionIndexPercent = request.RevisionIndexPercent,
            TurnoverRentPercent = request.TurnoverRentPercent,
            MarketingContribution = request.MarketingContribution,
            Notes = request.Notes
        };

        _db.Leases.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new LeaseDto
        {
            Id = entity.Id,
            Reference = entity.Reference,
            UnitId = entity.UnitId,
            LeaseTenantId = entity.LeaseTenantId,
            Type = entity.Type,
            Status = entity.Status,
            StartDate = entity.StartDate,
            MonthlyRent = entity.MonthlyRent
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateLeaseRequest request)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.UnitId = request.UnitId;
        entity.LeaseTenantId = request.LeaseTenantId;
        entity.Type = request.Type;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.DurationMonths = request.DurationMonths;
        entity.MonthlyRent = request.MonthlyRent;
        entity.Charges = request.Charges;
        entity.SecurityDeposit = request.SecurityDeposit;
        entity.AgencyFee = request.AgencyFee;
        entity.NextRevisionDate = request.NextRevisionDate;
        entity.RevisionIndexPercent = request.RevisionIndexPercent;
        entity.TurnoverRentPercent = request.TurnoverRentPercent;
        entity.MarketingContribution = request.MarketingContribution;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = LeaseStatus.Active;
        entity.UpdatedAt = DateTime.UtcNow;

        var unit = await _db.Units.FindAsync(entity.UnitId);
        if (unit != null)
        {
            unit.Status = UnitStatus.Occupied;
            unit.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/terminate")]
    public async Task<IActionResult> Terminate(Guid id)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = LeaseStatus.Terminated;
        entity.UpdatedAt = DateTime.UtcNow;

        var unit = await _db.Units.FindAsync(entity.UnitId);
        if (unit != null)
        {
            unit.Status = UnitStatus.Vacant;
            unit.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
