using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase6_ModulesSecondairesTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _anonClient = null!;
    private Guid _orgId;
    private Guid _buildingId;
    private Guid _coOwnershipId;
    private Guid _unitId;
    private Guid _ownerId;
    private Guid _supplierId;
    private Guid _incidentId;
    private Guid _workOrderId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();
        _anonClient = _factory.CreateClient();

        // Seed org
        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Phase6 Org", LegalName = "P6O" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        // Seed co-ownership
        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro P6", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        // Seed building
        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, CoOwnershipId = _coOwnershipId, Name = "Bat P6", PrimaryType = PropertyType.Apartment });
        _buildingId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        // Seed unit
        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = _buildingId, CoOwnershipId = _coOwnershipId, Reference = "P6-101", Type = PropertyType.Apartment, AreaSqm = 80 });
        _unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        // Seed owner
        var ownResp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Ibrahim", LastName = "Touré", Email = "ibrahim.p6@test.ci", Phone = "+2250700070001" });
        _ownerId = (await ownResp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;

        // Seed supplier
        var supResp = await _client.PostAsJsonAsync($"/api/suppliers?organizationId={_orgId}",
            new CreateSupplierRequest { Name = "Plomberie Express", ContactPerson = "Koné", Phone = "+2250700070010", Specialty = "Plomberie" });
        _supplierId = (await supResp.Content.ReadFromJsonAsync<SupplierDto>())!.Id;

        // Seed incident
        var incResp = await _client.PostAsJsonAsync("/api/incidents",
            new CreateIncidentRequest { Title = "Fuite robinet", Description = "Cuisine lot P6-101", Priority = IncidentPriority.High, Category = "Plomberie", BuildingId = _buildingId, UnitId = _unitId });
        _incidentId = (await incResp.Content.ReadFromJsonAsync<IncidentDto>())!.Id;

        // Seed work order
        var woResp = await _client.PostAsJsonAsync("/api/workorders",
            new CreateWorkOrderRequest { Title = "Réparation fuite", IncidentId = _incidentId, SupplierId = _supplierId, BuildingId = _buildingId, EstimatedCost = 75000 });
        _workOrderId = (await woResp.Content.ReadFromJsonAsync<WorkOrderDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _anonClient.Dispose();
        _factory.Dispose();
    }

    // ══════════════════════════════════
    //  INCIDENT WORKFLOW (acknowledge, close, reject, stats)
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Incident_Acknowledge_Works()
    {
        var resp = await _client.PutAsync($"/api/incidents/{_incidentId}/acknowledge", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify status changed
        var getResp = await _client.GetAsync($"/api/incidents/{_incidentId}");
        var body = await getResp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"status\":1")); // Acknowledged = 1
    }

    [Test, Order(2)]
    public async Task Incident_Acknowledge_Only_Reported_Fails()
    {
        // Already acknowledged, can't acknowledge again
        var resp = await _client.PutAsync($"/api/incidents/{_incidentId}/acknowledge", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(3)]
    public async Task Incident_Assign_Works()
    {
        var resp = await _client.PutAsJsonAsync($"/api/incidents/{_incidentId}/assign",
            new { assignedToUserId = "tech-user-001" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(4)]
    public async Task Incident_Resolve_Works()
    {
        var resp = await _client.PutAsync($"/api/incidents/{_incidentId}/resolve", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(5)]
    public async Task Incident_Close_Works()
    {
        var resp = await _client.PutAsync($"/api/incidents/{_incidentId}/close", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/incidents/{_incidentId}");
        var body = await getResp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"status\":4")); // Closed = 4
    }

    [Test, Order(6)]
    public async Task Incident_Close_Only_Resolved_Fails()
    {
        // Already closed, can't close again
        var resp = await _client.PutAsync($"/api/incidents/{_incidentId}/close", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(7)]
    public async Task Incident_Reject_Works()
    {
        // Create a new incident to reject
        var incResp = await _client.PostAsJsonAsync("/api/incidents",
            new CreateIncidentRequest { Title = "Bruit voisin", Description = "Musique forte", Priority = IncidentPriority.Low, BuildingId = _buildingId });
        var inc = await incResp.Content.ReadFromJsonAsync<IncidentDto>();

        var resp = await _client.PutAsJsonAsync($"/api/incidents/{inc!.Id}/reject",
            new { reason = "Pas de compétence syndic" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(8)]
    public async Task Incident_Stats_Returns_Aggregated_Data()
    {
        var resp = await _client.GetAsync("/api/incidents/stats");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"total\""));
        Assert.That(body, Does.Contain("\"open\""));
        Assert.That(body, Does.Contain("\"closed\""));
        Assert.That(body, Does.Contain("\"byStatus\""));
        Assert.That(body, Does.Contain("\"byPriority\""));
        Assert.That(body, Does.Contain("\"byCategory\""));
    }

    [Test, Order(9)]
    public async Task Incident_Stats_Filter_By_OrgId()
    {
        var fakeOrgId = Guid.NewGuid();
        var resp = await _client.GetAsync($"/api/incidents/stats?organizationId={fakeOrgId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"total\":0"));
    }

    // ══════════════════════════════════
    //  WORK ORDER WORKFLOW (approve, start, complete, invoice, pay, cancel)
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task WorkOrder_Approve_Works()
    {
        var resp = await _client.PutAsync($"/api/workorders/{_workOrderId}/approve", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/workorders/{_workOrderId}");
        var wo = await getResp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.That(wo!.Status, Is.EqualTo(WorkOrderStatus.Approved));
    }

    [Test, Order(21)]
    public async Task WorkOrder_Approve_Only_Draft_Fails()
    {
        var resp = await _client.PutAsync($"/api/workorders/{_workOrderId}/approve", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(22)]
    public async Task WorkOrder_Start_Works()
    {
        var resp = await _client.PutAsync($"/api/workorders/{_workOrderId}/start", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/workorders/{_workOrderId}");
        var wo = await getResp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.That(wo!.Status, Is.EqualTo(WorkOrderStatus.InProgress));
    }

    [Test, Order(23)]
    public async Task WorkOrder_Complete_With_ActualCost()
    {
        var resp = await _client.PutAsJsonAsync($"/api/workorders/{_workOrderId}/complete",
            new { actualCost = 80000, notes = "Remplacement joint complet" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/workorders/{_workOrderId}");
        var wo = await getResp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.That(wo!.Status, Is.EqualTo(WorkOrderStatus.Completed));
        Assert.That(wo.ActualCost, Is.EqualTo(80000));
    }

    [Test, Order(24)]
    public async Task WorkOrder_Invoice_Works()
    {
        var resp = await _client.PutAsJsonAsync($"/api/workorders/{_workOrderId}/invoice",
            new { actualCost = 82000 });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/workorders/{_workOrderId}");
        var wo = await getResp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.That(wo!.Status, Is.EqualTo(WorkOrderStatus.Invoiced));
        Assert.That(wo.ActualCost, Is.EqualTo(82000));
    }

    [Test, Order(25)]
    public async Task WorkOrder_Pay_Works()
    {
        var resp = await _client.PutAsync($"/api/workorders/{_workOrderId}/pay", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/workorders/{_workOrderId}");
        var wo = await getResp.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.That(wo!.Status, Is.EqualTo(WorkOrderStatus.Paid));
    }

    [Test, Order(26)]
    public async Task WorkOrder_Cancel_Works()
    {
        // Create a new WO to cancel
        var woResp = await _client.PostAsJsonAsync("/api/workorders",
            new CreateWorkOrderRequest { Title = "Travail à annuler", BuildingId = _buildingId, EstimatedCost = 10000 });
        var wo = await woResp.Content.ReadFromJsonAsync<WorkOrderDto>();

        var resp = await _client.PutAsync($"/api/workorders/{wo!.Id}/cancel", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    // ══════════════════════════════════
    //  ORGANIZATION SETTINGS
    // ══════════════════════════════════

    [Test, Order(30)]
    public async Task Settings_Get_Creates_Default()
    {
        var resp = await _client.GetAsync($"/api/organizations/{_orgId}/settings");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"currency\":\"XOF\""));
        Assert.That(body, Does.Contain("\"rentDueDay\":5"));
        Assert.That(body, Does.Contain("\"defaultVatRate\":18"));
        Assert.That(body, Does.Contain("\"timezone\":\"Africa/Abidjan\""));
        Assert.That(body, Does.Contain("\"locale\":\"fr-CI\""));
    }

    [Test, Order(31)]
    public async Task Settings_Update_Partial()
    {
        var resp = await _client.PutAsJsonAsync($"/api/organizations/{_orgId}/settings", new
        {
            rentDueDay = 10,
            applyLateFees = true,
            lateFeePercent = 3.5,
            defaultEmailFrom = "syndic@greencity.ci"
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"rentDueDay\":10"));
        Assert.That(body, Does.Contain("\"applyLateFees\":true"));
        Assert.That(body, Does.Contain("\"lateFeePercent\":3.5"));
        Assert.That(body, Does.Contain("\"defaultEmailFrom\":\"syndic@greencity.ci\""));
        // Unchanged values should persist
        Assert.That(body, Does.Contain("\"currency\":\"XOF\""));
    }

    [Test, Order(32)]
    public async Task Settings_Get_After_Update_Persists()
    {
        var resp = await _client.GetAsync($"/api/organizations/{_orgId}/settings");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"rentDueDay\":10"));
        Assert.That(body, Does.Contain("\"applyLateFees\":true"));
    }

    [Test, Order(33)]
    public async Task Settings_NotFound_For_Fake_Org()
    {
        var resp = await _client.GetAsync($"/api/organizations/{Guid.NewGuid()}/settings");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test, Order(34)]
    public async Task Settings_Requires_Auth()
    {
        var resp = await _anonClient.GetAsync($"/api/organizations/{_orgId}/settings");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  LEGAL REFERENCES (Veille Juridique)
    // ══════════════════════════════════

    [Test, Order(40)]
    public async Task LegalRef_Create_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/legalreferences", new CreateLegalReferenceRequest
        {
            Code = "CCH-414",
            Title = "Bail à usage d'habitation — Définition",
            Content = "Le bail à usage d'habitation est le contrat par lequel une personne met un immeuble...",
            Domain = LegalDomain.BailResidentiel,
            Source = "CCH",
            Tags = "[\"bail\",\"habitation\",\"définition\"]"
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"code\":\"CCH-414\""));
        Assert.That(body, Does.Contain("\"domain\":1")); // BailResidentiel
    }

    [Test, Order(41)]
    public async Task LegalRef_Create_Duplicate_Code_Fails()
    {
        var resp = await _client.PostAsJsonAsync("/api/legalreferences", new CreateLegalReferenceRequest
        {
            Code = "CCH-414",
            Title = "Duplicate",
            Content = "Test",
            Domain = LegalDomain.BailResidentiel
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test, Order(42)]
    public async Task LegalRef_Create_OHADA()
    {
        var resp = await _client.PostAsJsonAsync("/api/legalreferences", new CreateLegalReferenceRequest
        {
            Code = "AUDCG-101",
            Title = "Bail commercial — Champ d'application",
            Content = "Les dispositions du présent titre s'appliquent aux baux portant sur des immeubles...",
            Domain = LegalDomain.BailCommercial,
            Source = "AUDCG",
            Tags = "[\"bail\",\"commercial\",\"OHADA\"]"
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test, Order(43)]
    public async Task LegalRef_GetAll()
    {
        var resp = await _client.GetAsync("/api/legalreferences");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("CCH-414"));
        Assert.That(body, Does.Contain("AUDCG-101"));
    }

    [Test, Order(44)]
    public async Task LegalRef_Filter_By_Domain()
    {
        var resp = await _client.GetAsync("/api/legalreferences?domain=2"); // BailCommercial
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("AUDCG-101"));
        Assert.That(body, Does.Not.Contain("CCH-414"));
    }

    [Test, Order(45)]
    public async Task LegalRef_Search_By_Text()
    {
        var resp = await _client.GetAsync("/api/legalreferences?search=habitation");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("CCH-414"));
        Assert.That(body, Does.Not.Contain("AUDCG-101"));
    }

    [Test, Order(46)]
    public async Task LegalRef_Search_By_Tag()
    {
        var resp = await _client.GetAsync("/api/legalreferences?search=OHADA");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("AUDCG-101"));
    }

    [Test, Order(47)]
    public async Task LegalRef_GetById()
    {
        var listResp = await _client.GetAsync("/api/legalreferences");
        var list = await listResp.Content.ReadFromJsonAsync<List<LegalReferenceDto>>();
        var first = list!.First();

        var resp = await _client.GetAsync($"/api/legalreferences/{first.Id}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test, Order(48)]
    public async Task LegalRef_Update_Works()
    {
        var listResp = await _client.GetAsync("/api/legalreferences?domain=1"); // BailResidentiel
        var list = await listResp.Content.ReadFromJsonAsync<List<LegalReferenceDto>>();
        var item = list!.First();

        var resp = await _client.PutAsJsonAsync($"/api/legalreferences/{item.Id}", new CreateLegalReferenceRequest
        {
            Code = item.Code,
            Title = item.Title + " (mis à jour)",
            Content = item.Content,
            Domain = item.Domain,
            Source = item.Source
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(49)]
    public async Task LegalRef_Delete_Works()
    {
        // Create one to delete
        var createResp = await _client.PostAsJsonAsync("/api/legalreferences", new CreateLegalReferenceRequest
        {
            Code = "TEST-DEL-001",
            Title = "To delete",
            Content = "test",
            Domain = LegalDomain.Fiscalite
        });
        var created = await createResp.Content.ReadFromJsonAsync<LegalReferenceDto>();

        var resp = await _client.DeleteAsync($"/api/legalreferences/{created!.Id}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Should not appear in list anymore
        var listResp = await _client.GetAsync("/api/legalreferences");
        var body = await listResp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Not.Contain("TEST-DEL-001"));
    }

    [Test, Order(50)]
    public async Task LegalRef_Domains_Endpoint()
    {
        var resp = await _client.GetAsync("/api/legalreferences/domains");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Copropriete"));
        Assert.That(body, Does.Contain("BailCommercial"));
    }

    [Test, Order(51)]
    public async Task LegalRef_Requires_Auth()
    {
        var resp = await _anonClient.GetAsync("/api/legalreferences");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  CSV EXPORT
    // ══════════════════════════════════

    [Test, Order(60)]
    public async Task Export_List_Returns_Available_Exports()
    {
        var resp = await _client.GetAsync("/api/export");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("units"));
        Assert.That(body, Does.Contain("payments"));
        Assert.That(body, Does.Contain("incidents"));
        Assert.That(body, Does.Contain("workorders"));
        Assert.That(body, Does.Contain("owners"));
        Assert.That(body, Does.Contain("leases"));
        Assert.That(body, Does.Contain("suppliers"));
    }

    [Test, Order(61)]
    public async Task Export_Units_Returns_CSV()
    {
        var resp = await _client.GetAsync("/api/export/units");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Référence;Immeuble;Type"));
        Assert.That(body, Does.Contain("P6-101")); // Our seeded unit
    }

    [Test, Order(62)]
    public async Task Export_Payments_Returns_CSV()
    {
        var resp = await _client.GetAsync("/api/export/payments");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Référence;Date;Montant"));
    }

    [Test, Order(63)]
    public async Task Export_Incidents_Returns_CSV()
    {
        var resp = await _client.GetAsync("/api/export/incidents");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Titre;Priorité;Statut"));
        Assert.That(body, Does.Contain("Fuite robinet"));
    }

    [Test, Order(64)]
    public async Task Export_WorkOrders_Returns_CSV()
    {
        var resp = await _client.GetAsync("/api/export/workorders");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Référence;Titre;Statut"));
        Assert.That(body, Does.Contain("Réparation fuite"));
    }

    [Test, Order(65)]
    public async Task Export_Owners_Returns_CSV()
    {
        var resp = await _client.GetAsync("/api/export/owners");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Nom;Prénom;Email"));
        Assert.That(body, Does.Contain("Touré"));
    }

    [Test, Order(66)]
    public async Task Export_Suppliers_Returns_CSV()
    {
        var resp = await _client.GetAsync("/api/export/suppliers");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Nom;Contact;Email"));
        Assert.That(body, Does.Contain("Plomberie Express"));
    }

    [Test, Order(67)]
    public async Task Export_Leases_Returns_CSV()
    {
        var resp = await _client.GetAsync("/api/export/leases");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Référence;Type;Statut;Locataire"));
    }

    [Test, Order(68)]
    public async Task Export_Units_Filter_By_OrgId()
    {
        // Fake org — should return header only
        var resp = await _client.GetAsync($"/api/export/units?organizationId={Guid.NewGuid()}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Référence;Immeuble"));
        Assert.That(body, Does.Not.Contain("P6-101"));
    }

    [Test, Order(69)]
    public async Task Export_Requires_Auth()
    {
        var resp = await _anonClient.GetAsync("/api/export/units");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  DOCUMENTS (GED) — existing but verify it works
    // ══════════════════════════════════

    [Test, Order(70)]
    public async Task Document_Create_And_Get()
    {
        var resp = await _client.PostAsJsonAsync("/api/documents", new CreateDocumentRequest
        {
            OrganizationId = _orgId,
            FileName = "facture_plomberie.pdf",
            DisplayName = "Facture Plomberie Express - Mars 2026",
            ContentType = "application/pdf",
            SizeBytes = 245000,
            StoragePath = "/docs/factures/facture_plomberie.pdf",
            Category = DocumentCategory.Invoice,
            Description = "Facture réparation fuite P6-101",
            IncidentId = _incidentId,
            WorkOrderId = _workOrderId
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var doc = await resp.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.That(doc!.FileName, Is.EqualTo("facture_plomberie.pdf"));
        Assert.That(doc.Category, Is.EqualTo(DocumentCategory.Invoice));
        Assert.That(doc.IncidentId, Is.EqualTo(_incidentId));
        Assert.That(doc.WorkOrderId, Is.EqualTo(_workOrderId));
    }

    [Test, Order(71)]
    public async Task Document_Filter_By_Incident()
    {
        var resp = await _client.GetAsync($"/api/documents?incidentId={_incidentId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("facture_plomberie.pdf"));
    }

    [Test, Order(72)]
    public async Task Document_Filter_By_Category()
    {
        var resp = await _client.GetAsync($"/api/documents?category={DocumentCategory.Invoice}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("facture_plomberie.pdf"));
    }

    [Test, Order(73)]
    public async Task Document_Filter_By_WorkOrder()
    {
        var resp = await _client.GetAsync($"/api/documents?workOrderId={_workOrderId}");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("facture_plomberie.pdf"));
    }
}
