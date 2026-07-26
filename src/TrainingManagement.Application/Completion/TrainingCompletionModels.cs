using TrainingManagement.Application.Common;

namespace TrainingManagement.Application.Completion;

public sealed record TrainingCompletionRequirement(string Code, string Label, bool IsMet, string Details);
public sealed record TrainingCompletionEvaluation(int EnrollmentId, bool IsEligible,
    decimal? AverageScore, IReadOnlyCollection<TrainingCompletionRequirement> Requirements);
public sealed record TrainingCompletionResult(int EnrollmentId, bool WasAlreadyCompleted,
    bool Completed, DateTime? CompletedAt, TrainingCompletionEvaluation Evaluation);

public interface ITrainingCompletionService
{
    Task<TrainingCompletionEvaluation?> EvaluateAsync(int enrollmentId, CancellationToken cancellationToken = default);
    Task<ServiceResult<TrainingCompletionResult>> FinalizeAsync(int enrollmentId,
        CancellationToken cancellationToken = default);
}
