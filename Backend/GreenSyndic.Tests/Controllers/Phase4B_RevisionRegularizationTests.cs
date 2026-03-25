using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase4B_RevisionRegularizationTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;
    private Guid _leaseId;
    private Guid _unitId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Phase4B Org", LegalName = "P4B" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro Rev", Level = CoOwnershipLevel.Horizontal });
        var coId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, CoOwnershipId = coId, Name = "Bat B", PrimaryType = PropertyType.Apartment });
        var bldId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = bldId, CoOwnershipId = coId, Reference = "APT-REV-01", Type = PropertyType.Apartment, AreaSqm = 70 });
        _unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        var tenResp = await _client.PostAsJsonAsync("/api/leasetenants",
            new CreateLeaseTenantRequest { FirstName = "Fatou", LastName = "Diallo", Email = "fatou@test.ci", Phone = "+2250700000020" });
        var tenantId = (await tenResp.Content.ReadFromJsonAsync<LeaseTenantDto>())!.Id;

        var leaseResp = await _client.PostAsJsonAsync("/api/leases",
            new CreateLeaseRequest
            {
                UnitId = _unitId,
                LeaseTenantId = tenantId,
                Reference = "BAIL-REV-001",
                Type = LeaseType.Residential,
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DurationMonths = 36,
                MonthlyRent = 250000,
                Charges = 40000,
                SecurityDeposit = 500000
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

    // ── Lease Revisions ──

    [Test, Order(1)]
    public async Task Create_Revision_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/leaserevisions",
            new CreateLeaseRevisionRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Type = RevisionType.Triennale,
                EffectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                NewRent = 275000
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var rev = await resp.Content.ReadFromJsonAsync<LeaseRevisionDto>();
        Assert.That(rev!.PreviousRent, Is.EqualTo(250000));
        Assert.That(rev.NewRent, Is.EqualTo(275000));
        Assert.That(rev.VariationPercent, Is.EqualTo(10));
        Assert.That(rev.LegalBasis, Is.EqualTo("CCH art. 423-424"));
        Assert.That(rev.Status, Is.EqualTo(RevisionStatus.Pending));
    }

    [Test, Order(2)]
    public async Task Notify_Revision_Works()
    {
        var list = await _client.GetFromJsonAsync<List<LeaseRevisionDto>>($"/api/leaserevisions?leaseId={_leaseId}");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/leaserevisions/{id}/notify", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<LeaseRevisionDto>($"/api/leaserevisions/{id}");
        Assert.That(updated!.Status, Is.EqualTo(RevisionStatus.Notified));
        Assert.That(updated.NotificationDate, Is.Not.Null);
    }

    [Test, Order(3)]
    public async Task Accept_Revision_Works()
    {
        var list = await _client.GetFromJsonAsync<List<LeaseRevisionDto>>($"/api/leaserevisions?leaseId={_leaseId}");
        var id = list!.First().Id;

        var resp = await _client.PostAsJsonAsync($"/api/leaserevisions/{id}/respond",
            new RespondRevisionRequest { Accepted = true });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<LeaseRevisionDto>($"/api/leaserevisions/{id}");
        Assert.That(updated!.Status, Is.EqualTo(RevisionStatus.Accepted));
        Assert.That(updated.AcceptedAt, Is.Not.Null);
    }

    [Test, Order(4)]
    public async Task Apply_Revision_Updates_Lease()
    {
        var list = await _client.GetFromJsonAsync<List<LeaseRevisionDto>>($"/api/leaserevisions?leaseId={_leaseId}");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/leaserevisions/{id}/apply", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify lease was updated
        var lease = await _client.GetFromJsonAsync<LeaseDto>($"/api/leases/{_leaseId}");
        Assert.That(lease!.MonthlyRent, Is.EqualTo(275000));
        Assert.That(lease.NextRevisionDate, Is.Not.Null);
    }

    [Test, Order(5)]
    public async Task Contest_Revision_Works()
    {
        // Create a second revision to contest
        var createResp = await _client.PostAsJsonAsync("/api/leaserevisions",
            new CreateLeaseRevisionRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Type = RevisionType.Indexation,
                EffectiveDate = new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                NewRent = 310000,
                IndexName = "IRL"
            });
        var rev = await createResp.Content.ReadFromJsonAsync<LeaseRevisionDto>();

        await _client.PostAsync($"/api/leaserevisions/{rev!.Id}/notify", null);

        var resp = await _client.PostAsJsonAsync($"/api/leaserevisions/{rev.Id}/respond",
            new RespondRevisionRequest { Accepted = false, ContestationReason = "Augmentation excessive" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<LeaseRevisionDto>($"/api/leaserevisions/{rev.Id}");
        Assert.That(updated!.Status, Is.EqualTo(RevisionStatus.Contested));
        Assert.That(updated.ContestationReason, Is.EqualTo("Augmentation excessive"));
    }

    [Test, Order(6)]
    public async Task Cancel_Applied_Revision_Returns_BadRequest()
    {
        var list = await _client.GetFromJsonAsync<List<LeaseRevisionDto>>($"/api/leaserevisions?leaseId={_leaseId}&status={RevisionStatus.Applied}");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/leaserevisions/{id}/cancel", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ── Charge Regularizations ──

    [Test, Order(10)]
    public async Task Create_ChargeRegularization_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/chargeregularizations",
            new CreateChargeRegularizationRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Type = RegularizationType.Annual,
                PeriodStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PeriodEnd = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                TotalProvisioned = 480000,   // 40000 x 12
                TotalActual = 520000,
                BreakdownJson = "{\"water\":120000,\"security\":200000,\"maintenance\":200000}"
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var reg = await resp.Content.ReadFromJsonAsync<ChargeRegularizationDto>();
        Assert.That(reg!.Balance, Is.EqualTo(-40000)); // Under-provisioned
        Assert.That(reg.Status, Is.EqualTo(RegularizationStatus.Calculated));
    }

    [Test, Order(11)]
    public async Task Notify_Regularization_Works()
    {
        var list = await _client.GetFromJsonAsync<List<ChargeRegularizationDto>>($"/api/chargeregularizations?leaseId={_leaseId}");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/chargeregularizations/{id}/notify", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<ChargeRegularizationDto>($"/api/chargeregularizations/{id}");
        Assert.That(updated!.Status, Is.EqualTo(RegularizationStatus.Notified));
    }

    [Test, Order(12)]
    public async Task Accept_And_Settle_Regularization()
    {
        var list = await _client.GetFromJsonAsync<List<ChargeRegularizationDto>>($"/api/chargeregularizations?leaseId={_leaseId}");
        var id = list!.First().Id;

        // Accept
        var acceptResp = await _client.PostAsJsonAsync($"/api/chargeregularizations/{id}/respond",
            new RespondRegularizationRequest { Accepted = true });
        Assert.That(acceptResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Settle
        var settleResp = await _client.PostAsync($"/api/chargeregularizations/{id}/settle", null);
        Assert.That(settleResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<ChargeRegularizationDto>($"/api/chargeregularizations/{id}");
        Assert.That(updated!.Status, Is.EqualTo(RegularizationStatus.Settled));
        Assert.That(updated.SettledAt, Is.Not.Null);
    }

    [Test, Order(13)]
    public async Task Cancel_Settled_Regularization_Returns_BadRequest()
    {
        var list = await _client.GetFromJsonAsync<List<ChargeRegularizationDto>>($"/api/chargeregularizations?leaseId={_leaseId}&status={RegularizationStatus.Settled}");
        var id = list!.First().Id;

        var resp = await _client.PostAsync($"/api/chargeregularizations/{id}/cancel", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(14)]
    public async Task Contest_Regularization_Works()
    {
        var createResp = await _client.PostAsJsonAsync("/api/chargeregularizations",
            new CreateChargeRegularizationRequest
            {
                OrganizationId = _orgId,
                LeaseId = _leaseId,
                Type = RegularizationType.Annual,
                PeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PeriodEnd = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                TotalProvisioned = 480000,
                TotalActual = 600000
            });
        var reg = await createResp.Content.ReadFromJsonAsync<ChargeRegularizationDto>();

        await _client.PostAsync($"/api/chargeregularizations/{reg!.Id}/notify", null);

        var resp = await _client.PostAsJsonAsync($"/api/chargeregularizations/{reg.Id}/respond",
            new RespondRegularizationRequest { Accepted = false, ContestationReason = "Charges de sécurité injustifiées" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<ChargeRegularizationDto>($"/api/chargeregularizations/{reg.Id}");
        Assert.That(updated!.Status, Is.EqualTo(RegularizationStatus.Contested));
    }
}
