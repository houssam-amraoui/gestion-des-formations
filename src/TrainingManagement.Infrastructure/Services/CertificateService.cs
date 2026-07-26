using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Completion;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Certificates;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class CertificateService(ApplicationDbContext db, ITrainingCompletionService completion,
    ICertificateNumberGenerator generator, ICertificatePdfService pdf,
    ICertificateStorageService storage, IOptions<ApplicationOptions> application,
    ILogger<CertificateService> logger) : ICertificateService
{
    public async Task<ServiceResult<CertificateGenerateResult>> GenerateAsync(int enrollmentId,
        CancellationToken token = default)
    {
        var eligibility = await completion.FinalizeAsync(enrollmentId, token);
        if (!eligibility.Succeeded || eligibility.Value is null || !eligibility.Value.Completed)
            return ServiceResult<CertificateGenerateResult>.Failure(
                eligibility.Error ?? "Les critères de réussite ne sont pas satisfaits.");
        var existing = await db.Certificates.SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId, token);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.PdfRelativePath))
                await GeneratePdfAsync(existing, token);
            return ServiceResult<CertificateGenerateResult>.Success(new(existing.Id,
                existing.CertificateNumber, existing.VerificationCode, false,
                !string.IsNullOrWhiteSpace(existing.PdfRelativePath)));
        }
        var enrollment = await db.Enrollments.Include(x => x.Training)
            .SingleAsync(x => x.Id == enrollmentId, token);
        if (!enrollment.Training.CertificateEnabled)
            return ServiceResult<CertificateGenerateResult>.Failure("Les certificats sont désactivés pour cette formation.");
        var learner = await db.Users.AsNoTracking().SingleAsync(x => x.Id == enrollment.LearnerId, token);
        var trainerName = enrollment.Training.TrainerId is null ? null :
            await db.Users.AsNoTracking().Where(x => x.Id == enrollment.Training.TrainerId)
                .Select(x => x.FirstName + " " + x.LastName).SingleOrDefaultAsync(token);
        var now = DateTime.UtcNow;
        var certificate = new Certificate
        {
            EnrollmentId = enrollmentId,
            CertificateNumber = await UniqueNumberAsync(now, token),
            VerificationCode = await UniqueCodeAsync(token),
            LearnerFullNameSnapshot = $"{learner.FirstName} {learner.LastName}".Trim(),
            TrainingTitleSnapshot = enrollment.Training.Title,
            TrainerFullNameSnapshot = trainerName,
            CompletionDate = enrollment.CompletedAt ?? now,
            IssuedAt = now,
            ExpiresAt = enrollment.Training.CertificateValidityMonths is int months ? now.AddMonths(months) : null,
            CreatedAt = now
        };
        db.Certificates.Add(certificate);
        await db.SaveChangesAsync(token);
        var generated = await GeneratePdfAsync(certificate, token);
        logger.LogInformation("Certificat {CertificateNumber} généré pour l'inscription {EnrollmentId}.",
            certificate.CertificateNumber, enrollmentId);
        return ServiceResult<CertificateGenerateResult>.Success(new(certificate.Id,
            certificate.CertificateNumber, certificate.VerificationCode, true, generated));
    }

    public async Task<ServiceResult> RegeneratePdfAsync(int certificateId, CancellationToken token = default)
    {
        var certificate = await db.Certificates.SingleOrDefaultAsync(x => x.Id == certificateId, token);
        if (certificate is null) return ServiceResult.Failure("Certificat introuvable.");
        return await GeneratePdfAsync(certificate, token) ? ServiceResult.Success() :
            ServiceResult.Failure("La génération du PDF a échoué.");
    }

    public async Task<ServiceResult> RevokeAsync(CertificateRevokeModel model, CancellationToken token = default)
    {
        var certificate = await db.Certificates.SingleOrDefaultAsync(x => x.Id == model.CertificateId, token);
        if (certificate is null) return ServiceResult.Failure("Certificat introuvable.");
        try { certificate.Revoke(model.Reason, model.AdminId, DateTime.UtcNow); }
        catch (InvalidOperationException ex) { return ServiceResult.Failure(ex.Message); }
        await db.SaveChangesAsync(token);
        logger.LogWarning("Certificat {CertificateNumber} révoqué par l'administrateur {AdminId}.",
            certificate.CertificateNumber, model.AdminId);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReactivateAsync(int certificateId, string adminId,
        CancellationToken token = default)
    {
        var certificate = await db.Certificates.SingleOrDefaultAsync(x => x.Id == certificateId, token);
        if (certificate is null) return ServiceResult.Failure("Certificat introuvable.");
        try { certificate.Reactivate(DateTime.UtcNow); }
        catch (InvalidOperationException ex) { return ServiceResult.Failure(ex.Message); }
        await db.SaveChangesAsync(token);
        logger.LogInformation("Certificat {CertificateNumber} réactivé par {AdminId}.",
            certificate.CertificateNumber, adminId);
        return ServiceResult.Success();
    }

    public Task<CertificateDetailsModel?> GetAsync(int id, CancellationToken token = default) =>
        DetailsQuery(id).SingleOrDefaultAsync(token);

    public Task<CertificateDetailsModel?> GetForLearnerAsync(int id, string learnerId,
        CancellationToken token = default) => DetailsQuery(id, learnerId: learnerId)
        .SingleOrDefaultAsync(token);

    public Task<CertificateDetailsModel?> GetForTrainerAsync(int id, string trainerId,
        CancellationToken token = default) => DetailsQuery(id, trainerId: trainerId)
        .SingleOrDefaultAsync(token);

    public async Task<IReadOnlyCollection<CertificateListItem>> GetForLearnerAsync(string learnerId,
        CancellationToken token = default) => await db.Certificates.AsNoTracking()
            .Where(x => x.Enrollment.LearnerId == learnerId).OrderByDescending(x => x.IssuedAt)
            .Select(x => new CertificateListItem(x.Id, x.EnrollmentId, x.CertificateNumber,
                x.LearnerFullNameSnapshot, x.TrainingTitleSnapshot, x.CompletionDate,
                x.IssuedAt, x.ExpiresAt, x.Status)).ToListAsync(token);

    public async Task<IReadOnlyCollection<CertificateListItem>> GetForTrainerAsync(string trainerId,
        CancellationToken token = default) => await db.Certificates.AsNoTracking()
            .Where(x => x.Enrollment.Training.TrainerId == trainerId).OrderByDescending(x => x.IssuedAt)
            .Select(x => new CertificateListItem(x.Id, x.EnrollmentId, x.CertificateNumber,
                x.LearnerFullNameSnapshot, x.TrainingTitleSnapshot, x.CompletionDate,
                x.IssuedAt, x.ExpiresAt, x.Status)).ToListAsync(token);

    public async Task<PagedResult<CertificateListItem>> GetAdminAsync(CertificateFilter filter,
        CancellationToken token = default)
    {
        var page = Math.Max(1, filter.Page);
        var query = db.Certificates.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.CertificateNumber.Contains(search) ||
                x.LearnerFullNameSnapshot.Contains(search) || x.TrainingTitleSnapshot.Contains(search));
        }
        if (filter.Status is not null) query = query.Where(x => x.Status == filter.Status);
        if (filter.DateFrom is not null) query = query.Where(x => x.IssuedAt >= filter.DateFrom);
        if (filter.DateTo is not null) query = query.Where(x => x.IssuedAt < filter.DateTo.Value.AddDays(1));
        var count = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.IssuedAt).Skip((page - 1) * filter.PageSize)
            .Take(filter.PageSize).Select(x => new CertificateListItem(x.Id, x.EnrollmentId,
                x.CertificateNumber, x.LearnerFullNameSnapshot, x.TrainingTitleSnapshot,
                x.CompletionDate, x.IssuedAt, x.ExpiresAt, x.Status)).ToListAsync(token);
        return new(items, page, filter.PageSize, count);
    }

    public async Task<CertificateVerificationModel> VerifyAsync(string numberOrCode,
        CancellationToken token = default)
    {
        var value = numberOrCode.Trim();
        var certificate = await db.Certificates.SingleOrDefaultAsync(x =>
            x.VerificationCode == value || x.CertificateNumber == value, token);
        if (certificate is null) return new(false, false, null, null, null, null, null, null, null, null);
        certificate.RefreshExpiration(DateTime.UtcNow);
        if (db.Entry(certificate).Property(x => x.Status).IsModified) await db.SaveChangesAsync(token);
        return new(true, certificate.IsValidAt(DateTime.UtcNow), certificate.Status,
            certificate.CertificateNumber, certificate.LearnerFullNameSnapshot,
            certificate.TrainingTitleSnapshot, certificate.TrainerFullNameSnapshot,
            certificate.CompletionDate, certificate.IssuedAt, certificate.ExpiresAt);
    }

    public async Task<CertificateFile?> DownloadAsync(int id, string userId, bool isAdmin, bool isTrainer,
        CancellationToken token = default)
    {
        var certificate = await db.Certificates.AsNoTracking().Include(x => x.Enrollment)
            .ThenInclude(x => x.Training).SingleOrDefaultAsync(x => x.Id == id, token);
        if (certificate is null || (!isAdmin && certificate.Enrollment.LearnerId != userId &&
            (!isTrainer || certificate.Enrollment.Training.TrainerId != userId))) return null;
        if (string.IsNullOrWhiteSpace(certificate.PdfRelativePath)) return null;
        var bytes = await storage.ReadAsync(certificate.PdfRelativePath, token);
        return bytes is null ? null : new(certificate.PdfFileName ?? $"{certificate.CertificateNumber}.pdf", bytes);
    }

    public async Task UpdateExpiredStatusesAsync(CancellationToken token = default)
    {
        var now = DateTime.UtcNow;
        var certificates = await db.Certificates.Where(x => x.Status == CertificateStatus.Active &&
            x.ExpiresAt != null && x.ExpiresAt <= now).ToListAsync(token);
        foreach (var certificate in certificates) certificate.RefreshExpiration(now);
        if (certificates.Count > 0) await db.SaveChangesAsync(token);
    }

    private async Task<bool> GeneratePdfAsync(Certificate certificate, CancellationToken token)
    {
        try
        {
            var url = $"{application.Value.PublicBaseUrl.TrimEnd('/')}/Certificates/Verify/{certificate.VerificationCode}";
            var content = pdf.Generate(new(application.Value.Name, certificate.LearnerFullNameSnapshot,
                certificate.TrainingTitleSnapshot, certificate.TrainerFullNameSnapshot,
                certificate.CompletionDate, certificate.ExpiresAt, certificate.CertificateNumber,
                certificate.VerificationCode, url));
            var name = $"{Guid.NewGuid():N}.pdf";
            certificate.PdfRelativePath = await storage.SaveAsync(name, content, token);
            certificate.PdfFileName = $"{certificate.CertificateNumber}.pdf";
            certificate.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(token);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec PDF pour le certificat {CertificateId}.", certificate.Id);
            return false;
        }
    }

    private async Task<string> UniqueNumberAsync(DateTime now, CancellationToken token)
    {
        for (var i = 0; i < 10; i++)
        {
            var value = generator.CreateCertificateNumber(now);
            if (!await db.Certificates.AnyAsync(x => x.CertificateNumber == value, token)) return value;
        }
        throw new InvalidOperationException("Impossible de générer un numéro de certificat unique.");
    }

    private async Task<string> UniqueCodeAsync(CancellationToken token)
    {
        for (var i = 0; i < 10; i++)
        {
            var value = generator.CreateVerificationCode();
            if (!await db.Certificates.AnyAsync(x => x.VerificationCode == value, token)) return value;
        }
        throw new InvalidOperationException("Impossible de générer un code de vérification unique.");
    }

    private IQueryable<CertificateDetailsModel> DetailsQuery(int id, string? learnerId = null, string? trainerId = null)
    {
        var query = db.Certificates.AsNoTracking().Where(x => x.Id == id);
        if (learnerId is not null) query = query.Where(x => x.Enrollment.LearnerId == learnerId);
        if (trainerId is not null) query = query.Where(x => x.Enrollment.Training.TrainerId == trainerId);
        return query.Select(x => new CertificateDetailsModel(x.Id, x.EnrollmentId,
            x.CertificateNumber, x.VerificationCode, x.LearnerFullNameSnapshot,
            x.TrainingTitleSnapshot, x.TrainerFullNameSnapshot, x.CompletionDate, x.IssuedAt,
            x.ExpiresAt, x.Status, x.RevokedAt, x.RevocationReason,
            x.PdfRelativePath != null));
    }

}
