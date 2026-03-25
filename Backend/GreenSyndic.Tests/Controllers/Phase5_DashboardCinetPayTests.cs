using System.Net;
using System.Net.Http.Json;
using GreenSyndic.Core.Enums;
using GreenSyndic.Services.DTOs;
using GreenSyndic.Tests.Infrastructure;

namespace GreenSyndic.Tests.Controllers;

[TestFixture]
public class Phase5_DashboardCinetPayTests
{
    private GreenSyndicWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _anonClient = null!;
    private Guid _orgId;
    private Guid _buildingId;
    private Guid _coOwnershipId;
    private Guid _unitId;
    private Guid _ownerId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new GreenSyndicWebAppFactory();
        _client = _factory.CreateAuthenticatedClient();
        _anonClient = _factory.CreateClient(); // no auth headers

        // Seed: org
        var orgResp = await _client.PostAsJsonAsync("/api/organizations",
            new CreateOrganizationRequest { Name = "Phase5 Org", LegalName = "P5O" });
        _orgId = (await orgResp.Content.ReadFromJsonAsync<OrganizationDto>())!.Id;

        // Seed: building
        var bldResp = await _client.PostAsJsonAsync("/api/buildings",
            new CreateBuildingRequest { OrganizationId = _orgId, Name = "Immeuble A", Address = "Bassam" });
        _buildingId = (await bldResp.Content.ReadFromJsonAsync<BuildingDto>())!.Id;

        // Seed: co-ownership
        var coResp = await _client.PostAsJsonAsync("/api/coownerships",
            new CreateCoOwnershipRequest { OrganizationId = _orgId, Name = "Copro P5", Level = CoOwnershipLevel.Horizontal });
        _coOwnershipId = (await coResp.Content.ReadFromJsonAsync<CoOwnershipDto>())!.Id;

        // Seed: unit
        var unitResp = await _client.PostAsJsonAsync("/api/units",
            new CreateUnitRequest { BuildingId = _buildingId, CoOwnershipId = _coOwnershipId, Reference = "A101", Type = PropertyType.Apartment, AreaSqm = 75 });
        _unitId = (await unitResp.Content.ReadFromJsonAsync<UnitDto>())!.Id;

        // Seed: owner
        var ownResp = await _client.PostAsJsonAsync("/api/owners",
            new CreateOwnerRequest { FirstName = "Awa", LastName = "Koné", Email = "awa.p5@test.ci", Phone = "+2250700050001" });
        _ownerId = (await ownResp.Content.ReadFromJsonAsync<OwnerDto>())!.Id;

