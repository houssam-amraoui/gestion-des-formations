using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Modules;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class TrainingModuleService(ApplicationDbContext dbContext) : ITrainingModuleService
{
    public async Task<TrainingModuleCollectionModel?> GetByTrainingAsync(int trainingId, CancellationToken cancellationToken = default)
    {
        var training = await dbContext.Trainings.AsNoTracking()
            .Where(item => item.Id == trainingId)
            .Select(item => new { item.Id, item.Title, item.Status }).SingleOrDefaultAsync(cancellationToken);
        if (training is null) return null;
        var items = await dbContext.TrainingModules.AsNoTracking().Where(item => item.TrainingId == trainingId)
            .OrderBy(item => item.IsArchived).ThenBy(item => item.Order)
            .Select(item => new TrainingModuleListItem(item.Id, item.TrainingId, item.Title, item.Slug,
                item.Order, item.IsPublished, item.IsArchived, item.Lessons.Count))
            .ToListAsync(cancellationToken);
        return new(training.Id, training.Title, training.Status, items);
    }

    public Task<TrainingModuleDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.TrainingModules.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new TrainingModuleDetailsModel(item.Id, item.TrainingId, item.Training.Title,
                item.Training.Status, item.Title, item.Slug, item.Description, item.Order,
                item.IsPublished, item.IsArchived, item.Lessons.Count, item.CreatedAt, item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult<int>> CreateAsync(TrainingModuleCreateModel model, CancellationToken cancellationToken = default)
    {
        var training = await dbContext.Trainings.FindAsync([model.TrainingId], cancellationToken);
        if (training is null) return ServiceResult<int>.Failure("Formation introuvable.");
        if (string.IsNullOrWhiteSpace(model.Title)) return ServiceResult<int>.Failure("Le titre est obligatoire.");
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        if (await SlugExistsAsync(model.TrainingId, slug, null, cancellationToken))
            return ServiceResult<int>.Failure("Ce slug est déjà utilisé dans cette formation.");

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var order = await OrderingService.PrepareModuleInsertAsync(dbContext, model.TrainingId, model.Order, cancellationToken);
        var entity = new TrainingModule
        {
            TrainingId = model.TrainingId, Title = model.Title.Trim(), Slug = slug,
            Description = TrimOrNull(model.Description), Order = order, CreatedAt = DateTime.UtcNow
        };
        dbContext.TrainingModules.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult<int>.Success(entity.Id);
    }

    public async Task<ServiceResult> UpdateAsync(TrainingModuleEditModel model, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.TrainingModules.FindAsync([model.Id], cancellationToken);
        if (entity is null) return ServiceResult.Failure("Module introuvable.");
        if (entity.TrainingId != model.TrainingId) return ServiceResult.Failure("La formation parente est invalide.");
        if (entity.IsArchived) return ServiceResult.Failure("Un module archivé ne peut pas être modifié.");
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        if (await SlugExistsAsync(entity.TrainingId, slug, entity.Id, cancellationToken))
            return ServiceResult.Failure("Ce slug est déjà utilisé dans cette formation.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        entity.Title = model.Title.Trim();
        entity.Slug = slug;
        entity.Description = TrimOrNull(model.Description);
        entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.RepositionModuleAsync(dbContext, entity, model.Order, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.TrainingModules.Include(item => item.Training)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return ServiceResult.Failure("Module introuvable.");
        try { entity.Publish(DateTime.UtcNow); }
        catch (InvalidOperationException exception) { return ServiceResult.Failure(exception.Message); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default) =>
        SetPublishedAsync(id, false, cancellationToken);

    public async Task<ServiceResult> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.TrainingModules.FindAsync([id], cancellationToken);
        if (entity is null) return ServiceResult.Failure("Module introuvable.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        entity.IsArchived = true;
        entity.IsPublished = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.NormalizeModulesAsync(dbContext, entity.TrainingId, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.TrainingModules.Include(item => item.Lessons)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return ServiceResult.Failure("Module introuvable.");
        if (entity.Lessons.Count != 0)
            return ServiceResult.Failure("Ce module contient des leçons. Archivez-le au lieu de le supprimer.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var parentId = entity.TrainingId;
        dbContext.TrainingModules.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await OrderingService.NormalizeModulesAsync(dbContext, parentId, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default) => MoveAsync(id, -1, cancellationToken);
    public Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default) => MoveAsync(id, 1, cancellationToken);

    private async Task<ServiceResult> MoveAsync(int id, int direction, CancellationToken token)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(token) : null;
        var succeeded = await OrderingService.MoveModuleAsync(dbContext, id, direction, token);
        if (!succeeded) return ServiceResult.Failure("Module introuvable ou archivé.");
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> SetPublishedAsync(int id, bool value, CancellationToken token)
    {
        var entity = await dbContext.TrainingModules.FindAsync([id], token);
        if (entity is null) return ServiceResult.Failure("Module introuvable.");
        entity.IsPublished = value;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    private Task<bool> SlugExistsAsync(int trainingId, string slug, int? excludedId, CancellationToken token) =>
        dbContext.TrainingModules.AnyAsync(item =>
            item.TrainingId == trainingId && item.Id != excludedId && item.Slug == slug, token);

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
