using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase5_CollectionsTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _anonClient = null!;
    private Guid _orgId;
    private Guid _coOwnershipId;
    private Guid _buildingId;
    private Guid _unitId;
    private Guid _tenantId;
    private Guid _leaseId;
    private Guid _ownerId;
    private Guid _rentCallId;
    private Guid _chargeAssignmentId;
    private Guid _chargeDefinitionId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();
        _anonClient = _factory.CreateClient();

        // Org
        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Collections Org", LegalName = "CO" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        // CoOwnership
        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro Coll", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        // Building
        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, CoOwnershipId = _coOwnershipId, Name = "Bat Coll", PrimaryType = PropertyType.Apartment });
        _buildingId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        // Unit
        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = _buildingId, CoOwnershipId = _coOwnershipId, Reference = "COLL-01", Type = PropertyType.Apartment, AreaSqm = 70 });
        _unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        // Owner
        var ownResp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Mamadou", LastName = "Diallo", Email = "mamadou.coll@test.ci", Phone = "+2250700060001" });
        _ownerId = (await ownResp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;

        // Tenant
        var tenResp = await _client.PostAsJsonAsync("/api/leasetenants",
            new CreateLeaseTenantRequest { FirstName = "Fatou", LastName = "Bamba", Email = "fatou.coll@test.ci", Phone = "+2250700060002" });
        _tenantId = (await tenResp.Content.ReadFromJsonAsync<LeaseTenantDto>())!.Id;

        // Lease (active)
        var leaseResp = await _client.PostAsJsonAsync("/api/leases",
            new CreateLeaseRequest
            {
                UnitId = _unitId,
                LeaseTenantId = _tenantId,
                Reference = "BAIL-COLL-001",
                Type = LeaseType.Residential,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DurationMonths = 36,
                MonthlyRent = 200000,
                Charges = 30000,
                SecurityDeposit = 400000
            });
        _leaseId = (await leaseResp.Content.ReadFromJsonAsync<LeaseDto>())!.Id;
        await _client.PutAsync($"/api/leases/{_leaseId}/activate", null);

        // Rent call (Sent, unpaid) — should appear in pending
        var rcResp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Year = 2026,
                Month = 3,
                DueDate = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            });
        _rentCallId = (await rcResp.Content.ReadFromJsonAsync<RentCallDto>())!.Id;
        // Send it so it's not Draft
        await _client.PutAsync($"/api/rentcalls/{_rentCallId}/send", null);

        // Charge definition
        var cdResp = await _client.PostAsJsonAsync("/api/chargedefinitions",
            new CreateChargeDefinitionRequest
            {
                CoOwnershipId = _coOwnershipId,
                Name = "Gardiennage",
                Type = ChargeType.Security,
                AnnualAmount = 480000,
                IsRecoverable = true
            });
        _chargeDefinitionId = (await cdResp.Content.ReadFromJsonAsync<ChargeDefinitionDto>())!.Id;

        // Charge assignment (unpaid) — should appear in pending
        var caResp = await _client.PostAsJsonAsync("/api/chargeassignments",
            new CreateChargeAssignmentRequest
            {
                ChargeDefinitionId = _chargeDefinitionId,
                UnitId = _unitId,
                Year = 2026,
                Quarter = 1,
                Amount = 120000,
                DueDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
            });
        _chargeAssignmentId = (await caResp.Content.ReadFromJsonAsync<ChargeAssignmentDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _anonClient.Dispose();
        _factory.Dispose();
    }

    // ══════════════════════════════════
    //  GET /api/collections/pending
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Pending_Returns_RentCalls_And_Charges()
    {
        var resp = await _client.GetAsync("/api/collections/pending");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"rentCalls\""));
        Assert.That(body, Does.Contain("\"charges\""));
        Assert.That(body, Does.Contain("\"totalPending\""));
        Assert.That(body, Does.Contain("\"totalOverdue\""));
        Assert.That(body, Does.Contain("\"all\""));
    }

    [Test, Order(2)]
    public async Task Pending_Contains_Seeded_RentCall()
    {
        var resp = await _client.GetAsync("/api/collections/pending");
        var body = await resp.Content.ReadAsStringAsync();

        // Rent call should show type, tenant name, amounts
        Assert.That(body, Does.Contain("\"type\":\"RentCall\""));
        Assert.That(body, Does.Contain("\"totalAmount\":230000")); // 200000 rent + 30000 charges
        Assert.That(body, Does.Contain("Fatou"));
        Assert.That(body, Does.Contain("Bamba"));
    }

    [Test, Order(3)]
    public async Task Pending_Contains_Seeded_ChargeAssignment()
    {
        var resp = await _client.GetAsync("/api/collections/pending");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("\"type\":\"ChargeAssignment\""));
        Assert.That(body, Does.Contain("\"totalAmount\":120000"));
        Assert.That(body, Does.Contain("Gardiennage"));
    }

    [Test, Order(4)]
    public async Task Pending_Filter_By_OrgId_Works()
    {
        // Real org
        var resp = await _client.GetAsync($"/api/collections/pending?organizationId={_orgId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"type\":\"RentCall\""));

        // Fake org — no results
        var fakeResp = await _client.GetAsync($"/api/collections/pending?organizationId={Guid.NewGuid()}");
        var fakeBody = await fakeResp.Content.ReadAsStringAsync();
        Assert.That(fakeBody, Does.Contain("\"totalPending\":0"));
    }

    [Test, Order(5)]
    public async Task Pending_Requires_Auth()
    {
        var resp = await _anonClient.GetAsync("/api/collections/pending");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  POST /api/collections/record — Bank Transfer
    // ══════════════════════════════════

    [Test, Order(10)]
    public async Task Record_BankTransfer_Creates_Payment()
    {
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 50000,
            method = PaymentMethod.BankTransfer,
            description = "Virement reçu",
            bankReference = "VIR-2026-001",
            payerName = "Mamadou Diallo",
            ownerId = _ownerId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"paymentId\""));
        Assert.That(body, Does.Contain("\"reference\":\"ENC-"));
        Assert.That(body, Does.Contain("\"method\":\"BankTransfer\""));
        Assert.That(body, Does.Contain("\"status\":\"Completed\""));
    }

    [Test, Order(11)]
    public async Task Record_Cash_Creates_Payment()
    {
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 75000,
            method = PaymentMethod.Cash,
            description = "Espèces reçues",
            payerName = "Fatou Bamba",
            ownerId = _ownerId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"method\":\"Cash\""));
        Assert.That(body, Does.Contain("\"status\":\"Completed\""));
    }

    [Test, Order(12)]
    public async Task Record_Check_Creates_Payment()
    {
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 100000,
            method = PaymentMethod.Check,
            description = "Chèque reçu",
            bankReference = "CHQ-9876543",
            ownerId = _ownerId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"method\":\"Check\""));
    }

    [Test, Order(13)]
    public async Task Record_Requires_Auth()
    {
        var resp = await _anonClient.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 10000,
            method = PaymentMethod.Cash
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  POST /api/collections/record — Reconciliation with RentCall
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task Record_Partial_RentCall_Reconciliation()
    {
        // Pay partially: 100000 on a 230000 rent call
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 100000,
            method = PaymentMethod.BankTransfer,
            bankReference = "VIR-PART-001",
            rentCallId = _rentCallId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"reconciled\":true"));
        Assert.That(body, Does.Contain("\"receiptGenerated\":false")); // Not fully paid yet

        // Verify rent call status changed to PartiallyPaid in pending list
        var pendingResp = await _client.GetAsync("/api/collections/pending");
        var pendingBody = await pendingResp.Content.ReadAsStringAsync();
        Assert.That(pendingBody, Does.Contain("\"status\":\"PartiallyPaid\""));
        Assert.That(pendingBody, Does.Contain("\"paidAmount\":100000"));
    }

    [Test, Order(21)]
    public async Task Record_Full_RentCall_Generates_Receipt()
    {
        // Pay the remaining 130000 (230000 total - 100000 already paid)
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 130000,
            method = PaymentMethod.Cash,
            rentCallId = _rentCallId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"reconciled\":true"));
        Assert.That(body, Does.Contain("\"receiptGenerated\":true"));
        Assert.That(body, Does.Contain("\"receiptReference\":\"QT-"));
    }

    [Test, Order(22)]
    public async Task RentCall_Fully_Paid_Disappears_From_Pending()
    {
        // After full payment, this rent call should no longer be in pending
        var resp = await _client.GetAsync("/api/collections/pending");
        var body = await resp.Content.ReadAsStringAsync();

        // The rent call should be marked Paid, so it won't appear in pending
        // (filter: Status != Paid && Status != Cancelled)
        Assert.That(body, Does.Not.Contain($"\"{_rentCallId}\"").IgnoreCase
            .Or.Contain("\"status\":\"Paid\""));
    }

    // ══════════════════════════════════
    //  POST /api/collections/record — Reconciliation with ChargeAssignment
    // ══════════════════════════════════

    [Test, Order(30)]
    public async Task Record_Partial_ChargeAssignment_Reconciliation()
    {
        // Pay partially: 80000 on a 120000 charge assignment
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 80000,
            method = PaymentMethod.Cash,
            chargeAssignmentId = _chargeAssignmentId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"reconciled\":true"));
    }

    [Test, Order(31)]
    public async Task Record_Full_ChargeAssignment_Marks_Paid()
    {
        // Pay remaining 40000 (120000 - 80000)
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 40000,
            method = PaymentMethod.BankTransfer,
            bankReference = "VIR-CA-FULL",
            chargeAssignmentId = _chargeAssignmentId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"reconciled\":true"));
    }

    [Test, Order(32)]
    public async Task ChargeAssignment_Fully_Paid_Disappears_From_Pending()
    {
        var resp = await _client.GetAsync("/api/collections/pending");
        var body = await resp.Content.ReadAsStringAsync();

        // Charge assignment should no longer appear as pending (IsPaid = true)
        Assert.That(body, Does.Not.Contain("\"totalAmount\":120000"));
    }

    // ══════════════════════════════════
    //  GET /api/collections/summary
    // ══════════════════════════════════

    [Test, Order(40)]
    public async Task Summary_Returns_Period_And_Totals()
    {
        var resp = await _client.GetAsync("/api/collections/summary");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"period\""));
        Assert.That(body, Does.Contain("\"totalCollected\""));
        Assert.That(body, Does.Contain("\"totalCount\""));
        Assert.That(body, Does.Contain("\"byMethod\""));
    }

    [Test, Order(41)]
    public async Task Summary_Groups_By_Payment_Method()
    {
        var resp = await _client.GetAsync("/api/collections/summary");
        var body = await resp.Content.ReadAsStringAsync();

        // We recorded payments with both BankTransfer and Cash
        Assert.That(body, Does.Contain("\"method\":\"BankTransfer\""));
        Assert.That(body, Does.Contain("\"method\":\"Cash\""));
    }

    [Test, Order(42)]
    public async Task Summary_Filter_By_OrgId()
    {
        var fakeOrgId = Guid.NewGuid();
        var resp = await _client.GetAsync($"/api/collections/summary?organizationId={fakeOrgId}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"totalCollected\":0"));
        Assert.That(body, Does.Contain("\"totalCount\":0"));
    }

    [Test, Order(43)]
    public async Task Summary_Filter_By_Date_Range()
    {
        // Future range — no payments
        var from = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.AddYears(2).ToString("yyyy-MM-dd");
        var resp = await _client.GetAsync($"/api/collections/summary?from={from}&to={to}");

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"totalCollected\":0"));
    }

    [Test, Order(44)]
    public async Task Summary_Requires_Auth()
    {
        var resp = await _anonClient.GetAsync("/api/collections/summary");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  EDGE CASES
    // ══════════════════════════════════

    [Test, Order(50)]
    public async Task Record_Without_Reconciliation_Works()
    {
        // Free-standing payment, no rent call or charge assignment
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 25000,
            method = PaymentMethod.Cash,
            description = "Avance sur charges",
            payerName = "Visiteur"
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"reconciled\":false"));
    }

    [Test, Order(51)]
    public async Task Record_With_Custom_PaymentDate()
    {
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 15000,
            method = PaymentMethod.BankTransfer,
            paymentDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            ownerId = _ownerId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test, Order(52)]
    public async Task Record_MobileMoney_Method()
    {
        var resp = await _client.PostAsJsonAsync("/api/collections/record", new
        {
            amount = 30000,
            method = PaymentMethod.OrangeMoney,
            description = "Orange Money reçu",
            bankReference = "OM-TXN-12345"
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"method\":\"OrangeMoney\""));
    }
}
