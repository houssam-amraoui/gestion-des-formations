using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Modules;

public sealed record TrainingModuleListItem(
    int Id, int TrainingId, string Title, string Slug, int Order,
    bool IsPublished, bool IsArchived, int LessonCount);

public sealed record TrainingModuleDetailsModel(
    int Id, int TrainingId, string TrainingTitle, TrainingStatus TrainingStatus,
    string Title, string Slug, string? Description, int Order,
    bool IsPublished, bool IsArchived, int LessonCount,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record TrainingModuleCreateModel(
    int TrainingId, string Title, string? Slug, string? Description, int? Order);

public sealed record TrainingModuleEditModel(
    int Id, int TrainingId, string Title, string? Slug, string? Description, int Order);

public sealed record TrainingModuleCollectionModel(
    int TrainingId, string TrainingTitle, TrainingStatus TrainingStatus,
    IReadOnlyCollection<TrainingModuleListItem> Items);

public interface ITrainingModuleService
{
    Task<TrainingModuleCollectionModel?> GetByTrainingAsync(int trainingId, CancellationToken cancellationToken = default);
    Task<TrainingModuleDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(TrainingModuleCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(TrainingModuleEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ArchiveAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default);
}
