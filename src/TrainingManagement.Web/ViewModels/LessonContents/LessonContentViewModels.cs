using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.LessonContents;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Web.ViewModels.LessonContents;

public sealed class LessonContentIndexViewModel
{
    public required LessonContentCollectionModel Collection { get; init; }
}

public class LessonContentCreateViewModel
{
    [Range(1, int.MaxValue)]
    public int LessonId { get; set; }
    [StringLength(200), Display(Name = "Titre")]
    public string? Title { get; set; }
    [Required, Display(Name = "Type de contenu")]
    public LessonContentType ContentType { get; set; } = LessonContentType.Text;
    [Display(Name = "Contenu texte")]
    public string? TextContent { get; set; }
    [StringLength(1000), Display(Name = "URL externe")]
    public string? ExternalUrl { get; set; }
    [StringLength(1000)]
    public string? Description { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Ordre")]
    public int? Order { get; set; }
}

public sealed class LessonContentEditViewModel : LessonContentCreateViewModel
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)]
    public new int? Order { get; set; }
}

public sealed class LessonContentDetailsViewModel
{
    public required LessonContentDetailsModel Content { get; init; }
}
