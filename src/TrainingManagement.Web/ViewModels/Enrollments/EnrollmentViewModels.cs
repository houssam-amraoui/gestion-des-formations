using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Web.ViewModels.Enrollments;

public sealed class EnrollmentIndexViewModel
{
    public string? Search { get; set; }
    public int? TrainingId { get; set; }
    public EnrollmentStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public required EnrollmentListResult Results { get; init; }
    public IReadOnlyCollection<SelectListItem> Trainings { get; init; } = [];
}
public sealed class EnrollmentCreateViewModel
{
    [Required, Display(Name="Apprenant")] public string LearnerId { get; set; } = string.Empty;
    [Range(1,int.MaxValue), Display(Name="Formation")] public int TrainingId { get; set; }
    [Display(Name="Activer immédiatement")] public bool ActivateImmediately { get; set; } = true;
    public IReadOnlyCollection<SelectListItem> Learners { get; set; } = [];
    public IReadOnlyCollection<SelectListItem> Trainings { get; set; } = [];
}
public sealed class EnrollmentDetailsViewModel { public required EnrollmentDetailsModel Enrollment { get; init; } }
