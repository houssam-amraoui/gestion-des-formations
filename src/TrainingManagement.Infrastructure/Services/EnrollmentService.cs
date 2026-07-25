using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class EnrollmentService(ApplicationDbContext db, UserManager<ApplicationUser> users) : IEnrollmentService
{
    public async Task<EnrollmentListResult> GetAdminListAsync(EnrollmentFilter filter, CancellationToken token = default)
    {
        var query = db.Enrollments.AsNoTracking().AsQueryable();
        if (filter.TrainingId is not null) query = query.Where(x => x.TrainingId == filter.TrainingId);
        if (filter.Status is not null) query = query.Where(x => x.Status == filter.Status);
        if (filter.From is not null) query = query.Where(x => x.EnrolledAt >= filter.From);
        if (filter.To is not null) query = query.Where(x => x.EnrolledAt < filter.To.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => db.Users.Any(u => u.Id == x.LearnerId &&
                (u.Email!.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search))));
        }
        query = filter.Sort switch
        {
            "name" => query.OrderBy(x => db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.LastName).First()),
            "progress" => query.OrderByDescending(x => x.ProgressPercentage),
            _ => query.OrderByDescending(x => x.EnrolledAt)
        };
        var total = await query.CountAsync(token);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 5, 100);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new EnrollmentListItem(x.Id, x.LearnerId,
                db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.FirstName + " " + u.LastName).First(),
                db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.Email!).First(),
                x.TrainingId, x.Training.Title, x.Status, x.ProgressPercentage, x.EnrolledAt, x.LastAccessedAt))
            .ToListAsync(token);
        return new(items, page, pageSize, total);
    }

    public Task<EnrollmentDetailsModel?> GetByIdAsync(int id, CancellationToken token = default) =>
        db.Enrollments.AsNoTracking().Where(x => x.Id == id).Select(x => new EnrollmentDetailsModel(
            x.Id, x.LearnerId,
            db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.FirstName + " " + u.LastName).First(),
            db.Users.Where(u => u.Id == x.LearnerId).Select(u => u.Email!).First(),
            x.TrainingId, x.Training.Title, x.Status, x.ProgressPercentage, x.EnrolledAt,
            x.StartedAt, x.CompletedAt, x.CancelledAt, x.LastAccessedAt, x.CreatedByAdminId))
            .SingleOrDefaultAsync(token);

    public async Task<IReadOnlyCollection<EnrollmentOption>> GetLearnerOptionsAsync(CancellationToken token = default)
    {
        var learners = await users.GetUsersInRoleAsync(AppRoles.Learner);
        return learners.Where(x => x.IsActive).OrderBy(x => x.LastName)
            .Select(x => new EnrollmentOption(x.Id, $"{x.FullName} — {x.Email}")).ToArray();
    }
    public async Task<IReadOnlyCollection<EnrollmentOption>> GetTrainingOptionsAsync(CancellationToken token = default) =>
        await db.Trainings.AsNoTracking().Where(x => x.Status != TrainingStatus.Archived)
            .OrderBy(x => x.Title).Select(x => new EnrollmentOption(x.Id.ToString(), x.Title)).ToListAsync(token);

    public async Task<ServiceResult<int>> EnrollFreeAsync(int trainingId, string learnerId, CancellationToken token = default)
    {
        if (!await IsLearnerAsync(learnerId)) return ServiceResult<int>.Failure("Seul un apprenant peut s’inscrire.");
        var training = await db.Trainings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == trainingId, token);
        if (training is null) return ServiceResult<int>.Failure("Formation introuvable.");
        if (training.Status != TrainingStatus.Published) return ServiceResult<int>.Failure("La formation n’est pas disponible.");
        if (!training.IsFree) return ServiceResult<int>.Failure("Cette formation payante nécessite une validation administrative.");
        return await CreateCoreAsync(learnerId, trainingId, EnrollmentStatus.Active, null, token);
    }

    public async Task<ServiceResult<int>> CreateByAdminAsync(EnrollmentCreateModel model, CancellationToken token = default)
    {
        if (!await IsLearnerAsync(model.LearnerId)) return ServiceResult<int>.Failure("L’utilisateur sélectionné doit avoir le rôle Learner.");
        var training = await db.Trainings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.TrainingId, token);
        if (training is null) return ServiceResult<int>.Failure("Formation introuvable.");
        if (training.Status == TrainingStatus.Archived) return ServiceResult<int>.Failure("Une formation archivée refuse les inscriptions.");
        return await CreateCoreAsync(model.LearnerId, model.TrainingId,
            model.ActivateImmediately ? EnrollmentStatus.Active : EnrollmentStatus.Pending, model.AdminId, token);
    }

    public Task<ServiceResult> ActivateAsync(int id, CancellationToken token = default) => ChangeAsync(id, EnrollmentStatus.Active, token);
    public Task<ServiceResult> ReactivateAsync(int id, CancellationToken token = default) => ChangeAsync(id, EnrollmentStatus.Active, token);
    public Task<ServiceResult> SuspendAsync(int id, CancellationToken token = default) => ChangeAsync(id, EnrollmentStatus.Suspended, token);
    public Task<ServiceResult> CancelAsync(int id, CancellationToken token = default) => ChangeAsync(id, EnrollmentStatus.Cancelled, token);

    public Task<bool> CanAccessTrainingAsync(int trainingId, string learnerId, CancellationToken token = default) =>
        db.Enrollments.AnyAsync(x => x.TrainingId == trainingId && x.LearnerId == learnerId &&
            (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Completed), token);

    public Task<EnrollmentDetailsModel?> GetAuthorizedAsync(int trainingId, string learnerId, CancellationToken token = default) =>
        db.Enrollments.AsNoTracking().Where(x => x.TrainingId == trainingId && x.LearnerId == learnerId &&
            (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Completed))
        .Select(x => new EnrollmentDetailsModel(x.Id, x.LearnerId, "", "", x.TrainingId, x.Training.Title,
            x.Status, x.ProgressPercentage, x.EnrolledAt, x.StartedAt, x.CompletedAt, x.CancelledAt,
            x.LastAccessedAt, x.CreatedByAdminId)).FirstOrDefaultAsync(token);

    public async Task<IReadOnlyCollection<LearnerTrainingModel>> GetLearnerTrainingsAsync(string learnerId, CancellationToken token = default) =>
        await db.Enrollments.AsNoTracking().Where(x => x.LearnerId == learnerId)
            .OrderByDescending(x => x.LastAccessedAt ?? x.EnrolledAt)
            .Select(x => new LearnerTrainingModel(x.Id, x.TrainingId, x.Training.Title, x.Training.Slug,
                x.Training.ThumbnailUrl, x.Status, x.ProgressPercentage, x.LastAccessedAt,
                x.LessonProgresses.Count(p => p.Status == LessonProgressStatus.Completed &&
                    p.Lesson.IsPublished && !p.Lesson.IsArchived),
                x.Training.Modules.SelectMany(m => m.Lessons).Count(l => l.IsPublished && !l.IsArchived),
                Array.Empty<LearnerModuleModel>())).ToListAsync(token);

    public async Task<LearnerTrainingModel?> GetLearnerTrainingAsync(int trainingId, string learnerId, CancellationToken token = default)
    {
        var enrollment = await db.Enrollments.AsNoTracking().Where(x => x.TrainingId == trainingId &&
            x.LearnerId == learnerId && (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Completed))
            .Select(x => new { x.Id, x.TrainingId, x.Training.Title, x.Training.Slug, x.Training.ThumbnailUrl,
                x.Status, x.ProgressPercentage, x.LastAccessedAt }).FirstOrDefaultAsync(token);
        if (enrollment is null) return null;
        var modules = await db.TrainingModules.AsNoTracking().Where(x => x.TrainingId == trainingId && x.IsPublished && !x.IsArchived)
            .OrderBy(x => x.Order).Select(m => new LearnerModuleModel(m.Id, m.Title, m.Order,
                m.Lessons.Where(l => l.IsPublished && !l.IsArchived).OrderBy(l => l.Order)
                    .Select(l => new LearnerLessonModel(l.Id, l.Title, l.Slug, l.Order, l.EstimatedDurationMinutes,
                        l.IsPreview,
                        db.LessonProgresses.Any(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id && p.Status == LessonProgressStatus.Completed),
                        db.LessonProgresses.Where(p => p.EnrollmentId == enrollment.Id && p.LessonId == l.Id)
                            .Select(p => p.Status).FirstOrDefault(),
                        l.Assessments.Where(a => a.IsPublished && !a.IsArchived).OrderBy(a => a.Order)
                            .Select(a => new LearnerAssessmentModel(a.Id, a.Title, a.Slug, a.AssessmentType,
                                a.TimeLimitMinutes, a.MaximumAttempts,
                                db.AssessmentAttempts.Count(t => t.EnrollmentId == enrollment.Id && t.AssessmentId == a.Id),
                                db.AssessmentAttempts.Where(t => t.EnrollmentId == enrollment.Id && t.AssessmentId == a.Id &&
                                    t.Status == AttemptStatus.InProgress).Select(t => (int?)t.Id).FirstOrDefault())).ToList()))
                    .ToList())).ToListAsync(token);
        var total = modules.Sum(x => x.Lessons.Count);
        var completed = modules.Sum(x => x.Lessons.Count(l => l.IsCompleted));
        return new(enrollment.Id, enrollment.TrainingId, enrollment.Title, enrollment.Slug,
            enrollment.ThumbnailUrl, enrollment.Status, enrollment.ProgressPercentage,
            enrollment.LastAccessedAt, completed, total, modules);
    }

    public async Task<LearnerDashboardModel> GetLearnerDashboardAsync(string learnerId, CancellationToken token = default)
    {
        var trainings = await GetLearnerTrainingsAsync(learnerId, token);
        var recentLessons = await db.LessonProgresses.AsNoTracking().Where(x => x.Enrollment.LearnerId == learnerId)
            .OrderByDescending(x => x.LastAccessedAt).Take(5)
            .Select(x => new RecentLessonModel(x.Enrollment.Training.Title, x.Lesson.Title, x.LastAccessedAt)).ToListAsync(token);
        var recentAttempts = await db.AssessmentAttempts.AsNoTracking().Where(x => x.Enrollment.LearnerId == learnerId &&
            x.Status != AttemptStatus.InProgress).OrderByDescending(x => x.SubmittedAt).Take(5)
            .Select(x => new RecentAttemptModel(x.Assessment.Title, x.PercentageScore, x.Passed, x.SubmittedAt)).ToListAsync(token);
        return new(trainings.Count(x => x.Status == EnrollmentStatus.Active),
            trainings.Count(x => x.Status == EnrollmentStatus.Completed),
            trainings.Count == 0 ? 0 : Math.Round(trainings.Average(x => x.ProgressPercentage), 2),
            trainings.Take(3).ToArray(), recentLessons, recentAttempts);
    }

    private async Task<ServiceResult<int>> CreateCoreAsync(string learnerId, int trainingId,
        EnrollmentStatus status, string? adminId, CancellationToken token)
    {
        if (await db.Enrollments.AnyAsync(x => x.LearnerId == learnerId && x.TrainingId == trainingId &&
            (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Pending ||
             x.Status == EnrollmentStatus.Completed), token))
            return ServiceResult<int>.Failure("Une inscription active ou en attente existe déjà.");
        var now = DateTime.UtcNow;
        var entity = new Enrollment { LearnerId = learnerId, TrainingId = trainingId, Status = status,
            EnrolledAt = now, CreatedByAdminId = adminId, StartedAt = status == EnrollmentStatus.Active ? now : null };
        db.Enrollments.Add(entity);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException) { return ServiceResult<int>.Failure("L’inscription n’a pas pu être créée simultanément."); }
        return ServiceResult<int>.Success(entity.Id);
    }

    private async Task<ServiceResult> ChangeAsync(int id, EnrollmentStatus target, CancellationToken token)
    {
        var item = await db.Enrollments.FindAsync([id], token);
        if (item is null) return ServiceResult.Failure("Inscription introuvable.");
        var now = DateTime.UtcNow;
        if (target == EnrollmentStatus.Active) item.Activate(now);
        else if (target == EnrollmentStatus.Suspended) item.Suspend();
        else if (target == EnrollmentStatus.Cancelled) item.Cancel(now);
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }
    private async Task<bool> IsLearnerAsync(string id)
    {
        var user = await users.FindByIdAsync(id);
        return user is not null && user.IsActive && await users.IsInRoleAsync(user, AppRoles.Learner);
    }
}
