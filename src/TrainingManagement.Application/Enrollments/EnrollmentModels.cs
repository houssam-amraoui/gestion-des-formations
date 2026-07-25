using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Enrollments;

public sealed record EnrollmentFilter(string? Search = null, int? TrainingId = null,
    EnrollmentStatus? Status = null, DateTime? From = null, DateTime? To = null,
    string Sort = "date_desc", int Page = 1, int PageSize = 15);
public sealed record EnrollmentListItem(int Id, string LearnerId, string LearnerName, string LearnerEmail,
    int TrainingId, string TrainingTitle, EnrollmentStatus Status, decimal ProgressPercentage,
    DateTime EnrolledAt, DateTime? LastAccessedAt);
public sealed record EnrollmentListResult(IReadOnlyCollection<EnrollmentListItem> Items,
    int Page, int PageSize, int TotalCount);
public sealed record EnrollmentDetailsModel(int Id, string LearnerId, string LearnerName, string LearnerEmail,
    int TrainingId, string TrainingTitle, EnrollmentStatus Status, decimal ProgressPercentage,
    DateTime EnrolledAt, DateTime? StartedAt, DateTime? CompletedAt, DateTime? CancelledAt,
    DateTime? LastAccessedAt, string? CreatedByAdminId);
public sealed record EnrollmentCreateModel(string LearnerId, int TrainingId, bool ActivateImmediately, string AdminId);
public sealed record EnrollmentOption(string Value, string Label);

public sealed record LearnerLessonModel(int Id, string Title, string Slug, int Order, int DurationMinutes,
    bool IsPreview, bool IsCompleted, LessonProgressStatus ProgressStatus,
    IReadOnlyCollection<LearnerAssessmentModel> Assessments);
public sealed record LearnerAssessmentModel(int Id, string Title, string Slug, AssessmentType Type,
    int? TimeLimitMinutes, int? MaximumAttempts, int AttemptsUsed, int? InProgressAttemptId);
public sealed record LearnerModuleModel(int Id, string Title, int Order, IReadOnlyCollection<LearnerLessonModel> Lessons);
public sealed record LearnerTrainingModel(int EnrollmentId, int TrainingId, string Title, string Slug,
    string? ThumbnailUrl, EnrollmentStatus Status, decimal ProgressPercentage, DateTime? LastAccessedAt,
    int CompletedLessons, int TotalLessons, IReadOnlyCollection<LearnerModuleModel> Modules);
public sealed record EnrollmentProgressModel(int EnrollmentId, decimal Percentage, int CompletedLessons,
    int TotalLessons, EnrollmentStatus Status);
public sealed record LearnerDashboardModel(int ActiveTrainings, int CompletedTrainings,
    decimal AverageProgress, IReadOnlyCollection<LearnerTrainingModel> RecentTrainings,
    IReadOnlyCollection<RecentLessonModel> RecentLessons, IReadOnlyCollection<RecentAttemptModel> RecentAttempts);
public sealed record RecentLessonModel(string TrainingTitle, string LessonTitle, DateTime? AccessedAt);
public sealed record RecentAttemptModel(string AssessmentTitle, decimal? PercentageScore, bool? Passed, DateTime? SubmittedAt);

public interface IEnrollmentService
{
    Task<EnrollmentListResult> GetAdminListAsync(EnrollmentFilter filter, CancellationToken cancellationToken = default);
    Task<EnrollmentDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EnrollmentOption>> GetLearnerOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EnrollmentOption>> GetTrainingOptionsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> EnrollFreeAsync(int trainingId, string learnerId, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateByAdminAsync(EnrollmentCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> ActivateAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> SuspendAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ReactivateAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CanAccessTrainingAsync(int trainingId, string learnerId, CancellationToken cancellationToken = default);
    Task<EnrollmentDetailsModel?> GetAuthorizedAsync(int trainingId, string learnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LearnerTrainingModel>> GetLearnerTrainingsAsync(string learnerId, CancellationToken cancellationToken = default);
    Task<LearnerTrainingModel?> GetLearnerTrainingAsync(int trainingId, string learnerId, CancellationToken cancellationToken = default);
    Task<LearnerDashboardModel> GetLearnerDashboardAsync(string learnerId, CancellationToken cancellationToken = default);
}
