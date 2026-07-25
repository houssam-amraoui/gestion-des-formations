using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class Assessment
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(220)] public string Slug { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    public AssessmentType AssessmentType { get; set; }
    public int Order { get; set; }
    public decimal PassingScore { get; set; }
    public int? MaximumAttempts { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShowCorrectAnswers { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<AssessmentAttempt> Attempts { get; set; } = new List<AssessmentAttempt>();

    public void Publish(DateTime utcNow)
    {
        if (IsArchived) throw new InvalidOperationException("Une évaluation archivée ne peut pas être publiée.");
        if (Lesson.IsArchived || Lesson.TrainingModule.IsArchived ||
            Lesson.TrainingModule.Training.Status == TrainingStatus.Archived)
            throw new InvalidOperationException("L’évaluation appartient à un élément archivé.");
        if (!Questions.Any(question => question.IsPublished))
            throw new InvalidOperationException("L’évaluation doit contenir au moins une question publiée.");
        if (AssessmentType == AssessmentType.Exam &&
            !Questions.Any(question => question.IsPublished && question.Points > 0))
            throw new InvalidOperationException("Un examen doit contenir au moins une question publiée et notée.");
        IsPublished = true;
        UpdatedAt = utcNow;
    }
}
