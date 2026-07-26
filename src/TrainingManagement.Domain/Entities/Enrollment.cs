using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class Enrollment
{
    public int Id { get; set; }
    public string LearnerId { get; set; } = string.Empty;
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal ProgressPercentage { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public string? CreatedByAdminId { get; set; }
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    public ICollection<AssessmentAttempt> Attempts { get; set; } = new List<AssessmentAttempt>();
    public Certificate? Certificate { get; set; }

    public bool AllowsAccess => Status is EnrollmentStatus.Active or EnrollmentStatus.Completed;

    public void Activate(DateTime utcNow)
    {
        if (Status == EnrollmentStatus.Completed) return;
        Status = EnrollmentStatus.Active;
        StartedAt ??= utcNow;
        CancelledAt = null;
    }

    public void Suspend() { if (Status != EnrollmentStatus.Completed) Status = EnrollmentStatus.Suspended; }
    public void Cancel(DateTime utcNow) { if (Status != EnrollmentStatus.Completed) { Status = EnrollmentStatus.Cancelled; CancelledAt = utcNow; } }

    public void SetProgress(decimal percentage, DateTime utcNow)
    {
        ProgressPercentage = Math.Clamp(Math.Round(percentage, 2), 0, 100);
        if (ProgressPercentage == 100)
        {
            Status = EnrollmentStatus.Completed;
            CompletedAt ??= utcNow;
        }
    }
}
