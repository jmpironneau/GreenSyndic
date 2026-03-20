using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class BuildingsControllerTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        // Create a parent org for buildings
        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Building Test Org", LegalName = "BTO" });
        var org = await orgResp.Content.ReadFromJsonAsync<OrganizationDto>();
        _orgId = org!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Create_And_GetById_Works()
    {
        var request = new CreateBuildingRequest
        {
            OrganizationId = _orgId,
            Name = "Residence Palmiers",
            Address = "Lot 12, Green City"
        };

        var createResp = await _client.PostAsJsonAsync("/api/buildings", request);
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await createResp.Content.ReadFromJsonAsync<BuildingDto>();
        Assert.That(created!.Name, Is.EqualTo("Residence Palmiers"));

        var getResp = await _client.GetAsync($"/api/buildings/{created.Id}");
        Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetAll_FilterByOrganization_Works()
    {
        await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, Name = "Filter Test" });

        var response = await _client.GetAsync($"/api/buildings?organizationId={_orgId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var buildings = await response.Content.ReadFromJsonAsync<List<BuildingDto>>();
        Assert.That(buildings, Is.Not.Null);
        Assert.That(buildings!.Count, Is.GreaterThan(0));
        Assert.That(buildings.All(b => b.OrganizationId == _orgId), Is.True);
    }

    [Test]
    public async Task SoftDelete_HidesFromGetAll()
    {
        var createResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, Name = "To Delete Building" });
        var created = await createResp.Content.ReadFromJsonAsync<BuildingDto>();

        await _client.DeleteAsync($"/api/buildings/{created!.Id}");

        var getResp = await _client.GetAsync($"/api/buildings/{created.Id}");
        Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
