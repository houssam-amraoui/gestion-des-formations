using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AnalyticsService(ApplicationDbContext db) : IAnalyticsService
{
    public async Task<AdminDashboardAnalytics> GetAdminDashboardAsync(AnalyticsFilterModel filter,
        CancellationToken token = default)
    {
        var (from, to) = Range(filter);
        var learnerRoleId = await db.Roles.Where(x => x.Name == AppRoles.Learner).Select(x => x.Id).SingleAsync(token);
        var trainerRoleId = await db.Roles.Where(x => x.Name == AppRoles.Trainer).Select(x => x.Id).SingleAsync(token);
        var totalUsers = await db.Users.CountAsync(token);
        var learners = await db.UserRoles.CountAsync(x => x.RoleId == learnerRoleId, token);
        var trainers = await db.UserRoles.CountAsync(x => x.RoleId == trainerRoleId, token);
        var trainings = await db.Trainings.CountAsync(token);
        var published = await db.Trainings.CountAsync(x => x.Status == TrainingStatus.Published, token);
        var active = await db.Enrollments.CountAsync(x => x.Status == EnrollmentStatus.Active, token);
        var completed = await db.Enrollments.CountAsync(x => x.Status == EnrollmentStatus.Completed, token);
        var certificates = await db.Certificates.CountAsync(token);
        var progress = await db.Enrollments.Where(x => x.Status == EnrollmentStatus.Active ||
            x.Status == EnrollmentStatus.Completed).Select(x => (decimal?)x.ProgressPercentage).AverageAsync(token) ?? 0;
        var graded = db.AssessmentAttempts.Where(x => x.Status == AttemptStatus.Submitted ||
            x.Status == AttemptStatus.Expired);
        var attemptCount = await graded.CountAsync(token);
        var successRate = attemptCount == 0 ? 0 :
            Math.Round(await graded.CountAsync(x => x.Passed == true, token) * 100m / attemptCount, 2);
        return new(totalUsers, learners, trainers, trainings, published, active, completed, certificates,
            Math.Round(progress, 2), successRate,
            await EnrollmentSeries(from, to, null, token),
            await AttemptSeries(from, to, null, token),
            await CertificateSeries(from, to, null, token),
            await TrainingPerformance(null, token),
            await AssessmentPerformance(null, token),
            await DropOffs(null, token),
            await Recent(null, from, to, token),
            await CategoryDistribution(token));
    }

    public async Task<TrainingAnalyticsModel?> GetTrainingAsync(int trainingId, AnalyticsFilterModel filter,
        string? trainerId = null, CancellationToken token = default)
    {
        var training = await db.Trainings.AsNoTracking().Where(x => x.Id == trainingId &&
            (trainerId == null || x.TrainerId == trainerId)).Select(x => new { x.Id, x.Title }).SingleOrDefaultAsync(token);
        if (training is null) return null;
        var (from, to) = Range(filter);
        var enrollments = db.Enrollments.Where(x => x.TrainingId == trainingId &&
            x.EnrolledAt >= from && x.EnrolledAt < to && x.Status != EnrollmentStatus.Cancelled);
        var total = await enrollments.CountAsync(token);
        var completed = await enrollments.CountAsync(x => x.Status == EnrollmentStatus.Completed, token);
        var active = await enrollments.CountAsync(x => x.Status == EnrollmentStatus.Active, token);
        var avgProgress = await enrollments.Select(x => (decimal?)x.ProgressPercentage).AverageAsync(token) ?? 0;
        var attempts = db.AssessmentAttempts.Where(x => x.Enrollment.TrainingId == trainingId &&
            x.StartedAt >= from && x.StartedAt < to &&
            (x.Status == AttemptStatus.Submitted || x.Status == AttemptStatus.Expired));
        var attemptCount = await attempts.CountAsync(token);
        var rate = attemptCount == 0 ? 0 : await attempts.CountAsync(x => x.Passed == true, token) * 100m / attemptCount;
        var certs = await db.Certificates.CountAsync(x => x.Enrollment.TrainingId == trainingId &&
            x.IssuedAt >= from && x.IssuedAt < to, token);
        var lessonCount = await db.Lessons.CountAsync(x => x.TrainingModule.TrainingId == trainingId &&
            x.IsPublished && !x.IsArchived, token);
        var performances = await AssessmentPerformance(trainingId, token);
        return new(training.Id, training.Title, total, active, completed,
            total == 0 ? 0 : Math.Round(completed * 100m / total, 2), Math.Round(avgProgress, 2),
            lessonCount, Math.Round(rate, 2), certs,
            performances.OrderByDescending(x => x.SuccessRate).Take(5).ToArray(),
            performances.OrderBy(x => x.SuccessRate).Take(5).ToArray(),
            await DropOffs(trainingId, token), await Recent(trainingId, from, to, token));
    }

    public async Task<TrainerDashboardAnalytics> GetTrainerDashboardAsync(string trainerId,
        AnalyticsFilterModel filter, CancellationToken token = default)
    {
        var (from, to) = Range(filter);
        var enrollments = db.Enrollments.Where(x => x.Training.TrainerId == trainerId &&
            x.Status != EnrollmentStatus.Cancelled);
        var learners = await enrollments.Select(x => x.LearnerId).Distinct().CountAsync(token);
        var active = await enrollments.CountAsync(x => x.Status == EnrollmentStatus.Active, token);
        var completed = await enrollments.CountAsync(x => x.Status == EnrollmentStatus.Completed, token);
        var progress = await enrollments.Select(x => (decimal?)x.ProgressPercentage).AverageAsync(token) ?? 0;
        var certs = await db.Certificates.CountAsync(x => x.Enrollment.Training.TrainerId == trainerId, token);
        var attempts = db.AssessmentAttempts.Where(x => x.Enrollment.Training.TrainerId == trainerId &&
            (x.Status == AttemptStatus.Submitted || x.Status == AttemptStatus.Expired));
        var count = await attempts.CountAsync(token);
        var rate = count == 0 ? 0 : await attempts.CountAsync(x => x.Passed == true, token) * 100m / count;
        var learnerActivityRows = await (from enrollment in db.Enrollments.AsNoTracking()
            join learner in db.Users.AsNoTracking() on enrollment.LearnerId equals learner.Id
            where enrollment.Training.TrainerId == trainerId &&
                enrollment.Status != EnrollmentStatus.Cancelled
            select new
            {
                LearnerName = learner.FirstName + " " + learner.LastName,
                TrainingTitle = enrollment.Training.Title,
                enrollment.LastAccessedAt
            })
            .OrderByDescending(x => x.LastAccessedAt)
            .Take(100)
            .ToListAsync(token);
        var learnerActivity = learnerActivityRows.Select(x =>
            new LearnerActivityItem(x.LearnerName, x.TrainingTitle, x.LastAccessedAt)).ToArray();
        var inactivityThreshold = DateTime.UtcNow.AddDays(-30);
        return new(learners, active, Math.Round(progress, 2), completed, certs, Math.Round(rate, 2),
            await AssessmentPerformance(null, token, trainerId), await Recent(null, from, to, token, trainerId),
            learnerActivity.Where(x => x.LastActivity >= from && x.LastActivity < to).Take(10).ToArray(),
            learnerActivity.Where(x => x.LastActivity is null || x.LastActivity < inactivityThreshold)
                .OrderBy(x => x.LastActivity).Take(10).ToArray());
    }

    public async Task<LearnerDashboardAnalytics> GetLearnerDashboardAsync(string learnerId,
        CancellationToken token = default)
    {
        var enrollments = db.Enrollments.Where(x => x.LearnerId == learnerId);
        var active = await enrollments.CountAsync(x => x.Status == EnrollmentStatus.Active, token);
        var completed = await enrollments.CountAsync(x => x.Status == EnrollmentStatus.Completed, token);
        var progress = await enrollments.Where(x => x.Status == EnrollmentStatus.Active ||
            x.Status == EnrollmentStatus.Completed).Select(x => (decimal?)x.ProgressPercentage).AverageAsync(token) ?? 0;
        var lessons = await db.LessonProgresses.CountAsync(x => x.Enrollment.LearnerId == learnerId &&
            x.Status == LessonProgressStatus.Completed, token);
        var time = await db.LessonProgresses.Where(x => x.Enrollment.LearnerId == learnerId)
            .SumAsync(x => x.TimeSpentSeconds, token);
        var attempts = db.AssessmentAttempts.Where(x => x.Enrollment.LearnerId == learnerId &&
            (x.Status == AttemptStatus.Submitted || x.Status == AttemptStatus.Expired));
        var count = await attempts.CountAsync(token);
        var passed = await attempts.CountAsync(x => x.Passed == true, token);
        var best = await attempts.MaxAsync(x => (decimal?)x.PercentageScore, token) ?? 0;
        var certificates = await db.Certificates.CountAsync(x => x.Enrollment.LearnerId == learnerId, token);
        return new(Math.Round(progress, 2), active, completed, lessons, count,
            count == 0 ? 0 : Math.Round(passed * 100m / count, 2), best, time, certificates,
            await RecentForLearner(learnerId, token));
    }

    public async Task<IReadOnlyCollection<LearnerTrainingStatistics>> GetLearnerStatisticsAsync(
        string learnerId, CancellationToken token = default) =>
        await db.Enrollments.AsNoTracking().Where(x => x.LearnerId == learnerId)
            .OrderBy(x => x.Training.Title).Select(x => new LearnerTrainingStatistics(
                x.TrainingId, x.Training.Title, x.ProgressPercentage,
                x.LessonProgresses.Count(p => p.Status == LessonProgressStatus.Completed),
                x.Attempts.Count(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Expired),
                x.Attempts.Max(a => a.PercentageScore), x.Attempts.Average(a => a.PercentageScore),
                x.Attempts.Count, x.Certificate != null,
                new[] { x.LastAccessedAt, x.Attempts.Max(a => a.SubmittedAt) }.Max()))
            .ToListAsync(token);

    public static (DateTime From, DateTime To) Range(AnalyticsFilterModel filter)
    {
        var today = DateTime.UtcNow.Date;
        var range = filter.Period switch
        {
            AnalyticsPeriod.Last7Days => (today.AddDays(-6), today.AddDays(1)),
            AnalyticsPeriod.Last30Days => (today.AddDays(-29), today.AddDays(1)),
            AnalyticsPeriod.Last90Days => (today.AddDays(-89), today.AddDays(1)),
            AnalyticsPeriod.CurrentYear => (new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), today.AddDays(1)),
            AnalyticsPeriod.Custom when filter.DateFrom is not null && filter.DateTo is not null =>
                (DateTime.SpecifyKind(filter.DateFrom.Value.Date, DateTimeKind.Utc),
                 DateTime.SpecifyKind(filter.DateTo.Value.Date.AddDays(1), DateTimeKind.Utc)),
            _ => throw new ArgumentException("La période statistique personnalisée est invalide.")
        };
        if (range.Item1 >= range.Item2) throw new ArgumentException("La date de début doit précéder la date de fin.");
        if ((range.Item2 - range.Item1).TotalDays > 366) throw new ArgumentException("La période ne peut pas dépasser 366 jours.");
        return range;
    }

    private async Task<IReadOnlyCollection<TimeSeriesDataPoint>> EnrollmentSeries(DateTime from, DateTime to,
        int? trainingId, CancellationToken token) => (await db.Enrollments.AsNoTracking()
            .Where(x => x.EnrolledAt >= from && x.EnrolledAt < to &&
                (trainingId == null || x.TrainingId == trainingId))
            .GroupBy(x => x.EnrolledAt.Date).Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date).ToListAsync(token)).Select(x => new TimeSeriesDataPoint(x.Date, x.Count)).ToArray();

    private async Task<IReadOnlyCollection<TimeSeriesDataPoint>> AttemptSeries(DateTime from, DateTime to,
        int? trainingId, CancellationToken token) => (await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.StartedAt >= from && x.StartedAt < to &&
                (trainingId == null || x.Enrollment.TrainingId == trainingId))
            .GroupBy(x => x.StartedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date).ToListAsync(token)).Select(x => new TimeSeriesDataPoint(x.Date, x.Count)).ToArray();

    private async Task<IReadOnlyCollection<TimeSeriesDataPoint>> CertificateSeries(DateTime from, DateTime to,
        int? trainingId, CancellationToken token) => (await db.Certificates.AsNoTracking()
            .Where(x => x.IssuedAt >= from && x.IssuedAt < to &&
                (trainingId == null || x.Enrollment.TrainingId == trainingId))
            .GroupBy(x => x.IssuedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date).ToListAsync(token)).Select(x => new TimeSeriesDataPoint(x.Date, x.Count)).ToArray();

    private async Task<IReadOnlyCollection<TrainingPerformanceItem>> TrainingPerformance(string? trainerId,
        CancellationToken token)
    {
        var rows = await db.Trainings.AsNoTracking().Where(x => trainerId == null || x.TrainerId == trainerId)
            .Select(x => new { x.Id, x.Title,
                Enrollments = x.Enrollments.Count(e => e.Status != EnrollmentStatus.Cancelled),
                Completed = x.Enrollments.Count(e => e.Status == EnrollmentStatus.Completed) })
            .OrderByDescending(x => x.Enrollments).Take(5).ToListAsync(token);
        return rows.Select(x => new TrainingPerformanceItem(x.Id, x.Title, x.Enrollments, x.Completed,
            x.Enrollments == 0 ? 0 : Math.Round(x.Completed * 100m / x.Enrollments, 2))).ToArray();
    }

    private async Task<IReadOnlyCollection<AssessmentPerformanceItem>> AssessmentPerformance(int? trainingId,
        CancellationToken token, string? trainerId = null)
    {
        var rows = await db.Assessments.AsNoTracking()
            .Where(x => (trainingId == null || x.Lesson.TrainingModule.TrainingId == trainingId) &&
                (trainerId == null || x.Lesson.TrainingModule.Training.TrainerId == trainerId))
            .Select(x => new { x.Id, x.Title,
                Attempts = x.Attempts.Count(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Expired),
                Passed = x.Attempts.Count(a => a.Passed == true &&
                    (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Expired)) })
            .Where(x => x.Attempts > 0).ToListAsync(token);
        return rows.Select(x => new AssessmentPerformanceItem(x.Id, x.Title, x.Attempts,
            Math.Round(x.Passed * 100m / x.Attempts, 2))).OrderBy(x => x.SuccessRate).Take(10).ToArray();
    }

    private async Task<IReadOnlyCollection<LessonDropOffItem>> DropOffs(int? trainingId,
        CancellationToken token)
    {
        var rows = await db.Lessons.AsNoTracking()
            .Where(x => trainingId == null || x.TrainingModule.TrainingId == trainingId)
            .Select(x => new { x.Id, x.Title, Started = x.ProgressRecords.Count,
                NotCompleted = x.ProgressRecords.Count(p => p.Status != LessonProgressStatus.Completed) })
            .Where(x => x.Started > 0).ToListAsync(token);
        return rows.Select(x => new LessonDropOffItem(x.Id, x.Title, x.Started, x.NotCompleted,
            Math.Round(x.NotCompleted * 100m / x.Started, 2)))
            .OrderByDescending(x => x.DropOffRate).Take(10).ToArray();
    }

    private async Task<IReadOnlyCollection<RecentActivityItem>> Recent(int? trainingId, DateTime from,
        DateTime to, CancellationToken token, string? trainerId = null)
    {
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.EnrolledAt >= from &&
            x.EnrolledAt < to && (trainingId == null || x.TrainingId == trainingId) &&
            (trainerId == null || x.Training.TrainerId == trainerId))
            .OrderByDescending(x => x.EnrolledAt).Take(8)
            .Select(x => new RecentActivityItem("Inscription", x.Training.Title, x.EnrolledAt)).ToListAsync(token);
        var attempts = await db.AssessmentAttempts.AsNoTracking().Where(x => x.StartedAt >= from &&
            x.StartedAt < to && (trainingId == null || x.Enrollment.TrainingId == trainingId) &&
            (trainerId == null || x.Enrollment.Training.TrainerId == trainerId))
            .OrderByDescending(x => x.StartedAt).Take(8)
            .Select(x => new RecentActivityItem("Tentative", x.Assessment.Title, x.SubmittedAt ?? x.StartedAt))
            .ToListAsync(token);
        var completions = await db.Enrollments.AsNoTracking().Where(x => x.CompletedAt >= from &&
            x.CompletedAt < to && (trainingId == null || x.TrainingId == trainingId) &&
            (trainerId == null || x.Training.TrainerId == trainerId))
            .OrderByDescending(x => x.CompletedAt).Take(8)
            .Select(x => new RecentActivityItem("Formation terminée", x.Training.Title, x.CompletedAt!.Value))
            .ToListAsync(token);
        var certificates = await db.Certificates.AsNoTracking().Where(x => x.IssuedAt >= from &&
            x.IssuedAt < to && (trainingId == null || x.Enrollment.TrainingId == trainingId) &&
            (trainerId == null || x.Enrollment.Training.TrainerId == trainerId))
            .OrderByDescending(x => x.IssuedAt).Take(8)
            .Select(x => new RecentActivityItem("Certificat", x.TrainingTitleSnapshot, x.IssuedAt))
            .ToListAsync(token);
        return enrollments.Concat(attempts).Concat(completions).Concat(certificates)
            .OrderByDescending(x => x.OccurredAt).Take(12).ToArray();
    }

    private async Task<IReadOnlyCollection<CategoryDistributionItem>> CategoryDistribution(
        CancellationToken token)
    {
        var rows = await db.Categories.AsNoTracking()
            .Select(x => new { x.Name, Trainings = x.Trainings.Count })
            .Where(x => x.Trainings > 0)
            .OrderByDescending(x => x.Trainings)
            .ToListAsync(token);
        return rows.Select(x => new CategoryDistributionItem(x.Name, x.Trainings)).ToArray();
    }

    private async Task<IReadOnlyCollection<RecentActivityItem>> RecentForLearner(string learnerId,
        CancellationToken token) => await db.AssessmentAttempts.AsNoTracking()
        .Where(x => x.Enrollment.LearnerId == learnerId)
        .OrderByDescending(x => x.StartedAt).Take(10)
        .Select(x => new RecentActivityItem("Quiz", x.Assessment.Title, x.SubmittedAt ?? x.StartedAt))
        .ToListAsync(token);
}
