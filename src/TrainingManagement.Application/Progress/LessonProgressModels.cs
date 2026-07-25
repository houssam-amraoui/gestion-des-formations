using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Progress;

public sealed record LessonProgressModel(int Id, int EnrollmentId, int LessonId,
    LessonProgressStatus Status, DateTime? FirstAccessedAt, DateTime? LastAccessedAt,
    DateTime? CompletedAt, int TimeSpentSeconds);
public sealed record TrainingProgressSummary(int EnrollmentId, int TrainingId,
    int CompletedLessons, int TotalLessons, decimal ProgressPercentage, EnrollmentStatus EnrollmentStatus);

public interface ILessonProgressService
{
    Task<ServiceResult<LessonProgressModel>> RecordAccessAsync(int lessonId, string learnerId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<TrainingProgressSummary>> CompleteAsync(int lessonId, string learnerId,
        CancellationToken cancellationToken = default);
    Task<TrainingProgressSummary?> GetSummaryAsync(int enrollmentId, string learnerId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<TrainingProgressSummary>> RecalculateAsync(int enrollmentId,
        CancellationToken cancellationToken = default);
}
