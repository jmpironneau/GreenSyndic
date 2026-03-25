using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Seed;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── PostgreSQL only ──────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(connStr))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERREUR: Aucune connexion PostgreSQL configuree (ConnectionStrings:Default).");
    Console.WriteLine("Verifiez appsettings.Development.json.");
    Console.ResetColor();
    Environment.Exit(1);
}

builder.Services.AddDbContext<GreenSyndicDbContext>(opt => opt.UseNpgsql(connStr));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<GreenSyndicDbContext>()
.AddDefaultTokenProviders();
builder.Services.AddRazorPages();

var app = builder.Build();

// ── Verify database exists (do NOT create it) ────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GreenSyndicDbContext>();
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERREUR: Impossible de se connecter a la base de donnees.");
            Console.WriteLine("Lancez d'abord l'API pour creer/migrer la base.");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERREUR: {ex.Message}");
        Console.WriteLine("La base de donnees n'existe pas. Lancez d'abord l'API.");
        Console.ResetColor();
        Environment.Exit(1);
    }
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        ctx.Context.Response.Headers["Pragma"] = "no-cache";
        ctx.Context.Response.Headers["Expires"] = "0";
    }
});
app.MapRazorPages();

// ── Table metadata ───────────────────────────────────────────────
var tableMap = new Dictionary<string, Func<GreenSyndicDbContext, IQueryable<object>>>
{
    ["Organizations"] = db => db.Organizations.IgnoreQueryFilters(),
    ["CoOwnerships"] = db => db.CoOwnerships.IgnoreQueryFilters(),
    ["Buildings"] = db => db.Buildings.IgnoreQueryFilters(),
    ["Units"] = db => db.Units.IgnoreQueryFilters(),
    ["Owners"] = db => db.Owners.IgnoreQueryFilters(),
    ["LeaseTenants"] = db => db.LeaseTenants.IgnoreQueryFilters(),
    ["Leases"] = db => db.Leases.IgnoreQueryFilters(),
    ["ChargeDefinitions"] = db => db.ChargeDefinitions.IgnoreQueryFilters(),
    ["ChargeAssignments"] = db => db.ChargeAssignments.IgnoreQueryFilters(),
    ["Payments"] = db => db.Payments.IgnoreQueryFilters(),
    ["Incidents"] = db => db.Incidents.IgnoreQueryFilters(),
    ["Suppliers"] = db => db.Suppliers.IgnoreQueryFilters(),
    ["WorkOrders"] = db => db.WorkOrders.IgnoreQueryFilters(),
    ["Meetings"] = db => db.Meetings.IgnoreQueryFilters(),
    ["Resolutions"] = db => db.Resolutions.IgnoreQueryFilters(),
    ["Votes"] = db => db.Votes.IgnoreQueryFilters(),
    ["Documents"] = db => db.Documents.IgnoreQueryFilters(),
    ["Notifications"] = db => db.Notifications.IgnoreQueryFilters(),
    ["AccountingEntries"] = db => db.AccountingEntries.IgnoreQueryFilters(),
    ["MeetingAttendees"] = db => db.MeetingAttendees.IgnoreQueryFilters(),
    ["MeetingAgendaItems"] = db => db.MeetingAgendaItems.IgnoreQueryFilters(),
    ["ResolutionTemplates"] = db => db.ResolutionTemplates.IgnoreQueryFilters(),
    ["CommunicationMessages"] = db => db.CommunicationMessages.IgnoreQueryFilters(),
    ["MessageTemplates"] = db => db.MessageTemplates.IgnoreQueryFilters(),
    ["Broadcasts"] = db => db.Broadcasts.IgnoreQueryFilters(),
    ["BroadcastRecipients"] = db => db.BroadcastRecipients.IgnoreQueryFilters(),
    ["MessageDeliveryLogs"] = db => db.MessageDeliveryLogs.IgnoreQueryFilters(),
    ["RentCalls"] = db => db.RentCalls.IgnoreQueryFilters(),
    ["RentReceipts"] = db => db.RentReceipts.IgnoreQueryFilters(),
    ["LeaseRevisions"] = db => db.LeaseRevisions.IgnoreQueryFilters(),
    ["ChargeRegularizations"] = db => db.ChargeRegularizations.IgnoreQueryFilters(),
    ["TenantApplications"] = db => db.TenantApplications.IgnoreQueryFilters(),
    ["OrganizationSettings"] = db => db.OrganizationSettings.IgnoreQueryFilters(),
    ["LegalReferences"] = db => db.LegalReferences.IgnoreQueryFilters(),
};

