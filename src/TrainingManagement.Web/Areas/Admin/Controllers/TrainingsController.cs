using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrainingManagement.Application.Categories;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Trainings;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Trainings;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class TrainingsController(
    ITrainingService trainingService,
    ICategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] TrainingFilterViewModel filter)
    {
        var results = await trainingService.GetAdminPagedAsync(new(
            filter.Search, filter.CategoryId, filter.Level, filter.Status,
            filter.TrainerId, filter.Sort, filter.Page));
        return View(new TrainingListViewModel
        {
            Filter = filter, Results = results,
            Categories = await CategoryItemsAsync(false),
            Trainers = await TrainerItemsAsync()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var training = await trainingService.GetByIdAsync(id);
        return training is null ? NotFound() : View(new TrainingDetailsViewModel { Training = training });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new TrainingCreateViewModel();
        await PopulateAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await trainingService.CreateAsync(ToInput(model));
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "La formation a été créée en brouillon.";
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        await PopulateAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var training = await trainingService.GetByIdAsync(id);
        if (training is null) return NotFound();
        var model = new TrainingEditViewModel
        {
            Id = training.Id, Title = training.Title, Slug = training.Slug,
            ShortDescription = training.ShortDescription, Description = training.Description,
            ThumbnailUrl = training.ThumbnailUrl, CategoryId = training.CategoryId,
            TrainerId = training.TrainerId, Level = training.Level, Language = training.Language,
            EstimatedDurationHours = training.EstimatedDurationHours,
            Price = training.Price, IsFree = training.IsFree,
            RequireAllLessonsCompleted = training.RequireAllLessonsCompleted,
            RequireAllMandatoryAssessmentsPassed = training.RequireAllMandatoryAssessmentsPassed,
            MinimumAverageScore = training.MinimumAverageScore,
            CertificateEnabled = training.CertificateEnabled,
            CertificateValidityMonths = training.CertificateValidityMonths,
            CertificateTemplateName = training.CertificateTemplateName
        };
        await PopulateAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainingEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await trainingService.UpdateAsync(id, ToInput(model));
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "La formation a été mise à jour.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        await PopulateAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Publish(int id) => ChangeStatusAsync(trainingService.PublishAsync(id), "La formation est publiée.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Unpublish(int id) => ChangeStatusAsync(trainingService.UnpublishAsync(id), "La formation est dépubliée.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Archive(int id) => ChangeStatusAsync(trainingService.ArchiveAsync(id), "La formation est archivée.");

    private async Task<IActionResult> ChangeStatusAsync(Task<ServiceResult> operation, string success)
    {
        var result = await operation;
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? success : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAsync(TrainingCreateViewModel model)
    {
        model.Categories = await CategoryItemsAsync(true);
        model.Trainers = await TrainerItemsAsync();
    }

    private async Task<IReadOnlyCollection<SelectListItem>> CategoryItemsAsync(bool activeOnly) =>
        (await categoryService.GetOptionsAsync(activeOnly))
        .Select(item => new SelectListItem(item.Name, item.Id.ToString())).ToArray();

    private async Task<IReadOnlyCollection<SelectListItem>> TrainerItemsAsync() =>
        (await trainingService.GetTrainerOptionsAsync())
        .Select(item => new SelectListItem(item.DisplayName, item.Id)).ToArray();

    private static TrainingInput ToInput(TrainingCreateViewModel model) =>
        new(model.Title, model.Slug, model.ShortDescription, model.Description,
            model.ThumbnailUrl, model.CategoryId, model.TrainerId, model.Level,
            model.Language, model.EstimatedDurationHours, model.Price, model.IsFree,
            model.RequireAllLessonsCompleted, model.RequireAllMandatoryAssessmentsPassed,
            model.MinimumAverageScore, model.CertificateEnabled, model.CertificateValidityMonths,
            model.CertificateTemplateName);
}
