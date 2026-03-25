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
public class MessageTemplatesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public MessageTemplatesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MessageTemplateDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] MessageChannel? channel,
        [FromQuery] string? category,
        [FromQuery] bool? isActive)
    {
        var query = _db.MessageTemplates.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(t => t.OrganizationId == organizationId.Value);

        if (channel.HasValue)
            query = query.Where(t => t.Channel == channel.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var items = await query.OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Select(t => new MessageTemplateDto
            {
                Id = t.Id,
                OrganizationId = t.OrganizationId,
                Code = t.Code,
                Name = t.Name,
                Channel = t.Channel,
                Subject = t.Subject,
                Body = t.Body,
                Category = t.Category,
                AvailableVariables = t.AvailableVariables,
                IsActive = t.IsActive
            }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MessageTemplateDto>> GetById(Guid id)
    {
        var t = await _db.MessageTemplates.FindAsync(id);
        if (t == null) return NotFound();

        return Ok(new MessageTemplateDto
        {
            Id = t.Id,
            OrganizationId = t.OrganizationId,
            Code = t.Code,
            Name = t.Name,
            Channel = t.Channel,
            Subject = t.Subject,
            Body = t.Body,
            Category = t.Category,
            AvailableVariables = t.AvailableVariables,
            IsActive = t.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<MessageTemplateDto>> Create(
        [FromBody] CreateMessageTemplateRequest request)
    {
        var entity = new MessageTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Code = request.Code,
            Name = request.Name,
            Channel = request.Channel,
            Subject = request.Subject,
            Body = request.Body,
            Category = request.Category,
            AvailableVariables = request.AvailableVariables
        };

        _db.MessageTemplates.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new MessageTemplateDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Code = entity.Code,
            Name = entity.Name,
            Channel = entity.Channel,
            Subject = entity.Subject,
            Body = entity.Body,
            Category = entity.Category,
            AvailableVariables = entity.AvailableVariables,
            IsActive = entity.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMessageTemplateRequest request)
    {
        var entity = await _db.MessageTemplates.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.Channel = request.Channel;
        entity.Subject = request.Subject;
        entity.Body = request.Body;
        entity.Category = request.Category;
        entity.AvailableVariables = request.AvailableVariables;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Preview a template with sample merge data.
    /// </summary>
    [HttpPost("{id:guid}/preview")]
    public async Task<ActionResult<object>> Preview(Guid id,
        [FromBody] Dictionary<string, string> mergeData)
    {
        var template = await _db.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        var subject = template.Subject ?? "";
        var body = template.Body;

        foreach (var kvp in mergeData)
        {
            var placeholder = "{{" + kvp.Key + "}}";
            subject = subject.Replace(placeholder, kvp.Value);
            body = body.Replace(placeholder, kvp.Value);
        }

        return Ok(new { Subject = subject, Body = body });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.MessageTemplates.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
