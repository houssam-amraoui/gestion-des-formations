using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Authentication;
using TrainingManagement.Infrastructure.Configuration;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TrainingManagement.Infrastructure.Certificates;
using TrainingManagement.Web.Infrastructure;

var platformPort = Environment.GetEnvironmentVariable("PORT");
if (args.Contains("--healthcheck", StringComparer.OrdinalIgnoreCase))
{
    var probePort = int.TryParse(platformPort, out var configuredPort) ? configuredPort : 8080;
    try
    {
        using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        using var response = await probe.GetAsync($"http://127.0.0.1:{probePort}/health/live");
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch { Environment.ExitCode = 1; }
    return;
}

var builder = WebApplication.CreateBuilder(args);
if (int.TryParse(platformPort, out var port) && port is > 0 and <= 65535)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = AntiforgeryCookieSettings.Name(builder.Environment);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = 6_000_000);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("certificate-verification", context =>
        RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    static string UserKey(HttpContext context) => context.User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    options.AddPolicy("ai-session-start", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("ai-message", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("ai-audio", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("ai-status", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
    options.AddPolicy("download", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("export", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
    options.AddPolicy("assessment-submit", context => RateLimitPartition.GetFixedWindowLimiter(
        UserKey(context), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<IDashboardRedirectService, DashboardRedirectService>();
builder.Services.AddTrainingManagementInfrastructure(builder.Configuration, builder.Environment);
var dataProtectionSettings = builder.Configuration.GetSection(DataProtectionStorageOptions.SectionName)
    .Get<DataProtectionStorageOptions>() ?? new();
var keysPath = Path.GetFullPath(Path.IsPathRooted(dataProtectionSettings.KeysPath)
    ? dataProtectionSettings.KeysPath
    : Path.Combine(builder.Environment.ContentRootPath, dataProtectionSettings.KeysPath));
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection().SetApplicationName(dataProtectionSettings.ApplicationName)
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"])
    .AddCheck<WritablePathHealthCheck>("persistent-storage", tags: ["ready"]);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var publicUrl = builder.Configuration["Application:PublicBaseUrl"];
    if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicUri) ||
        publicUri.Scheme != Uri.UriSchemeHttps || publicUrl!.EndsWith('/'))
        throw new InvalidOperationException(
            "Application:PublicBaseUrl doit être une URL HTTPS absolue sans slash final en Production.");
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseResponseCompression();
if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
        context.Context.Response.Headers.CacheControl = "public,max-age=604800"
});
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
            context.Response.Headers.CacheControl = "no-store,private";
        return Task.CompletedTask;
    });
    await next();
});

app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealth
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealth
}).AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("SeedAdmin:Enabled"))
{
    await using var seedScope = app.Services.CreateAsyncScope();
    var seeder = seedScope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    await using var demoScope = app.Services.CreateAsyncScope();
    await demoScope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

app.Run();

static Task WriteHealth(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        traceId = context.TraceIdentifier
    });
}

public partial class Program;