var tableNames = tableMap.Keys.OrderBy(n => n).ToList();

// ── API: table counts ────────────────────────────────────────────
app.MapGet("/api/table-counts", async (GreenSyndicDbContext db) =>
{
    var counts = new Dictionary<string, int>();
    foreach (var (name, queryFactory) in tableMap)
    {
        counts[name] = await queryFactory(db).CountAsync();
    }
    return Results.Ok(counts);
});

// ── API: legacy stats ────────────────────────────────────────────
app.MapGet("/api/stats", async (GreenSyndicDbContext db) => new
{
    CoOwnerships = await db.CoOwnerships.CountAsync(),
    Buildings = await db.Buildings.CountAsync(),
    Units = await db.Units.CountAsync(),
    Owners = await db.Owners.CountAsync(),
    Suppliers = await db.Suppliers.CountAsync(),
    ChargeDefinitions = await db.ChargeDefinitions.CountAsync(),
    LegalReferences = await db.LegalReferences.CountAsync()
});

// ── API: table rows (first 100) ─────────────────────────────────
app.MapGet("/api/table/{name}", async (string name, GreenSyndicDbContext db) =>
{
    if (!tableMap.TryGetValue(name, out var queryFactory))
        return Results.NotFound(new { error = $"Table '{name}' not found." });

    var rows = await queryFactory(db).Take(100).ToListAsync();
    return Results.Ok(rows);
});

// ── API: truncate one table ──────────────────────────────────────
app.MapDelete("/api/table/{name}", async (string name, GreenSyndicDbContext db) =>
{
    if (!tableMap.ContainsKey(name))
        return Results.NotFound(new { error = $"Table '{name}' not found." });

    // Map DbSet name to actual PostgreSQL table name using EF metadata
    var entityType = db.Model.GetEntityTypes()
        .FirstOrDefault(e => db.GetType().GetProperty(name)?.PropertyType
            .GenericTypeArguments.FirstOrDefault() == e.ClrType);

    if (entityType == null)
        return Results.NotFound(new { error = $"Could not resolve entity for '{name}'." });

    var schema = entityType.GetSchema() ?? "public";
    var tableName = entityType.GetTableName();

    var sql = string.Concat("TRUNCATE TABLE \"", schema, "\".\"", tableName, "\" CASCADE");
    await db.Database.ExecuteSqlRawAsync(sql);

    return Results.Ok(new { message = $"Table '{name}' truncated." });
});

