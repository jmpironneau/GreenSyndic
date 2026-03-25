using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase4C_ApplicationLeaseTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;
    private Guid _unitId;
    private Guid _leaseId;
    private Guid _tenantId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Phase4C Org", LegalName = "P4C" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro App", Level = CoOwnershipLevel.Horizontal });
        var coId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, CoOwnershipId = coId, Name = "Bat C", PrimaryType = PropertyType.Apartment });
        var bldId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = bldId, CoOwnershipId = coId, Reference = "APT-APP-01", Type = PropertyType.Apartment, AreaSqm = 90 });
        _unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        var tenResp = await _client.PostAsJsonAsync("/api/leasetenants",
            new CreateLeaseTenantRequest { FirstName = "Ibrahim", LastName = "Sanogo", Email = "ibrahim@test.ci", Phone = "+2250700000030" });
        _tenantId = (await tenResp.Content.ReadFromJsonAsync<LeaseTenantDto>())!.Id;

        // Active lease for renew/terminate tests
        var leaseResp = await _client.PostAsJsonAsync("/api/leases",
            new CreateLeaseRequest
            {
                UnitId = _unitId,
                LeaseTenantId = _tenantId,
                Reference = "BAIL-APP-001",
                Type = LeaseType.Commercial,
                StartDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                DurationMonths = 36,
                MonthlyRent = 500000,
                Charges = 80000,
                SecurityDeposit = 1000000,
                TurnoverRentPercent = 5
            });
        _leaseId = (await leaseResp.Content.ReadFromJsonAsync<LeaseDto>())!.Id;
        await _client.PutAsync($"/api/leases/{_leaseId}/activate", null);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Tenant Applications ──

    [Test, Order(1)]
    public async Task Create_Application_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/tenantapplications",
            new CreateTenantApplicationRequest
            {
                OrganizationId = _orgId,
                UnitId = _unitId,
                FirstName = "Mariam",
                LastName = "Touré",
                Email = "mariam@test.ci",
                Phone = "+2250700000040",
                MonthlyIncome = 1500000,
                EmployerName = "COFIPRI",
                EmploymentType = "CDI",
                EmploymentDurationMonths = 24,
                GuarantorName = "Papa Touré",
                GuarantorPhone = "+2250700000041",
                GuarantorRelation = "Père",
                NationalId = "CI-123456",
                TaxId = "RCCM-789",
                DesiredRent = 400000
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var app = await resp.Content.ReadFromJsonAsync<TenantApplicationDto>();
        Assert.That(app!.Status, Is.EqualTo(ApplicationStatus.Submitted));
        Assert.That(app.Reference, Does.StartWith("CAND-"));
    }

    [Test, Order(2)]
    public async Task Score_Application_Works()
    {
        var list = await _client.GetFromJsonAsync<List<TenantApplicationDto>>($"/api/tenantapplications?organizationId={_orgId}");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/tenantapplications/{id}/score", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var score = await resp.Content.ReadFromJsonAsync<ApplicationScoreDto>();
        // Income 1.5M / Rent 400K = 3.75 → 30pts income
        // CDI 24 months → 25pts employment
        // Guarantor with phone → 20pts
        // NationalId (8) + TaxId (7) → 15pts documents
        Assert.That(score!.TotalScore, Is.EqualTo(90));
        Assert.That(score.Level, Is.EqualTo(ApplicationScoreLevel.Excellent));
        Assert.That(score.IncomeToRentRatio, Is.EqualTo(3.75m));
    }

    [Test, Order(3)]
    public async Task Request_Documents_Works()
    {
        var list = await _client.GetFromJsonAsync<List<TenantApplicationDto>>($"/api/tenantapplications?organizationId={_orgId}");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/tenantapplications/{id}/request-documents", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<TenantApplicationDto>($"/api/tenantapplications/{id}");
        Assert.That(updated!.Status, Is.EqualTo(ApplicationStatus.DocumentsPending));
    }

    [Test, Order(4)]
    public async Task Approve_Application_Works()
    {
        var list = await _client.GetFromJsonAsync<List<TenantApplicationDto>>($"/api/tenantapplications?organizationId={_orgId}");
        var id = list!.First().Id;

        var resp = await _client.PostAsJsonAsync($"/api/tenantapplications/{id}/review",
            new ReviewApplicationRequest { Approved = true });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<TenantApplicationDto>($"/api/tenantapplications/{id}");
        Assert.That(updated!.Status, Is.EqualTo(ApplicationStatus.Approved));
        Assert.That(updated.ReviewedAt, Is.Not.Null);
    }

    [Test, Order(5)]
    public async Task Reject_Application_Works()
    {
        // Create second application to reject
        var createResp = await _client.PostAsJsonAsync("/api/tenantapplications",
            new CreateTenantApplicationRequest
            {
                OrganizationId = _orgId,
                UnitId = _unitId,
                FirstName = "Koffi",
                LastName = "Yao",
                Email = "koffi@test.ci",
                Phone = "+2250700000050",
                DesiredRent = 400000
            });
        var app = await createResp.Content.ReadFromJsonAsync<TenantApplicationDto>();

        var resp = await _client.PostAsJsonAsync($"/api/tenantapplications/{app!.Id}/review",
            new ReviewApplicationRequest { Approved = false, RejectionReason = "Dossier incomplet" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<TenantApplicationDto>($"/api/tenantapplications/{app.Id}");
        Assert.That(updated!.Status, Is.EqualTo(ApplicationStatus.Rejected));
        Assert.That(updated.RejectionReason, Is.EqualTo("Dossier incomplet"));
    }

    [Test, Order(6)]
    public async Task Withdraw_Application_Works()
    {
        var createResp = await _client.PostAsJsonAsync("/api/tenantapplications",
            new CreateTenantApplicationRequest
            {
                OrganizationId = _orgId,
                UnitId = _unitId,
                FirstName = "Aya",
                LastName = "Bamba",
                Email = "aya@test.ci",
                Phone = "+2250700000060",
                DesiredRent = 400000
            });
        var app = await createResp.Content.ReadFromJsonAsync<TenantApplicationDto>();

        var resp = await _client.PostAsync($"/api/tenantapplications/{app!.Id}/withdraw", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(7)]
    public async Task Score_Weak_Application()
    {
        // Application with minimal info → low score
        var createResp = await _client.PostAsJsonAsync("/api/tenantapplications",
            new CreateTenantApplicationRequest
            {
                OrganizationId = _orgId,
                UnitId = _unitId,
                FirstName = "Test",
                LastName = "Faible",
                Email = "faible@test.ci",
                Phone = "+2250700000070",
                MonthlyIncome = 300000,
                DesiredRent = 400000  // ratio 0.75 → 0pts income
            });
        var app = await createResp.Content.ReadFromJsonAsync<TenantApplicationDto>();

        var resp = await _client.PostAsync($"/api/tenantapplications/{app!.Id}/score", null);
        var score = await resp.Content.ReadFromJsonAsync<ApplicationScoreDto>();
        Assert.That(score!.TotalScore, Is.LessThan(20));
        Assert.That(score.Level, Is.EqualTo(ApplicationScoreLevel.Insufficient));
    }

    // ── Lease Enrichment ──

    [Test, Order(10)]
    public async Task Renew_Lease_Works()
    {
        var resp = await _client.PostAsJsonAsync($"/api/leases/{_leaseId}/renew",
            new LeaseRenewRequest
            {
                NewDurationMonths = 36,
                NewMonthlyRent = 550000,
                Notes = "Renouvellement commercial AUDCG art. 123"
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var newLease = await resp.Content.ReadFromJsonAsync<LeaseDto>();
        Assert.That(newLease!.MonthlyRent, Is.EqualTo(550000));
        Assert.That(newLease.Status, Is.EqualTo(LeaseStatus.Active));
        Assert.That(newLease.NextRevisionDate, Is.Not.Null);

        // Original lease should be Renewed
        var original = await _client.GetFromJsonAsync<LeaseDto>($"/api/leases/{_leaseId}");
        Assert.That(original!.Status, Is.EqualTo(LeaseStatus.Renewed));
    }

    [Test, Order(11)]
    public async Task Terminate_With_Details_Works()
    {
        // Create a new lease to terminate
        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = (await _client.GetFromJsonAsync<List<BuildingDto>>("/api/buildings"))!.First().Id, Reference = "APT-TERM-01", Type = PropertyType.Apartment, AreaSqm = 60 });
        var unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        var leaseResp = await _client.PostAsJsonAsync("/api/leases",
            new CreateLeaseRequest
            {
                UnitId = unitId,
                LeaseTenantId = _tenantId,
                Reference = "BAIL-TERM-001",
                Type = LeaseType.Residential,
                StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                MonthlyRent = 200000,
                SecurityDeposit = 400000
            });
        var leaseId = (await leaseResp.Content.ReadFromJsonAsync<LeaseDto>())!.Id;
        await _client.PutAsync($"/api/leases/{leaseId}/activate", null);

        var resp = await _client.PostAsJsonAsync($"/api/leases/{leaseId}/terminate-with-details",
            new LeaseTerminateRequest
            {
                TerminationDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                Reason = "Congé pour reprise personnelle"
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var updated = await _client.GetFromJsonAsync<LeaseDto>($"/api/leases/{leaseId}");
        Assert.That(updated!.Status, Is.EqualTo(LeaseStatus.Terminated));
    }

    [Test, Order(12)]
    public async Task Landlord_Statement_Works()
    {
        // Create rent calls for the lease to have data
        var list = await _client.GetFromJsonAsync<List<LeaseDto>>($"/api/leases?status={LeaseStatus.Active}");
        var activeLease = list!.First();

        // Generate some rent calls
        for (int m = 1; m <= 3; m++)
        {
            await _client.PostAsJsonAsync("/api/rentcalls",
                new CreateRentCallRequest
                {
                    OrganizationId = _orgId,
                    LeaseId = activeLease.Id,
                    Year = 2026,
                    Month = m,
                    DueDate = new DateTime(2026, m, 5, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        var resp = await _client.GetAsync(
            $"/api/leases/{activeLease.Id}/landlord-statement?periodStart=2026-01-01T00:00:00Z&periodEnd=2026-03-31T23:59:59Z");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var statement = await resp.Content.ReadFromJsonAsync<LandlordStatementDto>();
        Assert.That(statement!.RentLines.Count, Is.EqualTo(3));
        Assert.That(statement.TotalRentDue, Is.GreaterThan(0));
    }

    // ── Payment Reconciliation ──

    [Test, Order(13)]
    public async Task Reconcile_Payment_Updates_RentCall()
    {
        // Get an active lease
        var leases = await _client.GetFromJsonAsync<List<LeaseDto>>($"/api/leases?status={LeaseStatus.Active}");
        var activeLease = leases!.First();

        // Create a rent call
        var rcResp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = activeLease.Id,
                Year = 2026,
                Month = 8,
                DueDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc)
            });
        var rc = await rcResp.Content.ReadFromJsonAsync<RentCallDto>();
        await _client.PostAsync($"/api/rentcalls/{rc!.Id}/send", null);

        // Create and confirm payment
        var payResp = await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest
            {
                Amount = rc.TotalAmount,
                Method = PaymentMethod.Wave,
                PaymentDate = DateTime.UtcNow,
                LeaseTenantId = _tenantId,
                Description = "Loyer août"
            });
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentDto>();
        await _client.PutAsync($"/api/payments/{payment!.Id}/confirm", null);

        // Reconcile
        var resp = await _client.PostAsJsonAsync($"/api/payments/{payment.Id}/reconcile",
            new ReconcilePaymentRequest { RentCallId = rc.Id });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"receiptGenerated\":true"));

        // Verify rent call is paid
        var updated = await _client.GetFromJsonAsync<RentCallDto>($"/api/rentcalls/{rc.Id}");
        Assert.That(updated!.Status, Is.EqualTo(RentCallStatus.Paid));
        Assert.That(updated.RemainingAmount, Is.EqualTo(0));
    }

    [Test, Order(14)]
    public async Task Partial_Payment_Sets_PartiallyPaid()
    {
        var leases = await _client.GetFromJsonAsync<List<LeaseDto>>($"/api/leases?status={LeaseStatus.Active}");
        var activeLease = leases!.First();

        var rcResp = await _client.PostAsJsonAsync("/api/rentcalls",
            new CreateRentCallRequest
            {
                OrganizationId = _orgId,
                LeaseId = activeLease.Id,
                Year = 2026,
                Month = 9,
                DueDate = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc)
            });
        var rc = await rcResp.Content.ReadFromJsonAsync<RentCallDto>();
        await _client.PostAsync($"/api/rentcalls/{rc!.Id}/send", null);

        // Partial payment (half)
        var payResp = await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest
            {
                Amount = rc.TotalAmount / 2,
                Method = PaymentMethod.MtnMoney,
                PaymentDate = DateTime.UtcNow,
                LeaseTenantId = _tenantId,
                Description = "Acompte septembre"
            });
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentDto>();
        await _client.PutAsync($"/api/payments/{payment!.Id}/confirm", null);

        await _client.PostAsJsonAsync($"/api/payments/{payment.Id}/reconcile",
            new ReconcilePaymentRequest { RentCallId = rc.Id });

        var updated = await _client.GetFromJsonAsync<RentCallDto>($"/api/rentcalls/{rc.Id}");
        Assert.That(updated!.Status, Is.EqualTo(RentCallStatus.PartiallyPaid));
        Assert.That(updated.RemainingAmount, Is.GreaterThan(0));
    }
}
