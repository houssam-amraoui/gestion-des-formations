using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.LessonContents;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.LessonContents;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class LessonContentsController(ILessonContentService service) : Controller
{
    public async Task<IActionResult> Index(int lessonId)
    {
        var collection = await service.GetByLessonAsync(lessonId);
        return collection is null ? NotFound() : View(new LessonContentIndexViewModel { Collection = collection });
    }

    public async Task<IActionResult> Details(int id)
    {
        var content = await service.GetByIdAsync(id);
        return content is null ? NotFound() : View(new LessonContentDetailsViewModel { Content = content });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int lessonId)
    {
        if (await service.GetByLessonAsync(lessonId) is null) return NotFound();
        return View(new LessonContentCreateViewModel { LessonId = lessonId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LessonContentCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await service.CreateAsync(new(model.LessonId, model.Title, model.ContentType,
            model.TextContent, model.ExternalUrl, model.Description, model.Order));
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View(model); }
        TempData["SuccessMessage"] = "Le bloc de contenu a été créé.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await service.GetByIdAsync(id);
        if (item is null) return NotFound();
        return View(new LessonContentEditViewModel
        {
            Id = item.Id, LessonId = item.LessonId, Title = item.Title, ContentType = item.ContentType,
            TextContent = item.TextContent, ExternalUrl = item.ExternalUrl,
            Description = item.Description, Order = item.Order
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LessonContentEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var result = await service.UpdateAsync(new(model.Id, model.LessonId, model.Title,
            model.ContentType, model.TextContent, model.ExternalUrl, model.Description, model.Order!.Value));
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View(model); }
        TempData["SuccessMessage"] = "Le bloc de contenu a été modifié.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Publish(int id) => ApplyAsync(id, () => service.PublishAsync(id), "Le contenu est publié.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Unpublish(int id) => ApplyAsync(id, () => service.UnpublishAsync(id), "Le contenu est dépublié.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Delete(int id) => ApplyAsync(id, () => service.DeleteAsync(id), "Le contenu est supprimé.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveUp(int id) => ApplyAsync(id, () => service.MoveUpAsync(id), "Le contenu a été déplacé.");
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveDown(int id) => ApplyAsync(id, () => service.MoveDownAsync(id), "Le contenu a été déplacé.");

    private async Task<IActionResult> ApplyAsync(int id, Func<Task<ServiceResult>> operation, string success)
    {
        var before = await service.GetByIdAsync(id);
        if (before is null) return NotFound();
        var result = await operation();
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? success : result.Error;
        return RedirectToAction(nameof(Index), new { lessonId = before.LessonId });
    }
}
