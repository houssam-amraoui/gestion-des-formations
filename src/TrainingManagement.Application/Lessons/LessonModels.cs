using TrainingManagement.Application.Common;

namespace TrainingManagement.Application.Lessons;

public sealed record LessonListItem(
    int Id, int TrainingModuleId, string Title, string Slug, int Order,
    int EstimatedDurationMinutes, bool IsPreview, bool IsPublished,
    bool IsArchived, int ContentCount);

public sealed record LessonDetailsModel(
    int Id, int TrainingModuleId, int TrainingId, string ModuleTitle, string TrainingTitle,
    string Title, string Slug, string? Summary, int Order, int EstimatedDurationMinutes,
    bool IsPreview, bool IsPublished, bool IsArchived, int ContentCount,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record LessonCreateModel(
    int TrainingModuleId, string Title, string? Slug, string? Summary,
    int? Order, int EstimatedDurationMinutes, bool IsPreview);

public sealed record LessonEditModel(
    int Id, int TrainingModuleId, string Title, string? Slug, string? Summary,
    int Order, int EstimatedDurationMinutes, bool IsPreview);

public sealed record LessonCollectionModel(
    int TrainingModuleId, int TrainingId, string ModuleTitle, string TrainingTitle,
    bool ModulePublished, IReadOnlyCollection<LessonListItem> Items);

public interface ILessonService
{
    Task<LessonCollectionModel?> GetByModuleAsync(int moduleId, CancellationToken cancellationToken = default);
    Task<LessonDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(LessonCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(LessonEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ArchiveAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default);
}
