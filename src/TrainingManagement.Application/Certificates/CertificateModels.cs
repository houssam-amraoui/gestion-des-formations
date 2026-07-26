using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Certificates;

public sealed record CertificateListItem(int Id, int EnrollmentId, string CertificateNumber,
    string LearnerFullName, string TrainingTitle, DateTime CompletionDate, DateTime IssuedAt,
    DateTime? ExpiresAt, CertificateStatus Status);
public sealed record CertificateDetailsModel(int Id, int EnrollmentId, string CertificateNumber,
    string VerificationCode, string LearnerFullName, string TrainingTitle, string? TrainerFullName,
    DateTime CompletionDate, DateTime IssuedAt, DateTime? ExpiresAt, CertificateStatus Status,
    DateTime? RevokedAt, string? RevocationReason, bool HasPdf);
public sealed record CertificateVerificationModel(bool Found, bool IsValid, CertificateStatus? Status,
    string? CertificateNumber, string? LearnerFullName, string? TrainingTitle, string? TrainerFullName,
    DateTime? CompletionDate, DateTime? IssuedAt, DateTime? ExpiresAt);
public sealed record CertificateGenerateResult(int CertificateId, string CertificateNumber,
    string VerificationCode, bool Created, bool PdfGenerated);
public sealed record CertificateRevokeModel(int CertificateId, string Reason, string AdminId);
public sealed record CertificateFilter(string? Search = null, CertificateStatus? Status = null,
    DateTime? DateFrom = null, DateTime? DateTo = null, int Page = 1, int PageSize = 20);
public sealed record CertificatePdfModel(string PlatformName, string LearnerName, string TrainingTitle,
    string? TrainerName, DateTime CompletionDate, DateTime? ExpiresAt, string CertificateNumber,
    string VerificationCode, string VerificationUrl);
public sealed record CertificateFile(string FileName, byte[] Content);

public interface ICertificateNumberGenerator
{
    string CreateCertificateNumber(DateTime issuedAt);
    string CreateVerificationCode();
}

public interface ICertificatePdfService
{
    byte[] Generate(CertificatePdfModel model);
}

public interface ICertificateStorageService
{
    Task<string> SaveAsync(string serverFileName, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default);
}

public interface ICertificateService
{
    Task<ServiceResult<CertificateGenerateResult>> GenerateAsync(int enrollmentId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> RegeneratePdfAsync(int certificateId, CancellationToken cancellationToken = default);
    Task<ServiceResult> RevokeAsync(CertificateRevokeModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> ReactivateAsync(int certificateId, string adminId,
        CancellationToken cancellationToken = default);
    Task<CertificateDetailsModel?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<CertificateDetailsModel?> GetForLearnerAsync(int id, string learnerId,
        CancellationToken cancellationToken = default);
    Task<CertificateDetailsModel?> GetForTrainerAsync(int id, string trainerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CertificateListItem>> GetForLearnerAsync(string learnerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CertificateListItem>> GetForTrainerAsync(string trainerId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<CertificateListItem>> GetAdminAsync(CertificateFilter filter,
        CancellationToken cancellationToken = default);
    Task<CertificateVerificationModel> VerifyAsync(string numberOrCode,
        CancellationToken cancellationToken = default);
    Task<CertificateFile?> DownloadAsync(int id, string userId, bool isAdmin, bool isTrainer,
        CancellationToken cancellationToken = default);
    Task UpdateExpiredStatusesAsync(CancellationToken cancellationToken = default);
}
