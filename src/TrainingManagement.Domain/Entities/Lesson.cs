using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class Lesson
{
    public int Id { get; set; }
    public int TrainingModuleId { get; set; }
    public TrainingModule TrainingModule { get; set; } = null!;
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Summary { get; set; }
    public int Order { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public bool IsPreview { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<LessonContent> Contents { get; set; } = new List<LessonContent>();

    public void Publish(DateTime utcNow)
    {
        if (!TrainingModule.IsPublished)
            throw new InvalidOperationException("La leçon ne peut être publiée que dans un module publié.");
        if (TrainingModule.Training.Status == TrainingStatus.Archived)
            throw new InvalidOperationException("La formation est archivée.");
        if (IsArchived) throw new InvalidOperationException("Une leçon archivée ne peut pas être publiée.");
        if (Contents.Count == 0)
            throw new InvalidOperationException("Une leçon publiée doit contenir au moins un bloc de contenu.");
        IsPublished = true;
        UpdatedAt = utcNow;
    }
}
