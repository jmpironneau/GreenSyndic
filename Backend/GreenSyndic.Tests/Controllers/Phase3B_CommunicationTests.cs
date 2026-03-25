using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase3B_CommunicationTests
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
            new CreateOrganizationRequest { Name = "Comm Phase3 Org", LegalName = "CP3" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro Comm", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        var ownResp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Moussa", LastName = "Traoré", Email = "moussa@test.ci", Phone = "+2250700000001" });
        _ownerId = (await ownResp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Message Templates ──

    [Test, Order(1)]
    public async Task Create_MessageTemplate_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/messagetemplates",
            new CreateMessageTemplateRequest
            {
                OrganizationId = _orgId,
                Code = "CONVOCATION-AG",
                Name = "Convocation AG",
                Channel = MessageChannel.Email,
                Subject = "Convocation à l'AG du {{date}}",
                Body = "Cher(e) {{ownerName}},\n\nVous êtes convoqué(e) à l'assemblée générale du {{date}}.\n\nCordialement,\nLe Syndic",
                Category = "convocation",
                AvailableVariables = "[\"ownerName\", \"date\", \"location\"]"
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var template = await resp.Content.ReadFromJsonAsync<MessageTemplateDto>();
        Assert.That(template!.Code, Is.EqualTo("CONVOCATION-AG"));
        Assert.That(template.IsActive, Is.True);
    }

    [Test, Order(2)]
    public async Task Preview_Template_MergesVariables()
    {
        // Get template
        var listResp = await _client.GetAsync($"/api/messagetemplates?organizationId={_orgId}&code=CONVOCATION-AG");
        var templates = await listResp.Content.ReadFromJsonAsync<List<MessageTemplateDto>>();
        var templateId = templates!.First().Id;

        var mergeData = new Dictionary<string, string>
        {
            { "ownerName", "Moussa Traoré" },
            { "date", "15 juin 2026" },
            { "location", "Club House" }
        };

        var resp = await _client.PostAsJsonAsync($"/api/messagetemplates/{templateId}/preview", mergeData);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Moussa Traoré"));
        Assert.That(body, Does.Contain("15 juin 2026"));
    }

    [Test, Order(3)]
    public async Task GetAll_Templates_FilterByChannel()
    {
        // Add SMS template
        await _client.PostAsJsonAsync("/api/messagetemplates",
            new CreateMessageTemplateRequest
            {
                OrganizationId = _orgId,
                Code = "RELANCE-SMS",
                Name = "Relance impayé SMS",
                Channel = MessageChannel.Sms,
                Body = "Rappel: votre solde de {{amount}} FCFA est en retard.",
                Category = "relance"
            });

        var resp = await _client.GetAsync($"/api/messagetemplates?channel={MessageChannel.Sms}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var items = await resp.Content.ReadFromJsonAsync<List<MessageTemplateDto>>();
        Assert.That(items!.All(t => t.Channel == MessageChannel.Sms), Is.True);
    }

    // ── Individual Messages ──

    [Test, Order(4)]
    public async Task Create_Message_Draft()
    {
        var resp = await _client.PostAsJsonAsync("/api/communicationmessages",
            new CreateMessageRequest
            {
                OrganizationId = _orgId,
                Channel = MessageChannel.Email,
                RecipientEmail = "moussa@test.ci",
                RecipientName = "Moussa Traoré",
                Subject = "Rappel de paiement",
                Body = "Cher Moussa, merci de régulariser votre situation."
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var msg = await resp.Content.ReadFromJsonAsync<CommunicationMessageDto>();
        Assert.That(msg!.Status, Is.EqualTo(MessageStatus.Draft));
        Assert.That(msg.Channel, Is.EqualTo(MessageChannel.Email));
    }

    [Test, Order(5)]
    public async Task Create_Scheduled_Message()
    {
        var resp = await _client.PostAsJsonAsync("/api/communicationmessages",
            new CreateMessageRequest
            {
                OrganizationId = _orgId,
                Channel = MessageChannel.Sms,
                RecipientPhone = "+2250700000001",
                RecipientName = "Moussa Traoré",
                Subject = "Rappel",
                Body = "Rappel: votre solde est en retard.",
                ScheduledAt = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc)
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var msg = await resp.Content.ReadFromJsonAsync<CommunicationMessageDto>();
        Assert.That(msg!.Status, Is.EqualTo(MessageStatus.Scheduled));
    }

    [Test, Order(6)]
    public async Task Send_Message_Works()
    {
        // Create draft
        var createResp = await _client.PostAsJsonAsync("/api/communicationmessages",
            new CreateMessageRequest
            {
                OrganizationId = _orgId,
                Channel = MessageChannel.Email,
                RecipientEmail = "test@test.ci",
                RecipientName = "Test User",
                Subject = "Test Send",
                Body = "Message to send"
            });
        var msg = await createResp.Content.ReadFromJsonAsync<CommunicationMessageDto>();

        // Send it
        var resp = await _client.PostAsync($"/api/communicationmessages/{msg!.Id}/send", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify status is Sent
        var getResp = await _client.GetAsync($"/api/communicationmessages/{msg.Id}");
        var updated = await getResp.Content.ReadFromJsonAsync<CommunicationMessageDto>();
        Assert.That(updated!.Status, Is.EqualTo(MessageStatus.Sent));
        Assert.That(updated.SentAt, Is.Not.Null);
    }

    [Test, Order(7)]
    public async Task Send_AlreadySent_Returns_BadRequest()
    {
        // Create + send
        var createResp = await _client.PostAsJsonAsync("/api/communicationmessages",
            new CreateMessageRequest
            {
                OrganizationId = _orgId,
                Channel = MessageChannel.Email,
                RecipientEmail = "x@x.ci",
                RecipientName = "X",
                Subject = "Double Send",
                Body = "Test"
            });
        var msg = await createResp.Content.ReadFromJsonAsync<CommunicationMessageDto>();
        await _client.PostAsync($"/api/communicationmessages/{msg!.Id}/send", null);

        // Send again → BadRequest
        var resp = await _client.PostAsync($"/api/communicationmessages/{msg.Id}/send", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(8)]
    public async Task Cancel_Draft_Message_Works()
    {
        var createResp = await _client.PostAsJsonAsync("/api/communicationmessages",
            new CreateMessageRequest
            {
                OrganizationId = _orgId,
                Channel = MessageChannel.Push,
                RecipientUserId = "user-123",
                Subject = "To Cancel",
                Body = "This will be cancelled"
            });
        var msg = await createResp.Content.ReadFromJsonAsync<CommunicationMessageDto>();

        var resp = await _client.PostAsync($"/api/communicationmessages/{msg!.Id}/cancel", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(9)]
    public async Task Delete_Sent_Message_Returns_BadRequest()
    {
        var createResp = await _client.PostAsJsonAsync("/api/communicationmessages",
            new CreateMessageRequest
            {
                OrganizationId = _orgId,
                Channel = MessageChannel.Email,
                RecipientEmail = "del@test.ci",
                RecipientName = "Del",
                Subject = "Sent then delete",
                Body = "Test"
            });
        var msg = await createResp.Content.ReadFromJsonAsync<CommunicationMessageDto>();
        await _client.PostAsync($"/api/communicationmessages/{msg!.Id}/send", null);

        var resp = await _client.DeleteAsync($"/api/communicationmessages/{msg.Id}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(10)]
    public async Task Delivery_Logs_Tracked_After_Send()
    {
        var createResp = await _client.PostAsJsonAsync("/api/communicationmessages",
            new CreateMessageRequest
            {
                OrganizationId = _orgId,
                Channel = MessageChannel.Email,
                RecipientEmail = "logs@test.ci",
                RecipientName = "Logs",
                Subject = "Track delivery",
                Body = "Check logs"
            });
        var msg = await createResp.Content.ReadFromJsonAsync<CommunicationMessageDto>();
        await _client.PostAsync($"/api/communicationmessages/{msg!.Id}/send", null);

        var resp = await _client.GetAsync($"/api/communicationmessages/{msg.Id}/delivery-logs");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var logs = await resp.Content.ReadFromJsonAsync<List<MessageDeliveryLogDto>>();
        Assert.That(logs!.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(logs[0].Status, Is.EqualTo(DeliveryStatus.Sent));
    }

    // ── Broadcasts ──

    [Test, Order(11)]
    public async Task Create_Broadcast_Draft()
    {
        var resp = await _client.PostAsJsonAsync("/api/broadcasts",
            new CreateBroadcastRequest
            {
                OrganizationId = _orgId,
                Name = "Appel de fonds Q3 2026",
                Channel = MessageChannel.Email,
                Subject = "Appel de fonds - 3ème trimestre 2026",
                Body = "Cher(e) {{recipientName}},\n\nVeuillez trouver ci-joint votre appel de fonds.",
                CoOwnershipId = _coOwnershipId
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var broadcast = await resp.Content.ReadFromJsonAsync<BroadcastDto>();
        Assert.That(broadcast!.Status, Is.EqualTo(BroadcastStatus.Draft));
        Assert.That(broadcast.Name, Is.EqualTo("Appel de fonds Q3 2026"));
    }

    [Test, Order(12)]
    public async Task Add_Recipient_To_Broadcast()
    {
        // Create broadcast
        var bcResp = await _client.PostAsJsonAsync("/api/broadcasts",
            new CreateBroadcastRequest
            {
                OrganizationId = _orgId,
                Name = "Test Recipients",
                Channel = MessageChannel.Email,
                Subject = "Test"
            });
        var broadcast = await bcResp.Content.ReadFromJsonAsync<BroadcastDto>();

        // Add recipient
        var resp = await _client.PostAsJsonAsync($"/api/broadcasts/{broadcast!.Id}/recipients",
            new AddBroadcastRecipientRequest
            {
                Name = "Moussa Traoré",
                Email = "moussa@test.ci",
                MergeData = "{\"amount\": \"150000\"}"
            });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var recipient = await resp.Content.ReadFromJsonAsync<BroadcastRecipientDto>();
        Assert.That(recipient!.Name, Is.EqualTo("Moussa Traoré"));
    }

    [Test, Order(13)]
    public async Task Send_Broadcast_Creates_Messages_Per_Recipient()
    {
        // Create broadcast
        var bcResp = await _client.PostAsJsonAsync("/api/broadcasts",
            new CreateBroadcastRequest
            {
                OrganizationId = _orgId,
                Name = "Broadcast Send Test",
                Channel = MessageChannel.Email,
                Subject = "Info pour {{recipientName}}",
                Body = "Bonjour {{recipientName}}, voici votre appel de {{amount}} FCFA."
            });
        var broadcast = await bcResp.Content.ReadFromJsonAsync<BroadcastDto>();

        // Add 2 recipients
        await _client.PostAsJsonAsync($"/api/broadcasts/{broadcast!.Id}/recipients",
            new AddBroadcastRecipientRequest
            {
                Name = "Recipient 1",
                Email = "r1@test.ci",
                MergeData = "{\"amount\": \"100000\"}"
            });
        await _client.PostAsJsonAsync($"/api/broadcasts/{broadcast.Id}/recipients",
            new AddBroadcastRecipientRequest
            {
                Name = "Recipient 2",
                Email = "r2@test.ci",
                MergeData = "{\"amount\": \"200000\"}"
            });

        // Send
        var resp = await _client.PostAsync($"/api/broadcasts/{broadcast.Id}/send", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"sentCount\":2"));

        // Verify broadcast is completed
        var getResp = await _client.GetAsync($"/api/broadcasts/{broadcast.Id}");
        var updated = await getResp.Content.ReadFromJsonAsync<BroadcastDto>();
        Assert.That(updated!.Status, Is.EqualTo(BroadcastStatus.Completed));
        Assert.That(updated.SentCount, Is.EqualTo(2));
    }

    [Test, Order(14)]
    public async Task Send_Broadcast_Without_Recipients_Returns_BadRequest()
    {
        var bcResp = await _client.PostAsJsonAsync("/api/broadcasts",
            new CreateBroadcastRequest
            {
                OrganizationId = _orgId,
                Name = "Empty Broadcast",
                Channel = MessageChannel.Email,
                Subject = "No recipients"
            });
        var broadcast = await bcResp.Content.ReadFromJsonAsync<BroadcastDto>();

        var resp = await _client.PostAsync($"/api/broadcasts/{broadcast!.Id}/send", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(15)]
    public async Task Cancel_Broadcast_Works()
    {
        var bcResp = await _client.PostAsJsonAsync("/api/broadcasts",
            new CreateBroadcastRequest
            {
                OrganizationId = _orgId,
                Name = "Cancel Me",
                Channel = MessageChannel.Sms,
                Subject = "To cancel"
            });
        var broadcast = await bcResp.Content.ReadFromJsonAsync<BroadcastDto>();

        var resp = await _client.PostAsync($"/api/broadcasts/{broadcast!.Id}/cancel", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test, Order(16)]
    public async Task Delete_Completed_Broadcast_Returns_BadRequest()
    {
        // Create + add recipient + send
        var bcResp = await _client.PostAsJsonAsync("/api/broadcasts",
            new CreateBroadcastRequest
            {
                OrganizationId = _orgId,
                Name = "Complete Then Delete",
                Channel = MessageChannel.Email,
                Subject = "Test"
            });
        var broadcast = await bcResp.Content.ReadFromJsonAsync<BroadcastDto>();

        await _client.PostAsJsonAsync($"/api/broadcasts/{broadcast!.Id}/recipients",
            new AddBroadcastRecipientRequest { Name = "Test", Email = "t@t.ci" });

        await _client.PostAsync($"/api/broadcasts/{broadcast.Id}/send", null);

        // Try to delete completed → BadRequest
        var resp = await _client.DeleteAsync($"/api/broadcasts/{broadcast.Id}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(17)]
    public async Task GetAll_Messages_FilterByChannel()
    {
        var resp = await _client.GetAsync($"/api/communicationmessages?channel={MessageChannel.Email}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var items = await resp.Content.ReadFromJsonAsync<List<CommunicationMessageDto>>();
        Assert.That(items!.All(m => m.Channel == MessageChannel.Email), Is.True);
    }
}
