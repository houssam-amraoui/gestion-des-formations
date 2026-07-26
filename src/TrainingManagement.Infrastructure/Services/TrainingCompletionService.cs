using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Completion;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class TrainingCompletionService(ApplicationDbContext db) : ITrainingCompletionService
{
    public async Task<TrainingCompletionEvaluation?> EvaluateAsync(int enrollmentId,
        CancellationToken token = default)
    {
        var enrollment = await db.Enrollments.AsNoTracking()
            .Include(x => x.Training).SingleOrDefaultAsync(x => x.Id == enrollmentId, token);
        if (enrollment is null) return null;
        var training = enrollment.Training;
        var eligibleLessons = db.Lessons.Where(x => x.TrainingModule.TrainingId == training.Id &&
            x.IsPublished && !x.IsArchived && x.TrainingModule.IsPublished && !x.TrainingModule.IsArchived);
        var lessonCount = await eligibleLessons.CountAsync(token);
        var completedLessons = await db.LessonProgresses.CountAsync(x => x.EnrollmentId == enrollmentId &&
            x.Status == LessonProgressStatus.Completed && x.Lesson.IsPublished && !x.Lesson.IsArchived &&
            x.Lesson.TrainingModule.IsPublished && !x.Lesson.TrainingModule.IsArchived, token);
        var lessonsMet = !training.RequireAllLessonsCompleted || completedLessons == lessonCount;

        var mandatoryIds = db.Assessments.Where(x => x.Lesson.TrainingModule.TrainingId == training.Id &&
            x.IsPublished && !x.IsArchived && x.IsMandatory && x.Lesson.IsPublished && !x.Lesson.IsArchived &&
            x.Lesson.TrainingModule.IsPublished && !x.Lesson.TrainingModule.IsArchived).Select(x => x.Id);
        var mandatoryCount = await mandatoryIds.CountAsync(token);
        var passedMandatory = await mandatoryIds.CountAsync(id => db.AssessmentAttempts.Any(a =>
            a.EnrollmentId == enrollmentId && a.AssessmentId == id && a.Passed == true &&
            (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Expired)), token);
        var assessmentsMet = !training.RequireAllMandatoryAssessmentsPassed || passedMandatory == mandatoryCount;

        var bestScores = await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.EnrollmentId == enrollmentId && x.Assessment.IsPublished && !x.Assessment.IsArchived &&
                x.Assessment.Lesson.IsPublished && !x.Assessment.Lesson.IsArchived &&
                x.Assessment.Lesson.TrainingModule.IsPublished && !x.Assessment.Lesson.TrainingModule.IsArchived &&
                x.PercentageScore != null &&
                (x.Status == AttemptStatus.Submitted || x.Status == AttemptStatus.Expired))
            .GroupBy(x => x.AssessmentId).Select(g => g.Max(x => x.PercentageScore!.Value)).ToListAsync(token);
        decimal? average = bestScores.Count == 0 ? null : Math.Round(bestScores.Average(), 2);
        var averageMet = training.MinimumAverageScore is null ||
            (average is null || average >= training.MinimumAverageScore);

        var requirements = new[]
        {
            new TrainingCompletionRequirement("lessons", "Leçons obligatoires", lessonsMet,
                $"{completedLessons}/{lessonCount} leçons terminées"),
            new TrainingCompletionRequirement("mandatory-assessments", "Évaluations obligatoires", assessmentsMet,
                $"{passedMandatory}/{mandatoryCount} évaluations réussies"),
            new TrainingCompletionRequirement("average", "Moyenne minimale", averageMet,
                training.MinimumAverageScore is null ? "Non applicable" :
                average is null ? "Aucune tentative éligible : critère non applicable" :
                $"{average:N2}% / {training.MinimumAverageScore:N2}%")
        };
        return new(enrollmentId, requirements.All(x => x.IsMet), average, requirements);
    }

    public async Task<ServiceResult<TrainingCompletionResult>> FinalizeAsync(int enrollmentId,
        CancellationToken token = default)
    {
        var enrollment = await db.Enrollments.SingleOrDefaultAsync(x => x.Id == enrollmentId, token);
        if (enrollment is null)
            return ServiceResult<TrainingCompletionResult>.Failure("Inscription introuvable.");
        var evaluation = await EvaluateAsync(enrollmentId, token);
        if (evaluation is null)
            return ServiceResult<TrainingCompletionResult>.Failure("Inscription introuvable.");
        var already = enrollment.Status == EnrollmentStatus.Completed;
        if (evaluation.IsEligible && !already)
        {
            enrollment.Status = EnrollmentStatus.Completed;
            enrollment.ProgressPercentage = 100;
            enrollment.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(token);
        }
        return ServiceResult<TrainingCompletionResult>.Success(new(enrollmentId, already,
            evaluation.IsEligible, enrollment.CompletedAt, evaluation));
    }
}
