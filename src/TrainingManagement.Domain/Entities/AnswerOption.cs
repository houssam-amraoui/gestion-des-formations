using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Domain.Entities;

public sealed class AnswerOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    [Required, MaxLength(1000)] public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
