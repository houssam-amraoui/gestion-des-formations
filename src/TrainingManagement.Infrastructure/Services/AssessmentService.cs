using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AssessmentService(ApplicationDbContext db) : IAssessmentService
{
    public async Task<AssessmentCollectionModel?> GetByLessonAsync(int lessonId, CancellationToken token = default)
    {
        var parent = await db.Lessons.AsNoTracking().Where(x => x.Id == lessonId)
            .Select(x => new { x.Id, ModuleId = x.TrainingModuleId, TrainingId = x.TrainingModule.TrainingId,
                LessonTitle = x.Title, ModuleTitle = x.TrainingModule.Title, TrainingTitle = x.TrainingModule.Training.Title })
            .SingleOrDefaultAsync(token);
        if (parent is null) return null;
        var items = await db.Assessments.AsNoTracking().Where(x => x.LessonId == lessonId)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.Order)
            .Select(x => new AssessmentListItem(x.Id, x.LessonId, x.Title, x.Slug, x.AssessmentType,
                x.Order, x.Questions.Count, x.Questions.Sum(q => q.Points), x.PassingScore,
                x.TimeLimitMinutes, x.MaximumAttempts, x.IsPublished, x.IsArchived)).ToListAsync(token);
        return new(parent.Id, parent.ModuleId, parent.TrainingId, parent.LessonTitle,
            parent.ModuleTitle, parent.TrainingTitle, items);
    }

    public Task<AssessmentDetailsModel?> GetByIdAsync(int id, CancellationToken token = default) =>
        db.Assessments.AsNoTracking().Where(x => x.Id == id).Select(x => new AssessmentDetailsModel(
            x.Id, x.LessonId, x.Lesson.TrainingModuleId, x.Lesson.TrainingModule.TrainingId,
            x.Lesson.Title, x.Lesson.TrainingModule.Title, x.Lesson.TrainingModule.Training.Title,
            x.Title, x.Slug, x.Description, x.AssessmentType, x.Order, x.PassingScore,
            x.MaximumAttempts, x.TimeLimitMinutes, x.ShuffleQuestions, x.ShowCorrectAnswers,
            x.IsPublished, x.IsArchived, x.Questions.Count, x.Questions.Sum(q => q.Points),
            x.CreatedAt, x.UpdatedAt)).SingleOrDefaultAsync(token);

    public async Task<ServiceResult<int>> CreateAsync(AssessmentCreateModel model, CancellationToken token = default)
    {
        if (!await db.Lessons.AnyAsync(x => x.Id == model.LessonId, token))
            return ServiceResult<int>.Failure("Leçon introuvable.");
        var error = Validate(model.Title, model.Order, model.PassingScore, model.MaximumAttempts, model.TimeLimitMinutes);
        if (error is not null) return ServiceResult<int>.Failure(error);
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        if (await db.Assessments.AnyAsync(x => x.LessonId == model.LessonId && x.Slug == slug, token))
            return ServiceResult<int>.Failure("Ce slug est déjà utilisé dans cette leçon.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var order = await OrderingService.PrepareAssessmentInsertAsync(db, model.LessonId, model.Order, token);
        var entity = new Assessment { LessonId = model.LessonId, Title = model.Title.Trim(), Slug = slug,
            Description = Trim(model.Description), AssessmentType = model.AssessmentType, Order = order,
            PassingScore = model.PassingScore, MaximumAttempts = model.MaximumAttempts,
            TimeLimitMinutes = model.TimeLimitMinutes, ShuffleQuestions = model.ShuffleQuestions,
            ShowCorrectAnswers = model.ShowCorrectAnswers, CreatedAt = DateTime.UtcNow };
        db.Assessments.Add(entity);
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult<int>.Success(entity.Id);
    }

    public async Task<ServiceResult> UpdateAsync(AssessmentEditModel model, CancellationToken token = default)
    {
        var entity = await db.Assessments.FindAsync([model.Id], token);
        if (entity is null) return ServiceResult.Failure("Évaluation introuvable.");
        if (entity.LessonId != model.LessonId) return ServiceResult.Failure("La leçon parente est invalide.");
        if (entity.IsArchived) return ServiceResult.Failure("Une évaluation archivée ne peut pas être modifiée.");
        var error = Validate(model.Title, model.Order, model.PassingScore, model.MaximumAttempts, model.TimeLimitMinutes);
        if (error is not null) return ServiceResult.Failure(error);
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        if (await db.Assessments.AnyAsync(x => x.LessonId == entity.LessonId && x.Id != entity.Id && x.Slug == slug, token))
            return ServiceResult.Failure("Ce slug est déjà utilisé dans cette leçon.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        entity.Title = model.Title.Trim(); entity.Slug = slug; entity.Description = Trim(model.Description);
        entity.AssessmentType = model.AssessmentType; entity.PassingScore = model.PassingScore;
        entity.MaximumAttempts = model.MaximumAttempts; entity.TimeLimitMinutes = model.TimeLimitMinutes;
        entity.ShuffleQuestions = model.ShuffleQuestions; entity.ShowCorrectAnswers = model.ShowCorrectAnswers;
        entity.UpdatedAt = DateTime.UtcNow;
        await OrderingService.RepositionAssessmentAsync(db, entity, model.Order, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> PublishAsync(int id, CancellationToken token = default)
    {
        var entity = await db.Assessments.Include(x => x.Questions)
            .Include(x => x.Lesson).ThenInclude(x => x.TrainingModule).ThenInclude(x => x.Training)
            .SingleOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return ServiceResult.Failure("Évaluation introuvable.");
        try { entity.Publish(DateTime.UtcNow); }
        catch (InvalidOperationException ex) { return ServiceResult.Failure(ex.Message); }
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }
    public Task<ServiceResult> UnpublishAsync(int id, CancellationToken token = default) => SetPublishedAsync(id, false, token);

    public async Task<ServiceResult> ArchiveAsync(int id, CancellationToken token = default)
    {
        var entity = await db.Assessments.FindAsync([id], token);
        if (entity is null) return ServiceResult.Failure("Évaluation introuvable.");
        entity.IsArchived = true; entity.IsPublished = false; entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken token = default)
    {
        var entity = await db.Assessments.Include(x => x.Questions).SingleOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return ServiceResult.Failure("Évaluation introuvable.");
        if (entity.Questions.Count != 0) return ServiceResult.Failure("Cette évaluation contient des questions. Archivez-la.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        var parent = entity.LessonId; db.Assessments.Remove(entity); await db.SaveChangesAsync(token);
        await OrderingService.NormalizeAssessmentsAsync(db, parent, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }

    public Task<ServiceResult> MoveUpAsync(int id, CancellationToken token = default) => MoveAsync(id, -1, token);
    public Task<ServiceResult> MoveDownAsync(int id, CancellationToken token = default) => MoveAsync(id, 1, token);

    public async Task<AssessmentPreviewModel?> GetAdminPreviewAsync(int id, CancellationToken token = default) =>
        await GetPreviewQuery(id).SingleOrDefaultAsync(token);

    public Task<IReadOnlyCollection<PublicAssessmentCard>> GetPublicByLessonAsync(int lessonId, CancellationToken token = default) =>
        db.Assessments.AsNoTracking().Where(x => x.LessonId == lessonId && x.IsPublished && !x.IsArchived &&
            x.Lesson.IsPreview && x.Lesson.IsPublished && !x.Lesson.IsArchived &&
            x.Lesson.TrainingModule.IsPublished && !x.Lesson.TrainingModule.IsArchived &&
            x.Lesson.TrainingModule.Training.Status == TrainingStatus.Published)
        .OrderBy(x => x.Order).Select(x => new PublicAssessmentCard(x.Id, x.Title, x.Slug, x.AssessmentType,
            x.Description, x.Questions.Count(q => q.IsPublished), x.TimeLimitMinutes, x.PassingScore))
        .ToListAsync(token).ContinueWith<IReadOnlyCollection<PublicAssessmentCard>>(x => x.Result, token);

    public async Task<IReadOnlyCollection<PublicAssessmentCard>> GetAccessibleByLessonAsync(int lessonId,
        string learnerId, CancellationToken token = default) =>
        await db.Assessments.AsNoTracking().Where(x => x.LessonId == lessonId && x.IsPublished && !x.IsArchived &&
            x.Lesson.IsPublished && !x.Lesson.IsArchived && x.Lesson.TrainingModule.IsPublished &&
            !x.Lesson.TrainingModule.IsArchived &&
            db.Enrollments.Any(e => e.LearnerId == learnerId &&
                e.TrainingId == x.Lesson.TrainingModule.TrainingId &&
                (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed)))
            .OrderBy(x => x.Order).Select(x => new PublicAssessmentCard(x.Id, x.Title, x.Slug, x.AssessmentType,
                x.Description, x.Questions.Count(q => q.IsPublished), x.TimeLimitMinutes, x.PassingScore))
            .ToListAsync(token);

    public Task<PublicAssessmentPage?> GetPublicAsync(string trainingSlug, string moduleSlug, string lessonSlug,
        string assessmentSlug, CancellationToken token = default) =>
        db.Assessments.AsNoTracking().Where(x => x.Slug == assessmentSlug && x.IsPublished && !x.IsArchived &&
            x.Lesson.Slug == lessonSlug && x.Lesson.IsPublished && x.Lesson.IsPreview && !x.Lesson.IsArchived &&
            x.Lesson.TrainingModule.Slug == moduleSlug && x.Lesson.TrainingModule.IsPublished && !x.Lesson.TrainingModule.IsArchived &&
            x.Lesson.TrainingModule.Training.Slug == trainingSlug && x.Lesson.TrainingModule.Training.Status == TrainingStatus.Published)
        .Select(x => new PublicAssessmentPage(x.Lesson.TrainingModule.Training.Title, trainingSlug,
            x.Lesson.TrainingModule.Title, moduleSlug, x.Lesson.Title, lessonSlug, x.Title, x.Slug,
            x.Description, x.AssessmentType, x.PassingScore, x.MaximumAttempts, x.TimeLimitMinutes,
            x.Questions.Where(q => q.IsPublished).OrderBy(q => q.Order)
                .Select(q => new PublicQuestionView(q.Id, q.QuestionType, q.Statement, q.Order, q.Points,
                    q.AnswerOptions.OrderBy(o => o.Order).Select(o => new PublicAnswerOptionView(o.Id, o.Text, o.Order)).ToList()))
                .ToList())).SingleOrDefaultAsync(token);

    public Task<AssessmentPreviewModel?> GetTrainerPreviewAsync(int id, string trainerId, CancellationToken token = default) =>
        GetPreviewQuery(id).Where(x => db.Assessments.Any(a => a.Id == id &&
            a.Lesson.TrainingModule.Training.TrainerId == trainerId)).SingleOrDefaultAsync(token);

    private IQueryable<AssessmentPreviewModel> GetPreviewQuery(int id) =>
        db.Assessments.AsNoTracking().Where(x => x.Id == id).Select(x => new AssessmentPreviewModel(
            new AssessmentDetailsModel(x.Id, x.LessonId, x.Lesson.TrainingModuleId, x.Lesson.TrainingModule.TrainingId,
                x.Lesson.Title, x.Lesson.TrainingModule.Title, x.Lesson.TrainingModule.Training.Title,
                x.Title, x.Slug, x.Description, x.AssessmentType, x.Order, x.PassingScore, x.MaximumAttempts,
                x.TimeLimitMinutes, x.ShuffleQuestions, x.ShowCorrectAnswers, x.IsPublished, x.IsArchived,
                x.Questions.Count, x.Questions.Sum(q => q.Points), x.CreatedAt, x.UpdatedAt),
            x.Questions.OrderBy(q => q.Order).Select(q => new AssessmentQuestionView(q.Id, q.QuestionType,
                q.Statement, q.Explanation, q.Order, q.Points, q.ExpectedAnswer, q.IsPublished,
                q.AnswerOptions.OrderBy(o => o.Order).Select(o => new AnswerOptionView(o.Id, o.Text, o.Order, o.IsCorrect)).ToList())).ToList(), true));

    private async Task<ServiceResult> MoveAsync(int id, int direction, CancellationToken token)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        if (!await OrderingService.MoveAssessmentAsync(db, id, direction, token))
            return ServiceResult.Failure("Évaluation introuvable ou archivée.");
        if (transaction is not null) await transaction.CommitAsync(token);
        return ServiceResult.Success();
    }
    private async Task<ServiceResult> SetPublishedAsync(int id, bool value, CancellationToken token)
    {
        var entity = await db.Assessments.FindAsync([id], token);
        if (entity is null) return ServiceResult.Failure("Évaluation introuvable.");
        entity.IsPublished = value; entity.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }
    private static string? Validate(string title, int? order, decimal passing, int? attempts, int? minutes) =>
        string.IsNullOrWhiteSpace(title) ? "Le titre est obligatoire." :
        order <= 0 ? "L’ordre doit être supérieur ou égal à 1." :
        passing is < 0 or > 100 ? "La note minimale doit être comprise entre 0 et 100." :
        attempts <= 0 ? "Le nombre maximal de tentatives doit être positif." :
        minutes <= 0 ? "La limite de temps doit être positive." : null;
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
