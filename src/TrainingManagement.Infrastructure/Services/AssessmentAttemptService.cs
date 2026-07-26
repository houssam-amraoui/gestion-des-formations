using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Application.Completion;
using TrainingManagement.Application.Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AssessmentAttemptService(
    ApplicationDbContext db,
    IAttemptScoringService scoring,
    IServiceProvider services) : IAssessmentAttemptService
{
    private sealed record SnapshotOption(int Id, string Text, int Order, bool IsCorrect);

    public async Task<ServiceResult<int>> StartAsync(AttemptStartModel model, CancellationToken token = default)
    {
        var assessment = await db.Assessments
            .Include(x => x.Lesson).ThenInclude(x => x.TrainingModule)
            .Include(x => x.Questions.Where(q => q.IsPublished)).ThenInclude(q => q.AnswerOptions)
            .SingleOrDefaultAsync(x => x.Id == model.AssessmentId && x.IsPublished && !x.IsArchived &&
                x.Lesson.IsPublished && !x.Lesson.IsArchived &&
                x.Lesson.TrainingModule.IsPublished && !x.Lesson.TrainingModule.IsArchived &&
                x.Lesson.TrainingModule.Training.Status == TrainingStatus.Published, token);
        if (assessment is null) return ServiceResult<int>.Failure("Évaluation indisponible.");
        var enrollment = await db.Enrollments.SingleOrDefaultAsync(x =>
            x.LearnerId == model.LearnerId &&
            x.TrainingId == assessment.Lesson.TrainingModule.TrainingId &&
            (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Completed), token);
        if (enrollment is null) return ServiceResult<int>.Failure("Une inscription active est requise.");

        var existing = await db.AssessmentAttempts.Include(x => x.Questions).ThenInclude(x => x.Answers)
            .Include(x => x.Assessment).SingleOrDefaultAsync(x => x.EnrollmentId == enrollment.Id &&
                x.AssessmentId == assessment.Id && x.Status == AttemptStatus.InProgress, token);
        if (existing is not null)
        {
            if (!IsExpired(existing)) return ServiceResult<int>.Success(existing.Id);
            await FinalizeAsync(existing, AttemptStatus.Expired, DateTime.UtcNow, token);
        }
        var used = await db.AssessmentAttempts.CountAsync(x =>
            x.EnrollmentId == enrollment.Id && x.AssessmentId == assessment.Id, token);
        if (assessment.MaximumAttempts is not null && used >= assessment.MaximumAttempts)
            return ServiceResult<int>.Failure("Le nombre maximal de tentatives est atteint.");
        if (assessment.Questions.Count == 0)
            return ServiceResult<int>.Failure("Cette évaluation ne contient aucune question publiée.");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, token) : null;
        var now = DateTime.UtcNow;
        var next = await db.AssessmentAttempts.Where(x => x.EnrollmentId == enrollment.Id &&
            x.AssessmentId == assessment.Id).Select(x => (int?)x.AttemptNumber).MaxAsync(token) ?? 0;
        var attempt = new AssessmentAttempt
        {
            EnrollmentId = enrollment.Id, AssessmentId = assessment.Id, AttemptNumber = next + 1,
            StartedAt = now, CreatedAt = now,
            ExpiresAt = assessment.TimeLimitMinutes is null ? null : now.AddMinutes(assessment.TimeLimitMinutes.Value),
            MaximumScore = assessment.Questions.Sum(x => x.Points)
        };
        db.AssessmentAttempts.Add(attempt);
        await db.SaveChangesAsync(token);
        var ordered = assessment.Questions.OrderBy(x => x.Order).ToList();
        if (assessment.ShuffleQuestions)
        {
            var random = new Random(HashCode.Combine(attempt.Id, assessment.Id));
            ordered = ordered.OrderBy(_ => random.Next()).ToList();
        }
        for (var index = 0; index < ordered.Count; index++)
        {
            var question = ordered[index];
            var options = question.AnswerOptions.OrderBy(x => x.Order)
                .Select(x => new SnapshotOption(x.Id, x.Text, x.Order, x.IsCorrect)).ToArray();
            db.AttemptQuestions.Add(new AttemptQuestion
            {
                AssessmentAttemptId = attempt.Id, QuestionId = question.Id, DisplayOrder = index + 1,
                PointsSnapshot = question.Points, StatementSnapshot = question.Statement,
                QuestionTypeSnapshot = question.QuestionType, ExplanationSnapshot = question.Explanation,
                ExpectedAnswerSnapshot = question.ExpectedAnswer,
                AnswerOptionsSnapshotJson = JsonSerializer.Serialize(options),
                CorrectAnswerOptionIdsSnapshot = string.Join(",", options.Where(x => x.IsCorrect).Select(x => x.Id))
            });
        }
        enrollment.LastAccessedAt = now;
        enrollment.StartedAt ??= now;
        try
        {
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(token);
            return ServiceResult<int>.Failure("Une tentative concurrente existe déjà.");
        }
        return ServiceResult<int>.Success(attempt.Id);
    }

    public async Task<AttemptDetailsModel?> GetAttemptAsync(int id, string learnerId, CancellationToken token = default)
    {
        var attempt = await LoadAttemptAsync(id, token);
        if (attempt is null || attempt.Enrollment.LearnerId != learnerId) return null;
        if (attempt.Status == AttemptStatus.InProgress && IsExpired(attempt))
            await FinalizeAsync(attempt, AttemptStatus.Expired, DateTime.UtcNow, token);
        return MapDetails(attempt);
    }

    public async Task<ServiceResult> SaveAnswersAsync(int attemptId, string learnerId,
        IReadOnlyCollection<AttemptAnswerInputModel> answers, CancellationToken token = default)
    {
        var attempt = await LoadAttemptAsync(attemptId, token);
        if (attempt is null || attempt.Enrollment.LearnerId != learnerId)
            return ServiceResult.Failure("Tentative introuvable.");
        if (attempt.Status != AttemptStatus.InProgress)
            return ServiceResult.Failure("Cette tentative ne peut plus être modifiée.");
        if (IsExpired(attempt))
        {
            await FinalizeAsync(attempt, AttemptStatus.Expired, DateTime.UtcNow, token);
            return ServiceResult.Failure("La tentative a expiré et les réponses sauvegardées ont été corrigées.");
        }
        var error = ApplyAnswers(attempt, answers);
        if (error is not null) return ServiceResult.Failure(error);
        attempt.ConcurrencyToken = Guid.NewGuid();
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<AttemptResultModel>> SubmitAsync(AttemptSubmissionModel model,
        CancellationToken token = default)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, token) : null;
        var attempt = await LoadAttemptAsync(model.AttemptId, token);
        if (attempt is null || attempt.Enrollment.LearnerId != model.LearnerId)
            return ServiceResult<AttemptResultModel>.Failure("Tentative introuvable.");
        if (attempt.Status != AttemptStatus.InProgress)
            return ServiceResult<AttemptResultModel>.Failure("Cette tentative a déjà été finalisée.");
        var now = DateTime.UtcNow;
        if (IsExpired(attempt))
        {
            await FinalizeAsync(attempt, AttemptStatus.Expired, now, token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return ServiceResult<AttemptResultModel>.Success(MapResult(attempt));
        }
        var error = ApplyAnswers(attempt, model.Answers);
        if (error is not null) return ServiceResult<AttemptResultModel>.Failure(error);
        await FinalizeAsync(attempt, AttemptStatus.Submitted, now, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        var completion = services.GetService<ITrainingCompletionService>();
        if (completion is not null)
        {
            var finalized = await completion.FinalizeAsync(attempt.EnrollmentId, token);
            var certificates = services.GetService<ICertificateService>();
            if (certificates is not null && finalized.Succeeded && finalized.Value?.Completed == true)
                await certificates.GenerateAsync(attempt.EnrollmentId, token);
        }
        return ServiceResult<AttemptResultModel>.Success(MapResult(attempt));
    }

    public async Task<AttemptResultModel?> GetResultAsync(int id, string learnerId, bool privileged = false,
        CancellationToken token = default)
    {
        var attempt = await LoadAttemptAsync(id, token);
        if (attempt is null || (!privileged && attempt.Enrollment.LearnerId != learnerId)) return null;
        if (attempt.Status == AttemptStatus.InProgress && IsExpired(attempt))
            await FinalizeAsync(attempt, AttemptStatus.Expired, DateTime.UtcNow, token);
        return attempt.Status == AttemptStatus.InProgress ? null : MapResult(attempt);
    }

    public async Task<IReadOnlyCollection<AttemptHistoryItem>> GetHistoryAsync(string learnerId,
        AttemptHistoryFilter filter, CancellationToken token = default)
    {
        var query = db.AssessmentAttempts.AsNoTracking().Where(x => x.Enrollment.LearnerId == learnerId);
        if (filter.TrainingId is not null) query = query.Where(x => x.Enrollment.TrainingId == filter.TrainingId);
        if (filter.Passed is not null) query = query.Where(x => x.Passed == filter.Passed);
        if (filter.Status is not null) query = query.Where(x => x.Status == filter.Status);
        return await query.OrderByDescending(x => x.StartedAt).Select(x => new AttemptHistoryItem(
            x.Id, x.Enrollment.TrainingId, x.Enrollment.Training.Title, x.Assessment.Title,
            x.AttemptNumber, x.Status, x.StartedAt, x.SubmittedAt, x.PercentageScore, x.Passed)).ToListAsync(token);
    }

    public async Task<IReadOnlyCollection<AdminAttemptListItem>> GetAdminAttemptsAsync(CancellationToken token = default) =>
        await db.AssessmentAttempts.AsNoTracking().OrderByDescending(x => x.StartedAt)
            .Select(x => new AdminAttemptListItem(x.Id,
                db.Users.Where(u => u.Id == x.Enrollment.LearnerId).Select(u => u.FirstName + " " + u.LastName).First(),
                x.Enrollment.Training.Title, x.Assessment.Title, x.AttemptNumber, x.Status,
                x.PercentageScore, x.Passed, x.StartedAt, x.SubmittedAt)).ToListAsync(token);

    public async Task<IReadOnlyCollection<TrainerLearnerProgressItem>> GetTrainerProgressAsync(string trainerId,
        CancellationToken token = default) =>
        await db.Enrollments.AsNoTracking().Where(x => x.Training.TrainerId == trainerId)
            .OrderBy(x => x.Training.Title).ThenBy(x =>
                db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.LastName).First())
            .Select(x => new TrainerLearnerProgressItem(x.Id, x.TrainingId, x.Training.Title,
                db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.FirstName + " " + u.LastName).First(),
                db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.Email!).First(),
                x.Status, x.ProgressPercentage,
                x.LessonProgresses.Count(p => p.Status == LessonProgressStatus.Completed &&
                    p.Lesson.IsPublished && !p.Lesson.IsArchived),
                x.Training.Modules.SelectMany(m => m.Lessons).Count(l => l.IsPublished && !l.IsArchived),
                x.Attempts.Count(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Expired),
                x.Attempts.Where(a => a.PercentageScore != null).Select(a => a.PercentageScore).Average()))
            .ToListAsync(token);

    public async Task<IReadOnlyCollection<AdminAttemptListItem>> GetTrainerAttemptsAsync(string trainerId,
        CancellationToken token = default) =>
        await db.AssessmentAttempts.AsNoTracking().Where(x => x.Enrollment.Training.TrainerId == trainerId)
            .OrderByDescending(x => x.StartedAt).Select(x => new AdminAttemptListItem(x.Id,
                db.Users.Where(u => u.Id == x.Enrollment.LearnerId).Select(u => u.FirstName + " " + u.LastName).First(),
                x.Enrollment.Training.Title, x.Assessment.Title, x.AttemptNumber, x.Status,
                x.PercentageScore, x.Passed, x.StartedAt, x.SubmittedAt)).ToListAsync(token);

    public async Task<AttemptResultModel?> GetTrainerResultAsync(int id, string trainerId,
        CancellationToken token = default)
    {
        var attempt = await LoadAttemptAsync(id, token);
        return attempt is null || attempt.Enrollment.Training.TrainerId != trainerId ||
            attempt.Status == AttemptStatus.InProgress ? null : MapResult(attempt, true);
    }

    private async Task<AssessmentAttempt?> LoadAttemptAsync(int id, CancellationToken token) =>
        await db.AssessmentAttempts.Include(x => x.Enrollment).ThenInclude(x => x.Training)
            .Include(x => x.Assessment)
            .Include(x => x.Questions.OrderBy(q => q.DisplayOrder)).ThenInclude(q => q.Answers)
            .SingleOrDefaultAsync(x => x.Id == id, token);

    private static bool IsExpired(AssessmentAttempt attempt) =>
        attempt.ExpiresAt is not null && DateTime.UtcNow >= attempt.ExpiresAt;

    private string? ApplyAnswers(AssessmentAttempt attempt, IReadOnlyCollection<AttemptAnswerInputModel> inputs)
    {
        foreach (var input in inputs)
        {
            var question = attempt.Questions.SingleOrDefault(x => x.Id == input.AttemptQuestionId);
            if (question is null) return "Une réponse ne correspond pas à cette tentative.";
            var options = Options(question);
            db.LearnerAnswers.RemoveRange(question.Answers);
            question.Answers.Clear();
            if (question.QuestionTypeSnapshot == QuestionType.ShortAnswer)
            {
                if (input.SelectedOptionIds.Count != 0) return "Une réponse courte ne peut pas sélectionner un choix.";
                var text = string.IsNullOrWhiteSpace(input.TextAnswer) ? null : input.TextAnswer.Trim();
                if (text?.Length > 2000) return "La réponse courte ne peut pas dépasser 2000 caractères.";
                if (text is not null) question.Answers.Add(new LearnerAnswer
                {
                    TextAnswer = text, AnswerTextSnapshot = text, CreatedAt = DateTime.UtcNow
                });
                continue;
            }
            var selected = input.SelectedOptionIds.Distinct().ToArray();
            if (question.QuestionTypeSnapshot is QuestionType.SingleChoice or QuestionType.TrueFalse && selected.Length > 1)
                return "Une seule réponse peut être sélectionnée.";
            if (selected.Any(id => options.All(x => x.Id != id)))
                return "Un choix ne correspond pas à cette question.";
            foreach (var id in selected)
            {
                var option = options.Single(x => x.Id == id);
                question.Answers.Add(new LearnerAnswer
                {
                    AnswerOptionId = id, AnswerTextSnapshot = option.Text,
                    WasCorrectSnapshot = option.IsCorrect, CreatedAt = DateTime.UtcNow
                });
            }
        }
        return null;
    }

    private async Task FinalizeAsync(AssessmentAttempt attempt, AttemptStatus status, DateTime now, CancellationToken token)
    {
        var input = attempt.Questions.Select(q => new ScoringAnswer(q.Id, q.QuestionTypeSnapshot,
            q.PointsSnapshot, q.CorrectAnswerOptionIdsSnapshot, q.ExpectedAnswerSnapshot,
            q.Answers.Where(a => a.AnswerOptionId != null).Select(a => a.AnswerOptionId!.Value).ToArray(),
            q.Answers.Select(a => a.TextAnswer).FirstOrDefault(x => x != null))).ToArray();
        var result = scoring.Score(input, attempt.Assessment.PassingScore);
        foreach (var question in attempt.Questions)
        {
            var score = result.Questions.Single(x => x.AttemptQuestionId == question.Id);
            var first = true;
            foreach (var answer in question.Answers)
            {
                answer.IsCorrect = score.IsCorrect;
                answer.PointsAwarded = first ? score.PointsAwarded : 0;
                answer.UpdatedAt = now;
                first = false;
            }
        }
        attempt.Status = status;
        attempt.Score = result.Score;
        attempt.MaximumScore = result.MaximumScore;
        attempt.PercentageScore = result.PercentageScore;
        attempt.Passed = result.Passed;
        attempt.SubmittedAt = now;
        attempt.DurationSeconds = Math.Max(0, (int)(now - attempt.StartedAt).TotalSeconds);
        attempt.ConcurrencyToken = Guid.NewGuid();
        await db.SaveChangesAsync(token);
    }

    private static AttemptDetailsModel MapDetails(AssessmentAttempt x) => new(x.Id, x.EnrollmentId,
        x.AssessmentId, x.Assessment.Title, x.AttemptNumber, x.Status, x.StartedAt, x.ExpiresAt,
        x.ExpiresAt is null ? null : Math.Max(0, (int)(x.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds),
        x.MaximumScore, x.Questions.OrderBy(q => q.DisplayOrder).Select(q =>
        {
            var selected = q.Answers.Where(a => a.AnswerOptionId != null).Select(a => a.AnswerOptionId!.Value).ToHashSet();
            return new AttemptQuestionModel(q.Id, q.DisplayOrder, q.StatementSnapshot, q.QuestionTypeSnapshot,
                q.PointsSnapshot, Options(q).Select(o => new AttemptOptionModel(o.Id, o.Text, o.Order,
                    selected.Contains(o.Id))).ToArray(), q.Answers.Select(a => a.TextAnswer).FirstOrDefault(v => v != null));
        }).ToArray());

    private static AttemptResultModel MapResult(AssessmentAttempt x, bool privileged = false)
    {
        var reveal = privileged || x.Assessment.ShowCorrectAnswers;
        var questions = x.Questions.OrderBy(q => q.DisplayOrder).Select(q =>
        {
            var options = Options(q);
            var learner = q.QuestionTypeSnapshot == QuestionType.ShortAnswer
                ? q.Answers.Select(a => a.TextAnswer).FirstOrDefault(v => v != null) ?? "Aucune réponse"
                : string.Join(", ", q.Answers.Select(a => a.AnswerTextSnapshot));
            string? correct = null;
            if (reveal) correct = q.QuestionTypeSnapshot == QuestionType.ShortAnswer
                ? q.ExpectedAnswerSnapshot
                : string.Join(", ", options.Where(o => o.IsCorrect).Select(o => o.Text));
            var points = q.Answers.Sum(a => a.PointsAwarded ?? 0);
            return new AttemptResultQuestionModel(q.StatementSnapshot, q.QuestionTypeSnapshot,
                q.PointsSnapshot, points, q.Answers.FirstOrDefault()?.IsCorrect ?? false,
                learner, correct, reveal ? q.ExplanationSnapshot : null);
        }).ToArray();
        return new(x.Id, x.Assessment.Title, x.Enrollment.Training.Title, x.AttemptNumber, x.Status,
            x.Score ?? 0, x.MaximumScore, x.PercentageScore ?? 0, x.Passed ?? false,
            x.Assessment.PassingScore, x.DurationSeconds ?? 0, x.SubmittedAt ?? x.StartedAt,
            reveal, questions);
    }
    private static SnapshotOption[] Options(AttemptQuestion question) =>
        JsonSerializer.Deserialize<SnapshotOption[]>(question.AnswerOptionsSnapshotJson ?? "[]") ?? [];
}
