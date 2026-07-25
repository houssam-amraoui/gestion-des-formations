using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Questions;

public sealed record QuestionListItem(int Id, int AssessmentId, QuestionType QuestionType,
    string Statement, int Order, decimal Points, bool IsPublished, int AnswerOptionCount);
public sealed record QuestionDetailsModel(int Id, int AssessmentId, int LessonId, string AssessmentTitle,
    QuestionType QuestionType, string Statement, string? Explanation, int Order, decimal Points,
    string? ExpectedAnswer, bool IsPublished, int AnswerOptionCount, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record QuestionCreateModel(int AssessmentId, QuestionType QuestionType, string Statement,
    string? Explanation, int? Order, decimal Points, string? ExpectedAnswer);
public sealed record QuestionEditModel(int Id, int AssessmentId, QuestionType QuestionType, string Statement,
    string? Explanation, int Order, decimal Points, string? ExpectedAnswer);
public sealed record QuestionCollectionModel(int AssessmentId, int LessonId, string AssessmentTitle,
    IReadOnlyCollection<QuestionListItem> Items, decimal TotalPoints);

public interface IQuestionService
{
    Task<QuestionCollectionModel?> GetByAssessmentAsync(int assessmentId, CancellationToken cancellationToken = default);
    Task<QuestionDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(QuestionCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(QuestionEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default);
}