        // Seed: payment (pending)
        await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest { Amount = 150000, Method = PaymentMethod.BankTransfer, PaymentDate = DateTime.UtcNow, OwnerId = _ownerId, Description = "Appel fonds Q1" });

        // Seed: payment (completed)
        var payResp = await _client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest { Amount = 250000, Method = PaymentMethod.Cash, PaymentDate = DateTime.UtcNow, OwnerId = _ownerId, Description = "Loyer mars" });
        var payId = (await payResp.Content.ReadFromJsonAsync<PaymentDto>())!.Id;
        await _client.PutAsync($"/api/payments/{payId}/confirm", null);

        // Seed: incident
        await _client.PostAsJsonAsync("/api/incidents",
            new CreateIncidentRequest { Title = "Fuite d'eau", Description = "Fuite robinet cuisine", Priority = IncidentPriority.High, BuildingId = _buildingId });
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _anonClient.Dispose();
        _factory.Dispose();
    }

    // ══════════════════════════════════
    //  DASHBOARD KPIs
    // ══════════════════════════════════

    [Test, Order(1)]
    public async Task Dashboard_KPIs_Returns_All_Metrics()
    {
        var resp = await _client.GetAsync("/api/dashboard/kpis");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        // Verify all KPI sections present
        Assert.That(body, Does.Contain("\"units\""));
        Assert.That(body, Does.Contain("\"payments\""));
        Assert.That(body, Does.Contain("\"incidents\""));
        Assert.That(body, Does.Contain("\"leases\""));
        Assert.That(body, Does.Contain("\"coOwnerships\""));
        Assert.That(body, Does.Contain("\"rentCalls\""));
    }

    [Test, Order(2)]
    public async Task Dashboard_KPIs_Counts_Units()
    {
        var resp = await _client.GetAsync("/api/dashboard/kpis");
        var body = await resp.Content.ReadAsStringAsync();
        // We created 1 unit
        Assert.That(body, Does.Contain("\"total\":1"));
    }

    [Test, Order(3)]
    public async Task Dashboard_KPIs_Counts_Pending_Payments()
    {
        var resp = await _client.GetAsync("/api/dashboard/kpis");
        var body = await resp.Content.ReadAsStringAsync();
        // 1 pending payment
        Assert.That(body, Does.Contain("\"pending\":1"));
    }

    [Test, Order(4)]
    public async Task Dashboard_KPIs_Sums_Confirmed_Revenue()
    {
        var resp = await _client.GetAsync("/api/dashboard/kpis");
        var body = await resp.Content.ReadAsStringAsync();
        // 250000 confirmed
        Assert.That(body, Does.Contain("\"confirmedRevenue\":250000"));
    }

    [Test, Order(5)]
    public async Task Dashboard_KPIs_Counts_Open_Incidents()
    {
        var resp = await _client.GetAsync("/api/dashboard/kpis");
        var body = await resp.Content.ReadAsStringAsync();
        // 1 reported incident counts as open
        Assert.That(body, Does.Contain("\"open\":1"));
    }

    [Test, Order(6)]
    public async Task Dashboard_KPIs_Filter_By_OrgId()
    {
        var fakeOrgId = Guid.NewGuid();
        var resp = await _client.GetAsync($"/api/dashboard/kpis?organizationId={fakeOrgId}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        // No data for fake org
        Assert.That(body, Does.Contain("\"open\":0"));
    }

    [Test, Order(7)]
    public async Task Dashboard_KPIs_Requires_Auth()
    {
        var resp = await _anonClient.GetAsync("/api/dashboard/kpis");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  CINETPAY INITIALIZE
    // ══════════════════════════════════

    [Test, Order(10)]
    public async Task CinetPay_Initialize_Creates_Payment()
    {
        var resp = await _client.PostAsJsonAsync("/api/cinetpay/initialize", new
        {
            amount = 100000,
            description = "Loyer test CinetPay",
            ownerId = _ownerId
        });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"paymentId\""));
        Assert.That(body, Does.Contain("\"reference\""));
        Assert.That(body, Does.Contain("\"cinetpay\""));
        Assert.That(body, Does.Contain("\"apikey\""));
        Assert.That(body, Does.Contain("\"site_id\""));
        Assert.That(body, Does.Contain("\"transaction_id\""));
        Assert.That(body, Does.Contain("\"amount\":100000"));
        Assert.That(body, Does.Contain("\"currency\":\"XOF\""));
    }

    [Test, Order(11)]
    public async Task CinetPay_Initialize_Reference_Starts_With_CP()
    {
        var resp = await _client.PostAsJsonAsync("/api/cinetpay/initialize", new
        {
            amount = 50000,
            description = "Test ref prefix"
        });

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"reference\":\"CP-"));
    }

    [Test, Order(12)]
    public async Task CinetPay_Initialize_Requires_Auth()
    {
        var resp = await _anonClient.PostAsJsonAsync("/api/cinetpay/initialize", new
        {
            amount = 10000
        });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ══════════════════════════════════
    //  CINETPAY STATUS
    // ══════════════════════════════════

    [Test, Order(20)]
    public async Task CinetPay_Status_Returns_Payment_Info()
    {
        // Create a payment first
        var initResp = await _client.PostAsJsonAsync("/api/cinetpay/initialize", new
        {
            amount = 75000,
            description = "Status check test"
        });
        var initBody = await initResp.Content.ReadAsStringAsync();

        // Extract paymentId from JSON
        var paymentIdStart = initBody.IndexOf("\"paymentId\":\"") + "\"paymentId\":\"".Length;
        var paymentIdEnd = initBody.IndexOf("\"", paymentIdStart);
        var paymentId = initBody[paymentIdStart..paymentIdEnd];

        var resp = await _client.GetAsync($"/api/cinetpay/status/{paymentId}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"amount\":75000"));
        Assert.That(body, Does.Contain("\"status\":\"Pending\""));
        Assert.That(body, Does.Contain("\"method\":\"CinetPay\""));
    }

    [Test, Order(21)]
    public async Task CinetPay_Status_NotFound_For_Fake_Id()
    {
        var resp = await _client.GetAsync($"/api/cinetpay/status/{Guid.NewGuid()}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ══════════════════════════════════
    //  CINETPAY WEBHOOK (NOTIFY)
    // ══════════════════════════════════

    [Test, Order(30)]
    public async Task CinetPay_Notify_Success_Marks_Payment_Completed()
    {
        // Create payment
        var initResp = await _client.PostAsJsonAsync("/api/cinetpay/initialize", new
        {
            amount = 200000,
            description = "Webhook test success"
        });
        var initBody = await initResp.Content.ReadAsStringAsync();

        // Extract reference
        var refStart = initBody.IndexOf("\"reference\":\"") + "\"reference\":\"".Length;
        var refEnd = initBody.IndexOf("\"", refStart);
        var reference = initBody[refStart..refEnd];

        // Extract paymentId
        var pidStart = initBody.IndexOf("\"paymentId\":\"") + "\"paymentId\":\"".Length;
        var pidEnd = initBody.IndexOf("\"", pidStart);
        var paymentId = initBody[pidStart..pidEnd];

        // Simulate CinetPay webhook (success)
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("cpm_trans_id", reference),
            new KeyValuePair<string, string>("cpm_result", "00"),
            new KeyValuePair<string, string>("cpm_amount", "200000"),
            new KeyValuePair<string, string>("cpm_currency", "XOF")
        });

        var notifyResp = await _anonClient.PostAsync("/api/cinetpay/notify", formContent);
        Assert.That(notifyResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Check payment status updated
        var statusResp = await _client.GetAsync($"/api/cinetpay/status/{paymentId}");
        var statusBody = await statusResp.Content.ReadAsStringAsync();
        Assert.That(statusBody, Does.Contain("\"status\":\"Completed\""));
    }

    [Test, Order(31)]
    public async Task CinetPay_Notify_Failure_Marks_Payment_Failed()
    {
        // Create payment
        var initResp = await _client.PostAsJsonAsync("/api/cinetpay/initialize", new
        {
            amount = 30000,
            description = "Webhook test fail"
        });
        var initBody = await initResp.Content.ReadAsStringAsync();

        var refStart = initBody.IndexOf("\"reference\":\"") + "\"reference\":\"".Length;
        var refEnd = initBody.IndexOf("\"", refStart);
        var reference = initBody[refStart..refEnd];

        var pidStart = initBody.IndexOf("\"paymentId\":\"") + "\"paymentId\":\"".Length;
        var pidEnd = initBody.IndexOf("\"", pidStart);
        var paymentId = initBody[pidStart..pidEnd];

        // Simulate failed payment
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("cpm_trans_id", reference),
            new KeyValuePair<string, string>("cpm_result", "600"),
        });

        var notifyResp = await _anonClient.PostAsync("/api/cinetpay/notify", formContent);
        Assert.That(notifyResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Check status = Failed
        var statusResp = await _client.GetAsync($"/api/cinetpay/status/{paymentId}");
        var statusBody = await statusResp.Content.ReadAsStringAsync();
        Assert.That(statusBody, Does.Contain("\"status\":\"Failed\""));
    }

    [Test, Order(32)]
    public async Task CinetPay_Notify_Unknown_Transaction_Returns_NotFound()
    {
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("cpm_trans_id", "FAKE-TRANS-ID"),
            new KeyValuePair<string, string>("cpm_result", "00"),
        });

        var resp = await _anonClient.PostAsync("/api/cinetpay/notify", formContent);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test, Order(33)]
    public async Task CinetPay_Notify_Missing_TransId_Returns_BadRequest()
    {
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("cpm_result", "00"),
        });

        var resp = await _anonClient.PostAsync("/api/cinetpay/notify", formContent);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ══════════════════════════════════
    //  PWA STATIC FILES
    // ══════════════════════════════════

    [Test, Order(40)]
    public async Task PWA_App_Page_Returns_HTML()
    {
        var resp = await _anonClient.GetAsync("/app");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("GreenSyndic"));
        Assert.That(body, Does.Contain("manifest.json"));
        Assert.That(body, Does.Contain("app.css"));
        Assert.That(body, Does.Contain("api.js"));
        Assert.That(body, Does.Contain("app.js"));
    }

    [Test, Order(41)]
    public async Task PWA_Manifest_Is_Served()
    {
        var resp = await _anonClient.GetAsync("/manifest.json");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("GreenSyndic"));
        Assert.That(body, Does.Contain("standalone"));
        Assert.That(body, Does.Contain("#2e7d32"));
    }

    [Test, Order(42)]
    public async Task PWA_ServiceWorker_Is_Served()
    {
        var resp = await _anonClient.GetAsync("/sw.js");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("greensyndic-v1"));
        Assert.That(body, Does.Contain("addEventListener"));
    }

    [Test, Order(43)]
    public async Task PWA_CSS_Is_Served()
    {
        var resp = await _anonClient.GetAsync("/css/app.css");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("--green"));
        Assert.That(body, Does.Contain(".kpi-card"));
        Assert.That(body, Does.Contain(".bottom-nav"));
    }

    [Test, Order(44)]
    public async Task PWA_JS_Api_Is_Served()
    {
        var resp = await _anonClient.GetAsync("/js/api.js");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Bearer"));
        Assert.That(body, Does.Contain("setToken"));
    }

    [Test, Order(45)]
    public async Task PWA_JS_App_Is_Served()
    {
        var resp = await _anonClient.GetAsync("/js/app.js");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Router"));
        Assert.That(body, Does.Contain("formatCurrency"));
    }

    [Test, Order(46)]
    public async Task PWA_Landing_Page_Has_App_Link()
    {
        var resp = await _anonClient.GetAsync("/index.html");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("href=\"/app\""));
        Assert.That(body, Does.Contain("Ouvrir l'application"));
    }

    [Test, Order(47)]
    public async Task PWA_Fallback_Catches_SubRoutes()
    {
        var resp = await _anonClient.GetAsync("/app/units");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("GreenSyndic"));
        Assert.That(body, Does.Contain("bottom-nav"));
    }

    [Test, Order(48)]
    public async Task PWA_App_Page_Has_Bottom_Nav()
    {
        var resp = await _anonClient.GetAsync("/app");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Accueil"));
        Assert.That(body, Does.Contain("Lots"));
        Assert.That(body, Does.Contain("Trésorerie"));
        Assert.That(body, Does.Contain("Incidents"));
        Assert.That(body, Does.Contain("Décaisser"));
    }

    [Test, Order(49)]
    public async Task PWA_App_Has_PWA_Meta_Tags()
    {
        var resp = await _anonClient.GetAsync("/app");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("theme-color"));
        Assert.That(body, Does.Contain("apple-mobile-web-app-capable"));
        Assert.That(body, Does.Contain("viewport"));
    }
}
