using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class Training
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(500)]
    public string ShortDescription { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string? TrainerId { get; set; }
    public TrainingLevel Level { get; set; }
    [Required, MaxLength(50)]
    public string Language { get; set; } = string.Empty;
    public int EstimatedDurationHours { get; set; }
    public decimal Price { get; set; }
    public bool IsFree { get; set; }
    public TrainingStatus Status { get; set; } = TrainingStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<TrainingModule> Modules { get; set; } = new List<TrainingModule>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public bool RequireAllLessonsCompleted { get; set; } = true;
    public bool RequireAllMandatoryAssessmentsPassed { get; set; } = true;
    public decimal? MinimumAverageScore { get; set; }
    public bool CertificateEnabled { get; set; } = true;
    public int? CertificateValidityMonths { get; set; }
    [MaxLength(100)] public string? CertificateTemplateName { get; set; }

    public void ApplyPricing()
    {
        if (IsFree) Price = 0;
    }

    public void Publish(DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(Title)) throw new InvalidOperationException("Le titre est obligatoire.");
        if (string.IsNullOrWhiteSpace(Description)) throw new InvalidOperationException("La description est obligatoire.");
        if (EstimatedDurationHours <= 0) throw new InvalidOperationException("La durée doit être supérieure à zéro.");
        if (Category is null || !Category.IsActive) throw new InvalidOperationException("La catégorie doit être active.");
        Status = TrainingStatus.Published;
        PublishedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Unpublish(DateTime utcNow)
    {
        if (Status == TrainingStatus.Archived) throw new InvalidOperationException("Une formation archivée ne peut pas être dépubliée.");
        Status = TrainingStatus.Unpublished;
        UpdatedAt = utcNow;
    }

    public void Archive(DateTime utcNow)
    {
        Status = TrainingStatus.Archived;
        UpdatedAt = utcNow;
    }
}
