using GreenSyndic.Core.Entities;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Services.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

/// <summary>
/// Veille juridique — base de références légales OHADA, CCH, décrets.
/// Shared knowledge base, not per-organization.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LegalReferencesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public LegalReferencesController(GreenSyndicDbContext db) => _db = db;

    /// <summary>
    /// List legal references with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<LegalReferenceDto>>> GetAll(
        [FromQuery] LegalDomain? domain,
        [FromQuery] string? search,
        [FromQuery] string? source)
    {
        var query = _db.LegalReferences.Where(l => l.IsActive).AsQueryable();

        if (domain.HasValue)
            query = query.Where(l => l.Domain == domain.Value);

        if (!string.IsNullOrEmpty(source))
            query = query.Where(l => l.Source == source);

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            query = query.Where(l =>
                l.Title.ToLower().Contains(s) ||
                l.Code.ToLower().Contains(s) ||
                l.Content.ToLower().Contains(s) ||
                (l.Tags != null && l.Tags.ToLower().Contains(s)));
        }

        var items = await query.OrderBy(l => l.Domain).ThenBy(l => l.Code)
            .Select(l => new LegalReferenceDto
            {
                Id = l.Id,
                Code = l.Code,
                Title = l.Title,
                Content = l.Content,
                Domain = l.Domain,
                Source = l.Source,
                Url = l.Url,
                EffectiveDate = l.EffectiveDate,
                IsActive = l.IsActive,
                Tags = l.Tags,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt
            }).ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Get a single legal reference by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LegalReferenceDto>> GetById(Guid id)
    {
        var l = await _db.LegalReferences.FindAsync(id);
        if (l == null) return NotFound();

        return Ok(new LegalReferenceDto
        {
            Id = l.Id,
            Code = l.Code,
            Title = l.Title,
            Content = l.Content,
            Domain = l.Domain,
            Source = l.Source,
            Url = l.Url,
            EffectiveDate = l.EffectiveDate,
            IsActive = l.IsActive,
            Tags = l.Tags,
            Notes = l.Notes,
            CreatedAt = l.CreatedAt
        });
    }

    /// <summary>
    /// Create a new legal reference.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LegalReferenceDto>> Create([FromBody] CreateLegalReferenceRequest request)
    {
        // Check unique code
        var exists = await _db.LegalReferences.AnyAsync(l => l.Code == request.Code);
        if (exists) return Conflict($"Code '{request.Code}' already exists");

        var entity = new LegalReference
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Title = request.Title,
            Content = request.Content,
            Domain = request.Domain,
            Source = request.Source,
            Url = request.Url,
            EffectiveDate = request.EffectiveDate,
            Tags = request.Tags,
            Notes = request.Notes
        };

        _db.LegalReferences.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new LegalReferenceDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Title = entity.Title,
            Content = entity.Content,
            Domain = entity.Domain,
            Source = entity.Source,
            Url = entity.Url,
            EffectiveDate = entity.EffectiveDate,
            IsActive = entity.IsActive,
            Tags = entity.Tags,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt
        });
    }

    /// <summary>
    /// Update a legal reference.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateLegalReferenceRequest request)
    {
        var entity = await _db.LegalReferences.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Code = request.Code;
        entity.Title = request.Title;
        entity.Content = request.Content;
        entity.Domain = request.Domain;
        entity.Source = request.Source;
        entity.Url = request.Url;
        entity.EffectiveDate = request.EffectiveDate;
        entity.Tags = request.Tags;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Soft-delete a legal reference.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.LegalReferences.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// List all available legal domains.
    /// </summary>
    [HttpGet("domains")]
    public ActionResult GetDomains()
    {
        var domains = Enum.GetValues<LegalDomain>()
            .Select(d => new { value = (int)d, name = d.ToString() })
            .ToList();
        return Ok(domains);
    }
}
