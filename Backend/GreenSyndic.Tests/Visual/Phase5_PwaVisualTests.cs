using Microsoft.Playwright;

namespace GreenSyndic.Tests.Visual;

/// <summary>
/// Phase 5 — Visual tests for the PWA mobile interface.
/// Validates layout, navigation, KPI dashboard, pages structure.
/// </summary>
[TestFixture]
public class Phase5_PwaVisualTests : VisualTestBase
{
    // ══════════════════════════════════
    //  LOGIN PAGE
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Login_Page_Has_Logo_And_Form()
    {
        await Page.GotoAsync($"{BaseUrl}/app");
        // Without token, should show login
        await Page.WaitForSelectorAsync("#main-content", new() { Timeout = 5000 });

        // Logo
        var logo = Page.Locator("img[alt='GreenSyndic']");
        await Expect(logo).ToBeVisibleAsync();

        // Email input
        var email = Page.Locator("input[placeholder='Email']");
        await Expect(email).ToBeVisibleAsync();

        // Password input
        var password = Page.Locator("input[placeholder='Mot de passe']");
        await Expect(password).ToBeVisibleAsync();

        // Submit button
        var btn = Page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" });
        await Expect(btn).ToBeVisibleAsync();
    }

    [Test, Order(2)]
    public async Task Login_Page_Hides_Navigation()
    {
        await Page.GotoAsync($"{BaseUrl}/app");
        await Page.WaitForSelectorAsync("#main-content", new() { Timeout = 5000 });

        // Top nav and bottom nav should be hidden on login
        var topNav = Page.Locator(".top-nav");
        await Expect(topNav).ToBeHiddenAsync();

        var bottomNav = Page.Locator(".bottom-nav");
        await Expect(bottomNav).ToBeHiddenAsync();
    }

    // ══════════════════════════════════
    //  DASHBOARD
    // ══════════════════════════════════

