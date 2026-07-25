using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.LessonContents;

public sealed record LessonContentListItem(
    int Id, int LessonId, string? Title, LessonContentType ContentType,
    string? Preview, int Order, bool IsPublished);

public sealed record LessonContentDetailsModel(
    int Id, int LessonId, int TrainingModuleId, int TrainingId,
    string LessonTitle, string ModuleTitle, string TrainingTitle,
    string? Title, LessonContentType ContentType, string? TextContent,
    string? ExternalUrl, string? Description, int Order, bool IsPublished,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record LessonContentCreateModel(
    int LessonId, string? Title, LessonContentType ContentType,
    string? TextContent, string? ExternalUrl, string? Description, int? Order);

public sealed record LessonContentEditModel(
    int Id, int LessonId, string? Title, LessonContentType ContentType,
    string? TextContent, string? ExternalUrl, string? Description, int Order);

public sealed record LessonContentCollectionModel(
    int LessonId, int TrainingModuleId, int TrainingId,
    string LessonTitle, string ModuleTitle, string TrainingTitle,
    IReadOnlyCollection<LessonContentListItem> Items);

public interface ILessonContentService
{
    Task<LessonContentCollectionModel?> GetByLessonAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<LessonContentDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(LessonContentCreateModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(LessonContentEditModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default);
}
