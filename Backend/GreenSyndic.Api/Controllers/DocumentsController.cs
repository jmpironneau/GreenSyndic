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
public class DocumentsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public DocumentsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] DocumentCategory? category,
        [FromQuery] Guid? unitId,
        [FromQuery] Guid? buildingId,
        [FromQuery] Guid? leaseId,
        [FromQuery] Guid? meetingId,
        [FromQuery] Guid? incidentId,
        [FromQuery] Guid? workOrderId)
    {
        var query = _db.Documents.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(d => d.OrganizationId == organizationId.Value);

        if (category.HasValue)
            query = query.Where(d => d.Category == category.Value);

        if (unitId.HasValue)
            query = query.Where(d => d.UnitId == unitId.Value);

        if (buildingId.HasValue)
            query = query.Where(d => d.BuildingId == buildingId.Value);

        if (leaseId.HasValue)
            query = query.Where(d => d.LeaseId == leaseId.Value);

        if (meetingId.HasValue)
            query = query.Where(d => d.MeetingId == meetingId.Value);

        if (incidentId.HasValue)
            query = query.Where(d => d.IncidentId == incidentId.Value);

        if (workOrderId.HasValue)
            query = query.Where(d => d.WorkOrderId == workOrderId.Value);

        var items = await query.OrderByDescending(d => d.CreatedAt).Select(d => new DocumentDto
        {
            Id = d.Id,
            OrganizationId = d.OrganizationId,
            FileName = d.FileName,
            DisplayName = d.DisplayName,
            ContentType = d.ContentType,
            SizeBytes = d.SizeBytes,
            StoragePath = d.StoragePath,
            Category = d.Category,
            Description = d.Description,
            UnitId = d.UnitId,
            BuildingId = d.BuildingId,
            LeaseId = d.LeaseId,
            MeetingId = d.MeetingId,
            IncidentId = d.IncidentId,
            WorkOrderId = d.WorkOrderId,
            CreatedAt = d.CreatedAt
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id)
    {
        var d = await _db.Documents.FirstOrDefaultAsync(x => x.Id == id);

        if (d == null) return NotFound();

        return Ok(new DocumentDto
        {
            Id = d.Id,
            OrganizationId = d.OrganizationId,
            FileName = d.FileName,
            DisplayName = d.DisplayName,
            ContentType = d.ContentType,
            SizeBytes = d.SizeBytes,
            StoragePath = d.StoragePath,
            Category = d.Category,
            Description = d.Description,
            UnitId = d.UnitId,
            BuildingId = d.BuildingId,
            LeaseId = d.LeaseId,
            MeetingId = d.MeetingId,
            IncidentId = d.IncidentId,
            WorkOrderId = d.WorkOrderId,
            CreatedAt = d.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Create([FromBody] CreateDocumentRequest request)
    {
        var entity = new Document
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            FileName = request.FileName,
            DisplayName = request.DisplayName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            StoragePath = request.StoragePath,
            Category = request.Category,
            Description = request.Description,
            UnitId = request.UnitId,
            BuildingId = request.BuildingId,
            LeaseId = request.LeaseId,
            MeetingId = request.MeetingId,
            IncidentId = request.IncidentId,
            WorkOrderId = request.WorkOrderId
        };

        _db.Documents.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new DocumentDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            FileName = entity.FileName,
            DisplayName = entity.DisplayName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            StoragePath = entity.StoragePath,
            Category = entity.Category,
            Description = entity.Description,
            UnitId = entity.UnitId,
            BuildingId = entity.BuildingId,
            LeaseId = entity.LeaseId,
            MeetingId = entity.MeetingId,
            IncidentId = entity.IncidentId,
            WorkOrderId = entity.WorkOrderId,
            CreatedAt = entity.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateDocumentRequest request)
    {
        var entity = await _db.Documents.FindAsync(id);
        if (entity == null) return NotFound();

        entity.DisplayName = request.DisplayName;
        entity.Category = request.Category;
        entity.Description = request.Description;
        entity.UnitId = request.UnitId;
        entity.BuildingId = request.BuildingId;
        entity.LeaseId = request.LeaseId;
        entity.MeetingId = request.MeetingId;
        entity.IncidentId = request.IncidentId;
        entity.WorkOrderId = request.WorkOrderId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Documents.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
