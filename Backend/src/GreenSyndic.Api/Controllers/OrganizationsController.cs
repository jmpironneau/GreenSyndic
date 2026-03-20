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
public class OrganizationsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public OrganizationsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrganizationDto>>> GetAll()
    {
        var items = await _db.Organizations
            .Where(o => o.IsActive)
            .Select(o => new OrganizationDto
            {
                Id = o.Id,
                Name = o.Name,
                LegalName = o.LegalName,
                Country = o.Country,
                Currency = o.Currency,
                Address = o.Address,
                City = o.City,
                Phone = o.Phone,
                Email = o.Email,
                IsActive = o.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationDto>> GetById(Guid id)
    {
        var o = await _db.Organizations.FindAsync(id);
        if (o == null) return NotFound();

        return Ok(new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            LegalName = o.LegalName,
            Country = o.Country,
            Currency = o.Currency,
            Address = o.Address,
            City = o.City,
            Phone = o.Phone,
            Email = o.Email,
            IsActive = o.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] CreateOrganizationRequest request)
    {
        var entity = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            LegalName = request.LegalName,
            Country = request.Country,
            Currency = request.Currency,
            Address = request.Address,
            City = request.City,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = true
        };

        _db.Organizations.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new OrganizationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalName = entity.LegalName,
            Country = entity.Country,
            Currency = entity.Currency,
            Address = entity.Address,
            City = entity.City,
            Phone = entity.Phone,
            Email = entity.Email,
            IsActive = entity.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateOrganizationRequest request)
    {
        var entity = await _db.Organizations.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = request.Name;
        entity.LegalName = request.LegalName;
        entity.Country = request.Country;
        entity.Currency = request.Currency;
        entity.Address = request.Address;
        entity.City = request.City;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Organizations.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
