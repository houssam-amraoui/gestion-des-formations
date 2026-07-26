using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Infrastructure.Maintenance;
using Microsoft.AspNetCore.Identity;
using TrainingManagement.Domain.Constants;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Migration refusée : ConnectionStrings__DefaultConnection est absente.");
    return 2;
}

using var loggerFactory = LoggerFactory.Create(logging =>
    logging.AddSimpleConsole(x => x.SingleLine = true).SetMinimumLevel(LogLevel.Information));
var options = new DbContextOptionsBuilder<ApplicationDbContext>();
if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
        .CommandTimeout(60));
else
    options.UseSqlite(connectionString);

try
{
    await using var db = new ApplicationDbContext(options.Options);
    var databaseExists = await db.GetService<IRelationalDatabaseCreator>().ExistsAsync();
    var applied = databaseExists
        ? (await db.Database.GetAppliedMigrationsAsync()).ToHashSet()
        : new HashSet<string>();
    var all = db.Database.GetMigrations().ToArray();
    var pending = all.Where(x => !applied.Contains(x)).ToArray();
    Console.WriteLine($"{all.Length} migration(s), {pending.Length} en attente.");
    foreach (var migration in pending) Console.WriteLine($"PENDING {migration}");
    if (args.Contains("--list", StringComparer.OrdinalIgnoreCase)) return 0;
    if (args.Contains("--cleanup", StringComparer.OrdinalIgnoreCase))
    {
        var certificatePath = configuration["CertificateStorage:BasePath"] ?? "/app/data/certificates";
        var temporaryPath = configuration["Maintenance:TempPath"] ?? "/app/data/temp";
        var retention = int.TryParse(configuration["AiTrainer:ConversationRetentionDays"], out var days)
            ? days : 365;
        var summary = await new ProductionCleanupService(db).RunAsync(
            certificatePath, temporaryPath, retention);
        Console.WriteLine($"Nettoyage terminé : {summary.ExpiredSessions} session(s), " +
            $"{summary.AnonymizedMessages} message(s), {summary.TemporaryFiles} temporaire(s), " +
            $"{summary.OrphanCertificates} certificat(s) orphelin(s).");
        return 0;
    }
    if (pending.Length > 0)
    {
        Console.WriteLine("Application contrôlée des migrations (une seule instance de cette commande doit être lancée).");
        await db.Database.MigrateAsync();
    }
    foreach (var roleName in AppRoles.All)
        if (!await db.Roles.AnyAsync(x => x.NormalizedName == roleName.ToUpperInvariant()))
            db.Roles.Add(new IdentityRole(roleName) { NormalizedName = roleName.ToUpperInvariant() });
    await db.SaveChangesAsync();
    Console.WriteLine($"{pending.Length} migration(s) appliquée(s), rôles vérifiés avec succès.");
    return 0;
}
catch (Exception exception)
{
    loggerFactory.CreateLogger("Migrations").LogError(
        "Échec de la commande de migration ({ExceptionType}). Aucun détail de connexion n’est journalisé.",
        exception.GetType().Name);
    return 1;
}
