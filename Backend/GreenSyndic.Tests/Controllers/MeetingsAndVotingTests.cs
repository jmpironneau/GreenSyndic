using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class MeetingsAndVotingTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _orgId;
    private Guid _coOwnershipId;
    private Guid _ownerId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();

        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "AG Test Org", LegalName = "AGT" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro Test", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        var ownResp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Amadou", LastName = "Konan" });
        _ownerId = (await ownResp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test, Order(1)]
    public async Task Create_Meeting_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            OrganizationId = _orgId,
            CoOwnershipId = _coOwnershipId,
            Title = "AG Ordinaire 2026",
            Type = MeetingType.OrdinaryGeneral,
            ScheduledDate = new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc),
            Location = "Club House"
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var meeting = await resp.Content.ReadFromJsonAsync<MeetingDto>();
        Assert.That(meeting!.Title, Is.EqualTo("AG Ordinaire 2026"));
        Assert.That(meeting.Status, Is.EqualTo(MeetingStatus.Planned));
    }

    [Test, Order(2)]
    public async Task Full_Voting_Flow_With_Tally()
    {
        // Create meeting
        var mtgResp = await _client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            OrganizationId = _orgId,
            CoOwnershipId = _coOwnershipId,
            Title = "AG Vote Test",
            Type = MeetingType.OrdinaryGeneral,
            ScheduledDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc)
        });
        var meeting = await mtgResp.Content.ReadFromJsonAsync<MeetingDto>();

        // Create resolution (simple majority)
        var resResp = await _client.PostAsJsonAsync("/api/resolutions", new CreateResolutionRequest
        {
            MeetingId = meeting!.Id,
            OrderNumber = 1,
            Title = "Budget 2026",
            RequiredMajority = ResolutionMajority.Simple
        });
        Assert.That(resResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var resolution = await resResp.Content.ReadFromJsonAsync<ResolutionDto>();

        // Create second owner for voting
        var own2Resp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Marie", LastName = "Diallo" });
        var owner2Id = (await own2Resp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;

        // Vote FOR (owner 1, 150 shares)
        var v1Resp = await _client.PostAsJsonAsync("/api/votes", new CreateVoteRequest
        {
            ResolutionId = resolution!.Id,
            OwnerId = _ownerId,
            Result = VoteResult.For,
            ShareWeight = 150
        });
        Assert.That(v1Resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        // Vote AGAINST (owner 2, 80 shares)
        var v2Resp = await _client.PostAsJsonAsync("/api/votes", new CreateVoteRequest
        {
            ResolutionId = resolution.Id,
            OwnerId = owner2Id,
            Result = VoteResult.Against,
            ShareWeight = 80
        });
        Assert.That(v2Resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        // Tally
        var tallyResp = await _client.PutAsync($"/api/resolutions/{resolution.Id}/tally", null);
        Assert.That(tallyResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var tallied = await tallyResp.Content.ReadFromJsonAsync<ResolutionDto>();
        Assert.That(tallied!.VotesFor, Is.EqualTo(1));
        Assert.That(tallied.VotesAgainst, Is.EqualTo(1));
        Assert.That(tallied.SharesFor, Is.EqualTo(150m));
        Assert.That(tallied.SharesAgainst, Is.EqualTo(80m));
        Assert.That(tallied.IsApproved, Is.True); // 150 > 80 = simple majority
    }

    [Test, Order(3)]
    public async Task Duplicate_Vote_Returns_Conflict()
    {
        var mtgResp = await _client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            OrganizationId = _orgId,
            CoOwnershipId = _coOwnershipId,
            Title = "AG Dup Vote",
            Type = MeetingType.OrdinaryGeneral,
            ScheduledDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
        });
        var meeting = await mtgResp.Content.ReadFromJsonAsync<MeetingDto>();

        var resResp = await _client.PostAsJsonAsync("/api/resolutions", new CreateResolutionRequest
        {
            MeetingId = meeting!.Id,
            OrderNumber = 1,
            Title = "Test Dup",
            RequiredMajority = ResolutionMajority.Simple
        });
        var resolution = await resResp.Content.ReadFromJsonAsync<ResolutionDto>();

        // First vote
        await _client.PostAsJsonAsync("/api/votes", new CreateVoteRequest
        {
            ResolutionId = resolution!.Id,
            OwnerId = _ownerId,
            Result = VoteResult.For,
            ShareWeight = 100
        });

        // Duplicate vote - same owner, same resolution
        var dupResp = await _client.PostAsJsonAsync("/api/votes", new CreateVoteRequest
        {
            ResolutionId = resolution.Id,
            OwnerId = _ownerId,
            Result = VoteResult.Against,
            ShareWeight = 100
        });

        Assert.That(dupResp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test, Order(4)]
    public async Task Meeting_GetResolutions_Endpoint()
    {
        var mtgResp = await _client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            OrganizationId = _orgId,
            CoOwnershipId = _coOwnershipId,
            Title = "AG With Resolutions",
            Type = MeetingType.OrdinaryGeneral,
            ScheduledDate = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
        });
        var meeting = await mtgResp.Content.ReadFromJsonAsync<MeetingDto>();

        await _client.PostAsJsonAsync("/api/resolutions", new CreateResolutionRequest
        {
            MeetingId = meeting!.Id, OrderNumber = 1, Title = "Res 1", RequiredMajority = ResolutionMajority.Simple
        });
        await _client.PostAsJsonAsync("/api/resolutions", new CreateResolutionRequest
        {
            MeetingId = meeting.Id, OrderNumber = 2, Title = "Res 2", RequiredMajority = ResolutionMajority.Absolute
        });

        var resp = await _client.GetAsync($"/api/meetings/{meeting.Id}/resolutions");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var resolutions = await resp.Content.ReadFromJsonAsync<List<ResolutionDto>>();
        Assert.That(resolutions!.Count, Is.EqualTo(2));
        Assert.That(resolutions[0].OrderNumber, Is.EqualTo(1));
        Assert.That(resolutions[1].OrderNumber, Is.EqualTo(2));
    }
}
