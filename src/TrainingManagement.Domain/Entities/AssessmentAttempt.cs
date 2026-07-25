using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class AssessmentAttempt
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public int AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal? Score { get; set; }
    public decimal MaximumScore { get; set; }
    public decimal? PercentageScore { get; set; }
    public bool? Passed { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public ICollection<AttemptQuestion> Questions { get; set; } = new List<AttemptQuestion>();
}
