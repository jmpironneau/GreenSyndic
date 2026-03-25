using Microsoft.Playwright;

namespace GreenSyndic.Tests.Visual;

/// <summary>
/// Phase 6 — Visual tests for secondary modules.
/// Tests incident page interactions, export downloads, landing page.
/// </summary>
[TestFixture]
public class Phase6_ModulesVisualTests : VisualTestBase
{
    // ══════════════════════════════════
    //  INCIDENTS PAGE — FAB + MODAL
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Incidents_FAB_Opens_Create_Modal()
    {
        await NavigateAuthenticated("/incidents");

        var fab = Page.Locator("#fab");
        await Expect(fab).ToBeVisibleAsync();
        await fab.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Modal should appear with incident creation form
        var modal = Page.Locator("#modal-overlay");
        var display = await modal.EvaluateAsync<string>(
            "el => getComputedStyle(el).display");
        Assert.That(display, Is.Not.EqualTo("none"),
            "Incident creation modal should be visible");
    }

    [Test, Order(2)]
    public async Task Incidents_Modal_Has_Title_And_Description_Fields()
    {
        await NavigateAuthenticated("/incidents");
        await Page.Locator("#fab").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var modalContent = await Page.Locator(".modal-body").InnerTextAsync();
        // Should have form fields for title and description
        Assert.That(modalContent, Does.Contain("Titre").Or.Contain("titre").Or.Contain("Title"));
    }

    [Test, Order(3)]
    public async Task Incidents_Modal_Has_Priority_Select()
    {
        await NavigateAuthenticated("/incidents");
        await Page.Locator("#fab").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Should have priority selection
        var selects = Page.Locator(".modal-body select");
        var count = await selects.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(1), "Should have at least one select (priority)");
    }

    // ══════════════════════════════════
    //  LANDING PAGE
    // ══════════════════════════════════

    [Test, Order(10)]
    public async Task Landing_Page_Has_App_Link()
    {
        await Page.GotoAsync($"{BaseUrl}/index.html");

        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("href=\"/app\""));
        Assert.That(content, Does.Contain("Ouvrir l'application").Or.Contain("GreenSyndic"));
    }

    [Test, Order(11)]
    public async Task Landing_Page_Has_Logo()
    {
        await Page.GotoAsync($"{BaseUrl}/index.html");

        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("GreenSyndic"));
    }

    // ══════════════════════════════════
    //  EXPORT — CSV DOWNLOAD
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task Export_CSV_Downloads_File()
    {
        await NavigateAuthenticated("/");

        // Intercept the download by navigating to the export URL directly via API
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await Page.EvaluateAsync(@"() => {
                const a = document.createElement('a');
                a.href = '/api/export/units';
                a.download = 'lots.csv';
                document.body.appendChild(a);
                a.click();
            }");
        });

        Assert.That(download.SuggestedFilename, Does.Contain(".csv"));
    }

    // ══════════════════════════════════
    //  SERVICE WORKER
    // ══════════════════════════════════

    [Test, Order(30)]
    public async Task ServiceWorker_File_Is_Accessible()
    {
        var resp = await Page.APIRequest.GetAsync($"{BaseUrl}/sw.js");
        Assert.That(resp.Status, Is.EqualTo(200));

        var body = await resp.TextAsync();
        Assert.That(body, Does.Contain("greensyndic-v1"));
        Assert.That(body, Does.Contain("addEventListener"));
    }

    [Test, Order(31)]
    public async Task Manifest_Is_Accessible()
    {
        var resp = await Page.APIRequest.GetAsync($"{BaseUrl}/manifest.json");
        Assert.That(resp.Status, Is.EqualTo(200));

        var body = await resp.TextAsync();
        Assert.That(body, Does.Contain("GreenSyndic"));
        Assert.That(body, Does.Contain("standalone"));
    }

    // ══════════════════════════════════
    //  MODAL SYSTEM
    // ══════════════════════════════════

    [Test, Order(40)]
    public async Task Modal_Closes_On_Overlay_Click()
    {
        await NavigateAuthenticated("/incidents");
        await Page.Locator("#fab").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Modal should be visible
        var overlay = Page.Locator("#modal-overlay");
        var displayBefore = await overlay.EvaluateAsync<string>(
            "el => getComputedStyle(el).display");
        Assert.That(displayBefore, Is.Not.EqualTo("none"));

        // Click on overlay (outside modal content) to close
        await overlay.ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Page.WaitForTimeoutAsync(300);

        var displayAfter = await overlay.EvaluateAsync<string>(
            "el => getComputedStyle(el).display");
        Assert.That(displayAfter, Is.EqualTo("none"),
            "Modal should close when clicking overlay");
    }

    // ══════════════════════════════════
    //  TOAST NOTIFICATION
    // ══════════════════════════════════

    [Test, Order(50)]
    public async Task Toast_Element_Exists()
    {
        await Page.GotoAsync($"{BaseUrl}/app");

        var toast = Page.Locator("#toast");
        // Toast should exist in DOM (hidden by default)
        Assert.That(await toast.CountAsync(), Is.EqualTo(1));
    }

    // ══════════════════════════════════
    //  LOGOUT
    // ══════════════════════════════════

    [Test, Order(60)]
    public async Task Logout_Button_Exists()
    {
        await NavigateAuthenticated("/");

        var logoutBtn = Page.Locator(".nav-btn");
        await Expect(logoutBtn).ToBeVisibleAsync();
        await Expect(logoutBtn).ToContainTextAsync("⏻");
    }

    [Test, Order(61)]
    public async Task Logout_Redirects_To_Login()
    {
        await NavigateAuthenticated("/");

        // Click logout
        await Page.Locator(".nav-btn").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Should redirect to login (token removed)
        var loginForm = Page.Locator("input[placeholder='Email']");
        await Expect(loginForm).ToBeVisibleAsync();
    }
}
