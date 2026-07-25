using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Lessons;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class LessonService(ApplicationDbContext dbContext) : ILessonService
{
    public async Task<LessonCollectionModel?> GetByModuleAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        var module = await dbContext.TrainingModules.AsNoTracking().Where(item => item.Id == moduleId)
            .Select(item => new { item.Id, item.TrainingId, item.Title, item.IsPublished, TrainingTitle = item.Training.Title })
            .SingleOrDefaultAsync(cancellationToken);
        if (module is null) return null;
        var items = await dbContext.Lessons.AsNoTracking().Where(item => item.TrainingModuleId == moduleId)
            .OrderBy(item => item.IsArchived).ThenBy(item => item.Order)
            .Select(item => new LessonListItem(item.Id, item.TrainingModuleId, item.Title, item.Slug,
                item.Order, item.EstimatedDurationMinutes, item.IsPreview, item.IsPublished,
                item.IsArchived, item.Contents.Count))
            .ToListAsync(cancellationToken);
        return new(module.Id, module.TrainingId, module.Title, module.TrainingTitle, module.IsPublished, items);
    }

    public Task<LessonDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Lessons.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new LessonDetailsModel(item.Id, item.TrainingModuleId,
                item.TrainingModule.TrainingId, item.TrainingModule.Title, item.TrainingModule.Training.Title,
                item.Title, item.Slug, item.Summary, item.Order, item.EstimatedDurationMinutes,
                item.IsPreview, item.IsPublished, item.IsArchived, item.Contents.Count,
                item.CreatedAt, item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult<int>> CreateAsync(LessonCreateModel model, CancellationToken cancellationToken = default)
    {
        var module = await dbContext.TrainingModules.FindAsync([model.TrainingModuleId], cancellationToken);
        if (module is null) return ServiceResult<int>.Failure("Module introuvable.");
        var validation = Validate(model.Title, model.EstimatedDurationMinutes);
        if (validation is not null) return ServiceResult<int>.Failure(validation);
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        if (await SlugExistsAsync(model.TrainingModuleId, slug, null, cancellationToken))
            return ServiceResult<int>.Failure("Ce slug est déjà utilisé dans ce module.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var order = await OrderingService.PrepareLessonInsertAsync(dbContext, model.TrainingModuleId, model.Order, cancellationToken);
        var entity = new Lesson
        {
            TrainingModuleId = model.TrainingModuleId, Title = model.Title.Trim(), Slug = slug,
            Summary = TrimOrNull(model.Summary), Order = order,
            EstimatedDurationMinutes = model.EstimatedDurationMinutes,
            IsPreview = model.IsPreview, CreatedAt = DateTime.UtcNow
        };
        dbContext.Lessons.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult<int>.Success(entity.Id);
    }

    public async Task<ServiceResult> UpdateAsync(LessonEditModel model, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Lessons.FindAsync([model.Id], cancellationToken);
        if (entity is null) return ServiceResult.Failure("Leçon introuvable.");
        if (entity.TrainingModuleId != model.TrainingModuleId) return ServiceResult.Failure("Le module parent est invalide.");
        if (entity.IsArchived) return ServiceResult.Failure("Une leçon archivée ne peut pas être modifiée.");
        var validation = Validate(model.Title, model.EstimatedDurationMinutes);
        if (validation is not null) return ServiceResult.Failure(validation);
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        if (await SlugExistsAsync(entity.TrainingModuleId, slug, entity.Id, cancellationToken))
            return ServiceResult.Failure("Ce slug est déjà utilisé dans ce module.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        entity.Title = model.Title.Trim();
        entity.Slug = slug;
        entity.Summary = TrimOrNull(model.Summary);
        entity.EstimatedDurationMinutes = model.EstimatedDurationMinutes;
        entity.IsPreview = model.IsPreview;
        entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.RepositionLessonAsync(dbContext, entity, model.Order, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Lessons
            .Include(item => item.Contents)
            .Include(item => item.TrainingModule).ThenInclude(module => module.Training)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return ServiceResult.Failure("Leçon introuvable.");
        try { entity.Publish(DateTime.UtcNow); }
        catch (InvalidOperationException exception) { return ServiceResult.Failure(exception.Message); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default) =>
        SetPublishedAsync(id, false, cancellationToken);

    public async Task<ServiceResult> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Lessons.FindAsync([id], cancellationToken);
        if (entity is null) return ServiceResult.Failure("Leçon introuvable.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        entity.IsArchived = true;
        entity.IsPublished = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.NormalizeLessonsAsync(dbContext, entity.TrainingModuleId, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Lessons.Include(item => item.Contents)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return ServiceResult.Failure("Leçon introuvable.");
        if (entity.Contents.Count != 0)
            return ServiceResult.Failure("Cette leçon contient des blocs. Archivez-la au lieu de la supprimer.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var parentId = entity.TrainingModuleId;
        dbContext.Lessons.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await OrderingService.NormalizeLessonsAsync(dbContext, parentId, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default) => MoveAsync(id, -1, cancellationToken);
    public Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default) => MoveAsync(id, 1, cancellationToken);

    private async Task<ServiceResult> MoveAsync(int id, int direction, CancellationToken token)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(token) : null;
        var succeeded = await OrderingService.MoveLessonAsync(dbContext, id, direction, token);
        if (!succeeded) return ServiceResult.Failure("Leçon introuvable ou archivée.");
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> SetPublishedAsync(int id, bool value, CancellationToken token)
    {
        var entity = await dbContext.Lessons.FindAsync([id], token);
        if (entity is null) return ServiceResult.Failure("Leçon introuvable.");
        entity.IsPublished = value;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    private Task<bool> SlugExistsAsync(int moduleId, string slug, int? excludedId, CancellationToken token) =>
        dbContext.Lessons.AnyAsync(item =>
            item.TrainingModuleId == moduleId && item.Id != excludedId && item.Slug == slug, token);

    private static string? Validate(string title, int duration) =>
        string.IsNullOrWhiteSpace(title) ? "Le titre est obligatoire." :
        duration < 0 ? "La durée ne peut pas être négative." : null;
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
