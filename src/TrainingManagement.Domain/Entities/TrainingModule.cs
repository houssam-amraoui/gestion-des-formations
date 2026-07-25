using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class TrainingModule
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public void Publish(DateTime utcNow)
    {
        if (Training.Status == TrainingStatus.Archived)
            throw new InvalidOperationException("Un module ne peut pas être publié dans une formation archivée.");
        if (IsArchived) throw new InvalidOperationException("Un module archivé ne peut pas être publié.");
        IsPublished = true;
        UpdatedAt = utcNow;
    }
}
