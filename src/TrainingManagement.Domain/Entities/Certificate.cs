using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class Certificate
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    [Required, MaxLength(100)] public string CertificateNumber { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string VerificationCode { get; set; } = string.Empty;
    [Required, MaxLength(250)] public string LearnerFullNameSnapshot { get; set; } = string.Empty;
    [Required, MaxLength(250)] public string TrainingTitleSnapshot { get; set; } = string.Empty;
    [MaxLength(250)] public string? TrainerFullNameSnapshot { get; set; }
    public DateTime CompletionDate { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public CertificateStatus Status { get; set; } = CertificateStatus.Active;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByAdminId { get; set; }
    [MaxLength(1000)] public string? RevocationReason { get; set; }
    [MaxLength(255)] public string? PdfFileName { get; set; }
    [MaxLength(500)] public string? PdfRelativePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsValidAt(DateTime utcNow) =>
        Status == CertificateStatus.Active && (ExpiresAt is null || ExpiresAt > utcNow);

    public void RefreshExpiration(DateTime utcNow)
    {
        if (Status != CertificateStatus.Revoked && ExpiresAt <= utcNow)
            Status = CertificateStatus.Expired;
    }

    public void Revoke(string reason, string adminId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Une raison de révocation est obligatoire.");
        Status = CertificateStatus.Revoked;
        RevocationReason = reason.Trim();
        RevokedByAdminId = adminId;
        RevokedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Reactivate(DateTime utcNow)
    {
        if (ExpiresAt <= utcNow)
            throw new InvalidOperationException("Un certificat expiré ne peut pas être réactivé.");
        Status = CertificateStatus.Active;
        UpdatedAt = utcNow;
    }
}
