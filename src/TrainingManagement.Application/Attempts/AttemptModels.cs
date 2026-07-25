using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Attempts;

public sealed record AttemptStartModel(int AssessmentId, string LearnerId);
public sealed record AttemptOptionModel(int Id, string Text, int Order, bool Selected);
public sealed record AttemptQuestionModel(int AttemptQuestionId, int DisplayOrder,
    string Statement, QuestionType QuestionType, decimal Points,
    IReadOnlyCollection<AttemptOptionModel> Options, string? TextAnswer);
public sealed record AttemptDetailsModel(int Id, int EnrollmentId, int AssessmentId,
    string AssessmentTitle, int AttemptNumber, AttemptStatus Status, DateTime StartedAt,
    DateTime? ExpiresAt, int? RemainingSeconds, decimal MaximumScore,
    IReadOnlyCollection<AttemptQuestionModel> Questions);
public sealed record AttemptAnswerInputModel(int AttemptQuestionId,
    IReadOnlyCollection<int> SelectedOptionIds, string? TextAnswer);
public sealed record AttemptSubmissionModel(int AttemptId, string LearnerId,
    IReadOnlyCollection<AttemptAnswerInputModel> Answers);
public sealed record AttemptResultQuestionModel(string Statement, QuestionType QuestionType,
    decimal Points, decimal PointsAwarded, bool IsCorrect, string LearnerAnswer,
    string? CorrectAnswer, string? Explanation);
public sealed record AttemptResultModel(int Id, string AssessmentTitle, string TrainingTitle,
    int AttemptNumber, AttemptStatus Status, decimal Score, decimal MaximumScore,
    decimal PercentageScore, bool Passed, decimal PassingScore, int DurationSeconds,
    DateTime ResultAt, bool ShowCorrectAnswers, IReadOnlyCollection<AttemptResultQuestionModel> Questions);
public sealed record AttemptHistoryItem(int Id, int TrainingId, string TrainingTitle,
    string AssessmentTitle, int AttemptNumber, AttemptStatus Status, DateTime StartedAt,
    DateTime? SubmittedAt, decimal? PercentageScore, bool? Passed);
public sealed record AttemptHistoryFilter(int? TrainingId = null, bool? Passed = null, AttemptStatus? Status = null);
public sealed record ScoringAnswer(int AttemptQuestionId, QuestionType Type, decimal Points,
    string? CorrectOptionIds, string? ExpectedAnswer, IReadOnlyCollection<int> SelectedOptionIds,
    string? TextAnswer);
public sealed record ScoringQuestionResult(int AttemptQuestionId, bool IsCorrect, decimal PointsAwarded);
public sealed record ScoringResult(decimal Score, decimal MaximumScore, decimal PercentageScore,
    bool Passed, IReadOnlyCollection<ScoringQuestionResult> Questions);
public sealed record TrainerLearnerProgressItem(int EnrollmentId, int TrainingId, string TrainingTitle,
    string LearnerName, string LearnerEmail, EnrollmentStatus Status, decimal Progress,
    int CompletedLessons, int TotalLessons, int SubmittedAttempts, decimal? AverageScore);
public sealed record AdminAttemptListItem(int Id, string LearnerName, string TrainingTitle,
    string AssessmentTitle, int AttemptNumber, AttemptStatus Status, decimal? PercentageScore,
    bool? Passed, DateTime StartedAt, DateTime? SubmittedAt);

public interface IAttemptScoringService
{
    ScoringResult Score(IReadOnlyCollection<ScoringAnswer> answers, decimal passingScore);
}

public interface IAssessmentAttemptService
{
    Task<ServiceResult<int>> StartAsync(AttemptStartModel model, CancellationToken cancellationToken = default);
    Task<AttemptDetailsModel?> GetAttemptAsync(int id, string learnerId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveAnswersAsync(int attemptId, string learnerId,
        IReadOnlyCollection<AttemptAnswerInputModel> answers, CancellationToken cancellationToken = default);
    Task<ServiceResult<AttemptResultModel>> SubmitAsync(AttemptSubmissionModel model,
        CancellationToken cancellationToken = default);
    Task<AttemptResultModel?> GetResultAsync(int id, string learnerId, bool privileged = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttemptHistoryItem>> GetHistoryAsync(string learnerId,
        AttemptHistoryFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdminAttemptListItem>> GetAdminAttemptsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TrainerLearnerProgressItem>> GetTrainerProgressAsync(string trainerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdminAttemptListItem>> GetTrainerAttemptsAsync(string trainerId,
        CancellationToken cancellationToken = default);
    Task<AttemptResultModel?> GetTrainerResultAsync(int id, string trainerId,
        CancellationToken cancellationToken = default);
}
