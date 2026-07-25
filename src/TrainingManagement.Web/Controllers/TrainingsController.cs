using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrainingManagement.Application.Categories;
using TrainingManagement.Application.Trainings;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Web.ViewModels.Trainings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Controllers;

[Route("Trainings")]
public sealed class TrainingsController(
    ITrainingService trainingService,
    ICategoryService categoryService,
    IPedagogyReadService pedagogyService,
    IEnrollmentService enrollments,
    UserManager<ApplicationUser> users) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int? categoryId, TrainingLevel? level, int page = 1)
    {
        var results = await trainingService.GetPublishedPagedAsync(new(search, categoryId, level, page));
        var categories = (await categoryService.GetOptionsAsync(true))
            .Select(item => new SelectListItem(item.Name, item.Id.ToString())).ToArray();
        return View(new PublicTrainingListViewModel
        {
            Search = search, CategoryId = categoryId, Level = level,
            Page = page, Results = results, Categories = categories
        });
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var training = await trainingService.GetPublishedBySlugAsync(slug);
        if (training is null) return NotFound();
        var learnerId = users.GetUserId(User);
        var isLearner = User.IsInRole(AppRoles.Learner);
        return View(new TrainingDetailsViewModel
        {
            Training = training,
            Curriculum = await pedagogyService.GetPublicCurriculumAsync(training.Id),
            IsLearner = isLearner,
            HasEnrollmentAccess = isLearner && learnerId is not null &&
                await enrollments.CanAccessTrainingAsync(training.Id, learnerId)
        });
    }

    [HttpPost("{id:int}/Enroll"), Authorize(Roles = AppRoles.Learner), ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int id, CancellationToken token)
    {
        var learnerId = users.GetUserId(User);
        if (learnerId is null) return Challenge();
        var result = await enrollments.EnrollFreeAsync(id, learnerId, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Votre inscription est active." : result.Error;
        return result.Succeeded
            ? RedirectToAction("Details", "Trainings", new { area = "Learner", id })
            : RedirectToAction(nameof(Index));
    }
}
