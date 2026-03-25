using System.Net.Http.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace GreenSyndic.Tests.Visual;

/// <summary>
/// Base class for all Playwright visual tests.
/// Provides a shared fixture (server) and browser page with auth.
/// </summary>
public abstract class VisualTestBase : PageTest
{
    protected PlaywrightFixture Fixture = null!;
    protected string BaseUrl => Fixture.BaseUrl;
    protected HttpClient Api => Fixture.ApiClient;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        Fixture = new PlaywrightFixture();
    }

    [OneTimeTearDown]
    public void TearDownFixture()
    {
        Fixture.Dispose();
    }

    /// <summary>
    /// Navigate to a PWA page with test auth headers injected on API calls,
    /// and a fake gs_token in localStorage so the SPA router doesn't redirect to login.
    /// </summary>
    protected async Task NavigateAuthenticated(string path = "/")
    {
        // Intercept API calls to inject test auth headers
        await Page.RouteAsync("**/api/**", async route =>
        {
            var headers = new Dictionary<string, string>(route.Request.Headers)
            {
                ["X-Test-UserId"] = "test-user-id",
                ["X-Test-Role"] = "SuperAdmin",
                ["X-Test-OrgId"] = "11111111-1111-1111-1111-111111111111"
            };
            await route.ContinueAsync(new RouteContinueOptions { Headers = headers });
        });

        // Go to the app page
        await Page.GotoAsync($"{BaseUrl}/app{(path == "/" ? "" : path)}");

        // Inject a fake token so the JS router thinks we're logged in
        await Page.EvaluateAsync(@"() => {
            localStorage.setItem('gs_token', 'fake-test-token');
        }");

        // Reload to pick up the token
        await Page.ReloadAsync();

        // Wait for main content to render
        await Page.WaitForSelectorAsync("#main-content", new() { Timeout = 5000 });
    }

    /// <summary>
    /// Seed data via the API client (uses test auth headers automatically).
    /// </summary>
    protected async Task<T?> PostApi<T>(string path, object body)
    {
        var resp = await Api.PostAsJsonAsync(path, body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>();
    }
}
