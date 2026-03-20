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
public class LeaseTenantsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public LeaseTenantsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaseTenantDto>>> GetAll([FromQuery] Guid? organizationId)
    {
        var query = _db.LeaseTenants.Include(t => t.Leases).AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(t => t.OrganizationId == organizationId.Value);

        var items = await query.Select(t => new LeaseTenantDto
        {
            Id = t.Id,
            FirstName = t.FirstName,
            LastName = t.LastName,
            CompanyName = t.CompanyName,
            TradeName = t.TradeName,
            Email = t.Email,
            Phone = t.Phone,
            Address = t.Address,
            ActiveLeaseCount = t.Leases.Count(l => l.Status == LeaseStatus.Active)
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaseTenantDto>> GetById(Guid id)
    {
        var t = await _db.LeaseTenants.Include(x => x.Leases).FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        return Ok(new LeaseTenantDto
        {
            Id = t.Id,
            FirstName = t.FirstName,
            LastName = t.LastName,
            CompanyName = t.CompanyName,
            TradeName = t.TradeName,
            Email = t.Email,
            Phone = t.Phone,
            Address = t.Address,
            ActiveLeaseCount = t.Leases.Count(l => l.Status == LeaseStatus.Active)
        });
    }

    [HttpPost]
    public async Task<ActionResult<LeaseTenantDto>> Create([FromBody] CreateLeaseTenantRequest request, [FromQuery] Guid organizationId)
    {
        var entity = new LeaseTenant
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            TradeName = request.TradeName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            NationalId = request.NationalId,
            TaxId = request.TaxId,
            EmergencyContact = request.EmergencyContact,
            EmergencyPhone = request.EmergencyPhone
        };

        _db.LeaseTenants.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new LeaseTenantDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            Phone = entity.Phone,
            ActiveLeaseCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateLeaseTenantRequest request)
    {
        var entity = await _db.LeaseTenants.FindAsync(id);
        if (entity == null) return NotFound();

        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.CompanyName = request.CompanyName;
        entity.TradeName = request.TradeName;
        entity.Email = request.Email;
        entity.Phone = request.Phone;
        entity.Address = request.Address;
        entity.NationalId = request.NationalId;
        entity.TaxId = request.TaxId;
        entity.EmergencyContact = request.EmergencyContact;
        entity.EmergencyPhone = request.EmergencyPhone;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.LeaseTenants.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
