using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Trainings;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Application.Pedagogy;

namespace TrainingManagement.Web.ViewModels.Trainings;

public sealed class TrainingFilterViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public TrainingLevel? Level { get; set; }
    public TrainingStatus? Status { get; set; }
    public string? TrainerId { get; set; }
    public string Sort { get; set; } = "created_desc";
    public int Page { get; set; } = 1;
}

public sealed class TrainingListViewModel
{
    public required TrainingFilterViewModel Filter { get; init; }
    public required PagedResult<TrainingSummary> Results { get; init; }
    public required IReadOnlyCollection<SelectListItem> Categories { get; init; }
    public required IReadOnlyCollection<SelectListItem> Trainers { get; init; }
}

public class TrainingCreateViewModel
{
    [Required, StringLength(200), Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;
    [StringLength(220)]
    public string? Slug { get; set; }
    [Required, StringLength(500), Display(Name = "Description courte")]
    public string ShortDescription { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [StringLength(500), Url, Display(Name = "URL de la miniature")]
    public string? ThumbnailUrl { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Catégorie")]
    public int CategoryId { get; set; }
    [Display(Name = "Formateur")]
    public string? TrainerId { get; set; }
    [Required, Display(Name = "Niveau")]
    public TrainingLevel Level { get; set; } = TrainingLevel.Beginner;
    [Required, StringLength(50), Display(Name = "Langue")]
    public string Language { get; set; } = "Français";
    [Range(1, 10000), Display(Name = "Durée estimée (heures)")]
    public int EstimatedDurationHours { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999"), Display(Name = "Prix")]
    public decimal Price { get; set; }
    [Display(Name = "Formation gratuite")]
    public bool IsFree { get; set; }
    [Display(Name = "Exiger toutes les leçons terminées")]
    public bool RequireAllLessonsCompleted { get; set; } = true;
    [Display(Name = "Exiger toutes les évaluations obligatoires réussies")]
    public bool RequireAllMandatoryAssessmentsPassed { get; set; } = true;
    [Range(typeof(decimal), "0", "100"), Display(Name = "Moyenne minimale (%)")]
    public decimal? MinimumAverageScore { get; set; }
    [Display(Name = "Activer les certificats")]
    public bool CertificateEnabled { get; set; } = true;
    [Range(1, 1200), Display(Name = "Validité du certificat (mois)")]
    public int? CertificateValidityMonths { get; set; }
    [StringLength(100), Display(Name = "Modèle de certificat")]
    public string? CertificateTemplateName { get; set; }
    public IReadOnlyCollection<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyCollection<SelectListItem> Trainers { get; set; } = [];
}

public sealed class TrainingEditViewModel : TrainingCreateViewModel
{
    public int Id { get; set; }
}

public sealed class TrainingDetailsViewModel
{
    public required TrainingDetails Training { get; init; }
    public PublicCurriculum Curriculum { get; init; } = new([]);
    public bool HasEnrollmentAccess { get; init; }
    public bool IsLearner { get; init; }
}

public sealed class PublicTrainingListViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public TrainingLevel? Level { get; set; }
    public int Page { get; set; } = 1;
    public required PagedResult<TrainingSummary> Results { get; init; }
    public required IReadOnlyCollection<SelectListItem> Categories { get; init; }
}
