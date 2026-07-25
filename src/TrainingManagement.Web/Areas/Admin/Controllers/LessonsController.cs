using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Lessons;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Lessons;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class LessonsController(ILessonService service, IPedagogyReadService pedagogy) : Controller
{
    public async Task<IActionResult> Index(int moduleId)
    {
        var collection = await service.GetByModuleAsync(moduleId);
        return collection is null ? NotFound() : View(new LessonIndexViewModel { Collection = collection });
    }

    public async Task<IActionResult> Details(int id)
    {
        var lesson = await service.GetByIdAsync(id);
        return lesson is null ? NotFound() : View(new LessonDetailsViewModel { Lesson = lesson });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int moduleId)
    {
        if (await service.GetByModuleAsync(moduleId) is null) return NotFound();
        return View(new LessonCreateViewModel { TrainingModuleId = moduleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LessonCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await service.CreateAsync(new(model.TrainingModuleId, model.Title, model.Slug,
            model.Summary, model.Order, model.EstimatedDurationMinutes, model.IsPreview));
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View(model); }
        TempData["SuccessMessage"] = "La leçon a été créée.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await service.GetByIdAsync(id);
        if (item is null) return NotFound();
        return View(new LessonEditViewModel
        {
            Id = item.Id, TrainingModuleId = item.TrainingModuleId, Title = item.Title,
            Slug = item.Slug, Summary = item.Summary, Order = item.Order,
            EstimatedDurationMinutes = item.EstimatedDurationMinutes, IsPreview = item.IsPreview
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LessonEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var result = await service.UpdateAsync(new(model.Id, model.TrainingModuleId, model.Title,
            model.Slug, model.Summary, model.Order!.Value, model.EstimatedDurationMinutes, model.IsPreview));
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View(model); }
        TempData["SuccessMessage"] = "La leçon a été modifiée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Preview(int id)
    {
        var lesson = await pedagogy.GetAdminPreviewAsync(id);
        return lesson is null ? NotFound() : View("Preview", new LessonPageViewModel { Lesson = lesson, IsAdminPreview = true });
    }

    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Publish(int id) => ApplyAsync(id, () => service.PublishAsync(id), "La leçon est publiée.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Unpublish(int id) => ApplyAsync(id, () => service.UnpublishAsync(id), "La leçon est dépubliée.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Archive(int id) => ApplyAsync(id, () => service.ArchiveAsync(id), "La leçon est archivée.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Delete(int id) => ApplyAsync(id, () => service.DeleteAsync(id), "La leçon est supprimée.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveUp(int id) => ApplyAsync(id, () => service.MoveUpAsync(id), "La leçon a été déplacée.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveDown(int id) => ApplyAsync(id, () => service.MoveDownAsync(id), "La leçon a été déplacée.");

    private async Task<IActionResult> ApplyAsync(int id, Func<Task<ServiceResult>> operation, string success)
    {
        var before = await service.GetByIdAsync(id);
        if (before is null) return NotFound();
        var result = await operation();
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? success : result.Error;
        return RedirectToAction(nameof(Index), new { moduleId = before.TrainingModuleId });
    }
}
