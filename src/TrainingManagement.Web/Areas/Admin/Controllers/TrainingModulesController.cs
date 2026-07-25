using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Modules;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Modules;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class TrainingModulesController(ITrainingModuleService service) : Controller
{
    public async Task<IActionResult> Index(int trainingId)
    {
        var collection = await service.GetByTrainingAsync(trainingId);
        return collection is null ? NotFound() : View(new TrainingModuleIndexViewModel { Collection = collection });
    }

    public async Task<IActionResult> Details(int id)
    {
        var module = await service.GetByIdAsync(id);
        return module is null ? NotFound() : View(new TrainingModuleDetailsViewModel { Module = module });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int trainingId)
    {
        if (await service.GetByTrainingAsync(trainingId) is null) return NotFound();
        return View(new TrainingModuleCreateViewModel { TrainingId = trainingId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingModuleCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await service.CreateAsync(new(model.TrainingId, model.Title, model.Slug, model.Description, model.Order));
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View(model); }
        TempData["SuccessMessage"] = "Le module a été créé.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await service.GetByIdAsync(id);
        if (item is null) return NotFound();
        return View(new TrainingModuleEditViewModel
        {
            Id = item.Id, TrainingId = item.TrainingId, Title = item.Title,
            Slug = item.Slug, Description = item.Description, Order = item.Order
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainingModuleEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var result = await service.UpdateAsync(new(model.Id, model.TrainingId, model.Title,
            model.Slug, model.Description, model.Order!.Value));
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View(model); }
        TempData["SuccessMessage"] = "Le module a été modifié.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Publish(int id) => ApplyAsync(id, () => service.PublishAsync(id), "Le module est publié.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Unpublish(int id) => ApplyAsync(id, () => service.UnpublishAsync(id), "Le module est dépublié.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Archive(int id) => ApplyAsync(id, () => service.ArchiveAsync(id), "Le module est archivé.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Delete(int id) => ApplyAsync(id, () => service.DeleteAsync(id), "Le module est supprimé.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveUp(int id) => ApplyAsync(id, () => service.MoveUpAsync(id), "Le module a été déplacé.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveDown(int id) => ApplyAsync(id, () => service.MoveDownAsync(id), "Le module a été déplacé.");

    private async Task<IActionResult> ApplyAsync(int id, Func<Task<ServiceResult>> operation, string success)
    {
        var before = await service.GetByIdAsync(id);
        if (before is null) return NotFound();
        var result = await operation();
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? success : result.Error;
        return RedirectToAction(nameof(Index), new { trainingId = before.TrainingId });
    }
}
