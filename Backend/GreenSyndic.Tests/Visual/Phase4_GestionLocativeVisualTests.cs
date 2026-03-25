using Microsoft.Playwright;

namespace GreenSyndic.Tests.Visual;

/// <summary>
/// Phase 4 — Visual tests for rental management views in the PWA.
/// Tests the Trésorerie page with payment data and collection recording.
/// </summary>
[TestFixture]
public class Phase4_GestionLocativeVisualTests : VisualTestBase
{
    // ══════════════════════════════════
    //  TRÉSORERIE — PAYMENT LIST
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Tresorerie_Page_Has_Historique_Section()
    {
        await NavigateAuthenticated("/payments");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Historique"));
    }

    [Test, Order(2)]
    public async Task Tresorerie_Empty_Shows_Aucun_Paiement()
    {
        await NavigateAuthenticated("/payments");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Aucun paiement"));
    }

    [Test, Order(3)]
    public async Task Tresorerie_FAB_Opens_Collection_Modal()
    {
        await NavigateAuthenticated("/payments");

        var fab = Page.Locator("#fab");
        await Expect(fab).ToBeVisibleAsync();
        await fab.ClickAsync();

        // Wait for modal to appear
        await Page.WaitForTimeoutAsync(500);

        // Check modal overlay is visible
        var modal = Page.Locator("#modal-overlay");
        var display = await modal.EvaluateAsync<string>("el => getComputedStyle(el).display");
        // Modal should be visible (flex or block, not 'none')
        Assert.That(display, Is.Not.EqualTo("none"),
            "Modal should be visible after clicking FAB");
    }

    [Test, Order(4)]
    public async Task Collection_Modal_Has_Payment_Method_Select()
    {
        await NavigateAuthenticated("/payments");
        await Page.Locator("#fab").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Check for method select or radio buttons
        var modalContent = await Page.Locator(".modal-body").InnerTextAsync();
        // Should mention payment methods
        Assert.That(modalContent, Does.Contain("Montant").Or.Contain("montant").Or.Contain("Méthode").Or.Contain("method"));
    }

    // ══════════════════════════════════
    //  DASHBOARD — KPI FINANCIAL DATA
    // ══════════════════════════════════

    [Test, Order(10)]
    public async Task Dashboard_Shows_Financial_KPIs()
    {
        await NavigateAuthenticated("/");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        // Financial KPIs should be present
        Assert.That(content, Does.Contain("RECETTES CONFIRMÉES"));
        Assert.That(content, Does.Contain("BAUX ACTIFS"));
        Assert.That(content, Does.Contain("F CFA"));
    }

    [Test, Order(11)]
    public async Task Dashboard_KPI_Cards_Have_Colored_Values()
    {
        await NavigateAuthenticated("/");

        // KPI cards should have colored value text
        var kpiValues = Page.Locator(".kpi-value");
        var count = await kpiValues.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(4));

        // First card should have a color style
        var firstColor = await kpiValues.First.EvaluateAsync<string>(
            "el => getComputedStyle(el).color");
        Assert.That(firstColor, Is.Not.EqualTo("rgb(0, 0, 0)"),
            "KPI values should have colored text, not plain black");
    }

    [Test, Order(12)]
    public async Task Dashboard_Shows_Coproprietes_Section()
    {
        await NavigateAuthenticated("/");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Copropriétés"));
    }

    // ══════════════════════════════════
    //  RESPONSIVE LAYOUT
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task KPI_Cards_Grid_Layout()
    {
        await NavigateAuthenticated("/");

        // KPI grid should use CSS grid
        var grid = Page.Locator(".kpi-grid");
        var display = await grid.EvaluateAsync<string>(
            "el => getComputedStyle(el).display");
        Assert.That(display, Is.EqualTo("grid"));
    }

    [Test, Order(21)]
    public async Task BottomNav_Fixed_At_Bottom()
    {
        await NavigateAuthenticated("/");

        var position = await Page.Locator(".bottom-nav").EvaluateAsync<string>(
            "el => getComputedStyle(el).position");
        Assert.That(position, Is.EqualTo("fixed"));

        var bottom = await Page.Locator(".bottom-nav").EvaluateAsync<string>(
            "el => getComputedStyle(el).bottom");
        Assert.That(bottom, Is.EqualTo("0px"));
    }

    [Test, Order(22)]
    public async Task TopNav_Fixed_At_Top()
    {
        await NavigateAuthenticated("/");

        var position = await Page.Locator(".top-nav").EvaluateAsync<string>(
            "el => getComputedStyle(el).position");
        Assert.That(position, Is.EqualTo("fixed"));
    }

    // ══════════════════════════════════
    //  STATIC FILES
    // ══════════════════════════════════

    [Test, Order(30)]
    public async Task CSS_Is_Loaded()
    {
        await Page.GotoAsync($"{BaseUrl}/app");

        // Check that CSS variables are applied (--green)
        var greenColor = await Page.EvaluateAsync<string>(
            "() => getComputedStyle(document.documentElement).getPropertyValue('--green').trim()");
        Assert.That(greenColor, Is.Not.Empty, "CSS variable --green should be defined");
    }

    [Test, Order(31)]
    public async Task JS_Api_Is_Loaded()
    {
        await Page.GotoAsync($"{BaseUrl}/app");

        var apiExists = await Page.EvaluateAsync<bool>("() => typeof API !== 'undefined'");
        Assert.That(apiExists, Is.True, "API object should be defined from api.js");
    }

    [Test, Order(32)]
    public async Task JS_Router_Is_Loaded()
    {
        await Page.GotoAsync($"{BaseUrl}/app");

        var routerExists = await Page.EvaluateAsync<bool>("() => typeof Router !== 'undefined'");
        Assert.That(routerExists, Is.True, "Router object should be defined from app.js");
    }
}
