using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.LessonContents;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class LessonContentService(
    ApplicationDbContext dbContext,
    IExternalMediaUrlService urlService) : ILessonContentService
{
    public async Task<LessonContentCollectionModel?> GetByLessonAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var lesson = await dbContext.Lessons.AsNoTracking().Where(item => item.Id == lessonId)
            .Select(item => new
            {
                item.Id, item.TrainingModuleId, TrainingId = item.TrainingModule.TrainingId,
                item.Title, ModuleTitle = item.TrainingModule.Title, TrainingTitle = item.TrainingModule.Training.Title
            }).SingleOrDefaultAsync(cancellationToken);
        if (lesson is null) return null;
        var items = await dbContext.LessonContents.AsNoTracking().Where(item => item.LessonId == lessonId)
            .OrderBy(item => item.Order)
            .Select(item => new LessonContentListItem(item.Id, item.LessonId, item.Title,
                item.ContentType, item.ContentType == LessonContentType.Text
                    ? item.TextContent!.Substring(0, Math.Min(item.TextContent.Length, 100))
                    : item.ExternalUrl, item.Order, item.IsPublished))
            .ToListAsync(cancellationToken);
        return new(lesson.Id, lesson.TrainingModuleId, lesson.TrainingId,
            lesson.Title, lesson.ModuleTitle, lesson.TrainingTitle, items);
    }

    public Task<LessonContentDetailsModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.LessonContents.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new LessonContentDetailsModel(item.Id, item.LessonId,
                item.Lesson.TrainingModuleId, item.Lesson.TrainingModule.TrainingId,
                item.Lesson.Title, item.Lesson.TrainingModule.Title, item.Lesson.TrainingModule.Training.Title,
                item.Title, item.ContentType, item.TextContent, item.ExternalUrl, item.Description,
                item.Order, item.IsPublished, item.CreatedAt, item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult<int>> CreateAsync(LessonContentCreateModel model, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Lessons.AnyAsync(item => item.Id == model.LessonId, cancellationToken))
            return ServiceResult<int>.Failure("Leçon introuvable.");
        var validation = Validate(model.ContentType, model.TextContent, model.ExternalUrl);
        if (validation is not null) return ServiceResult<int>.Failure(validation);
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var order = await OrderingService.PrepareContentInsertAsync(dbContext, model.LessonId, model.Order, cancellationToken);
        var entity = new LessonContent
        {
            LessonId = model.LessonId, Title = TrimOrNull(model.Title), ContentType = model.ContentType,
            TextContent = model.ContentType == LessonContentType.Text ? TrimOrNull(model.TextContent) : null,
            ExternalUrl = model.ContentType == LessonContentType.Text ? null : TrimOrNull(model.ExternalUrl),
            Description = TrimOrNull(model.Description), Order = order, CreatedAt = DateTime.UtcNow
        };
        dbContext.LessonContents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult<int>.Success(entity.Id);
    }

    public async Task<ServiceResult> UpdateAsync(LessonContentEditModel model, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.LessonContents.FindAsync([model.Id], cancellationToken);
        if (entity is null) return ServiceResult.Failure("Contenu introuvable.");
        if (entity.LessonId != model.LessonId) return ServiceResult.Failure("La leçon parente est invalide.");
        var validation = Validate(model.ContentType, model.TextContent, model.ExternalUrl);
        if (validation is not null) return ServiceResult.Failure(validation);
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        entity.Title = TrimOrNull(model.Title);
        entity.ContentType = model.ContentType;
        entity.TextContent = model.ContentType == LessonContentType.Text ? TrimOrNull(model.TextContent) : null;
        entity.ExternalUrl = model.ContentType == LessonContentType.Text ? null : TrimOrNull(model.ExternalUrl);
        entity.Description = TrimOrNull(model.Description);
        entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.RepositionContentAsync(dbContext, entity, model.Order, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default) =>
        SetPublishedAsync(id, true, cancellationToken);
    public Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default) =>
        SetPublishedAsync(id, false, cancellationToken);

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.LessonContents.FindAsync([id], cancellationToken);
        if (entity is null) return ServiceResult.Failure("Contenu introuvable.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var parentId = entity.LessonId;
        dbContext.LessonContents.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await OrderingService.NormalizeContentsAsync(dbContext, parentId, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public Task<ServiceResult> MoveUpAsync(int id, CancellationToken cancellationToken = default) => MoveAsync(id, -1, cancellationToken);
    public Task<ServiceResult> MoveDownAsync(int id, CancellationToken cancellationToken = default) => MoveAsync(id, 1, cancellationToken);

    private async Task<ServiceResult> SetPublishedAsync(int id, bool value, CancellationToken token)
    {
        var entity = await dbContext.LessonContents.FindAsync([id], token);
        if (entity is null) return ServiceResult.Failure("Contenu introuvable.");
        if (value)
        {
            var validation = Validate(entity.ContentType, entity.TextContent, entity.ExternalUrl);
            if (validation is not null) return ServiceResult.Failure(validation);
        }
        entity.IsPublished = value;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> MoveAsync(int id, int direction, CancellationToken token)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(token) : null;
        var succeeded = await OrderingService.MoveContentAsync(dbContext, id, direction, token);
        if (!succeeded) return ServiceResult.Failure("Contenu introuvable.");
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }

    private string? Validate(LessonContentType type, string? text, string? url)
    {
        if (type == LessonContentType.Text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Le contenu texte est obligatoire.";
            if (!string.IsNullOrWhiteSpace(url)) return "Une URL ne doit pas être fournie pour un contenu texte.";
            return null;
        }
        if (string.IsNullOrWhiteSpace(url)) return "L’URL externe est obligatoire.";
        if (!string.IsNullOrWhiteSpace(text)) return "Le texte doit être vide pour ce type de contenu.";
        return urlService.IsValidExternalUrl(url, type) ? null : "L’URL externe est invalide ou utilise un schéma interdit.";
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
