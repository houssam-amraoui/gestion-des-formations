using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.Modules;

namespace TrainingManagement.Web.ViewModels.Modules;

public sealed class TrainingModuleIndexViewModel
{
    public required TrainingModuleCollectionModel Collection { get; init; }
}

public class TrainingModuleCreateViewModel
{
    [Range(1, int.MaxValue)]
    public int TrainingId { get; set; }
    [Required, StringLength(200), Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;
    [StringLength(220)]
    public string? Slug { get; set; }
    [StringLength(1000)]
    public string? Description { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Ordre")]
    public int? Order { get; set; }
}

public sealed class TrainingModuleEditViewModel : TrainingModuleCreateViewModel
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)]
    public new int? Order { get; set; }
}

public sealed class TrainingModuleDetailsViewModel
{
    public required TrainingModuleDetailsModel Module { get; init; }
}
