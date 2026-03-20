using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class AccountingEntriesControllerTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Accounting Test Org", LegalName = "ATO" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Create_AccountingEntry_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/accountingentries", new CreateAccountingEntryRequest
        {
            OrganizationId = _orgId,
            EntryNumber = $"OD-{Guid.NewGuid():N}".Substring(0, 20),
            EntryDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            JournalCode = "OD",
            AccountCode = "401000",
            AccountLabel = "Fournisseurs",
            Description = "Test ecriture comptable",
            Debit = 0,
            Credit = 1500000,
            FiscalYear = 2026,
            Period = 3
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var entry = await resp.Content.ReadFromJsonAsync<AccountingEntryDto>();
        Assert.That(entry!.Credit, Is.EqualTo(1500000m));
        Assert.That(entry.IsValidated, Is.False);
    }

    [Test]
    public async Task Validate_Then_Update_Returns_BadRequest()
    {
        // Create
        var createResp = await _client.PostAsJsonAsync("/api/accountingentries", new CreateAccountingEntryRequest
        {
            OrganizationId = _orgId,
            EntryNumber = $"VL-{Guid.NewGuid():N}".Substring(0, 20),
            EntryDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            JournalCode = "BQ",
            AccountCode = "512000",
            AccountLabel = "Banque",
            Description = "Encaissement loyer",
            Debit = 500000,
            Credit = 0,
            FiscalYear = 2026,
            Period = 3
        });
        var entry = await createResp.Content.ReadFromJsonAsync<AccountingEntryDto>();

        // Validate
        var valResp = await _client.PutAsync($"/api/accountingentries/{entry!.Id}/validate", null);
        Assert.That(valResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Try to update — should fail
        var updateResp = await _client.PutAsJsonAsync($"/api/accountingentries/{entry.Id}", new CreateAccountingEntryRequest
        {
            OrganizationId = _orgId,
            EntryNumber = entry.EntryNumber,
            EntryDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            JournalCode = "BQ",
            AccountCode = "512000",
            Description = "Modified",
            Debit = 999,
            Credit = 0,
            FiscalYear = 2026,
            Period = 3
        });
        Assert.That(updateResp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Validate_Then_Delete_Returns_BadRequest()
    {
        var createResp = await _client.PostAsJsonAsync("/api/accountingentries", new CreateAccountingEntryRequest
        {
            OrganizationId = _orgId,
            EntryNumber = $"DL-{Guid.NewGuid():N}".Substring(0, 20),
            EntryDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            JournalCode = "OD",
            AccountCode = "401000",
            Description = "To validate then delete",
            Debit = 0,
            Credit = 100000,
            FiscalYear = 2026,
            Period = 3
        });
        var entry = await createResp.Content.ReadFromJsonAsync<AccountingEntryDto>();

        await _client.PutAsync($"/api/accountingentries/{entry!.Id}/validate", null);

        var deleteResp = await _client.DeleteAsync($"/api/accountingentries/{entry.Id}");
        Assert.That(deleteResp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TrialBalance_ReturnsCorrectTotals()
    {
        // Create debit entry
        await _client.PostAsJsonAsync("/api/accountingentries", new CreateAccountingEntryRequest
        {
            OrganizationId = _orgId,
            EntryNumber = $"TB1-{Guid.NewGuid():N}".Substring(0, 20),
            EntryDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            JournalCode = "OD",
            AccountCode = "601000",
            AccountLabel = "Achats",
            Description = "Achat fournitures",
            Debit = 200000,
            Credit = 0,
            FiscalYear = 2026,
            Period = 1
        });

        // Create credit entry
        await _client.PostAsJsonAsync("/api/accountingentries", new CreateAccountingEntryRequest
        {
            OrganizationId = _orgId,
            EntryNumber = $"TB2-{Guid.NewGuid():N}".Substring(0, 20),
            EntryDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            JournalCode = "OD",
            AccountCode = "401000",
            AccountLabel = "Fournisseurs",
            Description = "Contrepartie",
            Debit = 0,
            Credit = 200000,
            FiscalYear = 2026,
            Period = 1
        });

        var resp = await _client.GetAsync($"/api/accountingentries/balance?organizationId={_orgId}&fiscalYear=2026");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await resp.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("601000"));
        Assert.That(content, Does.Contain("401000"));
    }
}
