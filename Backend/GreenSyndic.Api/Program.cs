using System.Text;
using GreenSyndic.Infrastructure.Data;
using GreenSyndic.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GreenSyndic.Services.Auth;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// === Database ===
builder.Services.AddDbContext<GreenSyndicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Identity ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Will enable later with SMS/email
})
.AddEntityFrameworkStores<GreenSyndicDbContext>()
.AddDefaultTokenProviders();

// === JWT Authentication ===
var jwtKey = builder.Configuration["Jwt:Key"] ?? "GreenSyndic-Dev-Key-Change-In-Production-MinLength32!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GreenSyndic",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GreenSyndic",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// === Services ===
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<GreenSyndic.Api.Services.GoogleVisionService>();

// === API + Razor Pages ===
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GreenSyndic API", Version = "v1" });
});

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// === Seed Data ===
await SeedData.InitializeAsync(app.Services);

// === Middleware Pipeline ===
// Swagger always enabled (dev + prod)
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GreenSyndic API v1"));

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapFallbackToPage("/app/{**catch-all}", "/App");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithTags("Health");

// Read git info once at startup
var gitVersion = "v0.0.0";
var gitCommitHash = "------";
try
{
    var gitDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..");
    var pVersion = new System.Diagnostics.Process();
    pVersion.StartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "git",
        Arguments = "describe --tags --always",
        WorkingDirectory = gitDir,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    pVersion.Start();
    var tagOutput = pVersion.StandardOutput.ReadToEnd().Trim();
    pVersion.WaitForExit(3000);

    var pHash = new System.Diagnostics.Process();
    pHash.StartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "git",
        Arguments = "rev-parse --short HEAD",
        WorkingDirectory = gitDir,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    pHash.Start();
    gitCommitHash = pHash.StandardOutput.ReadToEnd().Trim();
    pHash.WaitForExit(3000);

    if (!string.IsNullOrEmpty(tagOutput))
        gitVersion = tagOutput.Contains('-') ? tagOutput.Split('-')[0] : tagOutput;
    if (!gitVersion.StartsWith("v"))
        gitVersion = "v" + gitVersion;
    if (string.IsNullOrEmpty(gitCommitHash))
        gitCommitHash = "------";
}
catch { /* git not available */ }

// Version endpoint for landing page and all apps
app.MapGet("/api/version", () =>
{
    var now = DateTime.Now;
    var timestamp = now.ToString("yyMMdd.HHmm");
    return Results.Ok(new
    {
        timestamp,
        version = gitVersion,
        commitHash = gitCommitHash,
        environment = app.Environment.IsDevelopment() ? "DEV" : "PROD"
    });
}).WithTags("Health");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
