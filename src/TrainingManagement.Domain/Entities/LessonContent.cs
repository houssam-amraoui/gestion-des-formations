using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class LessonContent
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    [MaxLength(200)]
    public string? Title { get; set; }
    public LessonContentType ContentType { get; set; }
    public string? TextContent { get; set; }
    [MaxLength(1000)]
    public string? ExternalUrl { get; set; }
    [MaxLength(1000)]
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
