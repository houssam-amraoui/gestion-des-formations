using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.AnswerOptions;

public sealed record AnswerOptionListItem(int Id, int QuestionId, string Text, int Order, bool IsCorrect);
public sealed record AnswerOptionCreateModel(int QuestionId, string Text, int? Order, bool IsCorrect);
public sealed record AnswerOptionEditModel(int Id, int QuestionId, string Text, int Order, bool IsCorrect);
public sealed record AnswerOptionCollectionModel(int QuestionId, int AssessmentId, string Statement,
    QuestionType QuestionType, bool QuestionPublished, IReadOnlyCollection<AnswerOptionListItem> Items);

public interface IAnswerOptionService
{
    Task<AnswerOptionCollectionModel?> GetByQuestionAsync(int questionId, CancellationToken cancellationToken = default);
    Task<AnswerOptionListItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(AnswerOptionCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(AnswerOptionEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ToggleCorrectAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateTrueFalseAsync(int questionId, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default);
}
