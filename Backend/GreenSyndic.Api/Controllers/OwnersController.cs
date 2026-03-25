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
public class OwnersController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public OwnersController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<OwnerDto>>> GetAll([FromQuery] Guid? organizationId)
    {
        var query = _db.Owners.Include(o => o.Units).AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(o => o.OrganizationId == organizationId.Value);

        var items = await query.Select(o => new OwnerDto
        {
            Id = o.Id,
            FirstName = o.FirstName,
            LastName = o.LastName,
            CompanyName = o.CompanyName,
            Email = o.Email,
            Phone = o.Phone,
            Address = o.Address,
            City = o.City,
            Country = o.Country,
            IsCouncilMember = o.IsCouncilMember,
            IsCouncilPresident = o.IsCouncilPresident,
            Balance = o.Balance,
            UnitCount = o.Units.Count
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OwnerDto>> GetById(Guid id)
    {
        var o = await _db.Owners.Include(x => x.Units).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound();

        return Ok(new OwnerDto
        {
            Id = o.Id,
            FirstName = o.FirstName,
            LastName = o.LastName,
            CompanyName = o.CompanyName,
            Email = o.Email,
            Phone = o.Phone,
            Address = o.Address,
            City = o.City,
            Country = o.Country,
            IsCouncilMember = o.IsCouncilMember,
            IsCouncilPresident = o.IsCouncilPresident,
            Balance = o.Balance,
            UnitCount = o.Units.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<OwnerDto>> Create([FromBody] CreateOwnerRequest request, [FromQuery] Guid organizationId)
    {
        var entity = new Owner
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            NationalId = request.NationalId,
            TaxId = request.TaxId
        };

        _db.Owners.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new OwnerDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            Phone = entity.Phone,
            UnitCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateOwnerRequest request)
    {
        var entity = await _db.Owners.FindAsync(id);
        if (entity == null) return NotFound();

        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.CompanyName = request.CompanyName;
        entity.Email = request.Email;
        entity.Phone = request.Phone;
        entity.Address = request.Address;
        entity.City = request.City;
        entity.Country = request.Country;
        entity.NationalId = request.NationalId;
        entity.TaxId = request.TaxId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Owners.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
