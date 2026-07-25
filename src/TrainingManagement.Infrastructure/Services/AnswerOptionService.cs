using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.AnswerOptions;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AnswerOptionService(ApplicationDbContext db) : IAnswerOptionService
{
    public async Task<AnswerOptionCollectionModel?> GetByQuestionAsync(int questionId, CancellationToken token = default)
    {
        var question = await db.Questions.AsNoTracking().Where(x => x.Id == questionId)
            .Select(x => new { x.Id, x.AssessmentId, x.Statement, x.QuestionType, x.IsPublished }).SingleOrDefaultAsync(token);
        if (question is null) return null;
        var items = await db.AnswerOptions.AsNoTracking().Where(x => x.QuestionId == questionId)
            .OrderBy(x => x.Order).Select(x => new AnswerOptionListItem(x.Id, x.QuestionId, x.Text, x.Order, x.IsCorrect))
            .ToListAsync(token);
        return new(question.Id, question.AssessmentId, question.Statement, question.QuestionType, question.IsPublished, items);
    }
    public Task<AnswerOptionListItem?> GetByIdAsync(int id, CancellationToken token = default) =>
        db.AnswerOptions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AnswerOptionListItem(x.Id, x.QuestionId, x.Text, x.Order, x.IsCorrect))
            .SingleOrDefaultAsync(token);

    public async Task<ServiceResult<int>> CreateAsync(AnswerOptionCreateModel model, CancellationToken token = default)
    {
        var question = await db.Questions.Include(x => x.AnswerOptions).SingleOrDefaultAsync(x => x.Id == model.QuestionId, token);
        var error = ValidateMutation(question, model.Text);
        if (error is not null) return ServiceResult<int>.Failure(error);
        if (model.Order <= 0) return ServiceResult<int>.Failure("L’ordre doit être supérieur ou égal à 1.");
        if (question!.QuestionType == QuestionType.TrueFalse && question.AnswerOptions.Count >= 2)
            return ServiceResult<int>.Failure("Une question Vrai ou Faux ne peut contenir que deux choix.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var order = await OrderingService.PrepareAnswerInsertAsync(db, model.QuestionId, model.Order, token);
        var entity = new AnswerOption { QuestionId = model.QuestionId, Text = model.Text.Trim(),
            Order = order, IsCorrect = model.IsCorrect, CreatedAt = DateTime.UtcNow };
        db.AnswerOptions.Add(entity); await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult<int>.Success(entity.Id);
    }
    public async Task<ServiceResult> UpdateAsync(AnswerOptionEditModel model, CancellationToken token = default)
    {
        var entity = await db.AnswerOptions.Include(x => x.Question).SingleOrDefaultAsync(x => x.Id == model.Id, token);
        if (entity is null) return ServiceResult.Failure("Réponse introuvable.");
        if (entity.QuestionId != model.QuestionId) return ServiceResult.Failure("La question parente est invalide.");
        var error = ValidateMutation(entity.Question, model.Text);
        if (error is not null) return ServiceResult.Failure(error);
        if (model.Order <= 0) return ServiceResult.Failure("L’ordre doit être supérieur ou égal à 1.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        entity.Text = model.Text.Trim(); entity.IsCorrect = model.IsCorrect; entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.RepositionAnswerAsync(db, entity, model.Order, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }
    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken token = default)
    {
        var entity = await db.AnswerOptions.Include(x => x.Question).SingleOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return ServiceResult.Failure("Réponse introuvable.");
        if (entity.Question.IsPublished) return ServiceResult.Failure("Dépubliez la question avant de supprimer une réponse.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var parent = entity.QuestionId; db.AnswerOptions.Remove(entity); await db.SaveChangesAsync(token);
        await OrderingService.NormalizeAnswersAsync(db, parent, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }
    public async Task<ServiceResult> ToggleCorrectAsync(int id, CancellationToken token = default)
    {
        var entity = await db.AnswerOptions.Include(x => x.Question).SingleOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return ServiceResult.Failure("Réponse introuvable.");
        if (entity.Question.IsPublished) return ServiceResult.Failure("Dépubliez la question avant de modifier ses réponses.");
        entity.IsCorrect = !entity.IsCorrect; entity.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }
    public async Task<ServiceResult> CreateTrueFalseAsync(int questionId, CancellationToken token = default)
    {
        var question = await db.Questions.Include(x => x.AnswerOptions).SingleOrDefaultAsync(x => x.Id == questionId, token);
        if (question is null) return ServiceResult.Failure("Question introuvable.");
        if (question.QuestionType != QuestionType.TrueFalse) return ServiceResult.Failure("Cette action est réservée aux questions Vrai ou Faux.");
        if (question.IsPublished) return ServiceResult.Failure("Dépubliez la question avant de modifier ses réponses.");
        if (question.AnswerOptions.Count != 0) return ServiceResult.Failure("Supprimez les choix existants avant de générer Vrai/Faux.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        db.AnswerOptions.AddRange(
            new AnswerOption { QuestionId = questionId, Text = "Vrai", Order = 1, IsCorrect = true },
            new AnswerOption { QuestionId = questionId, Text = "Faux", Order = 2 });
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }
    public Task<ServiceResult> MoveUpAsync(int id, CancellationToken token = default) => MoveAsync(id, -1, token);
    public Task<ServiceResult> MoveDownAsync(int id, CancellationToken token = default) => MoveAsync(id, 1, token);
    private async Task<ServiceResult> MoveAsync(int id, int direction, CancellationToken token)
    {
        var item = await db.AnswerOptions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Question.IsPublished }).SingleOrDefaultAsync(token);
        if (item is null) return ServiceResult.Failure("Réponse introuvable.");
        if (item.IsPublished) return ServiceResult.Failure("Dépubliez la question avant de réordonner ses réponses.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        await OrderingService.MoveAnswerAsync(db, id, direction, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }
    private static string? ValidateMutation(Question? question, string text) =>
        question is null ? "Question introuvable." :
        question.QuestionType == QuestionType.ShortAnswer ? "Une réponse courte ne peut pas contenir de choix." :
        question.IsPublished ? "Dépubliez la question avant de modifier ses réponses." :
        string.IsNullOrWhiteSpace(text) ? "Le texte de la réponse est obligatoire." :
        text.Trim().Length > 1000 ? "Le texte ne peut pas dépasser 1000 caractères." : null;
}
