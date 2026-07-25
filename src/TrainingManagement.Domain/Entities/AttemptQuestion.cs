using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class AttemptQuestion
{
    public int Id { get; set; }
    public int AssessmentAttemptId { get; set; }
    public AssessmentAttempt AssessmentAttempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public decimal PointsSnapshot { get; set; }
    [Required] public string StatementSnapshot { get; set; } = string.Empty;
    public QuestionType QuestionTypeSnapshot { get; set; }
    public string? ExplanationSnapshot { get; set; }
    public string? ExpectedAnswerSnapshot { get; set; }
    public string? AnswerOptionsSnapshotJson { get; set; }
    public string? CorrectAnswerOptionIdsSnapshot { get; set; }
    public ICollection<LearnerAnswer> Answers { get; set; } = new List<LearnerAnswer>();
}
