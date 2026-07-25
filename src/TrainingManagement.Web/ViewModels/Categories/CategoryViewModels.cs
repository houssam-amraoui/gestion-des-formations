using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.Categories;
using TrainingManagement.Application.Common;

namespace TrainingManagement.Web.ViewModels.Categories;

public sealed class CategoryListViewModel
{
    public string? Search { get; set; }
    public required PagedResult<CategorySummary> Results { get; init; }
}

public class CategoryCreateViewModel
{
    [Required, StringLength(150), Display(Name = "Nom")]
    public string Name { get; set; } = string.Empty;
    [StringLength(180)]
    public string? Slug { get; set; }
    [StringLength(1000)]
    public string? Description { get; set; }
    [StringLength(500), Url(ErrorMessage = "L’URL de l’image est invalide."), Display(Name = "URL de l’image")]
    public string? ImageUrl { get; set; }
}

public sealed class CategoryEditViewModel : CategoryCreateViewModel
{
    public int Id { get; set; }
}

public sealed class CategoryDetailsViewModel
{
    public required CategoryDetails Category { get; init; }
}
