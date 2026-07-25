using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Questions;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Questions;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class QuestionsController(IQuestionService service) : Controller
{
    public async Task<IActionResult> Index(int assessmentId, CancellationToken t) { var x=await service.GetByAssessmentAsync(assessmentId,t); return x is null?NotFound():View(new QuestionIndexViewModel{Collection=x}); }
    public async Task<IActionResult> Details(int id,CancellationToken t){var x=await service.GetByIdAsync(id,t);return x is null?NotFound():View(new QuestionDetailsViewModel{Question=x});}
    [HttpGet] public async Task<IActionResult> Create(int assessmentId,CancellationToken t)=>await service.GetByAssessmentAsync(assessmentId,t)is null?NotFound():View(new QuestionCreateViewModel{AssessmentId=assessmentId});
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(QuestionCreateViewModel m,CancellationToken t){if(!ModelState.IsValid)return View(m);var r=await service.CreateAsync(new(m.AssessmentId,m.QuestionType,m.Statement,m.Explanation,m.Order,m.Points,m.ExpectedAnswer),t);if(!r.Succeeded){ModelState.AddModelError("",r.Error!);return View(m);}TempData["SuccessMessage"]="Question créée.";return RedirectToAction(nameof(Details),new{id=r.Value});}
    [HttpGet] public async Task<IActionResult> Edit(int id,CancellationToken t){var x=await service.GetByIdAsync(id,t);return x is null?NotFound():View(new QuestionEditViewModel{Id=x.Id,AssessmentId=x.AssessmentId,QuestionType=x.QuestionType,Statement=x.Statement,Explanation=x.Explanation,Order=x.Order,Points=x.Points,ExpectedAnswer=x.ExpectedAnswer});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id,QuestionEditViewModel m,CancellationToken t){if(id!=m.Id)return BadRequest();if(!ModelState.IsValid)return View(m);var r=await service.UpdateAsync(new(m.Id,m.AssessmentId,m.QuestionType,m.Statement,m.Explanation,m.Order!.Value,m.Points,m.ExpectedAnswer),t);if(!r.Succeeded){ModelState.AddModelError("",r.Error!);return View(m);}TempData["SuccessMessage"]="Question modifiée.";return RedirectToAction(nameof(Details),new{id});}
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> Publish(int id,CancellationToken t)=>Apply(id,()=>service.PublishAsync(id,t),"Question publiée.",t);
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> Unpublish(int id,CancellationToken t)=>Apply(id,()=>service.UnpublishAsync(id,t),"Question dépubliée.",t);
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> Delete(int id,CancellationToken t)=>Apply(id,()=>service.DeleteAsync(id,t),"Question supprimée.",t);
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> MoveUp(int id,CancellationToken t)=>Apply(id,()=>service.MoveUpAsync(id,t),"Question déplacée.",t);
    [HttpPost,ValidateAntiForgeryToken] public Task<IActionResult> MoveDown(int id,CancellationToken t)=>Apply(id,()=>service.MoveDownAsync(id,t),"Question déplacée.",t);
    private async Task<IActionResult> Apply(int id,Func<Task<ServiceResult>> action,string success,CancellationToken t){var x=await service.GetByIdAsync(id,t);if(x is null)return NotFound();var r=await action();TempData[r.Succeeded?"SuccessMessage":"ErrorMessage"]=r.Succeeded?success:r.Error;return RedirectToAction(nameof(Index),new{assessmentId=x.AssessmentId});}
}
