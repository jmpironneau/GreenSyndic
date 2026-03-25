using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;

namespace GreenSyndic.Tests.Visual;

/// <summary>
/// Starts the real ASP.NET server on a random port for Playwright visual tests.
/// Uses the test auth scheme (same InMemory DB + TestAuthHandler as integration tests).
/// </summary>
public class PlaywrightFixture : IDisposable
{
    private readonly Infrastructure.GreenSyndicWebAppFactory _factory;
    public string BaseUrl { get; }
    public HttpClient ApiClient { get; }

    public PlaywrightFixture()
    {
        _factory = new Infrastructure.GreenSyndicWebAppFactory();
        // CreateClient() starts the TestServer; the factory's Server.BaseAddress has the actual URL
        ApiClient = _factory.CreateAuthenticatedClient();
        BaseUrl = _factory.Server.BaseAddress.ToString().TrimEnd('/');
    }

    /// <summary>
    /// Navigate to a PWA page, injecting test auth headers via cookie/localStorage workaround.
    /// Since Playwright can't set custom headers on navigation, we inject a fake JWT token
    /// and also set the test headers on API calls via route interception.
    /// </summary>
    public async Task<IPage> CreateAuthenticatedPage(IBrowser browser)
    {
        var page = await browser.NewPageAsync();

        // Intercept all API requests to add test auth headers
        await page.RouteAsync("**/api/**", async route =>
        {
            var headers = new Dictionary<string, string>(route.Request.Headers)
            {
                ["X-Test-UserId"] = "test-user-id",
                ["X-Test-Role"] = "SuperAdmin",
                ["X-Test-OrgId"] = "11111111-1111-1111-1111-111111111111"
            };
            await route.ContinueAsync(new RouteContinueOptions { Headers = headers });
        });

        return page;
    }

    public void Dispose()
    {
        ApiClient.Dispose();
        _factory.Dispose();
    }
}
