using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

/// <summary>
/// Tests CRUD operations for all Phase 2 controllers:
/// CoOwnerships, LeaseTenants, Leases, ChargeDefinitions,
/// ChargeAssignments, Payments, Incidents, Suppliers, WorkOrders, Documents.
/// </summary>
[TestFixture]
public class Phase2CrudTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;
    private Guid _coOwnershipId;
    private Guid _buildingId;
    private Guid _unitId;
    private Guid _ownerId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        // Seed base entities
        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Phase2 Test", LegalName = "P2T" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro H", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, Name = "Bat A" });
        _buildingId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = _buildingId, Reference = $"U-{Guid.NewGuid():N}".Substring(0, 10) });
        _unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        var ownResp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Test", LastName = "Owner" });
        _ownerId = (await ownResp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // === CoOwnership Hierarchy ===

    [Test]
    public async Task CoOwnership_Hierarchy_ParentChild()
    {
        var childResp = await _client.PostAsJsonAsync("/api/coownerships", new CreateCoOwnershipRequest
        {
            OrganizationId = _orgId,
            Name = "Copro Verticale Bat A",
            Level = CoOwnershipLevel.Vertical,
            ParentCoOwnershipId = _coOwnershipId
        });
        Assert.That(childResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var childrenResp = await _client.GetAsync($"/api/coownerships/{_coOwnershipId}/children");
        Assert.That(childrenResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var children = await childrenResp.Content.ReadFromJsonAsync<List<CoOwnershipDto>>();
        Assert.That(children!.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(children.All(c => c.Level == CoOwnershipLevel.Vertical), Is.True);
    }

    // === LeaseTenant ===

    [Test]
    public async Task LeaseTenant_Crud()
    {
        var createResp = await _client.PostAsJsonAsync("/api/leasetenants", new CreateLeaseTenantRequest
        {
            FirstName = "Ibrahim",
            LastName = "Diarra",
            Email = "ibrahim@example.ci",
            Phone = "+225 01 02 03 04"
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var tenant = await createResp.Content.ReadFromJsonAsync<LeaseTenantDto>();
        Assert.That(tenant!.FullName, Is.EqualTo("Ibrahim Diarra"));

        var getResp = await _client.GetAsync($"/api/leasetenants/{tenant.Id}");
        Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // === Lease ===

    [Test]
    public async Task Lease_Crud()
    {
        // Create tenant first
        var tenantResp = await _client.PostAsJsonAsync("/api/leasetenants", new CreateLeaseTenantRequest
        {
            FirstName = "Lease", LastName = "Test"
        });
        var tenantId = (await tenantResp.Content.ReadFromJsonAsync<LeaseTenantDto>())!.Id;

        var createResp = await _client.PostAsJsonAsync("/api/leases", new CreateLeaseRequest
        {
            UnitId = _unitId,
            LeaseTenantId = tenantId,
            Reference = $"BAIL-{Guid.NewGuid():N}".Substring(0, 15),
            Type = LeaseType.Residential,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MonthlyRent = 350000,
            SecurityDeposit = 700000
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var lease = await createResp.Content.ReadFromJsonAsync<LeaseDto>();
        Assert.That(lease!.MonthlyRent, Is.EqualTo(350000m));
        Assert.That(lease.Status, Is.EqualTo(LeaseStatus.Draft));
    }

    // === ChargeDefinition ===

    [Test]
    public async Task ChargeDefinition_Crud()
    {
        var createResp = await _client.PostAsJsonAsync("/api/chargedefinitions", new CreateChargeDefinitionRequest
        {
            CoOwnershipId = _coOwnershipId,
            Name = "Entretien espaces verts",
            Type = ChargeType.GreenSpaces,
            AnnualAmount = 12000000,
            IsRecoverable = true,
            FiscalYear = 2026
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var charge = await createResp.Content.ReadFromJsonAsync<ChargeDefinitionDto>();
        Assert.That(charge!.AnnualAmount, Is.EqualTo(12000000m));
        Assert.That(charge.IsRecoverable, Is.True);
    }

    // === ChargeAssignment ===

    [Test]
    public async Task ChargeAssignment_Crud()
    {
        var cdResp = await _client.PostAsJsonAsync("/api/chargedefinitions", new CreateChargeDefinitionRequest
        {
            CoOwnershipId = _coOwnershipId, Name = "Securite", Type = ChargeType.Security,
            AnnualAmount = 6000000, FiscalYear = 2026
        });
        var cdId = (await cdResp.Content.ReadFromJsonAsync<ChargeDefinitionDto>())!.Id;

        var createResp = await _client.PostAsJsonAsync("/api/chargeassignments", new CreateChargeAssignmentRequest
        {
            ChargeDefinitionId = cdId,
            UnitId = _unitId,
            Year = 2026,
            Quarter = 1,
            Amount = 1500000,
            DueDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    // === Payment ===

    [Test]
    public async Task Payment_Crud()
    {
        var createResp = await _client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest
        {
            Amount = 350000,
            Method = PaymentMethod.OrangeMoney,
            PaymentDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Loyer Mars 2026",
            OwnerId = _ownerId
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var payment = await createResp.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.That(payment!.Method, Is.EqualTo(PaymentMethod.OrangeMoney));
        Assert.That(payment.Amount, Is.EqualTo(350000m));
    }

    // === Incident ===

    [Test]
    public async Task Incident_CreateAndResolve()
    {
        var createResp = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
        {
            Title = "Fuite d'eau parking B2",
            Description = "Fuite au niveau du joint principal",
            Priority = IncidentPriority.High,
            Category = "Plomberie",
            BuildingId = _buildingId
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var incident = await createResp.Content.ReadFromJsonAsync<IncidentDto>();
        Assert.That(incident!.Status, Is.EqualTo(IncidentStatus.Reported));

        // Resolve
        var resolveResp = await _client.PutAsync($"/api/incidents/{incident.Id}/resolve", null);
        Assert.That(resolveResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/incidents/{incident.Id}");
        var resolved = await getResp.Content.ReadFromJsonAsync<IncidentDto>();
        Assert.That(resolved!.Status, Is.EqualTo(IncidentStatus.Resolved));
        Assert.That(resolved.ResolvedAt, Is.Not.Null);
    }

    // === Supplier ===

    [Test]
    public async Task Supplier_Crud()
    {
        var createResp = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            Name = "Abidjan Plomberie SARL",
            ContactPerson = "Kouame Jean",
            Phone = "+225 27 20 00 00",
            Specialty = "Plomberie"
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var supplier = await createResp.Content.ReadFromJsonAsync<SupplierDto>();
        Assert.That(supplier!.Name, Is.EqualTo("Abidjan Plomberie SARL"));
        Assert.That(supplier.IsActive, Is.True);
    }

    // === WorkOrder ===

    [Test]
    public async Task WorkOrder_Crud()
    {
        var suppResp = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            Name = "WO Test Supplier", Specialty = "Electricite"
        });
        var suppId = (await suppResp.Content.ReadFromJsonAsync<SupplierDto>())!.Id;

        var createResp = await _client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest
        {
            Title = "Reparation eclairage parking",
            Description = "3 lampadaires HS",
            SupplierId = suppId,
            EstimatedCost = 450000,
            ScheduledDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var wo = await createResp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.That(wo!.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(wo.EstimatedCost, Is.EqualTo(450000m));
    }

    // === Document (GED) ===

    [Test]
    public async Task Document_Crud_WithPolymorphicLinks()
    {
        var createResp = await _client.PostAsJsonAsync("/api/documents", new CreateDocumentRequest
        {
            OrganizationId = _orgId,
            FileName = "bail_palmiers_a101.pdf",
            DisplayName = "Bail Palmiers A101",
            ContentType = "application/pdf",
            SizeBytes = 245000,
            StoragePath = "/documents/2026/03/bail_palmiers_a101.pdf",
            Category = DocumentCategory.Lease,
            UnitId = _unitId,
            BuildingId = _buildingId
        });
        Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var doc = await createResp.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.That(doc!.Category, Is.EqualTo(DocumentCategory.Lease));
        Assert.That(doc.UnitId, Is.EqualTo(_unitId));

        // Filter by building
        var filterResp = await _client.GetAsync($"/api/documents?buildingId={_buildingId}");
        var docs = await filterResp.Content.ReadFromJsonAsync<List<DocumentDto>>();
        Assert.That(docs!.Count, Is.GreaterThanOrEqualTo(1));
    }
}
