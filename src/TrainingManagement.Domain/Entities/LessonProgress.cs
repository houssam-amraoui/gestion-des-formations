using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class LessonProgress
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public LessonProgressStatus Status { get; set; }
    public DateTime? FirstAccessedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeSpentSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public void RecordAccess(DateTime utcNow)
    {
        FirstAccessedAt ??= utcNow;
        LastAccessedAt = utcNow;
        if (Status == LessonProgressStatus.NotStarted) Status = LessonProgressStatus.InProgress;
        UpdatedAt = utcNow;
    }

    public void Complete(DateTime utcNow)
    {
        Status = LessonProgressStatus.Completed;
        FirstAccessedAt ??= utcNow;
        LastAccessedAt = utcNow;
        CompletedAt ??= utcNow;
        UpdatedAt = utcNow;
    }
}
