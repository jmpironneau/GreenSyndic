using System.Security.Claims;
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
public class IncidentsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public IncidentsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<IncidentDto>>> GetAll(
        [FromQuery] Guid? unitId,
        [FromQuery] Guid? buildingId,
        [FromQuery] IncidentStatus? status,
        [FromQuery] IncidentPriority? priority)
    {
        var query = _db.Incidents
            .Include(i => i.Unit)
            .Include(i => i.Building)
            .Include(i => i.WorkOrders)
            .AsQueryable();

        if (unitId.HasValue)
            query = query.Where(i => i.UnitId == unitId.Value);

        if (buildingId.HasValue)
            query = query.Where(i => i.BuildingId == buildingId.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(i => i.Priority == priority.Value);

        var items = await query.Select(i => new IncidentDto
        {
            Id = i.Id,
            Title = i.Title,
            Description = i.Description,
            Priority = i.Priority,
            Status = i.Status,
            Category = i.Category,
            UnitId = i.UnitId,
            UnitReference = i.Unit != null ? i.Unit.Reference : null,
            BuildingId = i.BuildingId,
            BuildingName = i.Building != null ? i.Building.Name : null,
            ReportedByUserId = i.ReportedByUserId,
            AssignedToUserId = i.AssignedToUserId,
            CreatedAt = i.CreatedAt,
            ResolvedAt = i.ResolvedAt,
            WorkOrderCount = i.WorkOrders.Count
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentDto>> GetById(Guid id)
    {
        var i = await _db.Incidents
            .Include(x => x.Unit)
            .Include(x => x.Building)
            .Include(x => x.WorkOrders)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (i == null) return NotFound();

        return Ok(new IncidentDto
        {
            Id = i.Id,
            Title = i.Title,
            Description = i.Description,
            Priority = i.Priority,
            Status = i.Status,
            Category = i.Category,
            UnitId = i.UnitId,
            UnitReference = i.Unit?.Reference,
            BuildingId = i.BuildingId,
            BuildingName = i.Building?.Name,
            ReportedByUserId = i.ReportedByUserId,
            AssignedToUserId = i.AssignedToUserId,
            CreatedAt = i.CreatedAt,
            ResolvedAt = i.ResolvedAt,
            WorkOrderCount = i.WorkOrders.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<IncidentDto>> Create([FromBody] CreateIncidentRequest request)
    {
        var orgClaim = User.FindFirst("organizationId")?.Value;
        Guid organizationId = Guid.TryParse(orgClaim, out var oid) ? oid : Guid.Empty;

        if (request.UnitId.HasValue)
        {
            var unit = await _db.Units.FindAsync(request.UnitId.Value);
            if (unit != null) organizationId = unit.OrganizationId;
        }
        else if (request.BuildingId.HasValue)
        {
            var building = await _db.Buildings.FindAsync(request.BuildingId.Value);
            if (building != null) organizationId = building.OrganizationId;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var entity = new Incident
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Category = request.Category,
            UnitId = request.UnitId,
            BuildingId = request.BuildingId,
            ReportedByUserId = userId
        };

        _db.Incidents.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new IncidentDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Priority = entity.Priority,
            Status = entity.Status,
            Category = entity.Category,
            UnitId = entity.UnitId,
            BuildingId = entity.BuildingId,
            ReportedByUserId = entity.ReportedByUserId,
            CreatedAt = entity.CreatedAt,
            WorkOrderCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateIncidentRequest request)
    {
        var entity = await _db.Incidents.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Priority = request.Priority;
        entity.Category = request.Category;
        entity.UnitId = request.UnitId;
        entity.BuildingId = request.BuildingId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignIncidentRequest request)
    {
        var entity = await _db.Incidents.FindAsync(id);
        if (entity == null) return NotFound();

        entity.AssignedToUserId = request.AssignedToUserId;
        entity.Status = IncidentStatus.InProgress;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var entity = await _db.Incidents.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = IncidentStatus.Resolved;
        entity.ResolvedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Acknowledge an incident (syndic marks it as received).
    /// </summary>
    [HttpPut("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id)
    {
        var entity = await _db.Incidents.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != IncidentStatus.Reported)
            return BadRequest("Only reported incidents can be acknowledged");

        entity.Status = IncidentStatus.Acknowledged;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Close a resolved incident (final status).
    /// </summary>
    [HttpPut("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        var entity = await _db.Incidents.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != IncidentStatus.Resolved)
            return BadRequest("Only resolved incidents can be closed");

        entity.Status = IncidentStatus.Closed;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Reject an incident (not actionable).
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectIncidentRequest? request)
    {
        var entity = await _db.Incidents.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = IncidentStatus.Rejected;
        entity.ResolutionNotes = request?.Reason;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Get incident statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats([FromQuery] Guid? organizationId)
    {
        var query = _db.Incidents.AsQueryable();
        if (organizationId.HasValue)
            query = query.Where(i => i.OrganizationId == organizationId.Value);

        var all = await query.ToListAsync();

        var byStatus = all.GroupBy(i => i.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        var byPriority = all.GroupBy(i => i.Priority)
            .Select(g => new { priority = g.Key.ToString(), count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        var byCategory = all.GroupBy(i => i.Category ?? "Non catégorisé")
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        return Ok(new
        {
            total = all.Count,
            open = all.Count(i => i.Status is IncidentStatus.Reported or IncidentStatus.Acknowledged or IncidentStatus.InProgress),
            resolved = all.Count(i => i.Status == IncidentStatus.Resolved),
            closed = all.Count(i => i.Status == IncidentStatus.Closed),
            avgResolutionDays = all.Where(i => i.ResolvedAt.HasValue)
                .Select(i => (i.ResolvedAt!.Value - i.CreatedAt).TotalDays)
                .DefaultIfEmpty(0)
                .Average(),
            byStatus,
            byPriority,
            byCategory
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Incidents.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class RejectIncidentRequest
{
    public string? Reason { get; set; }
}

public class AssignIncidentRequest
{
    public string AssignedToUserId { get; set; } = default!;
}
