using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Domain.Entities;

public sealed class Category
{
    public int Id { get; set; }
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(180)]
    public string Slug { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; set; }
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Training> Trainings { get; set; } = new List<Training>();
}
