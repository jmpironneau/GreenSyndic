using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public DashboardController(GreenSyndicDbContext db) => _db = db;

    /// <summary>
    /// Returns all KPI data for the dashboard in a single call.
    /// Minimizes frontend logic — all calculations done server-side.
    /// </summary>
    [HttpGet("kpis")]
    public async Task<ActionResult> GetKpis([FromQuery] Guid? organizationId)
    {
        var unitsQuery = _db.Units.AsQueryable();
        var paymentsQuery = _db.Payments.AsQueryable();
        var incidentsQuery = _db.Incidents.AsQueryable();
        var leasesQuery = _db.Leases.AsQueryable();
        var coopsQuery = _db.CoOwnerships.AsQueryable();
        var rentCallsQuery = _db.RentCalls.AsQueryable();

        if (organizationId.HasValue)
        {
            var orgId = organizationId.Value;
            unitsQuery = unitsQuery.Where(u => u.OrganizationId == orgId);
            paymentsQuery = paymentsQuery.Where(p => p.OrganizationId == orgId);
            incidentsQuery = incidentsQuery.Where(i => i.OrganizationId == orgId);
            leasesQuery = leasesQuery.Where(l => l.OrganizationId == orgId);
            coopsQuery = coopsQuery.Where(c => c.OrganizationId == orgId);
            rentCallsQuery = rentCallsQuery.Where(r => r.OrganizationId == orgId);
        }

        var totalUnits = await unitsQuery.CountAsync();
        var occupiedUnits = await unitsQuery.CountAsync(u => u.Status == UnitStatus.Occupied);
        var vacantUnits = totalUnits - occupiedUnits;
        var occupancyRate = totalUnits > 0 ? Math.Round((decimal)occupiedUnits / totalUnits * 100, 1) : 0;

        var totalPayments = await paymentsQuery.CountAsync();
        var pendingPayments = await paymentsQuery.CountAsync(p => p.Status == PaymentStatus.Pending);
        var confirmedRevenue = await paymentsQuery
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var openIncidents = await incidentsQuery.CountAsync(i =>
            i.Status == IncidentStatus.Reported || i.Status == IncidentStatus.Acknowledged || i.Status == IncidentStatus.InProgress);
        var totalIncidents = await incidentsQuery.CountAsync();

        var activeLeases = await leasesQuery.CountAsync(l => l.Status == LeaseStatus.Active);
        var totalCoOwnerships = await coopsQuery.CountAsync();

        // Rent calls this month
        var now = DateTime.UtcNow;
        var currentMonthRentCalls = await rentCallsQuery
            .Where(r => r.Year == now.Year && r.Month == now.Month)
            .ToListAsync();

        var rentCallsCount = currentMonthRentCalls.Count;
        var rentCallsPaid = currentMonthRentCalls.Count(r => r.Status == RentCallStatus.Paid);
        var rentCallsOverdue = currentMonthRentCalls.Count(r => r.Status == RentCallStatus.Overdue);
        var rentCallsTotal = currentMonthRentCalls.Sum(r => r.TotalAmount);
        var rentCallsCollected = currentMonthRentCalls.Sum(r => r.PaidAmount);
        var collectionRate = rentCallsTotal > 0 ? Math.Round(rentCallsCollected / rentCallsTotal * 100, 1) : 0;

        return Ok(new
        {
            units = new { total = totalUnits, occupied = occupiedUnits, vacant = vacantUnits, occupancyRate },
            payments = new { total = totalPayments, pending = pendingPayments, confirmedRevenue },
            incidents = new { open = openIncidents, total = totalIncidents },
            leases = new { active = activeLeases },
            coOwnerships = new { total = totalCoOwnerships },
            rentCalls = new
            {
                month = $"{now:yyyy-MM}",
                count = rentCallsCount,
                paid = rentCallsPaid,
                overdue = rentCallsOverdue,
                totalAmount = rentCallsTotal,
                collected = rentCallsCollected,
                collectionRate
            }
        });
    }
}
