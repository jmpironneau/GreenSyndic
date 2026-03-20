using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class OrganizationsControllerTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/organizations");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Create_And_GetById_ReturnsCreatedOrg()
    {
        var request = new CreateOrganizationRequest
        {
            Name = "Test Org",
            LegalName = "Test Org SARL",
            Country = "CI"
        };

        var createResp = await _client.PostAsJsonAsync("/api/organizations", request);
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await createResp.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Name, Is.EqualTo("Test Org"));

        var getResp = await _client.GetAsync($"/api/organizations/{created.Id}");
        Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var fetched = await getResp.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.That(fetched!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task Update_ExistingOrg_ReturnsNoContent()
    {
        var createResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "To Update", LegalName = "Update SARL" });
        var created = await createResp.Content.ReadFromJsonAsync<OrganizationDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/organizations/{created!.Id}",
            new CreateOrganizationRequest { Name = "Updated", LegalName = "Updated SARL" });
        Assert.That(updateResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Delete_ExistingOrg_ReturnsNoContent()
    {
        var createResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "To Delete", LegalName = "Delete SARL" });
        var created = await createResp.Content.ReadFromJsonAsync<OrganizationDto>();

        var deleteResp = await _client.DeleteAsync($"/api/organizations/{created!.Id}");
        Assert.That(deleteResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Soft deleted: should return 404
        var getResp = await _client.GetAsync($"/api/organizations/{created.Id}");
        Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/organizations/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
