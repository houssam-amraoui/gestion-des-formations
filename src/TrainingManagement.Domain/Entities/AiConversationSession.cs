using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class AiConversationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int AiTrainerProfileId { get; set; }
    public AiTrainerProfile AiTrainerProfile { get; set; } = null!;
    public int? EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    [Required] public string UserId { get; set; } = string.Empty;
    [MaxLength(500)] public string? ProviderSessionId { get; set; }
    public AiConversationStatus Status { get; set; } = AiConversationStatus.Starting;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int MessageCount { get; set; }
    public int InputAudioSeconds { get; set; }
    public int OutputAudioSeconds { get; set; }
    public decimal? EstimatedCost { get; set; }
    [MaxLength(2000)] public string? FailureReason { get; set; }
    public ICollection<AiConversationMessage> Messages { get; set; } = new List<AiConversationMessage>();
    public ICollection<AiProviderUsageRecord> UsageRecords { get; set; } = new List<AiProviderUsageRecord>();

    public bool IsExpired(DateTime utcNow) => Status == AiConversationStatus.Expired || utcNow >= ExpiresAt;

    public void Activate(DateTime utcNow)
    {
        Status = AiConversationStatus.Active;
        LastActivityAt = utcNow;
    }

    public void RegisterMessage(DateTime utcNow, int maximumMessages)
    {
        EnsureAcceptsMessages(utcNow, maximumMessages);
        MessageCount++;
        LastActivityAt = utcNow;
    }

    public void EnsureAcceptsMessages(DateTime utcNow, int maximumMessages)
    {
        if (utcNow >= ExpiresAt)
        {
            Status = AiConversationStatus.Expired;
            EndedAt ??= utcNow;
            throw new InvalidOperationException("Cette session a expiré.");
        }
        if (Status != AiConversationStatus.Active)
            throw new InvalidOperationException("Cette session ne peut plus recevoir de message.");
        if (MessageCount >= maximumMessages)
            throw new InvalidOperationException("Le nombre maximal de messages est atteint.");
    }

    public void End(DateTime utcNow, bool cancelled = false)
    {
        if (Status is AiConversationStatus.Completed or AiConversationStatus.Cancelled) return;
        Status = cancelled ? AiConversationStatus.Cancelled : AiConversationStatus.Completed;
        EndedAt = utcNow;
        LastActivityAt = utcNow;
    }

    public void Fail(string reason, DateTime utcNow)
    {
        Status = AiConversationStatus.Failed;
        FailureReason = reason.Length > 2000 ? reason[..2000] : reason;
        EndedAt = utcNow;
        LastActivityAt = utcNow;
    }
}
