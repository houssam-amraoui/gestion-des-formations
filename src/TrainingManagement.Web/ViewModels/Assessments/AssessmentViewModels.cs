using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Web.ViewModels.Assessments;

public sealed class AssessmentIndexViewModel { public required AssessmentCollectionModel Collection { get; init; } }
public class AssessmentCreateViewModel
{
    [Range(1, int.MaxValue)] public int LessonId { get; set; }
    [Required, StringLength(200), Display(Name = "Titre")] public string Title { get; set; } = string.Empty;
    [StringLength(220)] public string? Slug { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    [Display(Name = "Type")] public AssessmentType AssessmentType { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Ordre")] public int? Order { get; set; }
    [Range(0, 100), Display(Name = "Note minimale (%)")] public decimal PassingScore { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Tentatives maximales")] public int? MaximumAttempts { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Limite de temps (minutes)")] public int? TimeLimitMinutes { get; set; }
    [Display(Name = "Mélanger les questions")] public bool ShuffleQuestions { get; set; }
    [Display(Name = "Afficher les bonnes réponses")] public bool ShowCorrectAnswers { get; set; }
}
public sealed class AssessmentEditViewModel : AssessmentCreateViewModel
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)] public new int? Order { get; set; }
}
public sealed class AssessmentDetailsViewModel { public required AssessmentDetailsModel Assessment { get; init; } }
public sealed class AssessmentPreviewViewModel { public required AssessmentPreviewModel Preview { get; init; } public bool IsTrainer { get; init; } }
public sealed class PublicAssessmentViewModel { public required PublicAssessmentPage Assessment { get; init; } }
