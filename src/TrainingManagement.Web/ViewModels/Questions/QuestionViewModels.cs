using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.Questions;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Web.ViewModels.Questions;

public sealed class QuestionIndexViewModel { public required QuestionCollectionModel Collection { get; init; } }
public class QuestionCreateViewModel
{
    [Range(1, int.MaxValue)] public int AssessmentId { get; set; }
    [Display(Name = "Type")] public QuestionType QuestionType { get; set; }
    [Required, Display(Name = "Énoncé")] public string Statement { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Ordre")] public int? Order { get; set; }
    [Range(0, 100000), Display(Name = "Points")] public decimal Points { get; set; }
    [StringLength(2000), Display(Name = "Réponse attendue")] public string? ExpectedAnswer { get; set; }
}
public sealed class QuestionEditViewModel : QuestionCreateViewModel
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)] public new int? Order { get; set; }
}
public sealed class QuestionDetailsViewModel { public required QuestionDetailsModel Question { get; init; } }
