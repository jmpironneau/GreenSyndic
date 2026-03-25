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
public class LeasesController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public LeasesController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaseDto>>> GetAll(
        [FromQuery] Guid? unitId,
        [FromQuery] Guid? leaseTenantId,
        [FromQuery] LeaseStatus? status,
        [FromQuery] LeaseType? type)
    {
        var query = _db.Leases
            .Include(l => l.Unit)
            .Include(l => l.LeaseTenant)
            .AsQueryable();

        if (unitId.HasValue)
            query = query.Where(l => l.UnitId == unitId.Value);

        if (leaseTenantId.HasValue)
            query = query.Where(l => l.LeaseTenantId == leaseTenantId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (type.HasValue)
            query = query.Where(l => l.Type == type.Value);

        var items = await query.Select(l => new LeaseDto
        {
            Id = l.Id,
            Reference = l.Reference,
            UnitId = l.UnitId,
            UnitReference = l.Unit.Reference,
            LeaseTenantId = l.LeaseTenantId,
            TenantName = l.LeaseTenant.FirstName + " " + l.LeaseTenant.LastName,
            Type = l.Type,
            Status = l.Status,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            DurationMonths = l.DurationMonths,
            MonthlyRent = l.MonthlyRent,
            Charges = l.Charges,
            SecurityDeposit = l.SecurityDeposit,
            NextRevisionDate = l.NextRevisionDate,
            TurnoverRentPercent = l.TurnoverRentPercent,
            MarketingContribution = l.MarketingContribution
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaseDto>> GetById(Guid id)
    {
        var l = await _db.Leases
            .Include(x => x.Unit)
            .Include(x => x.LeaseTenant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (l == null) return NotFound();

        return Ok(new LeaseDto
        {
            Id = l.Id,
            Reference = l.Reference,
            UnitId = l.UnitId,
            UnitReference = l.Unit.Reference,
            LeaseTenantId = l.LeaseTenantId,
            TenantName = l.LeaseTenant.FirstName + " " + l.LeaseTenant.LastName,
            Type = l.Type,
            Status = l.Status,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            DurationMonths = l.DurationMonths,
            MonthlyRent = l.MonthlyRent,
            Charges = l.Charges,
            SecurityDeposit = l.SecurityDeposit,
            NextRevisionDate = l.NextRevisionDate,
            TurnoverRentPercent = l.TurnoverRentPercent,
            MarketingContribution = l.MarketingContribution
        });
    }

    [HttpPost]
    public async Task<ActionResult<LeaseDto>> Create([FromBody] CreateLeaseRequest request)
    {
        var unit = await _db.Units.FindAsync(request.UnitId);
        if (unit == null) return BadRequest("Unit not found.");

        var reference = $"BAIL-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}";

        var entity = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = unit.OrganizationId,
            UnitId = request.UnitId,
            LeaseTenantId = request.LeaseTenantId,
            Reference = reference,
            Type = request.Type,
            Status = LeaseStatus.Draft,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DurationMonths = request.DurationMonths,
            MonthlyRent = request.MonthlyRent,
            Charges = request.Charges,
            SecurityDeposit = request.SecurityDeposit,
            AgencyFee = request.AgencyFee,
            NextRevisionDate = request.NextRevisionDate,
            RevisionIndexPercent = request.RevisionIndexPercent,
            TurnoverRentPercent = request.TurnoverRentPercent,
            MarketingContribution = request.MarketingContribution,
            Notes = request.Notes
        };

        _db.Leases.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new LeaseDto
        {
            Id = entity.Id,
            Reference = entity.Reference,
            UnitId = entity.UnitId,
            LeaseTenantId = entity.LeaseTenantId,
            Type = entity.Type,
            Status = entity.Status,
            StartDate = entity.StartDate,
            MonthlyRent = entity.MonthlyRent
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateLeaseRequest request)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.UnitId = request.UnitId;
        entity.LeaseTenantId = request.LeaseTenantId;
        entity.Type = request.Type;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.DurationMonths = request.DurationMonths;
        entity.MonthlyRent = request.MonthlyRent;
        entity.Charges = request.Charges;
        entity.SecurityDeposit = request.SecurityDeposit;
        entity.AgencyFee = request.AgencyFee;
        entity.NextRevisionDate = request.NextRevisionDate;
        entity.RevisionIndexPercent = request.RevisionIndexPercent;
        entity.TurnoverRentPercent = request.TurnoverRentPercent;
        entity.MarketingContribution = request.MarketingContribution;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = LeaseStatus.Active;
        entity.UpdatedAt = DateTime.UtcNow;

        var unit = await _db.Units.FindAsync(entity.UnitId);
        if (unit != null)
        {
            unit.Status = UnitStatus.Occupied;
            unit.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/terminate")]
    public async Task<IActionResult> Terminate(Guid id)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = LeaseStatus.Terminated;
        entity.UpdatedAt = DateTime.UtcNow;

        var unit = await _db.Units.FindAsync(entity.UnitId);
        if (unit != null)
        {
            unit.Status = UnitStatus.Vacant;
            unit.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Renew a lease (droit au renouvellement après 3 ans — AUDCG art. 123).
    /// Creates a new lease based on the existing one.
    /// </summary>
    [HttpPost("{id:guid}/renew")]
    public async Task<ActionResult<LeaseDto>> Renew(Guid id, [FromBody] LeaseRenewRequest request)
    {
        var entity = await _db.Leases
            .Include(l => l.Unit)
            .Include(l => l.LeaseTenant)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entity == null) return NotFound();

        if (entity.Status != LeaseStatus.Active && entity.Status != LeaseStatus.Expired)
            return BadRequest("Only active or expired leases can be renewed.");

        // Mark old lease as renewed
        entity.Status = LeaseStatus.Renewed;
        entity.UpdatedAt = DateTime.UtcNow;

        // Create new lease
        var newStartDate = request.NewStartDate ?? entity.EndDate?.AddDays(1) ?? DateTime.UtcNow;
        var newLease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = entity.OrganizationId,
            UnitId = entity.UnitId,
            LeaseTenantId = entity.LeaseTenantId,
            Reference = $"BAIL-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Type = entity.Type,
            Status = LeaseStatus.Active,
            StartDate = newStartDate,
            EndDate = request.NewDurationMonths.HasValue
                ? newStartDate.AddMonths(request.NewDurationMonths.Value) : null,
            DurationMonths = request.NewDurationMonths ?? entity.DurationMonths,
            MonthlyRent = request.NewMonthlyRent ?? entity.MonthlyRent,
            Charges = entity.Charges,
            SecurityDeposit = entity.SecurityDeposit,
            AgencyFee = entity.AgencyFee,
            NextRevisionDate = newStartDate.AddYears(3),
            RevisionIndexPercent = entity.RevisionIndexPercent,
            TurnoverRentPercent = entity.TurnoverRentPercent,
            MarketingContribution = entity.MarketingContribution,
            Notes = request.Notes ?? $"Renouvellement du bail {entity.Reference}"
        };

        _db.Leases.Add(newLease);
        await _db.SaveChangesAsync();

        newLease.Unit = entity.Unit;
        newLease.LeaseTenant = entity.LeaseTenant;

        return CreatedAtAction(nameof(GetById), new { id = newLease.Id }, new LeaseDto
        {
            Id = newLease.Id,
            Reference = newLease.Reference,
            UnitId = newLease.UnitId,
            UnitReference = newLease.Unit.Reference,
            LeaseTenantId = newLease.LeaseTenantId,
            TenantName = newLease.LeaseTenant.FirstName + " " + newLease.LeaseTenant.LastName,
            Type = newLease.Type,
            Status = newLease.Status,
            StartDate = newLease.StartDate,
            EndDate = newLease.EndDate,
            DurationMonths = newLease.DurationMonths,
            MonthlyRent = newLease.MonthlyRent,
            Charges = newLease.Charges,
            SecurityDeposit = newLease.SecurityDeposit,
            NextRevisionDate = newLease.NextRevisionDate,
            TurnoverRentPercent = newLease.TurnoverRentPercent,
            MarketingContribution = newLease.MarketingContribution
        });
    }

    /// <summary>
    /// Terminate a lease with reason and date (résiliation).
    /// </summary>
    [HttpPost("{id:guid}/terminate-with-details")]
    public async Task<IActionResult> TerminateWithDetails(Guid id, [FromBody] LeaseTerminateRequest request)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != LeaseStatus.Active)
            return BadRequest("Only active leases can be terminated.");

        entity.Status = LeaseStatus.Terminated;
        entity.EndDate = request.TerminationDate;
        entity.Notes = (entity.Notes ?? "") + $"\nRésiliation: {request.Reason}";
        entity.UpdatedAt = DateTime.UtcNow;

        var unit = await _db.Units.FindAsync(entity.UnitId);
        if (unit != null)
        {
            unit.Status = UnitStatus.Vacant;
            unit.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// CRG Bailleur — Compte-rendu de gestion pour le propriétaire/bailleur.
    /// </summary>
    [HttpGet("{id:guid}/landlord-statement")]
    public async Task<ActionResult<LandlordStatementDto>> GetLandlordStatement(
        Guid id,
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd)
    {
        var lease = await _db.Leases
            .Include(l => l.LeaseTenant)
            .Include(l => l.Unit)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lease == null) return NotFound();

        // Rent calls in the period
        var rentCalls = await _db.RentCalls
            .Where(r => r.LeaseId == id
                && r.PeriodStart >= periodStart
                && r.PeriodEnd <= periodEnd
                && r.Status != RentCallStatus.Cancelled)
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .ToListAsync();

        var rentLines = rentCalls.Select(r => new LandlordStatementLineDto
        {
            Year = r.Year,
            Month = r.Month,
            Label = $"Loyer {r.Month:D2}/{r.Year}",
            AmountDue = r.RentAmount,
            AmountPaid = r.PaidAmount > r.RentAmount ? r.RentAmount : r.PaidAmount,
            Balance = r.RentAmount - Math.Min(r.PaidAmount, r.RentAmount)
        }).ToList();

        var chargeLines = rentCalls.Select(r => new LandlordStatementLineDto
        {
            Year = r.Year,
            Month = r.Month,
            Label = $"Charges {r.Month:D2}/{r.Year}",
            AmountDue = r.ChargesAmount,
            AmountPaid = Math.Max(0, r.PaidAmount - r.RentAmount),
            Balance = r.ChargesAmount - Math.Max(0, r.PaidAmount - r.RentAmount)
        }).ToList();

        // Charge regularizations
        var regularizations = await _db.ChargeRegularizations
            .Where(cr => cr.LeaseId == id
                && cr.PeriodStart >= periodStart
                && cr.PeriodEnd <= periodEnd
                && cr.Status != RegularizationStatus.Cancelled)
            .ToListAsync();

        var totalChargesActual = regularizations.Sum(r => r.TotalActual);

        var statement = new LandlordStatementDto
        {
            LeaseId = id,
            LeaseReference = lease.Reference,
            TenantName = lease.LeaseTenant.FirstName + " " + lease.LeaseTenant.LastName,
            UnitReference = lease.Unit.Reference,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalRentDue = rentLines.Sum(r => r.AmountDue),
            TotalRentReceived = rentLines.Sum(r => r.AmountPaid),
            TotalChargesProvisioned = chargeLines.Sum(c => c.AmountDue),
            TotalChargesActual = totalChargesActual > 0 ? totalChargesActual : chargeLines.Sum(c => c.AmountDue),
            ChargesBalance = chargeLines.Sum(c => c.AmountDue) - (totalChargesActual > 0 ? totalChargesActual : chargeLines.Sum(c => c.AmountDue)),
            RentLines = rentLines,
            ChargeLines = chargeLines
        };

        statement.NetResult = statement.TotalRentReceived - statement.TotalChargesActual;

        return Ok(statement);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Leases.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
