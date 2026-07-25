using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrainingManagement.Application.Categories;
using TrainingManagement.Application.Trainings;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Web.ViewModels.Trainings;

namespace TrainingManagement.Web.Controllers;

[Route("Trainings")]
public sealed class TrainingsController(
    ITrainingService trainingService,
    ICategoryService categoryService,
    IPedagogyReadService pedagogyService) : Controller
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
        return training is null ? NotFound() : View(new TrainingDetailsViewModel
        {
            Training = training,
            Curriculum = await pedagogyService.GetPublicCurriculumAsync(training.Id)
        });
    }
}
