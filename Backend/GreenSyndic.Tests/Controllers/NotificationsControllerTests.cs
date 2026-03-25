using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class NotificationsControllerTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private const string TestUserId = "notif-test-user";
    private Guid _orgId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient(userId: TestUserId);

        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Notif Test Org", LegalName = "NTO" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test, Order(1)]
    public async Task Create_Notification_Works()
    {
        var resp = await _client.PostAsJsonAsync("/api/notifications", new CreateNotificationRequest
        {
            OrganizationId = _orgId,
            UserId = TestUserId,
            Title = "Test Notification",
            Message = "Ceci est un test"
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var notif = await resp.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.That(notif!.IsRead, Is.False);
    }

    [Test, Order(2)]
    public async Task UnreadCount_ReturnsCorrectCount()
    {
        // Create 2 more notifications
        await _client.PostAsJsonAsync("/api/notifications", new CreateNotificationRequest
        {
            OrganizationId = _orgId, UserId = TestUserId, Title = "N1", Message = "M1"
        });
        await _client.PostAsJsonAsync("/api/notifications", new CreateNotificationRequest
        {
            OrganizationId = _orgId, UserId = TestUserId, Title = "N2", Message = "M2"
        });

        var resp = await _client.GetAsync("/api/notifications/unread-count");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var count = await resp.Content.ReadFromJsonAsync<int>();
        Assert.That(count, Is.GreaterThanOrEqualTo(2));
    }

    [Test, Order(3)]
    public async Task MarkAsRead_SingleNotification()
    {
        var createResp = await _client.PostAsJsonAsync("/api/notifications", new CreateNotificationRequest
        {
            OrganizationId = _orgId, UserId = TestUserId, Title = "Read Me", Message = "Please"
        });
        var notif = await createResp.Content.ReadFromJsonAsync<NotificationDto>();

        var markResp = await _client.PutAsync($"/api/notifications/{notif!.Id}/read", null);
        Assert.That(markResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/notifications/{notif.Id}");
        var updated = await getResp.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.That(updated!.IsRead, Is.True);
        Assert.That(updated.ReadAt, Is.Not.Null);
    }

    [Test, Order(4)]
    public async Task MarkAllRead_SetsAllToRead()
    {
        var resp = await _client.PutAsync("/api/notifications/read-all", null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var countResp = await _client.GetAsync("/api/notifications/unread-count");
        var count = await countResp.Content.ReadFromJsonAsync<int>();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test, Order(5)]
    public async Task GetAll_FilterByIsRead()
    {
        // Create a new unread notification
        await _client.PostAsJsonAsync("/api/notifications", new CreateNotificationRequest
        {
            OrganizationId = _orgId, UserId = TestUserId, Title = "Unread", Message = "Filter test"
        });

        var resp = await _client.GetAsync("/api/notifications?isRead=false");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var notifications = await resp.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.That(notifications!.All(n => !n.IsRead), Is.True);
    }
}
