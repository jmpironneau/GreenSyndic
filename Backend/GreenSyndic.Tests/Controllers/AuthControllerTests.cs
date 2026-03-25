using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Register_ValidRequest_ReturnsToken()
    {
        var request = new RegisterRequest
        {
            Email = $"test-{Guid.NewGuid():N}@greensyndic.ci",
            Password = "Test@2026!",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.Not.Null.And.Not.Empty);
        Assert.That(result.User.Email, Is.EqualTo(request.Email));
    }

    [Test]
    public async Task Register_DuplicateEmail_Fails()
    {
        var email = $"dup-{Guid.NewGuid():N}@greensyndic.ci";
        var request = new RegisterRequest
        {
            Email = email,
            Password = "Test@2026!",
            FirstName = "Test",
            LastName = "User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response2 = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var email = $"login-{Guid.NewGuid():N}@greensyndic.ci";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test@2026!",
            FirstName = "Login",
            LastName = "Test"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Test@2026!"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.That(result!.Token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "nobody@greensyndic.ci",
            Password = "WrongPassword1"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetUserInfo_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
