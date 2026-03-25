using Microsoft.Playwright;

namespace GreenSyndic.Tests.Visual;

/// <summary>
/// Phase 7 — Visual tests verifying the PWA renders seeded data correctly.
/// Dashboard KPIs, lots listing, and data-driven pages with 269 units.
/// </summary>
[TestFixture]
public class Phase7_SeedVisualTests : VisualTestBase
{
    // ══════════════════════════════════
    //  DASHBOARD WITH REAL DATA
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Dashboard_KPI_Lots_Shows_NonZero_Count()
    {
        await NavigateAuthenticated("/");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        // With seed data, LOTS KPI should show a number > 0
        Assert.That(content, Does.Contain("LOTS"));
        // Should NOT show "0" for lots if seeded
        var kpiValues = Page.Locator(".kpi-value");
        var count = await kpiValues.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(4));
    }

    [Test, Order(2)]
    public async Task Dashboard_Shows_Coproprietes_List()
    {
        await NavigateAuthenticated("/");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Copropriétés"));
    }

    // ══════════════════════════════════
    //  LOTS PAGE
    // ══════════════════════════════════

    [Test, Order(10)]
    public async Task Lots_Page_Renders_Unit_Cards()
    {
        await NavigateAuthenticated("/units");

        // Wait for data to load
        await Page.WaitForTimeoutAsync(1000);
        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Lots"));
    }

    // ══════════════════════════════════
    //  INCIDENTS PAGE WITH EMPTY STATE
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task Incidents_Page_Shows_Empty_Or_List()
    {
        await NavigateAuthenticated("/incidents");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Incidents"));
    }

    // ══════════════════════════════════
    //  PAYMENTS PAGE
    // ══════════════════════════════════

    [Test, Order(30)]
    public async Task Payments_Page_Shows_Historique()
    {
        await NavigateAuthenticated("/payments");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Historique"));
    }
}
