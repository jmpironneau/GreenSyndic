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
public class SuppliersController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public SuppliersController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? specialty)
    {
        var query = _db.Suppliers.Include(s => s.WorkOrders).AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(s => s.OrganizationId == organizationId.Value);

        if (!string.IsNullOrEmpty(specialty))
            query = query.Where(s => s.Specialty == specialty);

        var items = await query.Select(s => new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Email = s.Email,
            Phone = s.Phone,
            Specialty = s.Specialty,
            IsActive = s.IsActive,
            WorkOrderCount = s.WorkOrders.Count
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id)
    {
        var s = await _db.Suppliers
            .Include(x => x.WorkOrders)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (s == null) return NotFound();

        return Ok(new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Email = s.Email,
            Phone = s.Phone,
            Specialty = s.Specialty,
            IsActive = s.IsActive,
            WorkOrderCount = s.WorkOrders.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request, [FromQuery] Guid organizationId)
    {
        var entity = new Supplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            TaxId = request.TaxId,
            Specialty = request.Specialty,
            BankDetails = request.BankDetails
        };

        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new SupplierDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ContactPerson = entity.ContactPerson,
            Email = entity.Email,
            Phone = entity.Phone,
            Specialty = entity.Specialty,
            IsActive = entity.IsActive,
            WorkOrderCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateSupplierRequest request)
    {
        var entity = await _db.Suppliers.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = request.Name;
        entity.ContactPerson = request.ContactPerson;
        entity.Email = request.Email;
        entity.Phone = request.Phone;
        entity.Address = request.Address;
        entity.TaxId = request.TaxId;
        entity.Specialty = request.Specialty;
        entity.BankDetails = request.BankDetails;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Suppliers.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
