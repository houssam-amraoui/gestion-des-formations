using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Progress;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Application.Completion;
using TrainingManagement.Application.Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace TrainingManagement.Infrastructure.Services;

public sealed class LessonProgressService(ApplicationDbContext db, IServiceProvider services) : ILessonProgressService
{
    public async Task<ServiceResult<LessonProgressModel>> RecordAccessAsync(int lessonId, string learnerId,
        CancellationToken token = default)
    {
        var link = await FindEnrollmentAndLessonAsync(lessonId, learnerId, token);
        if (link is null) return ServiceResult<LessonProgressModel>.Failure("Vous n’avez pas accès à cette leçon.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var progress = await db.LessonProgresses.SingleOrDefaultAsync(x =>
            x.EnrollmentId == link.Value.Enrollment.Id && x.LessonId == lessonId, token);
        var now = DateTime.UtcNow;
        if (progress is null)
        {
            progress = new LessonProgress { EnrollmentId = link.Value.Enrollment.Id, LessonId = lessonId };
            db.LessonProgresses.Add(progress);
        }
        progress.RecordAccess(now);
        link.Value.Enrollment.StartedAt ??= now;
        link.Value.Enrollment.LastAccessedAt = now;
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult<LessonProgressModel>.Success(Map(progress));
    }

    public async Task<ServiceResult<TrainingProgressSummary>> CompleteAsync(int lessonId, string learnerId,
        CancellationToken token = default)
    {
        var link = await FindEnrollmentAndLessonAsync(lessonId, learnerId, token);
        if (link is null) return ServiceResult<TrainingProgressSummary>.Failure("Vous n’avez pas accès à cette leçon.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var progress = await db.LessonProgresses.SingleOrDefaultAsync(x =>
            x.EnrollmentId == link.Value.Enrollment.Id && x.LessonId == lessonId, token);
        if (progress is null)
        {
            progress = new LessonProgress { EnrollmentId = link.Value.Enrollment.Id, LessonId = lessonId };
            db.LessonProgresses.Add(progress);
        }
        var now = DateTime.UtcNow;
        progress.Complete(now);
        link.Value.Enrollment.LastAccessedAt = now;
        await db.SaveChangesAsync(token);
        var summary = await RecalculateCoreAsync(link.Value.Enrollment, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        await FinalizeTrainingAsync(link.Value.Enrollment, token);
        summary = await GetSummaryAsync(link.Value.Enrollment.Id, learnerId, token) ?? summary;
        return ServiceResult<TrainingProgressSummary>.Success(summary);
    }

    public Task<TrainingProgressSummary?> GetSummaryAsync(int enrollmentId, string learnerId,
        CancellationToken token = default) =>
        db.Enrollments.AsNoTracking().Where(x => x.Id == enrollmentId && x.LearnerId == learnerId)
            .Select(x => new TrainingProgressSummary(x.Id, x.TrainingId,
                x.LessonProgresses.Count(p => p.Status == LessonProgressStatus.Completed &&
                    p.Lesson.IsPublished && !p.Lesson.IsArchived),
                x.Training.Modules.SelectMany(m => m.Lessons).Count(l => l.IsPublished && !l.IsArchived),
                x.ProgressPercentage, x.Status)).SingleOrDefaultAsync(token);

    public async Task<ServiceResult<TrainingProgressSummary>> RecalculateAsync(int enrollmentId,
        CancellationToken token = default)
    {
        var enrollment = await db.Enrollments.FindAsync([enrollmentId], token);
        if (enrollment is null) return ServiceResult<TrainingProgressSummary>.Failure("Inscription introuvable.");
        var summary = await RecalculateCoreAsync(enrollment, token);
        await FinalizeTrainingAsync(enrollment, token);
        return ServiceResult<TrainingProgressSummary>.Success(
            await GetSummaryAsync(enrollment.Id, enrollment.LearnerId, token) ?? summary);
    }

    private async Task<TrainingProgressSummary> RecalculateCoreAsync(Enrollment enrollment, CancellationToken token)
    {
        var total = await db.Lessons.CountAsync(x => x.TrainingModule.TrainingId == enrollment.TrainingId &&
            x.IsPublished && !x.IsArchived && x.TrainingModule.IsPublished && !x.TrainingModule.IsArchived, token);
        var completed = await db.LessonProgresses.CountAsync(x => x.EnrollmentId == enrollment.Id &&
            x.Status == LessonProgressStatus.Completed && x.Lesson.IsPublished && !x.Lesson.IsArchived &&
            x.Lesson.TrainingModule.IsPublished && !x.Lesson.TrainingModule.IsArchived, token);
        var percentage = total == 0 ? 0 : Math.Round(completed * 100m / total, 2);
        enrollment.ProgressPercentage = Math.Clamp(percentage, 0, 100);
        if (services.GetService<ITrainingCompletionService>() is null)
            enrollment.SetProgress(percentage, DateTime.UtcNow);
        await db.SaveChangesAsync(token);
        return new(enrollment.Id, enrollment.TrainingId, completed, total, enrollment.ProgressPercentage, enrollment.Status);
    }

    private async Task<(Enrollment Enrollment, Lesson Lesson)?> FindEnrollmentAndLessonAsync(int lessonId,
        string learnerId, CancellationToken token)
    {
        var lesson = await db.Lessons.Include(x => x.TrainingModule)
            .SingleOrDefaultAsync(x => x.Id == lessonId && x.IsPublished && !x.IsArchived &&
                x.TrainingModule.IsPublished && !x.TrainingModule.IsArchived, token);
        if (lesson is null) return null;
        var enrollment = await db.Enrollments.SingleOrDefaultAsync(x => x.TrainingId == lesson.TrainingModule.TrainingId &&
            x.LearnerId == learnerId && (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Completed), token);
        return enrollment is null ? null : (enrollment, lesson);
    }
    private static LessonProgressModel Map(LessonProgress x) => new(x.Id, x.EnrollmentId, x.LessonId,
        x.Status, x.FirstAccessedAt, x.LastAccessedAt, x.CompletedAt, x.TimeSpentSeconds);

    private async Task FinalizeTrainingAsync(Enrollment enrollment, CancellationToken token)
    {
        var completion = services.GetService<ITrainingCompletionService>();
        if (completion is null) return;
        var finalized = await completion.FinalizeAsync(enrollment.Id, token);
        var certificates = services.GetService<ICertificateService>();
        if (certificates is not null && finalized.Succeeded && finalized.Value?.Completed == true)
            await certificates.GenerateAsync(enrollment.Id, token);
    }
}
