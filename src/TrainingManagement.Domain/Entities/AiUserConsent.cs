using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Domain.Entities;

public sealed class AiUserConsent
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ConsentType { get; set; } = "AudioProcessing";
    [Required, MaxLength(100)] public string Provider { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    [Required, MaxLength(50)] public string PolicyVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsValid(string provider, string policyVersion) =>
        RevokedAt is null && string.Equals(Provider, provider, StringComparison.OrdinalIgnoreCase) &&
        PolicyVersion == policyVersion;
}
