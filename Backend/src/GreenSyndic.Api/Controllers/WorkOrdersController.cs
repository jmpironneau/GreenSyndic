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
public class WorkOrdersController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public WorkOrdersController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkOrderDto>>> GetAll(
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? incidentId,
        [FromQuery] WorkOrderStatus? status)
    {
        var query = _db.WorkOrders
            .Include(wo => wo.Supplier)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(wo => wo.SupplierId == supplierId.Value);

        if (incidentId.HasValue)
            query = query.Where(wo => wo.IncidentId == incidentId.Value);

        if (status.HasValue)
            query = query.Where(wo => wo.Status == status.Value);

        var items = await query.Select(wo => new WorkOrderDto
        {
            Id = wo.Id,
            Reference = wo.Reference,
            Title = wo.Title,
            Description = wo.Description,
            Status = wo.Status,
            SupplierId = wo.SupplierId,
            SupplierName = wo.Supplier != null ? wo.Supplier.Name : null,
            IncidentId = wo.IncidentId,
            EstimatedCost = wo.EstimatedCost,
            ActualCost = wo.ActualCost,
            ScheduledDate = wo.ScheduledDate,
            CompletedDate = wo.CompletedDate
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkOrderDto>> GetById(Guid id)
    {
        var wo = await _db.WorkOrders
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (wo == null) return NotFound();

        return Ok(new WorkOrderDto
        {
            Id = wo.Id,
            Reference = wo.Reference,
            Title = wo.Title,
            Description = wo.Description,
            Status = wo.Status,
            SupplierId = wo.SupplierId,
            SupplierName = wo.Supplier?.Name,
            IncidentId = wo.IncidentId,
            EstimatedCost = wo.EstimatedCost,
            ActualCost = wo.ActualCost,
            ScheduledDate = wo.ScheduledDate,
            CompletedDate = wo.CompletedDate
        });
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderDto>> Create([FromBody] CreateWorkOrderRequest request)
    {
        var reference = $"WO-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}";

        Guid organizationId = Guid.Empty;

        if (request.IncidentId.HasValue)
        {
            var incident = await _db.Incidents.FindAsync(request.IncidentId.Value);
            if (incident != null) organizationId = incident.OrganizationId;
        }
        else if (request.BuildingId.HasValue)
        {
            var building = await _db.Buildings.FindAsync(request.BuildingId.Value);
            if (building != null) organizationId = building.OrganizationId;
        }
        else if (request.UnitId.HasValue)
        {
            var unit = await _db.Units.FindAsync(request.UnitId.Value);
            if (unit != null) organizationId = unit.OrganizationId;
        }

        var entity = new WorkOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Reference = reference,
            Title = request.Title,
            Description = request.Description,
            SupplierId = request.SupplierId,
            IncidentId = request.IncidentId,
            BuildingId = request.BuildingId,
            UnitId = request.UnitId,
            EstimatedCost = request.EstimatedCost,
            ScheduledDate = request.ScheduledDate
        };

        _db.WorkOrders.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new WorkOrderDto
        {
            Id = entity.Id,
            Reference = entity.Reference,
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status,
            SupplierId = entity.SupplierId,
            IncidentId = entity.IncidentId,
            EstimatedCost = entity.EstimatedCost,
            ScheduledDate = entity.ScheduledDate
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateWorkOrderRequest request)
    {
        var entity = await _db.WorkOrders.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.SupplierId = request.SupplierId;
        entity.IncidentId = request.IncidentId;
        entity.BuildingId = request.BuildingId;
        entity.UnitId = request.UnitId;
        entity.EstimatedCost = request.EstimatedCost;
        entity.ScheduledDate = request.ScheduledDate;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var entity = await _db.WorkOrders.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = WorkOrderStatus.Completed;
        entity.CompletedDate = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.WorkOrders.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