// ── API: seed demo ───────────────────────────────────────────────
app.MapPost("/api/seed-demo", async (HttpContext ctx, IServiceProvider sp) =>
{
    // SSE streaming for verbose progress
    ctx.Response.ContentType = "text/event-stream";
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.Headers["Connection"] = "keep-alive";

    async Task send(string msg)
    {
        await ctx.Response.WriteAsync($"data: {msg}\n\n");
        await ctx.Response.Body.FlushAsync();
    }

    try
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GreenSyndicDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await send("Suppression des donnees existantes...");
        var domainTables = new List<string>();
        foreach (var name in tableMap.Keys)
        {
            var entityType = db.Model.GetEntityTypes()
                .FirstOrDefault(e => typeof(GreenSyndicDbContext).GetProperty(name)?.PropertyType
                    .GenericTypeArguments.FirstOrDefault() == e.ClrType);
            if (entityType != null)
            {
                var schema = entityType.GetSchema() ?? "public";
                var tableName = entityType.GetTableName();
                domainTables.Add(string.Concat("\"", schema, "\".\"", tableName, "\""));
            }
        }
        var identityTables = new[] {
            "\"public\".\"AspNetUserRoles\"", "\"public\".\"AspNetUserClaims\"",
            "\"public\".\"AspNetUserLogins\"", "\"public\".\"AspNetUserTokens\"",
            "\"public\".\"AspNetRoleClaims\"", "\"public\".\"AspNetUsers\"",
            "\"public\".\"AspNetRoles\""
        };
        var allTables = domainTables.Concat(identityTables).ToList();
        if (allTables.Count > 0)
        {
            var sql = "TRUNCATE TABLE " + string.Join(", ", allTables) + " CASCADE";
            await db.Database.ExecuteSqlRawAsync(sql);
        }

        await send("Creation des roles (12 profils)...");
        string[] roles = ["SuperAdmin", "SyndicManager", "SyndicAccountant", "SyndicTechnician",
            "CouncilPresident", "CouncilMember", "Owner", "Tenant", "CommercialTenant",
            "Supplier", "SecurityAgent", "ReadOnly"];
        foreach (var role in roles)
            await roleManager.CreateAsync(new IdentityRole(role));

        await send("Creation de l'organisation Green City Bassam...");
        var org = new GreenSyndic.Core.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Green City Bassam",
            LegalName = "COFIPRI - Green City Bassam",
            Country = "CI",
            Currency = "XOF",
            City = "Grand Bassam",
            Address = "Grand Bassam, Côte d'Ivoire",
            IsActive = true
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        await send("Creation du compte administrateur...");
        var admin = new ApplicationUser
        {
            UserName = "admin@greensyndic.ci",
            Email = "admin@greensyndic.ci",
            FirstName = "Admin",
            LastName = "GreenSyndic",
            OrganizationId = org.Id,
            ProfileRole = "SuperAdmin",
            IsActive = true,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(admin, "Admin@2026!");
        if (createResult.Succeeded)
            await userManager.AddToRoleAsync(admin, "SuperAdmin");

        await send("Creation de la structure : 8 coproprietes, 10 batiments, 269 lots...");
        await SeedGreenCity.SeedAsync(db);

        await send("Generation de 26 mois d'activite (jan 2024 - mars 2026)...");
        await SeedActivity.SeedAsync(db, userManager, async (msg) => await send(msg));

        await send("[DONE] Demo ideale generee avec succes !");
    }
    catch (Exception ex)
    {
        await send($"[ERROR] {ex.InnerException?.Message ?? ex.Message}");
    }
});

// ── API: seed test ───────────────────────────────────────────────
app.MapPost("/api/seed-test", async (GreenSyndicDbContext db) =>
{
    try
    {
        await SeedGreenCity.SeedAsync(db);
        return Results.Ok(new { message = "Test data generated successfully." });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.InnerException?.Message ?? ex.Message,
            title: "Seed failed",
            statusCode: 500);
    }
});

// ── API: delete all domain data ──────────────────────────────────
app.MapPost("/api/delete-all", async (GreenSyndicDbContext db) =>
{
    try
    {
        // Build TRUNCATE for all domain tables + Identity tables
        var domainTables = new List<string>();
        foreach (var name in tableMap.Keys)
        {
            var entityType = db.Model.GetEntityTypes()
                .FirstOrDefault(e => db.GetType().GetProperty(name)?.PropertyType
                    .GenericTypeArguments.FirstOrDefault() == e.ClrType);

            if (entityType != null)
            {
                var schema = entityType.GetSchema() ?? "public";
                var tableName = entityType.GetTableName();
                domainTables.Add(string.Concat("\"", schema, "\".\"", tableName, "\""));
            }
        }
        // Include Identity tables
        var identityTables = new[] {
            "\"public\".\"AspNetUserRoles\"", "\"public\".\"AspNetUserClaims\"",
            "\"public\".\"AspNetUserLogins\"", "\"public\".\"AspNetUserTokens\"",
            "\"public\".\"AspNetRoleClaims\"", "\"public\".\"AspNetUsers\"",
            "\"public\".\"AspNetRoles\""
        };
        var allTables = domainTables.Concat(identityTables).ToList();

        if (allTables.Count > 0)
        {
            var sql = "TRUNCATE TABLE " + string.Join(", ", allTables) + " CASCADE";
            await db.Database.ExecuteSqlRawAsync(sql);
        }

        return Results.Ok(new { message = "All data deleted." });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.InnerException?.Message ?? ex.Message,
            title: "Delete failed",
            statusCode: 500);
    }
});

// ── API: table names list ────────────────────────────────────────
app.MapGet("/api/tables", () => Results.Ok(tableNames));

app.Run();
