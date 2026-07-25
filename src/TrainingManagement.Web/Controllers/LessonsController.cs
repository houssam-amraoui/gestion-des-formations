using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Application.Assessments;
using Microsoft.AspNetCore.Identity;
using TrainingManagement.Application.Progress;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Lessons;

namespace TrainingManagement.Web.Controllers;

[Route("Trainings/{trainingSlug}/Modules/{moduleSlug}/Lessons")]
public sealed class LessonsController(IPedagogyReadService service, IAssessmentService assessments,
    ILessonProgressService progress, UserManager<ApplicationUser> users) : Controller
{
    [HttpGet("{lessonSlug}")]
    public async Task<IActionResult> Details(
        string trainingSlug, string moduleSlug, string lessonSlug, CancellationToken cancellationToken)
    {
        var learnerId = users.GetUserId(User);
        var enrolled = User.IsInRole(AppRoles.Learner) && learnerId is not null;
        var lesson = enrolled
            ? await service.GetEnrolledLessonAsync(trainingSlug, moduleSlug, lessonSlug, learnerId!, cancellationToken)
            : null;
        var enrolledAccess = lesson is not null;
        lesson ??= await service.GetPublicLessonAsync(trainingSlug, moduleSlug, lessonSlug, cancellationToken);
        if (lesson is null) return NotFound();
        if (enrolledAccess) await progress.RecordAccessAsync(lesson.Id, learnerId!, cancellationToken);
        var available = enrolledAccess
            ? await assessments.GetAccessibleByLessonAsync(lesson.Id, learnerId!, cancellationToken)
            : lesson.ContentAccessible
                ? await assessments.GetPublicByLessonAsync(lesson.Id, cancellationToken)
            : Array.Empty<PublicAssessmentCard>();
        return View(new LessonPageViewModel { Lesson = lesson, Assessments = available, IsEnrolledAccess = enrolledAccess });
    }
}
