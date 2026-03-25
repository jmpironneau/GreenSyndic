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
public class LeaseRevisionsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public LeaseRevisionsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaseRevisionDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? leaseId,
        [FromQuery] RevisionStatus? status,
        [FromQuery] RevisionType? type)
    {
        var query = _db.LeaseRevisions
            .Include(r => r.Lease).ThenInclude(l => l.LeaseTenant)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(r => r.OrganizationId == organizationId.Value);
        if (leaseId.HasValue)
            query = query.Where(r => r.LeaseId == leaseId.Value);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        var items = await query.OrderByDescending(r => r.EffectiveDate)
            .Select(r => MapToDto(r)).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaseRevisionDto>> GetById(Guid id)
    {
        var r = await _db.LeaseRevisions
            .Include(x => x.Lease).ThenInclude(l => l.LeaseTenant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();
        return Ok(MapToDto(r));
    }

    [HttpPost]
    public async Task<ActionResult<LeaseRevisionDto>> Create([FromBody] CreateLeaseRevisionRequest request)
    {
        var lease = await _db.Leases
            .Include(l => l.LeaseTenant)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId);

        if (lease == null) return BadRequest("Lease not found.");

        var previousRent = lease.MonthlyRent;
        var variationPercent = previousRent > 0
            ? (request.NewRent - previousRent) / previousRent * 100m
            : 0;

        // Auto-detect legal basis if not provided
        var legalBasis = request.LegalBasis;
        if (string.IsNullOrEmpty(legalBasis))
        {
            legalBasis = lease.Type switch
            {
                LeaseType.Residential => "CCH art. 423-424",
                LeaseType.Commercial => "AUDCG art. 116",
                _ => null
            };
        }

        var entity = new LeaseRevision
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            LeaseId = request.LeaseId,
            Reference = $"REV-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Type = request.Type,
            Status = RevisionStatus.Pending,
            EffectiveDate = request.EffectiveDate,
            PreviousRent = previousRent,
            NewRent = request.NewRent,
            VariationPercent = Math.Round(variationPercent, 2),
            IndexName = request.IndexName,
            IndexValueOld = request.IndexValueOld,
            IndexValueNew = request.IndexValueNew,
            LegalBasis = legalBasis,
            Justification = request.Justification,
            Notes = request.Notes
        };

        _db.LeaseRevisions.Add(entity);
        await _db.SaveChangesAsync();

        entity.Lease = lease;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Notify tenant of revision.
    /// </summary>
    [HttpPost("{id:guid}/notify")]
    public async Task<IActionResult> Notify(Guid id)
    {
        var entity = await _db.LeaseRevisions.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RevisionStatus.Pending)
            return BadRequest("Only pending revisions can be notified.");

        entity.Status = RevisionStatus.Notified;
        entity.NotificationDate = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Tenant responds to revision (accept or contest).
    /// </summary>
    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondRevisionRequest request)
    {
        var entity = await _db.LeaseRevisions.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != RevisionStatus.Notified)
            return BadRequest("Only notified revisions can be responded to.");

        if (request.Accepted)
        {
            entity.Status = RevisionStatus.Accepted;
            entity.AcceptedAt = DateTime.UtcNow;
        }
        else
        {
            entity.Status = RevisionStatus.Contested;
            entity.ContestedAt = DateTime.UtcNow;
            entity.ContestationReason = request.ContestationReason;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Apply the revision: update the lease's MonthlyRent and NextRevisionDate.
    /// </summary>
    [HttpPost("{id:guid}/apply")]
    public async Task<IActionResult> Apply(Guid id)
    {
        var entity = await _db.LeaseRevisions
            .Include(r => r.Lease)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity == null) return NotFound();

        if (entity.Status != RevisionStatus.Accepted)
            return BadRequest("Only accepted revisions can be applied.");

        // Update lease
        entity.Lease.MonthlyRent = entity.NewRent;
        entity.Lease.NextRevisionDate = entity.EffectiveDate.AddYears(3); // Triennale
        entity.Lease.UpdatedAt = DateTime.UtcNow;

        entity.Status = RevisionStatus.Applied;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var entity = await _db.LeaseRevisions.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RevisionStatus.Applied)
            return BadRequest("Cannot cancel an applied revision.");

        entity.Status = RevisionStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.LeaseRevisions.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == RevisionStatus.Applied)
            return BadRequest("Cannot delete an applied revision.");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static LeaseRevisionDto MapToDto(LeaseRevision r) => new()
    {
        Id = r.Id,
        OrganizationId = r.OrganizationId,
        LeaseId = r.LeaseId,
        LeaseReference = r.Lease?.Reference,
        TenantName = r.Lease?.LeaseTenant != null
            ? r.Lease.LeaseTenant.FirstName + " " + r.Lease.LeaseTenant.LastName : null,
        Reference = r.Reference,
        Type = r.Type,
        Status = r.Status,
        EffectiveDate = r.EffectiveDate,
        NotificationDate = r.NotificationDate,
        PreviousRent = r.PreviousRent,
        NewRent = r.NewRent,
        VariationPercent = r.VariationPercent,
        IndexName = r.IndexName,
        IndexValueOld = r.IndexValueOld,
        IndexValueNew = r.IndexValueNew,
        LegalBasis = r.LegalBasis,
        Justification = r.Justification,
        AcceptedAt = r.AcceptedAt,
        ContestedAt = r.ContestedAt,
        ContestationReason = r.ContestationReason,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt
    };
}
