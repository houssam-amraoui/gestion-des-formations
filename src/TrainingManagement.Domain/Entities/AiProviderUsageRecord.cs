using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Domain.Entities;

public sealed class AiProviderUsageRecord
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public AiConversationSession Session { get; set; } = null!;
    [Required, MaxLength(100)] public string Provider { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Operation { get; set; } = string.Empty;
    public int? InputUnits { get; set; }
    public int? OutputUnits { get; set; }
    public int? AudioSeconds { get; set; }
    public decimal? EstimatedCost { get; set; }
    public bool Succeeded { get; set; }
    [MaxLength(200)] public string? ErrorCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
