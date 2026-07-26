namespace TrainingManagement.Application.Analytics;

public enum AnalyticsPeriod { Last7Days, Last30Days, Last90Days, CurrentYear, Custom }
public sealed record AnalyticsFilterModel(AnalyticsPeriod Period = AnalyticsPeriod.Last30Days,
    DateTime? DateFrom = null, DateTime? DateTo = null);
public sealed record TimeSeriesDataPoint(DateTime Date, decimal Value);
public sealed record AssessmentPerformanceItem(int AssessmentId, string Title, int Attempts, decimal SuccessRate);
public sealed record LessonDropOffItem(int LessonId, string Title, int Started, int NotCompleted, decimal DropOffRate);
public sealed record RecentActivityItem(string Type, string Description, DateTime OccurredAt);
public sealed record CategoryDistributionItem(string CategoryName, int Trainings);
public sealed record LearnerActivityItem(string LearnerName, string TrainingTitle, DateTime? LastActivity);
public sealed record TrainingPerformanceItem(int TrainingId, string Title, int Enrollments,
    int Completed, decimal CompletionRate);
public sealed record AdminDashboardAnalytics(int TotalUsers, int Learners, int Trainers, int Trainings,
    int PublishedTrainings, int ActiveEnrollments, int CompletedEnrollments, int CertificatesIssued,
    decimal AverageProgress, decimal AssessmentSuccessRate,
    IReadOnlyCollection<TimeSeriesDataPoint> EnrollmentsSeries,
    IReadOnlyCollection<TimeSeriesDataPoint> AttemptsSeries,
    IReadOnlyCollection<TimeSeriesDataPoint> CertificatesSeries,
    IReadOnlyCollection<TrainingPerformanceItem> PopularTrainings,
    IReadOnlyCollection<AssessmentPerformanceItem> DifficultAssessments,
    IReadOnlyCollection<LessonDropOffItem> LessonDropOffs,
    IReadOnlyCollection<RecentActivityItem> RecentActivities,
    IReadOnlyCollection<CategoryDistributionItem> CategoryDistribution);
public sealed record TrainerDashboardAnalytics(int Learners, int ActiveEnrollments, decimal AverageProgress,
    int CompletedTrainings, int Certificates, decimal AssessmentSuccessRate,
    IReadOnlyCollection<AssessmentPerformanceItem> DifficultAssessments,
    IReadOnlyCollection<RecentActivityItem> RecentActivities,
    IReadOnlyCollection<LearnerActivityItem> RecentlyActiveLearners,
    IReadOnlyCollection<LearnerActivityItem> InactiveLearners);
public sealed record LearnerDashboardAnalytics(decimal GlobalProgress, int ActiveTrainings,
    int CompletedTrainings, int CompletedLessons, int AssessmentsTaken, decimal SuccessRate,
    decimal BestScore, int EstimatedLearningSeconds, int Certificates,
    IReadOnlyCollection<RecentActivityItem> RecentActivities);
public sealed record LearnerTrainingStatistics(int TrainingId, string TrainingTitle, decimal Progress,
    int CompletedLessons, int AssessmentsTaken, decimal? BestScore, decimal? AverageScore,
    int Attempts, bool HasCertificate, DateTime? LastActivity);
public sealed record TrainingAnalyticsModel(int TrainingId, string TrainingTitle, int Enrollments,
    int ActiveEnrollments, int CompletedEnrollments, decimal CompletionRate, decimal AverageProgress,
    int Lessons, decimal AssessmentSuccessRate, int Certificates,
    IReadOnlyCollection<AssessmentPerformanceItem> BestAssessments,
    IReadOnlyCollection<AssessmentPerformanceItem> DifficultAssessments,
    IReadOnlyCollection<LessonDropOffItem> LessonDropOffs,
    IReadOnlyCollection<RecentActivityItem> RecentActivities);

public interface IAnalyticsService
{
    Task<AdminDashboardAnalytics> GetAdminDashboardAsync(AnalyticsFilterModel filter, CancellationToken cancellationToken = default);
    Task<TrainingAnalyticsModel?> GetTrainingAsync(int trainingId, AnalyticsFilterModel filter, string? trainerId = null, CancellationToken cancellationToken = default);
    Task<TrainerDashboardAnalytics> GetTrainerDashboardAsync(string trainerId, AnalyticsFilterModel filter, CancellationToken cancellationToken = default);
    Task<LearnerDashboardAnalytics> GetLearnerDashboardAsync(string learnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LearnerTrainingStatistics>> GetLearnerStatisticsAsync(string learnerId, CancellationToken cancellationToken = default);
}
