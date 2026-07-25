using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class PedagogyReadService(ApplicationDbContext dbContext) : IPedagogyReadService
{
    public async Task<PublicCurriculum> GetPublicCurriculumAsync(int trainingId, CancellationToken cancellationToken = default)
    {
        var published = await dbContext.Trainings.AsNoTracking()
            .AnyAsync(item => item.Id == trainingId && item.Status == TrainingStatus.Published, cancellationToken);
        if (!published) return new([]);
        var modules = await dbContext.TrainingModules.AsNoTracking()
            .Where(module => module.TrainingId == trainingId && module.IsPublished && !module.IsArchived)
            .OrderBy(module => module.Order)
            .Select(module => new CurriculumModule(module.Id, module.Title, module.Slug, module.Description,
                module.Order, module.Lessons.Where(lesson => lesson.IsPublished && !lesson.IsArchived)
                    .OrderBy(lesson => lesson.Order)
                    .Select(lesson => new CurriculumLesson(lesson.Id, lesson.Title, lesson.Slug,
                        lesson.Summary, lesson.Order, lesson.EstimatedDurationMinutes, lesson.IsPreview))
                    .ToList()))
            .ToListAsync(cancellationToken);
        return new(modules);
    }

    public async Task<PublicLessonPage?> GetPublicLessonAsync(
        string trainingSlug, string moduleSlug, string lessonSlug, CancellationToken cancellationToken = default)
    {
        var lesson = await BaseLessonQuery()
            .Where(item => item.TrainingModule.Training.Slug == trainingSlug
                && item.TrainingModule.Slug == moduleSlug && item.Slug == lessonSlug
                && item.TrainingModule.Training.Status == TrainingStatus.Published
                && item.TrainingModule.IsPublished && !item.TrainingModule.IsArchived
                && item.IsPublished && !item.IsArchived)
            .SingleOrDefaultAsync(cancellationToken);
        if (lesson is null) return null;
        return await MapPublicLessonAsync(lesson.Id, lesson.IsPreview, publishedOnly: true, cancellationToken);
    }

    public async Task<PublicLessonPage?> GetAdminPreviewAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Lessons.AnyAsync(item => item.Id == lessonId, cancellationToken);
        return exists ? await MapPublicLessonAsync(lessonId, true, publishedOnly: false, cancellationToken) : null;
    }

    public async Task<IReadOnlyCollection<TrainerTrainingItem>> GetTrainerTrainingsAsync(
        string trainerId, CancellationToken cancellationToken = default) =>
        await dbContext.Trainings.AsNoTracking().Where(item => item.TrainerId == trainerId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new TrainerTrainingItem(item.Id, item.Title, item.Slug, item.Status, item.Modules.Count))
            .ToListAsync(cancellationToken);

    public async Task<TrainerTrainingDetails?> GetTrainerTrainingAsync(
        int trainingId, string trainerId, CancellationToken cancellationToken = default) =>
        await dbContext.Trainings.AsNoTracking()
            .Where(training => training.Id == trainingId && training.TrainerId == trainerId)
            .Select(training => new TrainerTrainingDetails(training.Id, training.Title, training.Status,
                training.Modules.OrderBy(module => module.Order)
                    .Select(module => new TrainerModuleItem(module.Id, module.Title, module.Order,
                        module.IsPublished, module.Lessons.OrderBy(lesson => lesson.Order)
                            .Select(lesson => new TrainerLessonItem(lesson.Id, lesson.Title, lesson.Order,
                                lesson.IsPublished, lesson.Contents.Count)).ToList())).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    private IQueryable<TrainingManagement.Domain.Entities.Lesson> BaseLessonQuery() => dbContext.Lessons.AsNoTracking();

    private async Task<PublicLessonPage?> MapPublicLessonAsync(
        int lessonId, bool accessible, bool publishedOnly, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons.AsNoTracking().Where(item => item.Id == lessonId)
            .Select(item => new
            {
                item.Id, item.Title, LessonSlug = item.Slug, item.Summary,
                item.EstimatedDurationMinutes, item.IsPreview, item.Order, item.TrainingModuleId,
                ModuleTitle = item.TrainingModule.Title, ModuleSlug = item.TrainingModule.Slug,
                TrainingTitle = item.TrainingModule.Training.Title,
                TrainingSlug = item.TrainingModule.Training.Slug
            }).SingleOrDefaultAsync(cancellationToken);
        if (lesson is null) return null;

        var contents = accessible
            ? await dbContext.LessonContents.AsNoTracking()
                .Where(item => item.LessonId == lessonId && (!publishedOnly || item.IsPublished))
                .OrderBy(item => item.Order)
                .Select(item => new PublicLessonContent(item.Id, item.Title, item.ContentType,
                    item.TextContent, item.ExternalUrl, item.Description, item.Order))
                .ToListAsync(cancellationToken)
            : [];

        var siblings = await dbContext.Lessons.AsNoTracking()
            .Where(item => item.TrainingModuleId == lesson.TrainingModuleId
                && (!publishedOnly || item.IsPublished && !item.IsArchived))
            .OrderBy(item => item.Order)
            .Select(item => new { item.Id, item.Title, item.Slug, item.Order })
            .ToListAsync(cancellationToken);
        var index = siblings.FindIndex(item => item.Id == lessonId);
        LessonNavigation? previous = index > 0
            ? new(lesson.TrainingSlug, lesson.ModuleSlug, siblings[index - 1].Slug, siblings[index - 1].Title) : null;
        LessonNavigation? next = index >= 0 && index < siblings.Count - 1
            ? new(lesson.TrainingSlug, lesson.ModuleSlug, siblings[index + 1].Slug, siblings[index + 1].Title) : null;

        return new(lesson.Id, lesson.TrainingTitle, lesson.TrainingSlug, lesson.ModuleTitle,
            lesson.ModuleSlug, lesson.Title, lesson.LessonSlug, lesson.Summary,
            lesson.EstimatedDurationMinutes, lesson.IsPreview, accessible, contents, previous, next);
    }
}
