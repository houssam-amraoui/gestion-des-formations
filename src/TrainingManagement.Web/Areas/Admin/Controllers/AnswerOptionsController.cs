using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.AnswerOptions;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Web.ViewModels.AnswerOptions;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class AnswerOptionsController(IAnswerOptionService service) : Controller
{
    public async Task<IActionResult> Index(int questionId,CancellationToken t){var x=await service.GetByQuestionAsync(questionId,t);if(x is null)return NotFound();if(x.QuestionType==QuestionType.ShortAnswer)return BadRequest();return View(new AnswerOptionIndexViewModel{Collection=x});}
    [HttpGet] public async Task<IActionResult> Create(int questionId,CancellationToken t){var x=await service.GetByQuestionAsync(questionId,t);if(x is null)return NotFound();if(x.QuestionType==QuestionType.ShortAnswer)return BadRequest();return View(new AnswerOptionCreateViewModel{QuestionId=questionId});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(AnswerOptionCreateViewModel m,CancellationToken t){if(!ModelState.IsValid)return View(m);var r=await service.CreateAsync(new(m.QuestionId,m.Text,m.Order,m.IsCorrect),t);if(!r.Succeeded){ModelState.AddModelError("",r.Error!);return View(m);}TempData["SuccessMessage"]="Réponse créée.";return RedirectToAction(nameof(Index),new{questionId=m.QuestionId});}
    [HttpGet] public async Task<IActionResult> Edit(int id,CancellationToken t){var x=await service.GetByIdAsync(id,t);return x is null?NotFound():View(new AnswerOptionEditViewModel{Id=x.Id,QuestionId=x.QuestionId,Text=x.Text,Order=x.Order,IsCorrect=x.IsCorrect});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id,AnswerOptionEditViewModel m,CancellationToken t){if(id!=m.Id)return BadRequest();if(!ModelState.IsValid)return View(m);var r=await service.UpdateAsync(new(m.Id,m.QuestionId,m.Text,m.Order!.Value,m.IsCorrect),t);if(!r.Succeeded){ModelState.AddModelError("",r.Error!);return View(m);}TempData["SuccessMessage"]="Réponse modifiée.";return RedirectToAction(nameof(Index),new{questionId=m.QuestionId});}
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> Delete(int id,CancellationToken t)=>Apply(id,()=>service.DeleteAsync(id,t),"Réponse supprimée.",t);
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> MoveUp(int id,CancellationToken t)=>Apply(id,()=>service.MoveUpAsync(id,t),"Réponse déplacée.",t);
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> MoveDown(int id,CancellationToken t)=>Apply(id,()=>service.MoveDownAsync(id,t),"Réponse déplacée.",t);
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> ToggleCorrect(int id,CancellationToken t)=>Apply(id,()=>service.ToggleCorrectAsync(id,t),"Réponse mise à jour.",t);
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateTrueFalse(int questionId,CancellationToken t){var r=await service.CreateTrueFalseAsync(questionId,t);TempData[r.Succeeded?"SuccessMessage":"ErrorMessage"]=r.Succeeded?"Réponses Vrai/Faux créées.":r.Error;return RedirectToAction(nameof(Index),new{questionId});}
    private async Task<IActionResult> Apply(int id,Func<Task<ServiceResult>> action,string success,CancellationToken t){var x=await service.GetByIdAsync(id,t);if(x is null)return NotFound();var r=await action();TempData[r.Succeeded?"SuccessMessage":"ErrorMessage"]=r.Succeeded?success:r.Error;return RedirectToAction(nameof(Index),new{questionId=x.QuestionId});}
}
