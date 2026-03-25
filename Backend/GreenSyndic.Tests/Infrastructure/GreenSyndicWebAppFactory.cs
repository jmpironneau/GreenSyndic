using System.Security.Claims;
using System.Text.Encodings.Web;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GreenSyndic.Tests.Infrastructure;

/// <summary>
/// Test server factory: replaces PostgreSQL with InMemory DB and JWT with a test auth scheme.
/// </summary>
public class GreenSyndicWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext/EF registrations to avoid dual-provider error
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.FullName != null &&
                    (d.ServiceType.FullName.Contains("DbContextOptions") ||
                     d.ServiceType.FullName.Contains("IDbContextPool") ||
                     d.ServiceType == typeof(GreenSyndicDbContext)))
                .ToList();
            foreach (var d in descriptorsToRemove) services.Remove(d);

            // Add InMemory DB
            services.AddDbContext<GreenSyndicDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Remove existing auth registrations and replace with test scheme
            var authDescriptors = services
                .Where(d => d.ServiceType.FullName != null &&
                    d.ServiceType.FullName.Contains("Authentication"))
                .ToList();
            foreach (var d in authDescriptors) services.Remove(d);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(string userId = "test-user-id", string role = "SuperAdmin", string orgId = "11111111-1111-1111-1111-111111111111")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-OrgId", orgId);
        return client;
    }
}

/// <summary>
/// Fake auth handler that reads claims from test headers.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Test-UserId"].FirstOrDefault();

        // No auth header = unauthenticated request
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "SuperAdmin";
        var orgId = Request.Headers["X-Test-OrgId"].FirstOrDefault() ?? "11111111-1111-1111-1111-111111111111";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, "test@greensyndic.ci"),
            new Claim(ClaimTypes.Role, role),
            new Claim("organizationId", orgId)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
