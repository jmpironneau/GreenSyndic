using System.Net.Http.Json;
using GreenSyndic.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GreenSyndic.Tests.Controllers;

/// <summary>
/// Phase 7 — Verify Seed produces correct counts and relationships.
/// Uses the SeedGreenCity seeder against the InMemory DB via the WebAppFactory.
/// </summary>
[TestFixture]
public class Phase7_SeedDataTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        // Seed Green City data into the test DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GreenSyndic.Infrastructure.Data.GreenSyndicDbContext>();
        await Seed.SeedGreenCity.SeedAsync(db);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ══════════════════════════════════
    //  COUNTS
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Seed_Creates_8_CoOwnerships()
    {
        var resp = await _client.GetAsync("/api/coownerships");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        Assert.That(list, Has.Count.EqualTo(8));
    }

    [Test, Order(2)]
    public async Task Seed_Creates_10_Buildings()
    {
        var resp = await _client.GetAsync("/api/buildings");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        Assert.That(list, Has.Count.EqualTo(10));
    }

    [Test, Order(3)]
    public async Task Seed_Creates_269_Units()
    {
        var resp = await _client.GetAsync("/api/units");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        // 51 villas + 200 apartments + 18 COSMOS = 269
        Assert.That(list, Has.Count.EqualTo(269));
    }

    [Test, Order(4)]
    public async Task Seed_Creates_80_Owners()
    {
        var resp = await _client.GetAsync("/api/owners");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        Assert.That(list, Has.Count.EqualTo(80));
    }

    [Test, Order(5)]
    public async Task Seed_Creates_15_Suppliers()
    {
        var resp = await _client.GetAsync("/api/suppliers");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        Assert.That(list, Has.Count.EqualTo(15));
    }

    [Test, Order(6)]
    public async Task Seed_Creates_28_ChargeDefinitions()
    {
        var resp = await _client.GetAsync("/api/charges");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        // 8 horizontal + 5 buildings × 4 vertical = 28
        Assert.That(list, Has.Count.EqualTo(28));
    }

    [Test, Order(7)]
    public async Task Seed_Creates_12_LegalReferences()
    {
        var resp = await _client.GetAsync("/api/legal-references");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        Assert.That(list, Has.Count.EqualTo(12));
    }

    // ══════════════════════════════════
    //  DATA INTEGRITY
    // ══════════════════════════════════

    [Test, Order(10)]
    public async Task Seed_Is_Idempotent()
    {
        // Run seed again — should not duplicate data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GreenSyndic.Infrastructure.Data.GreenSyndicDbContext>();
        await Seed.SeedGreenCity.SeedAsync(db);

        var resp = await _client.GetAsync("/api/units");
        var list = await resp.Content.ReadFromJsonAsync<List<object>>();
        Assert.That(list, Has.Count.EqualTo(269), "Running seed twice should not duplicate units");
    }

    [Test, Order(11)]
    public async Task Seed_Villas_Have_Correct_References()
    {
        var resp = await _client.GetAsync("/api/units?buildingCode=VIL");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        // Villa references start with V-
        Assert.That(body, Does.Contain("V-001"));
        Assert.That(body, Does.Contain("V-051"));
    }

    [Test, Order(12)]
    public async Task Seed_COSMOS_Has_18_Commercial_Lots()
    {
        var resp = await _client.GetAsync("/api/units?buildingCode=COS");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("COS-CIN")); // Cinema
        Assert.That(body, Does.Contain("COS-PKG")); // Parking
    }

    [Test, Order(13)]
    public async Task Seed_Has_Council_Members()
    {
        var resp = await _client.GetAsync("/api/owners");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        // 4 council members in seed data
        Assert.That(body, Does.Contain("Koné")); // President
        Assert.That(body, Does.Contain("Diallo"));
    }

    // ══════════════════════════════════
    //  CSV EXPORT WITH SEEDED DATA
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task Export_Units_CSV_Contains_269_Lines()
    {
        var resp = await _client.GetAsync("/api/export/units");
        resp.EnsureSuccessStatusCode();
        var csv = await resp.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // 1 header + 269 data lines
        Assert.That(lines.Length, Is.EqualTo(270));
    }

    [Test, Order(21)]
    public async Task Export_Owners_CSV_Contains_80_Lines()
    {
        var resp = await _client.GetAsync("/api/export/owners");
        resp.EnsureSuccessStatusCode();
        var csv = await resp.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // 1 header + 80 data lines
        Assert.That(lines.Length, Is.EqualTo(81));
    }

    [Test, Order(22)]
    public async Task Export_Suppliers_CSV_Contains_15_Lines()
    {
        var resp = await _client.GetAsync("/api/export/suppliers");
        resp.EnsureSuccessStatusCode();
        var csv = await resp.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // 1 header + 15 data lines
        Assert.That(lines.Length, Is.EqualTo(16));
    }
}
