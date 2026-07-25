using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Web.ViewModels.Lessons;

namespace TrainingManagement.Web.Controllers;

[Route("Trainings/{trainingSlug}/Modules/{moduleSlug}/Lessons")]
public sealed class LessonsController(IPedagogyReadService service) : Controller
{
    [HttpGet("{lessonSlug}")]
    public async Task<IActionResult> Details(
        string trainingSlug, string moduleSlug, string lessonSlug, CancellationToken cancellationToken)
    {
        var lesson = await service.GetPublicLessonAsync(trainingSlug, moduleSlug, lessonSlug, cancellationToken);
        return lesson is null ? NotFound() : View(new LessonPageViewModel { Lesson = lesson });
    }
}
