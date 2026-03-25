using System.Text;
using GreenSyndic.Core.Enums;
using GreenSyndic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenSyndic.Api.Controllers;

/// <summary>
/// Import/Export — CSV export of all major entities.
/// Excel requires a NuGet (ClosedXML) — here we do pure CSV which can be opened in Excel.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly GreenSyndicDbContext _db;

    public ExportController(GreenSyndicDbContext db) => _db = db;

    /// <summary>
    /// Export units list as CSV.
    /// </summary>
    [HttpGet("units")]
    public async Task<IActionResult> ExportUnits([FromQuery] Guid? organizationId)
    {
        var query = _db.Units
            .Include(u => u.Building)
            .Include(u => u.Owner)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(u => u.OrganizationId == organizationId.Value);

        var items = await query.OrderBy(u => u.Reference).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Référence;Immeuble;Type;Surface m²;Statut;Propriétaire;Tantièmes");
        foreach (var u in items)
        {
            sb.AppendLine(string.Join(";",
                Escape(u.Reference),
                Escape(u.Building?.Name),
                u.Type.ToString(),
                u.AreaSqm.ToString("F2"),
                u.Status.ToString(),
                u.Owner != null ? Escape($"{u.Owner.FirstName} {u.Owner.LastName}") : "",
                u.ShareRatio?.ToString("F2") ?? ""
            ));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"lots_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export payments list as CSV.
    /// </summary>
    [HttpGet("payments")]
    public async Task<IActionResult> ExportPayments(
        [FromQuery] Guid? organizationId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _db.Payments
            .Include(p => p.Owner)
            .Include(p => p.LeaseTenant)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(p => p.OrganizationId == organizationId.Value);
        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue)
            query = query.Where(p => p.PaymentDate <= to.Value);

        var items = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Référence;Date;Montant;Méthode;Statut;Description;Payeur");
        foreach (var p in items)
        {
            var payer = p.Owner != null
                ? $"{p.Owner.FirstName} {p.Owner.LastName}"
                : p.LeaseTenant != null
                    ? $"{p.LeaseTenant.FirstName} {p.LeaseTenant.LastName}"
                    : "";
            sb.AppendLine(string.Join(";",
                Escape(p.Reference),
                p.PaymentDate.ToString("yyyy-MM-dd"),
                p.Amount.ToString("F2"),
                p.Method.ToString(),
                p.Status.ToString(),
                Escape(p.Description),
                Escape(payer)
            ));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"paiements_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export incidents list as CSV.
    /// </summary>
    [HttpGet("incidents")]
    public async Task<IActionResult> ExportIncidents([FromQuery] Guid? organizationId)
    {
        var query = _db.Incidents
            .Include(i => i.Unit)
            .Include(i => i.Building)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(i => i.OrganizationId == organizationId.Value);

        var items = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Titre;Priorité;Statut;Catégorie;Immeuble;Lot;Date signalement;Date résolution");
        foreach (var i in items)
        {
            sb.AppendLine(string.Join(";",
                Escape(i.Title),
                i.Priority.ToString(),
                i.Status.ToString(),
                Escape(i.Category),
                Escape(i.Building?.Name),
                Escape(i.Unit?.Reference),
                i.CreatedAt.ToString("yyyy-MM-dd"),
                i.ResolvedAt?.ToString("yyyy-MM-dd") ?? ""
            ));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"incidents_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export work orders as CSV.
    /// </summary>
    [HttpGet("workorders")]
    public async Task<IActionResult> ExportWorkOrders([FromQuery] Guid? organizationId)
    {
        var query = _db.WorkOrders
            .Include(wo => wo.Supplier)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(wo => wo.OrganizationId == organizationId.Value);

        var items = await query.OrderByDescending(wo => wo.CreatedAt).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Référence;Titre;Statut;Fournisseur;Coût estimé;Coût réel;Date planifiée;Date achèvement");
        foreach (var wo in items)
        {
            sb.AppendLine(string.Join(";",
                Escape(wo.Reference),
                Escape(wo.Title),
                wo.Status.ToString(),
                Escape(wo.Supplier?.Name),
                wo.EstimatedCost?.ToString("F2") ?? "",
                wo.ActualCost?.ToString("F2") ?? "",
                wo.ScheduledDate?.ToString("yyyy-MM-dd") ?? "",
                wo.CompletedDate?.ToString("yyyy-MM-dd") ?? ""
            ));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"ordres_service_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export owners list as CSV.
    /// </summary>
    [HttpGet("owners")]
    public async Task<IActionResult> ExportOwners([FromQuery] Guid? organizationId)
    {
        var query = _db.Owners.Include(o => o.Units).AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(o => o.OrganizationId == organizationId.Value);

        var items = await query.OrderBy(o => o.LastName).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Nom;Prénom;Email;Téléphone;Nombre de lots");
        foreach (var o in items)
        {
            sb.AppendLine(string.Join(";",
                Escape(o.LastName),
                Escape(o.FirstName),
                Escape(o.Email),
                Escape(o.Phone),
                o.Units.Count.ToString()
            ));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"proprietaires_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export leases as CSV.
    /// </summary>
    [HttpGet("leases")]
    public async Task<IActionResult> ExportLeases([FromQuery] Guid? organizationId)
    {
        var query = _db.Leases
            .Include(l => l.LeaseTenant)
            .Include(l => l.Unit)
            .AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(l => l.OrganizationId == organizationId.Value);

        var items = await query.OrderBy(l => l.Reference).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Référence;Type;Statut;Locataire;Lot;Loyer mensuel;Charges;Début;Fin");
        foreach (var l in items)
        {
            sb.AppendLine(string.Join(";",
                Escape(l.Reference),
                l.Type.ToString(),
                l.Status.ToString(),
                l.LeaseTenant != null ? Escape($"{l.LeaseTenant.FirstName} {l.LeaseTenant.LastName}") : "",
                Escape(l.Unit?.Reference),
                l.MonthlyRent.ToString("F2"),
                l.Charges?.ToString("F2") ?? "0",
                l.StartDate.ToString("yyyy-MM-dd"),
                l.EndDate?.ToString("yyyy-MM-dd") ?? ""
            ));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"baux_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export suppliers as CSV.
    /// </summary>
    [HttpGet("suppliers")]
    public async Task<IActionResult> ExportSuppliers([FromQuery] Guid? organizationId)
    {
        var query = _db.Suppliers.Include(s => s.WorkOrders).AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(s => s.OrganizationId == organizationId.Value);

        var items = await query.OrderBy(s => s.Name).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Nom;Contact;Email;Téléphone;Spécialité;RCCM;Actif;Nb ordres");
        foreach (var s in items)
        {
            sb.AppendLine(string.Join(";",
                Escape(s.Name),
                Escape(s.ContactPerson),
                Escape(s.Email),
                Escape(s.Phone),
                Escape(s.Specialty),
                Escape(s.TaxId),
                s.IsActive ? "Oui" : "Non",
                s.WorkOrders.Count.ToString()
            ));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"fournisseurs_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// List available export endpoints.
    /// </summary>
    [HttpGet]
    public ActionResult ListExports()
    {
        return Ok(new[]
        {
            new { endpoint = "/api/export/units", label = "Lots", format = "CSV" },
            new { endpoint = "/api/export/payments", label = "Paiements", format = "CSV" },
            new { endpoint = "/api/export/incidents", label = "Incidents", format = "CSV" },
            new { endpoint = "/api/export/workorders", label = "Ordres de service", format = "CSV" },
            new { endpoint = "/api/export/owners", label = "Propriétaires", format = "CSV" },
            new { endpoint = "/api/export/leases", label = "Baux", format = "CSV" },
            new { endpoint = "/api/export/suppliers", label = "Fournisseurs", format = "CSV" }
        });
    }

    private static string Escape(string? val) =>
        val == null ? "" : val.Contains(';') || val.Contains('"')
            ? $"\"{val.Replace("\"", "\"\"")}\""
            : val;
}
