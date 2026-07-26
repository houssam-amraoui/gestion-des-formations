using Microsoft.EntityFrameworkCore;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Maintenance;

public sealed record CleanupSummary(int ExpiredSessions, int AnonymizedMessages,
    int TemporaryFiles, int OrphanCertificates);

public sealed class ProductionCleanupService(ApplicationDbContext db)
{
    public async Task<CleanupSummary> RunAsync(string certificateRoot, string temporaryRoot,
        int conversationRetentionDays, CancellationToken token = default)
    {
        var certificates = SafeRoot(certificateRoot);
        var temporary = SafeRoot(temporaryRoot);
        Directory.CreateDirectory(certificates);
        Directory.CreateDirectory(temporary);
        await using var cleanupLock = new FileStream(Path.Combine(temporary, ".cleanup.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var now = DateTime.UtcNow;
        var expired = await db.AiConversationSessions.Where(x =>
            x.Status == AiConversationStatus.Active && x.ExpiresAt <= now).ToListAsync(token);
        foreach (var session in expired) { session.Status = AiConversationStatus.Expired; session.EndedAt = now; }
        var threshold = now.AddDays(-Math.Max(1, conversationRetentionDays));
        var messages = await db.AiConversationMessages.Where(x => x.AiConversationSession.EndedAt < threshold &&
            x.TextContent != "[Contenu supprimé selon la politique de conservation]").ToListAsync(token);
        foreach (var message in messages)
        {
            message.TextContent = "[Contenu supprimé selon la politique de conservation]";
            message.TranscriptionText = null;
            message.AudioStoragePath = null;
        }
        await db.SaveChangesAsync(token);
        var tempDeleted = DeleteOldFiles(temporary, now.AddDays(-1), [".tmp", ".webm", ".wav", ".mp3"]);
        var referenced = (await db.Certificates.AsNoTracking().Where(x => x.PdfRelativePath != null)
            .Select(x => x.PdfRelativePath!).ToListAsync(token)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var orphanDeleted = 0;
        foreach (var file in Directory.EnumerateFiles(certificates, "*.pdf", SearchOption.TopDirectoryOnly))
            if (!referenced.Contains(Path.GetFileName(file)) && File.GetLastWriteTimeUtc(file) < now.AddDays(-7))
            { File.Delete(file); orphanDeleted++; }
        return new(expired.Count, messages.Count, tempDeleted, orphanDeleted);
    }

    private static int DeleteOldFiles(string root, DateTime threshold, IReadOnlyCollection<string> extensions)
    {
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
            if (extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase) &&
                File.GetLastWriteTimeUtc(file) < threshold)
            { File.Delete(file); count++; }
        return count;
    }

    private static string SafeRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Chemin de maintenance absent.");
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
    }
}
