using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using TrainingManagement.Infrastructure.AiTrainer;
using TrainingManagement.Infrastructure.Certificates;
using TrainingManagement.Infrastructure.Configuration;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Maintenance;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Web.Infrastructure;
using AdminExports = TrainingManagement.Web.Areas.Admin.Controllers.ExportsController;
using LearnerAttempts = TrainingManagement.Web.Areas.Learner.Controllers.AttemptsController;
using LearnerAiTrainer = TrainingManagement.Web.Areas.Learner.Controllers.AiTrainerController;

namespace TrainingManagement.Tests;

public sealed class ProductionReadinessTests
{
    [Fact] public void Development_UsesSqlite() => Assert.Contains("Sqlite", Provider("Development"));
    [Fact] public void Testing_UsesSqlite() => Assert.Contains("Sqlite", Provider("Testing"));
    [Fact] public void Production_UsesSqlServer() => Assert.Contains("SqlServer", Provider("Production"));
    [Fact] public void ProductionCookie_IsSecure()
    {
        using var services=Services("Production","Server=localhost;Database=Test;User Id=sa;Password=Strong123!;TrustServerCertificate=True");
        var cookie=services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
        Assert.Equal(CookieSecurePolicy.Always,cookie.Cookie.SecurePolicy);
    }
    [Fact] public void DevelopmentAntiforgeryCookie_HasStableName() =>
        Assert.Equal("TrainingManagement.Antiforgery",
            AntiforgeryCookieSettings.Name(new TestEnvironment("Development")));
    [Fact] public void ProductionAntiforgeryCookie_UsesHostPrefix() =>
        Assert.Equal("__Host-TrainingManagement.Antiforgery",
            AntiforgeryCookieSettings.Name(new TestEnvironment("Production")));
    [Fact] public void ProductionAntiforgeryCookieDeletion_IsSecure()
    {
        var options = AntiforgeryCookieSettings.DeleteOptions(new TestEnvironment("Production"));
        Assert.True(options.Secure);
        Assert.Equal("/", options.Path);
        Assert.True(options.HttpOnly);
    }
    [Fact] public void ProductionCookie_IsHttpOnly()
    {
        using var services=Services("Production","Server=localhost;Database=Test;User Id=sa;Password=Strong123!;TrustServerCertificate=True");
        Assert.True(services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme).Cookie.HttpOnly);
    }
    [Fact] public void MissingConnectionString_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() => Services("Production", connection: null));
    [Fact] public void ProductionSeed_IsDisabledByDefault() =>
        Assert.False(ReadJson("src/TrainingManagement.Web/appsettings.Production.json")
            .RootElement.GetProperty("SeedAdmin").GetProperty("Enabled").GetBoolean());
    [Fact] public void StrongAdminSeed_IsAccepted() =>
        Assert.True(new SeedAdminOptions { Enabled=true, Email="admin@example.test", Password="VeryStrong123!",
            FirstName="Admin", LastName="Test" }.IsValid(out _));
    [Theory, InlineData("short"), InlineData("alllowercase123!"), InlineData("ALLUPPERCASE123!"),
     InlineData("NoDigitsHere!"), InlineData("NoSymbols1234")]
    public void WeakAdminSeed_IsRejected(string password) =>
        Assert.False(new SeedAdminOptions { Enabled=true, Email="admin@example.test", Password=password,
            FirstName="Admin", LastName="Test" }.IsValid(out _));
    [Fact] public void DisabledAdminSeed_NeedsNoSecret() =>
        Assert.True(new SeedAdminOptions { Enabled=false }.IsValid(out _));
    [Fact] public void EnabledAnamWithoutKey_IsRejected()
    {
        using var services = Services("Production",
            "Server=localhost;Database=Test;Integrated Security=True;TrustServerCertificate=True",
            new Dictionary<string, string?>
            {
                ["AiTrainer:Enabled"] = "true",
                ["Anam:ApiKey"] = null,
                ["Anam:LlmId"] = null
            });
        Assert.Throws<OptionsValidationException>(() =>
            services.GetRequiredService<IOptions<AnamOptions>>().Value);
    }
    [Fact] public void DisabledAi_DoesNotRequireAnamSecret()
    {
        using var services = Services("Production",
            "Server=localhost;Database=Test;Integrated Security=True;TrustServerCertificate=True");
        Assert.NotNull(services.GetRequiredService<IOptions<AnamOptions>>().Value);
    }

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("X-Frame-Options", "SAMEORIGIN")]
    public async Task SecurityHeaders_ArePresent(string name, string value)
    {
        var context = new DefaultHttpContext();
        await new SecurityHeadersMiddleware(_ => Task.CompletedTask).InvokeAsync(context);
        Assert.Equal(value, context.Response.Headers[name]);
    }
    [Fact] public async Task PermissionsPolicy_DisablesGeolocation()
    {
        var context=new DefaultHttpContext();await new SecurityHeadersMiddleware(_=>Task.CompletedTask).InvokeAsync(context);
        Assert.Contains("geolocation=()", context.Response.Headers["Permissions-Policy"].ToString());
    }
    [Fact] public void Csp_RejectsUnsafeEval() => Assert.DoesNotContain("unsafe-eval", SecurityHeadersMiddleware.ContentSecurityPolicy);
    [Fact] public void Csp_HasNoWildcardDefault() => Assert.DoesNotContain("default-src *", SecurityHeadersMiddleware.ContentSecurityPolicy);
    [Fact] public void Csp_RestrictsObjects() => Assert.Contains("object-src 'none'", SecurityHeadersMiddleware.ContentSecurityPolicy);
    [Fact] public void Csp_RestrictsBaseUri() => Assert.Contains("base-uri 'self'", SecurityHeadersMiddleware.ContentSecurityPolicy);
    [Fact] public void Csp_AllowsKnownVideoHosts() { Assert.Contains("youtube.com", SecurityHeadersMiddleware.ContentSecurityPolicy); Assert.Contains("vimeo.com", SecurityHeadersMiddleware.ContentSecurityPolicy); }
    [Fact] public async Task CorrelationId_IsReturned()
    {
        var context=new DefaultHttpContext();await new CorrelationIdMiddleware(_=>Task.CompletedTask,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CorrelationIdMiddleware>.Instance).InvokeAsync(context);
        Assert.False(string.IsNullOrWhiteSpace(context.Response.Headers[CorrelationIdMiddleware.HeaderName]));
    }
    [Fact] public async Task OversizedClientCorrelationId_IsReplaced()
    {
        var context=new DefaultHttpContext();context.Request.Headers[CorrelationIdMiddleware.HeaderName]=new string('x',65);
        await new CorrelationIdMiddleware(_=>Task.CompletedTask,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CorrelationIdMiddleware>.Instance).InvokeAsync(context);
        Assert.NotEqual(new string('x',65),context.TraceIdentifier);
    }

    [Fact] public async Task Readiness_IsUnhealthyWhenDatabaseCannotBeOpened()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var services = Services("Testing", $"Data Source={missing};Mode=ReadOnly");
        var check = new DatabaseReadinessHealthCheck(
            services.GetRequiredService<IServiceScopeFactory>());
        Assert.Equal(HealthStatus.Unhealthy,
            (await check.CheckHealthAsync(new HealthCheckContext())).Status);
    }
    [Fact] public async Task Readiness_VerifiesWritablePersistentPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), $"tm-health-{Guid.NewGuid():N}");
        var check = new WritablePathHealthCheck(new TestEnvironment("Testing"),
            Options.Create(new CertificateStorageOptions
                { Provider = "Local", BasePath = Path.Combine(root, "certificates") }),
            Options.Create(new DataProtectionStorageOptions
                { KeysPath = Path.Combine(root, "keys"), ApplicationName = "Tests" }));
        Assert.Equal(HealthStatus.Healthy,
            (await check.CheckHealthAsync(new HealthCheckContext())).Status);
    }
    [Fact] public async Task Cleanup_RemovesOnlyExpiredTemporaryFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"tm-cleanup-{Guid.NewGuid():N}");
        var temporary = Path.Combine(root, "temporary");
        var certificates = Path.Combine(root, "certificates");
        Directory.CreateDirectory(temporary);
        var expired = Path.Combine(temporary, "expired.tmp");
        var recent = Path.Combine(temporary, "recent.tmp");
        await File.WriteAllTextAsync(expired, "expired");
        await File.WriteAllTextAsync(recent, "recent");
        File.SetLastWriteTimeUtc(expired, DateTime.UtcNow.AddDays(-2));
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var summary = await new ProductionCleanupService(db)
            .RunAsync(certificates, temporary, 365);

        Assert.Equal(1, summary.TemporaryFiles);
        Assert.False(File.Exists(expired));
        Assert.True(File.Exists(recent));
    }

    [Fact] public void AccountLogin_UsesLocalReturnUrlCheck() =>
        Assert.Contains("Url.IsLocalUrl", Text("src/TrainingManagement.Web/Controllers/AccountController.cs"));
    [Fact] public void ErrorView_HasNoStackTrace() => Assert.DoesNotContain("StackTrace", Text("src/TrainingManagement.Web/Views/Home/Error.cshtml"));
    [Fact] public void ErrorView_ShowsTraceReference() => Assert.Contains("RequestId", Text("src/TrainingManagement.Web/Views/Home/Error.cshtml"));
    [Fact] public void AdminExports_AreRateLimited() => Assert.Equal("export",
        typeof(AdminExports).GetCustomAttributes(typeof(EnableRateLimitingAttribute),true)
            .Cast<EnableRateLimitingAttribute>().Single().PolicyName);
    [Fact] public void AttemptSubmission_IsRateLimited() => Assert.Equal("assessment-submit",
        typeof(LearnerAttempts).GetMethod("Submit")!.GetCustomAttributes(typeof(EnableRateLimitingAttribute),true)
            .Cast<EnableRateLimitingAttribute>().Single().PolicyName);
    [Fact] public void AttemptSubmission_UsesAntiforgery() => Assert.Contains("ValidateAntiForgeryToken",
        string.Join(',',typeof(LearnerAttempts).GetMethod("Submit")!.GetCustomAttributes().Select(x=>x.GetType().Name)));
    [Fact] public void AiMessageForm_UsesItsRazorBindingPrefix()
    {
        var model = typeof(LearnerAiTrainer).GetMethod("SendMessage")!.GetParameters()[0];
        Assert.Equal("Message", model.GetCustomAttribute<BindAttribute>()?.Prefix);
    }

    [Theory, InlineData("Dockerfile"), InlineData(".dockerignore"), InlineData("compose.production-like.yml"),
     InlineData(".env.example"), InlineData("railway.json"), InlineData("global.json"),
     InlineData(".github/workflows/ci.yml")]
    public void ProductionArtifacts_Exist(string path) => Assert.True(File.Exists(Path.Combine(Root(),path)));
    [Fact] public void Dockerfile_IsMultiStage() { var x=Text("Dockerfile");Assert.Contains(" AS build",x);Assert.Contains(" AS final",x); }
    [Fact] public void Dockerfile_UsesRuntimeFinalImage() => Assert.Contains("dotnet/aspnet:10.0.5 AS final",Text("Dockerfile"));
    [Fact] public void Dockerfile_RunsAsNonRoot() => Assert.Contains("USER app",Text("Dockerfile"));
    [Fact] public void Dockerfile_HasHealthCheck() => Assert.Contains("HEALTHCHECK",Text("Dockerfile"));
    [Fact] public void Container_UsesPlatformPort() { Assert.Contains("PORT=8080",Text("Dockerfile")); Assert.Contains("0.0.0.0:{port}",Text("src/TrainingManagement.Web/Program.cs")); }
    [Fact] public void DockerContext_ExcludesDevelopmentSettings() => Assert.Contains("appsettings.Development.json",Text(".dockerignore"));
    [Fact] public void Dockerfile_DoesNotCopySecrets() => Assert.DoesNotContain("ApiKey=",Text("Dockerfile"));
    [Fact] public void Compose_UsesPinnedSqlServer() => Assert.Contains("2022-CU25-ubuntu-22.04",Text("compose.production-like.yml"));
    [Fact] public void Compose_UsesPersistentVolumes() { var x=Text("compose.production-like.yml");Assert.Contains("sqlserver-data",x);Assert.Contains("app-data",x); }
    [Fact] public void Railway_UsesLiveness() => Assert.Contains("/health/live",Text("railway.json"));

    [Theory, InlineData("dotnet restore"), InlineData("dotnet build"), InlineData("dotnet test"),
     InlineData("docker build")]
    public void Ci_ContainsRequiredCommand(string command) => Assert.Contains(command,Text(".github/workflows/ci.yml"));
    [Fact] public void Ci_DoesNotDeploy() { var x=Text(".github/workflows/ci.yml");Assert.DoesNotContain("railway up",x);Assert.DoesNotContain("docker push",x); }
    [Fact] public void Ci_DisablesRealAi() => Assert.Contains("AiTrainer__Enabled: \"false\"",Text(".github/workflows/ci.yml"));
    [Fact] public void ProductionJson_ContainsNoPassword() => Assert.DoesNotContain("Password",Text("src/TrainingManagement.Web/appsettings.Production.json"));
    [Fact] public void ProductionJson_ContainsNoApiKey() => Assert.DoesNotContain("ApiKey",Text("src/TrainingManagement.Web/appsettings.Production.json"));
    [Fact] public void WebOnlyMigratesInDevelopment() => Assert.Contains("if (app.Environment.IsDevelopment())",Text("src/TrainingManagement.Web/Program.cs"));
    [Fact] public void DedicatedMigrationCommand_Exists() => Assert.Contains("MigrateAsync",Text("src/TrainingManagement.Migrations/Program.cs"));
    [Fact] public void MigrationCommand_DoesNotLogConnection() => Assert.DoesNotContain("connectionString}",Text("src/TrainingManagement.Migrations/Program.cs"));

    private static string Provider(string environment)
    {
        using var services=Services(environment,"Server=localhost;Database=Test;User Id=sa;Password=Strong123!;TrustServerCertificate=True");
        return services.GetRequiredService<ApplicationDbContext>().Database.ProviderName!;
    }
    private static ServiceProvider Services(string environment,string? connection,
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values=new Dictionary<string,string?>
        {
            ["ConnectionStrings:DefaultConnection"]=connection,
            ["Application:Name"]="Test",["Application:PublicBaseUrl"]="https://test.invalid",
            ["CertificateStorage:Provider"]="Local",["CertificateStorage:BasePath"]="App_Data/Test",
            ["DataProtection:KeysPath"]="App_Data/Keys",["DataProtection:ApplicationName"]="Tests",
            ["SeedAdmin:Enabled"]="false",["AiTrainer:Enabled"]="false",
            ["AiTrainer:Provider"]="Anam",["AiTrainer:LanguageModelProvider"]="External",
            ["AiTrainer:AvatarProvider"]="Anam",["AiTrainer:SpeechToTextProvider"]="External",
            ["AiTrainer:TextToSpeechProvider"]="External",["Anam:BaseAddress"]="https://api.anam.ai/v1/"
        };
        if (overrides is not null)
            foreach (var pair in overrides) values[pair.Key] = pair.Value;
        var config=new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services=new ServiceCollection();services.AddLogging();
        services.AddTrainingManagementInfrastructure(config,new TestEnvironment(environment));
        return services.BuildServiceProvider();
    }
    private static JsonDocument ReadJson(string path)=>JsonDocument.Parse(Text(path));
    private static string Text(string path)=>File.ReadAllText(Path.Combine(Root(),path));
    private static string Root()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"TrainingManagement.sln")))directory=directory.Parent;
        return directory?.FullName??throw new DirectoryNotFoundException();
    }
    private sealed class TestEnvironment(string name):IWebHostEnvironment
    {
        public string EnvironmentName{get;set;}=name; public string ApplicationName{get;set;}="Tests";
        public string WebRootPath{get;set;}=Root(); public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
        public string ContentRootPath{get;set;}=Root(); public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
