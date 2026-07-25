using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.AnswerOptions;

namespace TrainingManagement.Web.ViewModels.AnswerOptions;

public sealed class AnswerOptionIndexViewModel { public required AnswerOptionCollectionModel Collection { get; init; } }
public class AnswerOptionCreateViewModel
{
    [Range(1, int.MaxValue)] public int QuestionId { get; set; }
    [Required, StringLength(1000), Display(Name = "Réponse")] public string Text { get; set; } = string.Empty;
    [Range(1, int.MaxValue), Display(Name = "Ordre")] public int? Order { get; set; }
    [Display(Name = "Bonne réponse")] public bool IsCorrect { get; set; }
}
public sealed class AnswerOptionEditViewModel : AnswerOptionCreateViewModel
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)] public new int? Order { get; set; }
}
