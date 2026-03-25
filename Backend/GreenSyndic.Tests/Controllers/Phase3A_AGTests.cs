using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase3A_AGTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;
    private Guid _coOwnershipId;
    private Guid _buildingId;
    private Guid _ownerId;
    private Guid _owner2Id;
    private Guid _unitId;
    private Guid _unit2Id;
    private Guid _meetingId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        // Create org
        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "AG Phase3 Org", LegalName = "AP3" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        // Create co-ownership
        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro AG Test", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        // Create building
        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, CoOwnershipId = _coOwnershipId, Name = "Acajou", Code = "AC", PrimaryType = PropertyType.Apartment });
        _buildingId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        // Create 2 owners
        var own1Resp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Konan", LastName = "Bédié" });
        _ownerId = (await own1Resp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;

        var own2Resp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Alassane", LastName = "Ouattara" });
        _owner2Id = (await own2Resp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;

        // Create 2 units with owners + shares
        var u1Resp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest
            {
                BuildingId = _buildingId, CoOwnershipId = _coOwnershipId,
                Reference = "AC-301", Type = PropertyType.Apartment, AreaSqm = 97,
                OwnerId = _ownerId, ShareRatio = 150
            });
        _unitId = (await u1Resp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        var u2Resp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest
            {
                BuildingId = _buildingId, CoOwnershipId = _coOwnershipId,
                Reference = "AC-302", Type = PropertyType.Apartment, AreaSqm = 131,
                OwnerId = _owner2Id, ShareRatio = 200
            });
        _unit2Id = (await u2Resp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        // Create meeting for use across tests
        var mtgResp = await _client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            OrganizationId = _orgId,
            CoOwnershipId = _coOwnershipId,
            Title = "AG Ordinaire Phase3 2026",
            Type = MeetingType.OrdinaryGeneral,
            ScheduledDate = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            Location = "Club House"
        });
        _meetingId = (await mtgResp.Content.ReadFromJsonAsync<MeetingDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Attendees ──

    [Test, Order(1)]
    public async Task Create_Attendee_Works()
    {
        var resp = await _client.PostAsJsonAsync($"/api/meetings/{_meetingId}/attendees",
            new CreateMeetingAttendeeRequest
            {
                MeetingId = _meetingId,
                OwnerId = _ownerId,
                Status = AttendanceStatus.Expected,
                SharesRepresented = 150,
                ConvocationMethod = ConvocationMethod.Email
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var attendee = await resp.Content.ReadFromJsonAsync<MeetingAttendeeDto>();
        Assert.That(attendee!.OwnerId, Is.EqualTo(_ownerId));
        Assert.That(attendee.SharesRepresented, Is.EqualTo(150));
    }

    [Test, Order(2)]
    public async Task Duplicate_Attendee_Returns_Conflict()
    {
        var resp = await _client.PostAsJsonAsync($"/api/meetings/{_meetingId}/attendees",
            new CreateMeetingAttendeeRequest
            {
                MeetingId = _meetingId,
                OwnerId = _ownerId,
                Status = AttendanceStatus.Expected,
                SharesRepresented = 150
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test, Order(3)]
    public async Task Update_Attendance_Status_Works()
    {
        // Add second owner
        var addResp = await _client.PostAsJsonAsync($"/api/meetings/{_meetingId}/attendees",
            new CreateMeetingAttendeeRequest
            {
                MeetingId = _meetingId,
                OwnerId = _owner2Id,
                Status = AttendanceStatus.Expected,
                SharesRepresented = 200
            });
        var attendee = await addResp.Content.ReadFromJsonAsync<MeetingAttendeeDto>();

        // Check in as present
        var resp = await _client.PutAsJsonAsync(
            $"/api/meetings/{_meetingId}/attendees/{attendee!.Id}/status",
            new UpdateAttendanceStatusRequest
            {
                Status = AttendanceStatus.PresentInPerson,
                HasSigned = true
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(4)]
    public async Task GetAll_Attendees_With_Filter()
    {
        var resp = await _client.GetAsync($"/api/meetings/{_meetingId}/attendees?status=Expected");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var items = await resp.Content.ReadFromJsonAsync<List<MeetingAttendeeDto>>();
        Assert.That(items!.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(items.All(a => a.Status == AttendanceStatus.Expected), Is.True);
    }

    // ── Convocations ──

    [Test, Order(5)]
    public async Task Send_Convocations_Updates_Meeting_Status()
    {
        // Create a fresh meeting for convocation test
        var mtgResp = await _client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            OrganizationId = _orgId,
            CoOwnershipId = _coOwnershipId,
            Title = "AG Convocation Test",
            Type = MeetingType.OrdinaryGeneral,
            ScheduledDate = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
        });
        var meeting = await mtgResp.Content.ReadFromJsonAsync<MeetingDto>();

        // Register an attendee
        await _client.PostAsJsonAsync($"/api/meetings/{meeting!.Id}/attendees",
            new CreateMeetingAttendeeRequest
            {
                MeetingId = meeting.Id,
                OwnerId = _ownerId,
                Status = AttendanceStatus.Expected,
                SharesRepresented = 150
            });

        // Send convocations
        var resp = await _client.PostAsJsonAsync($"/api/meetings/{meeting.Id}/send-convocations",
            new SendConvocationsRequest { Method = ConvocationMethod.Email });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Check meeting is now ConvocationSent
        var getResp = await _client.GetAsync($"/api/meetings/{meeting.Id}");
        var updated = await getResp.Content.ReadFromJsonAsync<MeetingDto>();
        Assert.That(updated!.Status, Is.EqualTo(MeetingStatus.ConvocationSent));
    }

    [Test, Order(6)]
    public async Task Send_Convocations_Rejects_NonPlanned_Meeting()
    {
        // Use the meeting that already had convocations sent (from test 5 or the main meeting)
        // Create + send convocation first
        var mtgResp = await _client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            OrganizationId = _orgId,
            CoOwnershipId = _coOwnershipId,
            Title = "AG Already Sent",
            Type = MeetingType.OrdinaryGeneral,
            ScheduledDate = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)
        });
        var meeting = await mtgResp.Content.ReadFromJsonAsync<MeetingDto>();

        await _client.PostAsJsonAsync($"/api/meetings/{meeting!.Id}/attendees",
            new CreateMeetingAttendeeRequest { MeetingId = meeting.Id, OwnerId = _ownerId, SharesRepresented = 100 });

        await _client.PostAsJsonAsync($"/api/meetings/{meeting.Id}/send-convocations",
            new SendConvocationsRequest { Method = ConvocationMethod.Email });

        // Try to send again → should fail
        var resp = await _client.PostAsJsonAsync($"/api/meetings/{meeting.Id}/send-convocations",
            new SendConvocationsRequest { Method = ConvocationMethod.RegisteredMail });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ── Quorum ──

    [Test, Order(7)]
    public async Task Quorum_Calculation_Works()
    {
        // Update first attendee to present
        var attendeesResp = await _client.GetAsync($"/api/meetings/{_meetingId}/attendees");
        var attendees = await attendeesResp.Content.ReadFromJsonAsync<List<MeetingAttendeeDto>>();
        var firstAttendee = attendees!.First(a => a.OwnerId == _ownerId);

        await _client.PutAsJsonAsync(
            $"/api/meetings/{_meetingId}/attendees/{firstAttendee.Id}/status",
            new UpdateAttendanceStatusRequest { Status = AttendanceStatus.PresentInPerson, HasSigned = true });

        // Get quorum
        var resp = await _client.GetAsync($"/api/meetings/{_meetingId}/quorum");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var quorum = await resp.Content.ReadFromJsonAsync<QuorumResultDto>();
        Assert.That(quorum!.MeetingId, Is.EqualTo(_meetingId));
        Assert.That(quorum.PresentOrRepresented, Is.GreaterThanOrEqualTo(1));
        Assert.That(quorum.RepresentedShares, Is.GreaterThan(0));
    }

    // ── Attendance Sheet ──

    [Test, Order(8)]
    public async Task Attendance_Sheet_Returns_Summary()
    {
        var resp = await _client.GetAsync($"/api/meetings/{_meetingId}/attendance-sheet");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("summary"));
        Assert.That(body, Does.Contain("presentInPerson"));
    }

    // ── PV (Procès-Verbal) ──

    [Test, Order(9)]
    public async Task PV_Generation_Returns_Full_Data()
    {
        // Add a resolution to the meeting
        await _client.PostAsJsonAsync("/api/resolutions", new CreateResolutionRequest
        {
            MeetingId = _meetingId,
            OrderNumber = 1,
            Title = "Approbation budget 2026",
            RequiredMajority = ResolutionMajority.Simple
        });

        var resp = await _client.GetAsync($"/api/meetings/{_meetingId}/pv");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("header"));
        Assert.That(body, Does.Contain("attendance"));
        Assert.That(body, Does.Contain("resolutions"));
        Assert.That(body, Does.Contain("Approbation budget 2026"));
    }

    // ── Agenda Items ──

    [Test, Order(10)]
    public async Task Create_AgendaItem_Works()
    {
        var resp = await _client.PostAsJsonAsync($"/api/meetings/{_meetingId}/agenda-items",
            new CreateMeetingAgendaItemRequest
            {
                MeetingId = _meetingId,
                OrderNumber = 1,
                Title = "Ouverture de la séance",
                Type = AgendaItemType.Information,
                EstimatedDurationMinutes = 10
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var item = await resp.Content.ReadFromJsonAsync<MeetingAgendaItemDto>();
        Assert.That(item!.Title, Is.EqualTo("Ouverture de la séance"));
        Assert.That(item.Type, Is.EqualTo(AgendaItemType.Information));
    }

    [Test, Order(11)]
    public async Task GetAll_AgendaItems_Ordered()
    {
        // Add a second item
        await _client.PostAsJsonAsync($"/api/meetings/{_meetingId}/agenda-items",
            new CreateMeetingAgendaItemRequest
            {
                MeetingId = _meetingId,
                OrderNumber = 2,
                Title = "Vote budget 2026",
                Type = AgendaItemType.Resolution,
                EstimatedDurationMinutes = 30
            });

        var resp = await _client.GetAsync($"/api/meetings/{_meetingId}/agenda-items");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var items = await resp.Content.ReadFromJsonAsync<List<MeetingAgendaItemDto>>();
        Assert.That(items!.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(items[0].OrderNumber, Is.LessThan(items[1].OrderNumber));
    }

    [Test, Order(12)]
    public async Task Reorder_AgendaItems_Works()
    {
        var getResp = await _client.GetAsync($"/api/meetings/{_meetingId}/agenda-items");
        var items = await getResp.Content.ReadFromJsonAsync<List<MeetingAgendaItemDto>>();

        // Swap order
        var reorderPayload = items!.Select((item, i) => new { item.Id, OrderNumber = items.Count - i }).ToList();

        var resp = await _client.PutAsJsonAsync($"/api/meetings/{_meetingId}/agenda-items/reorder", reorderPayload);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(13)]
    public async Task Delete_AgendaItem_Works()
    {
        var getResp = await _client.GetAsync($"/api/meetings/{_meetingId}/agenda-items");
        var items = await getResp.Content.ReadFromJsonAsync<List<MeetingAgendaItemDto>>();
        var lastId = items!.Last().Id;

        var resp = await _client.DeleteAsync($"/api/meetings/{_meetingId}/agenda-items/{lastId}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    // ── Resolution Templates ──

    [Test, Order(14)]
    public async Task Create_ResolutionTemplate_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/resolutiontemplates",
            new CreateResolutionTemplateRequest
            {
                OrganizationId = _orgId,
                Code = "BUDGET-ANNUEL",
                Title = "Approbation du budget prévisionnel annuel",
                DefaultMajority = ResolutionMajority.Simple,
                Category = "budget",
                LegalReference = "Art. 388 CCH",
                TemplateText = "L'assemblée générale approuve le budget prévisionnel de l'exercice {{year}}."
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var template = await resp.Content.ReadFromJsonAsync<ResolutionTemplateDto>();
        Assert.That(template!.Code, Is.EqualTo("BUDGET-ANNUEL"));
        Assert.That(template.IsActive, Is.True);
    }

    [Test, Order(15)]
    public async Task GetAll_ResolutionTemplates_FilterByCategory()
    {
        // Add another template in a different category
        await _client.PostAsJsonAsync("/api/resolutiontemplates",
            new CreateResolutionTemplateRequest
            {
                OrganizationId = _orgId,
                Code = "MANDAT-SYNDIC",
                Title = "Renouvellement du mandat du syndic",
                DefaultMajority = ResolutionMajority.Absolute,
                Category = "mandat"
            });

        var resp = await _client.GetAsync($"/api/resolutiontemplates?category=budget");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var items = await resp.Content.ReadFromJsonAsync<List<ResolutionTemplateDto>>();
        Assert.That(items!.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(items.All(t => t.Category == "budget"), Is.True);
    }

    [Test, Order(16)]
    public async Task Delete_ResolutionTemplate_SoftDelete()
    {
        var createResp = await _client.PostAsJsonAsync("/api/resolutiontemplates",
            new CreateResolutionTemplateRequest
            {
                OrganizationId = _orgId,
                Code = "DELETE-ME",
                Title = "Template to delete",
                DefaultMajority = ResolutionMajority.Simple
            });
        var template = await createResp.Content.ReadFromJsonAsync<ResolutionTemplateDto>();

        var delResp = await _client.DeleteAsync($"/api/resolutiontemplates/{template!.Id}");
        Assert.That(delResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/resolutiontemplates/{template.Id}");
        Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