    [Test, Order(10)]
    public async Task Dashboard_Shows_KPI_Cards()
    {
        await NavigateAuthenticated("/");

        // Page title
        var title = Page.Locator(".page-title");
        await Expect(title).ToContainTextAsync("Tableau de bord");

        // KPI cards should be present
        var kpiCards = Page.Locator(".kpi-card");
        var count = await kpiCards.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(4), "Should have at least 4 KPI cards");
    }

    [Test, Order(11)]
    public async Task Dashboard_Shows_KPI_Labels()
    {
        await NavigateAuthenticated("/");

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("LOTS"));
        Assert.That(content, Does.Contain("TAUX OCCUPATION"));
        Assert.That(content, Does.Contain("IMPAYÉS"));
        Assert.That(content, Does.Contain("INCIDENTS"));
    }

    [Test, Order(12)]
    public async Task Dashboard_Shows_TopNav()
    {
        await NavigateAuthenticated("/");

        var topNav = Page.Locator(".top-nav");
        await Expect(topNav).ToBeVisibleAsync();
        await Expect(topNav).ToContainTextAsync("GreenSyndic");
    }

    [Test, Order(13)]
    public async Task Dashboard_Shows_BottomNav_With_5_Tabs()
    {
        await NavigateAuthenticated("/");

        var bottomNav = Page.Locator(".bottom-nav");
        await Expect(bottomNav).ToBeVisibleAsync();

        var links = bottomNav.Locator("a");
        Assert.That(await links.CountAsync(), Is.EqualTo(5));

        var navText = await bottomNav.InnerTextAsync();
        Assert.That(navText, Does.Contain("Accueil"));
        Assert.That(navText, Does.Contain("Lots"));
        Assert.That(navText, Does.Contain("Trésorerie"));
        Assert.That(navText, Does.Contain("Incidents"));
        Assert.That(navText, Does.Contain("Décaisser"));
    }

    [Test, Order(14)]
    public async Task Dashboard_TopNav_Has_Green_Background()
    {
        await NavigateAuthenticated("/");

        var bg = await Page.Locator(".top-nav").EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");
        // rgb(46, 125, 50) = #2e7d32
        Assert.That(bg, Does.Contain("46, 125, 50"));
    }

    // ══════════════════════════════════
    //  LOTS PAGE
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task Lots_Page_Shows_Empty_State()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/units']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Lots"));
    }

    [Test, Order(21)]
    public async Task Lots_Page_Highlights_Active_Tab()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/units']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var activeLink = Page.Locator("[data-nav='/units']");
        var className = await activeLink.GetAttributeAsync("class") ?? "";
        Assert.That(className, Does.Contain("active"));
    }

    // ══════════════════════════════════
    //  TRÉSORERIE PAGE
    // ══════════════════════════════════

    [Test, Order(30)]
    public async Task Tresorerie_Page_Shows_Title()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/payments']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Paiements").Or.Contain("Historique"));
    }

    [Test, Order(31)]
    public async Task Tresorerie_Page_Shows_FAB()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/payments']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var fab = Page.Locator("#fab");
        await Expect(fab).ToBeVisibleAsync();
        await Expect(fab).ToContainTextAsync("+");
    }

    // ══════════════════════════════════
    //  INCIDENTS PAGE
    // ══════════════════════════════════

    [Test, Order(40)]
    public async Task Incidents_Page_Shows_Empty_State()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/incidents']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Incidents"));
    }

    [Test, Order(41)]
    public async Task Incidents_Page_Shows_FAB()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/incidents']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var fab = Page.Locator("#fab");
        await Expect(fab).ToBeVisibleAsync();
    }

    // ══════════════════════════════════
    //  DÉCAISSER PAGE
    // ══════════════════════════════════

    [Test, Order(50)]
    public async Task Decaisser_Page_Shows_Form()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/pay']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Décaissement"));
    }

    [Test, Order(51)]
    public async Task Decaisser_Page_Has_Beneficiary_And_Amount_Fields()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/pay']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Beneficiary input
        var beneficiary = Page.Locator("input[placeholder*='prestataire'], input[placeholder*='fournisseur']");
        await Expect(beneficiary).ToBeVisibleAsync();

        // Amount input
        var amount = Page.Locator("input[placeholder*='150000']");
        await Expect(amount).ToBeVisibleAsync();
    }

    [Test, Order(52)]
    public async Task Decaisser_Page_Shows_Mobile_Money_Providers()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/pay']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var content = await Page.Locator("#main-content").InnerTextAsync();
        Assert.That(content, Does.Contain("Orange Money"));
        Assert.That(content, Does.Contain("MTN Mobile Money"));
        Assert.That(content, Does.Contain("Wave"));
    }

    [Test, Order(53)]
    public async Task Decaisser_Page_Has_Submit_Button()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/pay']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var btn = Page.GetByRole(AriaRole.Button, new() { Name = "Envoyer le paiement" });
        await Expect(btn).ToBeVisibleAsync();
    }

    // ══════════════════════════════════
    //  PWA META TAGS
    // ══════════════════════════════════

    [Test, Order(60)]
    public async Task PWA_Has_Manifest_Link()
    {
        await Page.GotoAsync($"{BaseUrl}/app");
        var manifest = Page.Locator("link[rel='manifest']");
        await Expect(manifest).ToHaveAttributeAsync("href", "/manifest.json");
    }

    [Test, Order(61)]
    public async Task PWA_Has_Theme_Color()
    {
        await Page.GotoAsync($"{BaseUrl}/app");
        var meta = Page.Locator("meta[name='theme-color']");
        await Expect(meta).ToHaveAttributeAsync("content", "#2e7d32");
    }

    [Test, Order(62)]
    public async Task PWA_Has_Viewport_Meta()
    {
        await Page.GotoAsync($"{BaseUrl}/app");
        var viewport = Page.Locator("meta[name='viewport']");
        var content = await viewport.GetAttributeAsync("content") ?? "";
        Assert.That(content, Does.Contain("width=device-width"));
    }

    [Test, Order(63)]
    public async Task PWA_Has_Apple_Mobile_Meta()
    {
        await Page.GotoAsync($"{BaseUrl}/app");
        var meta = Page.Locator("meta[name='apple-mobile-web-app-capable']");
        await Expect(meta).ToHaveAttributeAsync("content", "yes");
    }

    // ══════════════════════════════════
    //  NAVIGATION FLOW
    // ══════════════════════════════════

    [Test, Order(70)]
    public async Task Navigation_Between_All_Tabs_Works()
    {
        await NavigateAuthenticated("/");

        // Navigate to each tab and verify content changes
        string[] navPaths = { "/units", "/payments", "/incidents", "/pay", "/" };
        string[] expectedTexts = { "Lots", "Historique", "Incidents", "Décaissement", "Tableau de bord" };

        for (int i = 0; i < navPaths.Length; i++)
        {
            await Page.Locator($"[data-nav='{navPaths[i]}']").ClickAsync();
            await Page.WaitForTimeoutAsync(400);
            var text = await Page.Locator("#main-content").InnerTextAsync();
            Assert.That(text, Does.Contain(expectedTexts[i]),
                $"Tab '{navPaths[i]}' should show '{expectedTexts[i]}'");
        }
    }

    [Test, Order(71)]
    public async Task SPA_Routing_Preserves_URL()
    {
        await NavigateAuthenticated("/");
        await Page.Locator("[data-nav='/units']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        Assert.That(Page.Url, Does.Contain("/app/units"));
    }

    [Test, Order(72)]
    public async Task Fallback_Route_Loads_App()
    {
        await NavigateAuthenticated("/some-unknown-route");
        // Should still load the app shell (fallback to App.cshtml)
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("GreenSyndic"));
    }
}
