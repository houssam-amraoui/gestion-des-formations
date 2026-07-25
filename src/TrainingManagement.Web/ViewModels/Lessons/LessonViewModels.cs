using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.Lessons;
using TrainingManagement.Application.Pedagogy;

namespace TrainingManagement.Web.ViewModels.Lessons;

public sealed class LessonIndexViewModel
{
    public required LessonCollectionModel Collection { get; init; }
}

public class LessonCreateViewModel
{
    [Range(1, int.MaxValue)]
    public int TrainingModuleId { get; set; }
    [Required, StringLength(200), Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;
    [StringLength(220)]
    public string? Slug { get; set; }
    [StringLength(1000), Display(Name = "Résumé")]
    public string? Summary { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Ordre")]
    public int? Order { get; set; }
    [Range(0, 100000), Display(Name = "Durée estimée (minutes)")]
    public int EstimatedDurationMinutes { get; set; }
    [Display(Name = "Aperçu gratuit")]
    public bool IsPreview { get; set; }
}

public sealed class LessonEditViewModel : LessonCreateViewModel
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)]
    public new int? Order { get; set; }
}

public sealed class LessonDetailsViewModel
{
    public required LessonDetailsModel Lesson { get; init; }
}

public sealed class LessonPageViewModel
{
    public required PublicLessonPage Lesson { get; init; }
    public bool IsAdminPreview { get; init; }
    public IReadOnlyCollection<TrainingManagement.Application.Assessments.PublicAssessmentCard> Assessments { get; init; }
        = Array.Empty<TrainingManagement.Application.Assessments.PublicAssessmentCard>();
    public bool IsEnrolledAccess { get; init; }
    public bool IsCompleted { get; init; }
}
