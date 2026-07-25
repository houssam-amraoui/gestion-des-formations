using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Questions;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class QuestionService(ApplicationDbContext db) : IQuestionService
{
    public async Task<QuestionCollectionModel?> GetByAssessmentAsync(int assessmentId, CancellationToken token = default)
    {
        var assessment = await db.Assessments.AsNoTracking().Where(x => x.Id == assessmentId)
            .Select(x => new { x.Id, x.LessonId, x.Title }).SingleOrDefaultAsync(token);
        if (assessment is null) return null;
        var items = await db.Questions.AsNoTracking().Where(x => x.AssessmentId == assessmentId)
            .OrderBy(x => x.Order).Select(x => new QuestionListItem(x.Id, x.AssessmentId, x.QuestionType,
                x.Statement, x.Order, x.Points, x.IsPublished, x.AnswerOptions.Count)).ToListAsync(token);
        return new(assessment.Id, assessment.LessonId, assessment.Title, items, items.Sum(x => x.Points));
    }

    public Task<QuestionDetailsModel?> GetByIdAsync(int id, CancellationToken token = default) =>
        db.Questions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new QuestionDetailsModel(x.Id, x.AssessmentId, x.Assessment.LessonId,
                x.Assessment.Title, x.QuestionType, x.Statement, x.Explanation, x.Order, x.Points,
                x.ExpectedAnswer, x.IsPublished, x.AnswerOptions.Count, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync(token);

    public async Task<ServiceResult<int>> CreateAsync(QuestionCreateModel model, CancellationToken token = default)
    {
        var assessment = await db.Assessments.FindAsync([model.AssessmentId], token);
        if (assessment is null) return ServiceResult<int>.Failure("Évaluation introuvable.");
        if (assessment.IsArchived) return ServiceResult<int>.Failure("L’évaluation est archivée.");
        var error = ValidateBase(model.QuestionType, model.Statement, model.Order, model.Points, model.ExpectedAnswer);
        if (error is not null) return ServiceResult<int>.Failure(error);
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var order = await OrderingService.PrepareQuestionInsertAsync(db, model.AssessmentId, model.Order, token);
        var entity = new Question { AssessmentId = model.AssessmentId, QuestionType = model.QuestionType,
            Statement = model.Statement.Trim(), Explanation = Trim(model.Explanation), Order = order,
            Points = model.Points, ExpectedAnswer = model.QuestionType == QuestionType.ShortAnswer ? Trim(model.ExpectedAnswer) : null,
            CreatedAt = DateTime.UtcNow };
        db.Questions.Add(entity); await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult<int>.Success(entity.Id);
    }

    public async Task<ServiceResult> UpdateAsync(QuestionEditModel model, CancellationToken token = default)
    {
        var entity = await db.Questions.Include(x => x.AnswerOptions).SingleOrDefaultAsync(x => x.Id == model.Id, token);
        if (entity is null) return ServiceResult.Failure("Question introuvable.");
        if (entity.AssessmentId != model.AssessmentId) return ServiceResult.Failure("L’évaluation parente est invalide.");
        if (entity.QuestionType != model.QuestionType && entity.AnswerOptions.Count != 0)
            return ServiceResult.Failure("Supprimez les choix avant de changer le type de question.");
        var error = ValidateBase(model.QuestionType, model.Statement, model.Order, model.Points, model.ExpectedAnswer);
        if (error is not null) return ServiceResult.Failure(error);
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        entity.QuestionType = model.QuestionType; entity.Statement = model.Statement.Trim();
        entity.Explanation = Trim(model.Explanation); entity.Points = model.Points;
        entity.ExpectedAnswer = model.QuestionType == QuestionType.ShortAnswer ? Trim(model.ExpectedAnswer) : null;
        entity.IsPublished = false; entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.RepositionQuestionAsync(db, entity, model.Order, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> PublishAsync(int id, CancellationToken token = default)
    {
        var entity = await db.Questions.Include(x => x.AnswerOptions).Include(x => x.Assessment)
            .SingleOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return ServiceResult.Failure("Question introuvable.");
        try { entity.Publish(DateTime.UtcNow); }
        catch (InvalidOperationException ex) { return ServiceResult.Failure(ex.Message); }
        await db.SaveChangesAsync(token); return ServiceResult.Success();
    }
    public async Task<ServiceResult> UnpublishAsync(int id, CancellationToken token = default)
    {
        var entity = await db.Questions.FindAsync([id], token);
        if (entity is null) return ServiceResult.Failure("Question introuvable.");
        entity.IsPublished = false; entity.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }
    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken token = default)
    {
        var entity = await db.Questions.Include(x => x.AnswerOptions).SingleOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return ServiceResult.Failure("Question introuvable.");
        if (entity.AnswerOptions.Count != 0) return ServiceResult.Failure("Cette question contient des réponses et ne peut pas être supprimée.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var parent = entity.AssessmentId; db.Questions.Remove(entity); await db.SaveChangesAsync(token);
        await OrderingService.NormalizeQuestionsAsync(db, parent, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }
    public Task<ServiceResult> MoveUpAsync(int id, CancellationToken token = default) => MoveAsync(id, -1, token);
    public Task<ServiceResult> MoveDownAsync(int id, CancellationToken token = default) => MoveAsync(id, 1, token);
    private async Task<ServiceResult> MoveAsync(int id, int direction, CancellationToken token)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        if (!await OrderingService.MoveQuestionAsync(db, id, direction, token))
            return ServiceResult.Failure("Question introuvable.");
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }
    private static string? ValidateBase(QuestionType type, string statement, int? order, decimal points, string? expected) =>
        string.IsNullOrWhiteSpace(statement) ? "L’énoncé est obligatoire." :
        order <= 0 ? "L’ordre doit être supérieur ou égal à 1." :
        points < 0 ? "Les points doivent être supérieurs ou égaux à zéro." :
        type == QuestionType.ShortAnswer && string.IsNullOrWhiteSpace(expected) ? "La réponse attendue est obligatoire." :
        type == QuestionType.ShortAnswer && expected is { Length: > 2000 } ? "La réponse attendue ne peut pas dépasser 2000 caractères." : null;
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
