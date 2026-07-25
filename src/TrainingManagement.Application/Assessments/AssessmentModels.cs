using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Assessments;

public sealed record AssessmentListItem(int Id, int LessonId, string Title, string Slug,
    AssessmentType AssessmentType, int Order, int QuestionCount, decimal TotalPoints,
    decimal PassingScore, int? TimeLimitMinutes, int? MaximumAttempts, bool IsPublished, bool IsArchived);

public sealed record AssessmentDetailsModel(int Id, int LessonId, int ModuleId, int TrainingId,
    string LessonTitle, string ModuleTitle, string TrainingTitle, string Title, string Slug,
    string? Description, AssessmentType AssessmentType, int Order, decimal PassingScore,
    int? MaximumAttempts, int? TimeLimitMinutes, bool ShuffleQuestions, bool ShowCorrectAnswers,
    bool IsPublished, bool IsArchived, int QuestionCount, decimal TotalPoints,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record AssessmentCreateModel(int LessonId, string Title, string? Slug,
    string? Description, AssessmentType AssessmentType, int? Order, decimal PassingScore,
    int? MaximumAttempts, int? TimeLimitMinutes, bool ShuffleQuestions, bool ShowCorrectAnswers);

public sealed record AssessmentEditModel(int Id, int LessonId, string Title, string? Slug,
    string? Description, AssessmentType AssessmentType, int Order, decimal PassingScore,
    int? MaximumAttempts, int? TimeLimitMinutes, bool ShuffleQuestions, bool ShowCorrectAnswers);

public sealed record AssessmentCollectionModel(int LessonId, int ModuleId, int TrainingId,
    string LessonTitle, string ModuleTitle, string TrainingTitle,
    IReadOnlyCollection<AssessmentListItem> Items);

public sealed record AssessmentQuestionView(int Id, QuestionType QuestionType, string Statement,
    string? Explanation, int Order, decimal Points, string? ExpectedAnswer,
    bool IsPublished, IReadOnlyCollection<AnswerOptionView> Options);
public sealed record AnswerOptionView(int Id, string Text, int Order, bool IsCorrect);
public sealed record AssessmentPreviewModel(AssessmentDetailsModel Assessment,
    IReadOnlyCollection<AssessmentQuestionView> Questions, bool RevealAnswers);

public sealed record PublicAssessmentCard(int Id, string Title, string Slug, AssessmentType AssessmentType,
    string? Description, int QuestionCount, int? TimeLimitMinutes, decimal PassingScore);
public sealed record PublicAnswerOptionView(int Id, string Text, int Order);
public sealed record PublicQuestionView(int Id, QuestionType QuestionType, string Statement,
    int Order, decimal Points, IReadOnlyCollection<PublicAnswerOptionView> Options);
public sealed record PublicAssessmentPage(string TrainingTitle, string TrainingSlug, string ModuleTitle,
    string ModuleSlug, string LessonTitle, string LessonSlug, string Title, string Slug,
    string? Description, AssessmentType AssessmentType, decimal PassingScore, int? MaximumAttempts,
    int? TimeLimitMinutes, IReadOnlyCollection<PublicQuestionView> Questions);

public interface IAssessmentService
{
    Task<AssessmentCollectionModel?> GetByLessonAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<AssessmentDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(AssessmentCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(AssessmentEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ArchiveAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default);
    Task<AssessmentPreviewModel?> GetAdminPreviewAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PublicAssessmentCard>> GetPublicByLessonAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PublicAssessmentCard>> GetAccessibleByLessonAsync(int lessonId, string learnerId,
        CancellationToken cancellationToken = default);
    Task<PublicAssessmentPage?> GetPublicAsync(string trainingSlug, string moduleSlug, string lessonSlug,
        string assessmentSlug, CancellationToken cancellationToken = default);
    Task<AssessmentPreviewModel?> GetTrainerPreviewAsync(int id, string trainerId, CancellationToken cancellationToken = default);
}
