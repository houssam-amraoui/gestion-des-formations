using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class AiConversationMessage
{
    public long Id { get; set; }
    public Guid AiConversationSessionId { get; set; }
    public AiConversationSession AiConversationSession { get; set; } = null!;
    public AiMessageRole Role { get; set; }
    [Required] public string TextContent { get; set; } = string.Empty;
    public string? TranscriptionText { get; set; }
    [MaxLength(500)] public string? AudioStoragePath { get; set; }
    [MaxLength(500)] public string? ProviderMessageId { get; set; }
    public int SequenceNumber { get; set; }
    public int? TokenCount { get; set; }
    public int? AudioDurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsModerated { get; set; }
    [MaxLength(1000)] public string? ModerationReason { get; set; }
}
