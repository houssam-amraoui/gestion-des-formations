using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Pedagogy;

public sealed record CurriculumLesson(
    int Id, string Title, string Slug, string? Summary, int Order,
    int EstimatedDurationMinutes, bool IsPreview);

public sealed record CurriculumModule(
    int Id, string Title, string Slug, string? Description, int Order,
    IReadOnlyCollection<CurriculumLesson> Lessons);

public sealed record PublicCurriculum(IReadOnlyCollection<CurriculumModule> Modules);

public sealed record PublicLessonContent(
    int Id, string? Title, LessonContentType ContentType, string? TextContent,
    string? ExternalUrl, string? Description, int Order);

public sealed record LessonNavigation(string TrainingSlug, string ModuleSlug, string LessonSlug, string Title);

public sealed record PublicLessonPage(
    int Id, string TrainingTitle, string TrainingSlug, string ModuleTitle, string ModuleSlug,
    string Title, string LessonSlug, string? Summary, int EstimatedDurationMinutes,
    bool IsPreview, bool ContentAccessible, IReadOnlyCollection<PublicLessonContent> Contents,
    LessonNavigation? Previous, LessonNavigation? Next);

public sealed record TrainerTrainingItem(int Id, string Title, string Slug, TrainingStatus Status, int ModuleCount);
public sealed record TrainerLessonItem(int Id, string Title, int Order, bool IsPublished, int ContentCount);
public sealed record TrainerModuleItem(int Id, string Title, int Order, bool IsPublished, IReadOnlyCollection<TrainerLessonItem> Lessons);
public sealed record TrainerTrainingDetails(int Id, string Title, TrainingStatus Status, IReadOnlyCollection<TrainerModuleItem> Modules);

public interface IPedagogyReadService
{
    Task<PublicCurriculum> GetPublicCurriculumAsync(int trainingId, CancellationToken cancellationToken = default);
    Task<PublicLessonPage?> GetPublicLessonAsync(string trainingSlug, string moduleSlug, string lessonSlug, CancellationToken cancellationToken = default);
    Task<PublicLessonPage?> GetEnrolledLessonAsync(string trainingSlug, string moduleSlug,
        string lessonSlug, string learnerId, CancellationToken cancellationToken = default);
    Task<PublicLessonPage?> GetEnrolledLessonByIdAsync(int lessonId, string learnerId,
        CancellationToken cancellationToken = default);
    Task<PublicLessonPage?> GetAdminPreviewAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TrainerTrainingItem>> GetTrainerTrainingsAsync(string trainerId, CancellationToken cancellationToken = default);
    Task<TrainerTrainingDetails?> GetTrainerTrainingAsync(int trainingId, string trainerId, CancellationToken cancellationToken = default);
}

public interface IExternalMediaUrlService
{
    bool IsValidExternalUrl(string? url, LessonContentType contentType);
    string? GetSafeEmbedUrl(string? url);
}
