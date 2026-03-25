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
public class TenantApplicationsController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public TenantApplicationsController(GreenSyndicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantApplicationDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? unitId,
        [FromQuery] ApplicationStatus? status)
    {
        var query = _db.TenantApplications
            .Include(a => a.Unit)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);
        if (unitId.HasValue)
            query = query.Where(a => a.UnitId == unitId.Value);
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Select(a => MapToDto(a)).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantApplicationDto>> GetById(Guid id)
    {
        var a = await _db.TenantApplications
            .Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return NotFound();
        return Ok(MapToDto(a));
    }

    [HttpPost]
    public async Task<ActionResult<TenantApplicationDto>> Create([FromBody] CreateTenantApplicationRequest request)
    {
        var unit = await _db.Units.FindAsync(request.UnitId);
        if (unit == null) return BadRequest("Unit not found.");

        var entity = new TenantApplication
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            UnitId = request.UnitId,
            Reference = $"CAND-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..3].ToUpper()}",
            Status = ApplicationStatus.Submitted,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            Email = request.Email,
            Phone = request.Phone,
            NationalId = request.NationalId,
            TaxId = request.TaxId,
            MonthlyIncome = request.MonthlyIncome,
            EmployerName = request.EmployerName,
            EmploymentType = request.EmploymentType,
            EmploymentDurationMonths = request.EmploymentDurationMonths,
            GuarantorName = request.GuarantorName,
            GuarantorPhone = request.GuarantorPhone,
            GuarantorRelation = request.GuarantorRelation,
            DesiredRent = request.DesiredRent,
            DesiredMoveInDate = request.DesiredMoveInDate,
            Notes = request.Notes
        };

        _db.TenantApplications.Add(entity);
        await _db.SaveChangesAsync();

        entity.Unit = unit;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Auto-score a tenant application.
    /// Scoring: income/rent ratio (40pts), employment stability (25pts), guarantor (20pts), documents (15pts).
    /// </summary>
    [HttpPost("{id:guid}/score")]
    public async Task<ActionResult<ApplicationScoreDto>> Score(Guid id)
    {
        var entity = await _db.TenantApplications.FindAsync(id);
        if (entity == null) return NotFound();

        var incomeScore = 0;
        decimal incomeToRentRatio = 0;

        if (entity.MonthlyIncome.HasValue && entity.DesiredRent > 0)
        {
            incomeToRentRatio = entity.MonthlyIncome.Value / entity.DesiredRent;
            incomeScore = incomeToRentRatio switch
            {
                >= 4m => 40,    // Excellent: 4x rent
                >= 3m => 30,    // Good: 3x rent
                >= 2.5m => 20,  // Average: 2.5x rent
                >= 2m => 10,    // Below average
                _ => 0          // Insufficient
            };
        }

        var employmentScore = entity.EmploymentType switch
        {
            "CDI" when entity.EmploymentDurationMonths >= 12 => 25,
            "CDI" => 20,
            "CDD" when entity.EmploymentDurationMonths >= 6 => 15,
            "CDD" => 10,
            _ => entity.EmploymentDurationMonths >= 24 ? 15 : 5  // Indépendant / other
        };

        var guarantorScore = 0;
        if (!string.IsNullOrEmpty(entity.GuarantorName))
            guarantorScore = !string.IsNullOrEmpty(entity.GuarantorPhone) ? 20 : 10;

        var documentsScore = 0;
        if (!string.IsNullOrEmpty(entity.NationalId)) documentsScore += 8;
        if (!string.IsNullOrEmpty(entity.TaxId)) documentsScore += 7;

        var totalScore = incomeScore + employmentScore + guarantorScore + documentsScore;
        var level = totalScore switch
        {
            >= 80 => ApplicationScoreLevel.Excellent,
            >= 60 => ApplicationScoreLevel.Good,
            >= 40 => ApplicationScoreLevel.Average,
            >= 20 => ApplicationScoreLevel.Poor,
            _ => ApplicationScoreLevel.Insufficient
        };

        entity.Score = totalScore;
        entity.ScoreLevel = level;
        entity.ScoreDetailsJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            incomeToRentRatio = Math.Round(incomeToRentRatio, 2),
            incomeScore,
            employmentScore,
            guarantorScore,
            documentsScore
        });
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new ApplicationScoreDto
        {
            TotalScore = totalScore,
            Level = level,
            IncomeToRentRatio = Math.Round(incomeToRentRatio, 2),
            IncomeScore = incomeScore,
            EmploymentScore = employmentScore,
            GuarantorScore = guarantorScore,
            DocumentsScore = documentsScore
        });
    }

    /// <summary>
    /// Review application (approve or reject).
    /// </summary>
    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewApplicationRequest request)
    {
        var entity = await _db.TenantApplications.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status != ApplicationStatus.Submitted
            && entity.Status != ApplicationStatus.UnderReview
            && entity.Status != ApplicationStatus.DocumentsPending)
            return BadRequest("Application cannot be reviewed in its current status.");

        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedBy = User.Identity?.Name;

        if (request.Approved)
        {
            entity.Status = ApplicationStatus.Approved;
        }
        else
        {
            entity.Status = ApplicationStatus.Rejected;
            entity.RejectionReason = request.RejectionReason;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Mark as under review (documents pending).
    /// </summary>
    [HttpPost("{id:guid}/request-documents")]
    public async Task<IActionResult> RequestDocuments(Guid id)
    {
        var entity = await _db.TenantApplications.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = ApplicationStatus.DocumentsPending;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Withdraw application.
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var entity = await _db.TenantApplications.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == ApplicationStatus.LeaseCreated)
            return BadRequest("Cannot withdraw — lease already created.");

        entity.Status = ApplicationStatus.Withdrawn;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.TenantApplications.FindAsync(id);
        if (entity == null) return NotFound();

        if (entity.Status == ApplicationStatus.LeaseCreated)
            return BadRequest("Cannot delete — lease already created.");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static TenantApplicationDto MapToDto(TenantApplication a) => new()
    {
        Id = a.Id,
        OrganizationId = a.OrganizationId,
        UnitId = a.UnitId,
        UnitReference = a.Unit?.Reference,
        Reference = a.Reference,
        Status = a.Status,
        FirstName = a.FirstName,
        LastName = a.LastName,
        CompanyName = a.CompanyName,
        Email = a.Email,
        Phone = a.Phone,
        NationalId = a.NationalId,
        TaxId = a.TaxId,
        MonthlyIncome = a.MonthlyIncome,
        EmployerName = a.EmployerName,
        EmploymentType = a.EmploymentType,
        EmploymentDurationMonths = a.EmploymentDurationMonths,
        GuarantorName = a.GuarantorName,
        GuarantorPhone = a.GuarantorPhone,
        GuarantorRelation = a.GuarantorRelation,
        Score = a.Score,
        ScoreLevel = a.ScoreLevel,
        ScoreDetailsJson = a.ScoreDetailsJson,
        DesiredRent = a.DesiredRent,
        DesiredMoveInDate = a.DesiredMoveInDate,
        ReviewedAt = a.ReviewedAt,
        ReviewedBy = a.ReviewedBy,
        RejectionReason = a.RejectionReason,
        LeaseId = a.LeaseId,
        Notes = a.Notes,
        CreatedAt = a.CreatedAt
    };
}
