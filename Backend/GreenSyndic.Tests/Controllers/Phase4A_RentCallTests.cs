using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase4A_RentCallTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;
    private Guid _coOwnershipId;
    private Guid _buildingId;
    private Guid _unitId;
    private Guid _tenantId;
    private Guid _leaseId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        // Org
        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Phase4 Org", LegalName = "P4" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        // CoOwnership
        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro P4", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        // Building
        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, CoOwnershipId = _coOwnershipId, Name = "Bat A", PrimaryType = PropertyType.Apartment });
        _buildingId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        // Unit
        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = _buildingId, CoOwnershipId = _coOwnershipId, Reference = "APT-P4-01", Type = PropertyType.Apartment, AreaSqm = 85 });
        _unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        // Tenant
        var tenResp = await _client.PostAsJsonAsync("/api/leasetenants",
            new CreateLeaseTenantRequest { FirstName = "Amadou", LastName = "Koné", Email = "amadou@test.ci", Phone = "+2250700000010" });
        _tenantId = (await tenResp.Content.ReadFromJsonAsync<LeaseTenantDto>())!.Id;

        // Lease (active)
        var leaseResp = await _client.PostAsJsonAsync("/api/leases",
            new CreateLeaseRequest
            {
                UnitId = _unitId,
                LeaseTenantId = _tenantId,
                Reference = "BAIL-P4-001",
                Type = LeaseType.Residential,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DurationMonths = 36,
                MonthlyRent = 350000,
                Charges = 50000,
                SecurityDeposit = 700000
            });
        _leaseId = (await leaseResp.Content.ReadFromJsonAsync<LeaseDto>())!.Id;

        // Activate lease
        await _client.PutAsync($"/api/leases/{_leaseId}/activate", null);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Rent Calls ──

    [Test, Order(1)]
    public async Task Create_RentCall_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Year = 2026,
                Month = 3,
                DueDate = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var rc = await resp.Content.ReadFromJsonAsync<RentCallDto>();
        Assert.That(rc!.RentAmount, Is.EqualTo(350000));
        Assert.That(rc.ChargesAmount, Is.EqualTo(50000));
        Assert.That(rc.TotalAmount, Is.EqualTo(400000));
        Assert.That(rc.Status, Is.EqualTo(RentCallStatus.Draft));
    }

    [Test, Order(2)]
    public async Task Create_Duplicate_RentCall_Returns_Conflict()
    {
        var resp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Year = 2026,
                Month = 3,
                DueDate = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test, Order(3)]
    public async Task Send_RentCall_Works()
    {
        var list = await _client.GetFromJsonAsync<List<RentCallDto>>($"/api/rentcalls?leaseId={_leaseId}&year=2026&month=3");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/rentcalls/{id}/send", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<RentCallDto>($"/api/rentcalls/{id}");
        Assert.That(updated!.Status, Is.EqualTo(RentCallStatus.Sent));
        Assert.That(updated.SentAt, Is.Not.Null);
    }

    [Test, Order(4)]
    public async Task Generate_RentCalls_Batch()
    {
        var resp = await _client.PostAsJsonAsync("/api/rentcalls/generate",
            new GenerateRentCallsRequest
            {
                OrganizationId = _orgId,
                Year = 2026,
                Month = 4,
                DueDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await resp.Content.ReadFromJsonAsync<GenerateRentCallsResultDto>();
        Assert.That(result!.GeneratedCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test, Order(5)]
    public async Task Generate_RentCalls_Skips_Existing()
    {
        var resp = await _client.PostAsJsonAsync("/api/rentcalls/generate",
            new GenerateRentCallsRequest
            {
                OrganizationId = _orgId,
                Year = 2026,
                Month = 4,
                DueDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            });

        var result = await resp.Content.ReadFromJsonAsync<GenerateRentCallsResultDto>();
        Assert.That(result!.SkippedCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(result.GeneratedCount, Is.EqualTo(0));
    }

    [Test, Order(6)]
    public async Task Cancel_RentCall_Works()
    {
        // Create a new rent call to cancel
        var createResp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Year = 2026,
                Month = 5,
                DueDate = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc)
            });
        var rc = await createResp.Content.ReadFromJsonAsync<RentCallDto>();

        var resp = await _client.PostAsync($"/api/rentcalls/{rc!.Id}/cancel", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(7)]
    public async Task Delete_Paid_RentCall_Returns_BadRequest()
    {
        // Use the March rent call (sent). We can't easily mark it paid here,
        // but we can test delete of non-paid
        var list = await _client.GetFromJsonAsync<List<RentCallDto>>($"/api/rentcalls?leaseId={_leaseId}&year=2026&month=3");
        var id = list!.First().Id;

        // Sent rent call can be deleted (not paid)
        // Let's create one, reconcile via payment to make it paid, then try delete
        var createResp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Year = 2026,
                Month = 6,
                DueDate = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)
            });
        var rc = await createResp.Content.ReadFromJsonAsync<RentCallDto>();

        // Send it
        await _client.PostAsync($"/api/rentcalls/{rc!.Id}/send", null);

        // Create payment and reconcile
        var payResp = await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest
            {
                Amount = 400000,
                Method = PaymentMethod.OrangeMoney,
                PaymentDate = DateTime.UtcNow,
                LeaseTenantId = _tenantId,
                LeaseId = _leaseId,
                Description = "Loyer juin"
            });
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentDto>();
        await _client.PutAsync($"/api/payments/{payment!.Id}/confirm", null);

        await _client.PostAsJsonAsync($"/api/payments/{payment.Id}/reconcile",
            new ReconcilePaymentRequest { RentCallId = rc.Id });

        // Now try to delete paid rent call
        var resp = await _client.DeleteAsync($"/api/rentcalls/{rc.Id}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(8)]
    public async Task GetAll_Filter_By_Status()
    {
        var resp = await _client.GetAsync($"/api/rentcalls?organizationId={_orgId}&status={RentCallStatus.Sent}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var items = await resp.Content.ReadFromJsonAsync<List<RentCallDto>>();
        Assert.That(items!.All(r => r.Status == RentCallStatus.Sent), Is.True);
    }

    // ── Receipts ──

    [Test, Order(9)]
    public async Task AutoGenerated_Receipt_Exists_After_Reconciliation()
    {
        // The June reconciliation (Order 7) should have auto-generated a receipt
        var resp = await _client.GetAsync($"/api/rentreceipts?leaseId={_leaseId}&year=2026&month=6");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var items = await resp.Content.ReadFromJsonAsync<List<RentReceiptDto>>();
        Assert.That(items!.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(items[0].Status, Is.EqualTo(RentReceiptStatus.Issued));
    }

    [Test, Order(10)]
    public async Task Create_Receipt_Manually()
    {
        // Create a rent call for July first
        var rcResp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Year = 2026,
                Month = 7,
                DueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc)
            });
        var rc = await rcResp.Content.ReadFromJsonAsync<RentCallDto>();

        var resp = await _client.PostAsJsonAsync("/api/rentreceipts",
            new CreateRentReceiptRequest
            {
                OrganizationId = _orgId,
                RentCallId = rc!.Id,
                RentAmount = 350000,
                ChargesAmount = 50000
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var receipt = await resp.Content.ReadFromJsonAsync<RentReceiptDto>();
        Assert.That(receipt!.TotalAmount, Is.EqualTo(400000));
        Assert.That(receipt.Status, Is.EqualTo(RentReceiptStatus.Draft));
    }

    [Test, Order(11)]
    public async Task Issue_And_Send_Receipt()
    {
        var list = await _client.GetFromJsonAsync<List<RentReceiptDto>>($"/api/rentreceipts?leaseId={_leaseId}&year=2026&month=7");
        var id = list!.First().Id;

        var issueResp = await _client.PostAsync($"/api/rentreceipts/{id}/issue", null);
        Assert.That(issueResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var sendResp = await _client.PostAsync($"/api/rentreceipts/{id}/send", null);
        Assert.That(sendResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<RentReceiptDto>($"/api/rentreceipts/{id}");
        Assert.That(updated!.Status, Is.EqualTo(RentReceiptStatus.Sent));
        Assert.That(updated.SentAt, Is.Not.Null);
    }

    [Test, Order(12)]
    public async Task Delete_Sent_Receipt_Returns_BadRequest()
    {
        var list = await _client.GetFromJsonAsync<List<RentReceiptDto>>($"/api/rentreceipts?leaseId={_leaseId}&year=2026&month=7");
        var id = list!.First().Id;

        var resp = await _client.DeleteAsync($"/api/rentreceipts/{id}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
