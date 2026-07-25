using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Domain.Entities;

public sealed class LearnerAnswer
{
    public int Id { get; set; }
    public int AttemptQuestionId { get; set; }
    public AttemptQuestion AttemptQuestion { get; set; } = null!;
    public int? AnswerOptionId { get; set; }
    [MaxLength(2000)] public string? TextAnswer { get; set; }
    [MaxLength(1000)] public string? AnswerTextSnapshot { get; set; }
    public bool? WasCorrectSnapshot { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? PointsAwarded { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
