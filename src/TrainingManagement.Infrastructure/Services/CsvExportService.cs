using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrainingManagement.Application.Exports;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class CsvExportService(ApplicationDbContext db, ILogger<CsvExportService> logger) : ICsvExportService
{
    public byte[] Create(IEnumerable<IReadOnlyCollection<string?>> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
            builder.AppendLine(string.Join(';', row.Select(Escape)));
        return new UTF8Encoding(true).GetPreamble().Concat(new UTF8Encoding(false).GetBytes(builder.ToString())).ToArray();
    }

    public async Task<CsvFileResult> ExportAdminEnrollmentsAsync(CancellationToken token = default)
    {
        var rows = await db.Enrollments.AsNoTracking().OrderByDescending(x => x.EnrolledAt)
            .Select(x => new[] { x.Id.ToString(), x.LearnerId, x.Training.Title, x.Status.ToString(),
                x.ProgressPercentage.ToString(), x.EnrolledAt.ToString("O") }).ToListAsync(token);
        return Result("inscriptions", ["Id","ApprenantId","Formation","Statut","Progression","InscritLe"], rows);
    }

    public async Task<CsvFileResult> ExportAdminProgressAsync(CancellationToken token = default)
    {
        var rows = await db.LessonProgresses.AsNoTracking().OrderBy(x => x.EnrollmentId)
            .Select(x => new[] { x.EnrollmentId.ToString(), x.Enrollment.LearnerId,
                x.Enrollment.Training.Title, x.Lesson.Title, x.Status.ToString(),
                x.TimeSpentSeconds.ToString(), x.LastAccessedAt!.Value.ToString("O") }).ToListAsync(token);
        return Result("progression", ["InscriptionId","ApprenantId","Formation","Lecon","Statut","TempsSecondes","DernierAcces"], rows);
    }

    public async Task<CsvFileResult> ExportAdminAssessmentResultsAsync(CancellationToken token = default)
    {
        var rows = await AttemptRows(null, token);
        return Result("resultats-evaluations", AttemptHeaders, rows);
    }

    public async Task<CsvFileResult> ExportAdminCertificatesAsync(CancellationToken token = default)
    {
        var rows = await db.Certificates.AsNoTracking().OrderByDescending(x => x.IssuedAt)
            .Select(x => new[] { x.CertificateNumber, x.LearnerFullNameSnapshot,
                x.TrainingTitleSnapshot, x.Status.ToString(), x.CompletionDate.ToString("O"),
                x.IssuedAt.ToString("O"), x.ExpiresAt == null ? "" : x.ExpiresAt.Value.ToString("O") })
            .ToListAsync(token);
        return Result("certificats", ["Numero","Apprenant","Formation","Statut","Reussite","Emission","Expiration"], rows);
    }

    public async Task<CsvFileResult?> ExportTrainingAnalyticsAsync(int trainingId, string? trainerId = null,
        CancellationToken token = default)
    {
        var training = await db.Trainings.AsNoTracking().Where(x => x.Id == trainingId &&
            (trainerId == null || x.TrainerId == trainerId)).Select(x => x.Title).SingleOrDefaultAsync(token);
        if (training is null) return null;
        var total = await db.Enrollments.CountAsync(x => x.TrainingId == trainingId &&
            x.Status != EnrollmentStatus.Cancelled, token);
        var completed = await db.Enrollments.CountAsync(x => x.TrainingId == trainingId &&
            x.Status == EnrollmentStatus.Completed, token);
        var rows = new List<IReadOnlyCollection<string?>>
        {
            new[] { "Indicateur", "Valeur" },
            new[] { "Formation", training },
            new[] { "Inscriptions", total.ToString(CultureInfo.InvariantCulture) },
            new[] { "Terminées", completed.ToString(CultureInfo.InvariantCulture) },
            new[] { "Taux de complétion", total == 0 ? "0" :
                (completed * 100m / total).ToString("0.00", CultureInfo.InvariantCulture) }
        };
        return new($"statistiques-formation-{trainingId}.csv", Create(rows));
    }

    public async Task<CsvFileResult> ExportTrainerLearnersAsync(string trainerId,
        CancellationToken token = default)
    {
        var rows = await db.Enrollments.AsNoTracking().Where(x => x.Training.TrainerId == trainerId)
            .Select(x => new[] { x.LearnerId, x.Training.Title, x.Status.ToString(),
                x.ProgressPercentage.ToString(), x.LastAccessedAt == null ? "" : x.LastAccessedAt.Value.ToString("O") })
            .ToListAsync(token);
        return Result("mes-apprenants", ["ApprenantId","Formation","Statut","Progression","DerniereActivite"], rows);
    }

    public async Task<CsvFileResult> ExportTrainerProgressAsync(string trainerId,
        CancellationToken token = default)
    {
        var rows = await db.LessonProgresses.AsNoTracking().Where(x =>
            x.Enrollment.Training.TrainerId == trainerId)
            .Select(x => new[] { x.Enrollment.LearnerId, x.Enrollment.Training.Title,
                x.Lesson.Title, x.Status.ToString(), x.TimeSpentSeconds.ToString() }).ToListAsync(token);
        return Result("progression-apprenants", ["ApprenantId","Formation","Lecon","Statut","TempsSecondes"], rows);
    }

    public async Task<CsvFileResult> ExportTrainerAssessmentResultsAsync(string trainerId,
        CancellationToken token = default) =>
        Result("resultats-agreges", AttemptHeaders, await AttemptRows(trainerId, token));

    private async Task<List<string[]>> AttemptRows(string? trainerId, CancellationToken token) =>
        await db.AssessmentAttempts.AsNoTracking().Where(x => trainerId == null ||
            x.Enrollment.Training.TrainerId == trainerId)
        .Select(x => new[] { x.Enrollment.LearnerId, x.Enrollment.Training.Title, x.Assessment.Title,
            x.AttemptNumber.ToString(), x.Status.ToString(),
            x.PercentageScore == null ? "" : x.PercentageScore.Value.ToString(),
            x.Passed == null ? "" : x.Passed.Value.ToString() }).ToListAsync(token);

    private CsvFileResult Result(string name, IReadOnlyCollection<string?> header,
        IEnumerable<IReadOnlyCollection<string?>> rows)
    {
        logger.LogInformation("Export CSV {ExportName} généré.", name);
        return new($"{name}-{DateTime.UtcNow:yyyyMMdd}.csv", Create(new[] { header }.Concat(rows)));
    }

    private static string Escape(string? raw)
    {
        var value = raw ?? string.Empty;
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static readonly string[] AttemptHeaders =
        ["ApprenantId","Formation","Evaluation","Tentative","Statut","Score","Reussi"];
}
