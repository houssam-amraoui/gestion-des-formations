using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Web.ViewModels.Lessons;

namespace TrainingManagement.Web.Controllers;

[Route("Trainings/{trainingSlug}/Modules/{moduleSlug}/Lessons")]
public sealed class LessonsController(IPedagogyReadService service, IAssessmentService assessments) : Controller
{
    [HttpGet("{lessonSlug}")]
    public async Task<IActionResult> Details(
        string trainingSlug, string moduleSlug, string lessonSlug, CancellationToken cancellationToken)
    {
        var lesson = await service.GetPublicLessonAsync(trainingSlug, moduleSlug, lessonSlug, cancellationToken);
        if (lesson is null) return NotFound();
        var available = lesson.ContentAccessible
            ? await assessments.GetPublicByLessonAsync(lesson.Id, cancellationToken)
            : Array.Empty<PublicAssessmentCard>();
        return View(new LessonPageViewModel { Lesson = lesson, Assessments = available });
    }
}
