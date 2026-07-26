using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Assessments;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class AssessmentsController(IAssessmentService service) : Controller
{
    public async Task<IActionResult> Index(int lessonId, CancellationToken token)
    {
        var collection = await service.GetByLessonAsync(lessonId, token);
        return collection is null ? NotFound() : View(new AssessmentIndexViewModel { Collection = collection });
    }
    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var item = await service.GetByIdAsync(id, token);
        return item is null ? NotFound() : View(new AssessmentDetailsViewModel { Assessment = item });
    }
    [HttpGet] public async Task<IActionResult> Create(int lessonId, CancellationToken token) =>
        await service.GetByLessonAsync(lessonId, token) is null ? NotFound() : View(new AssessmentCreateViewModel { LessonId = lessonId });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssessmentCreateViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await service.CreateAsync(new(model.LessonId, model.Title, model.Slug, model.Description,
            model.AssessmentType, model.Order, model.PassingScore, model.MaximumAttempts, model.TimeLimitMinutes,
            model.ShuffleQuestions, model.ShowCorrectAnswers, model.IsMandatory), token);
        if (!result.Succeeded) { ModelState.AddModelError("", result.Error!); return View(model); }
        TempData["SuccessMessage"] = "L’évaluation a été créée.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken token)
    {
        var x = await service.GetByIdAsync(id, token);
        return x is null ? NotFound() : View(new AssessmentEditViewModel { Id=x.Id, LessonId=x.LessonId,
            Title=x.Title, Slug=x.Slug, Description=x.Description, AssessmentType=x.AssessmentType,
            Order=x.Order, PassingScore=x.PassingScore, MaximumAttempts=x.MaximumAttempts,
            TimeLimitMinutes=x.TimeLimitMinutes, ShuffleQuestions=x.ShuffleQuestions,
            ShowCorrectAnswers=x.ShowCorrectAnswers, IsMandatory=x.IsMandatory });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssessmentEditViewModel model, CancellationToken token)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var result = await service.UpdateAsync(new(model.Id, model.LessonId, model.Title, model.Slug,
            model.Description, model.AssessmentType, model.Order!.Value, model.PassingScore,
            model.MaximumAttempts, model.TimeLimitMinutes, model.ShuffleQuestions,
            model.ShowCorrectAnswers, model.IsMandatory), token);
        if (!result.Succeeded) { ModelState.AddModelError("", result.Error!); return View(model); }
        TempData["SuccessMessage"] = "L’évaluation a été modifiée.";
        return RedirectToAction(nameof(Details), new { id });
    }
    public async Task<IActionResult> Preview(int id, CancellationToken token)
    {
        var preview = await service.GetAdminPreviewAsync(id, token);
        return preview is null ? NotFound() : View(new AssessmentPreviewViewModel { Preview = preview });
    }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Publish(int id, CancellationToken t) => Apply(id, () => service.PublishAsync(id,t), "Évaluation publiée.", t);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Unpublish(int id, CancellationToken t) => Apply(id, () => service.UnpublishAsync(id,t), "Évaluation dépubliée.", t);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Archive(int id, CancellationToken t) => Apply(id, () => service.ArchiveAsync(id,t), "Évaluation archivée.", t);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Delete(int id, CancellationToken t) => Apply(id, () => service.DeleteAsync(id,t), "Évaluation supprimée.", t);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveUp(int id, CancellationToken t) => Apply(id, () => service.MoveUpAsync(id,t), "Évaluation déplacée.", t);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MoveDown(int id, CancellationToken t) => Apply(id, () => service.MoveDownAsync(id,t), "Évaluation déplacée.", t);
    private async Task<IActionResult> Apply(int id, Func<Task<ServiceResult>> action, string success, CancellationToken token)
    {
        var before = await service.GetByIdAsync(id, token); if (before is null) return NotFound();
        var result = await action(); TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? success : result.Error;
        return RedirectToAction(nameof(Index), new { lessonId = before.LessonId });
    }
}
