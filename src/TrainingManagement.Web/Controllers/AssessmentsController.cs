using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Web.ViewModels.Assessments;

namespace TrainingManagement.Web.Controllers;

[Route("Trainings/{trainingSlug}/Modules/{moduleSlug}/Lessons/{lessonSlug}/Assessments")]
public sealed class AssessmentsController(IAssessmentService service) : Controller
{
    [HttpGet("{assessmentSlug}")]
    public async Task<IActionResult> Details(string trainingSlug, string moduleSlug, string lessonSlug,
        string assessmentSlug, CancellationToken token)
    {
        var assessment = await service.GetPublicAsync(trainingSlug, moduleSlug, lessonSlug, assessmentSlug, token);
        return assessment is null ? NotFound() : View(new PublicAssessmentViewModel { Assessment = assessment });
    }
}
