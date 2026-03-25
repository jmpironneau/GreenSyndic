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
public class ResolutionTemplatesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public ResolutionTemplatesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResolutionTemplateDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? category,
        [FromQuery] bool? isActive)
    {
        var query = _db.ResolutionTemplates.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(t => t.OrganizationId == organizationId.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var items = await query.OrderBy(t => t.Category).ThenBy(t => t.Code)
            .Select(t => new ResolutionTemplateDto
            {
                Id = t.Id,
                OrganizationId = t.OrganizationId,
                Code = t.Code,
                Title = t.Title,
                Description = t.Description,
                DefaultMajority = t.DefaultMajority,
                Category = t.Category,
                LegalReference = t.LegalReference,
                TemplateText = t.TemplateText,
                IsActive = t.IsActive
            }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResolutionTemplateDto>> GetById(Guid id)
    {
        var t = await _db.ResolutionTemplates.FindAsync(id);
        if (t == null) return NotFound();

        return Ok(new ResolutionTemplateDto
        {
            Id = t.Id,
            OrganizationId = t.OrganizationId,
            Code = t.Code,
            Title = t.Title,
            Description = t.Description,
            DefaultMajority = t.DefaultMajority,
            Category = t.Category,
            LegalReference = t.LegalReference,
            TemplateText = t.TemplateText,
            IsActive = t.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<ResolutionTemplateDto>> Create(
        [FromBody] CreateResolutionTemplateRequest request)
    {
        var entity = new ResolutionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Code = request.Code,
            Title = request.Title,
            Description = request.Description,
            DefaultMajority = request.DefaultMajority,
            Category = request.Category,
            LegalReference = request.LegalReference,
            TemplateText = request.TemplateText
        };

        _db.ResolutionTemplates.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new ResolutionTemplateDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Code = entity.Code,
            Title = entity.Title,
            Description = entity.Description,
            DefaultMajority = entity.DefaultMajority,
            Category = entity.Category,
            LegalReference = entity.LegalReference,
            TemplateText = entity.TemplateText,
            IsActive = entity.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateResolutionTemplateRequest request)
    {
        var entity = await _db.ResolutionTemplates.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Code = request.Code;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.DefaultMajority = request.DefaultMajority;
        entity.Category = request.Category;
        entity.LegalReference = request.LegalReference;
        entity.TemplateText = request.TemplateText;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.ResolutionTemplates.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
