using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Attempts;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class AttemptsController(IAssessmentAttemptService service,UserManager<ApplicationUser> users):Controller
{
    public async Task<IActionResult> Details(int id,CancellationToken token){var user=users.GetUserId(User);if(user is null)return Challenge();var a=await service.GetAttemptAsync(id,user,token);if(a is null)return NotFound();if(a.Status!=AttemptStatus.InProgress)return RedirectToAction(nameof(Result),new{id});return View(ToView(a));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Save(int id,AttemptPageViewModel model,CancellationToken token){var user=users.GetUserId(User);if(user is null)return Challenge();var r=await service.SaveAnswersAsync(id,user,Inputs(model),token);TempData[r.Succeeded?"SuccessMessage":"ErrorMessage"]=r.Succeeded?"Réponses enregistrées.":r.Error;return RedirectToAction(nameof(Details),new{id});}
    [HttpPost,ValidateAntiForgeryToken,EnableRateLimiting("assessment-submit")] public async Task<IActionResult> Submit(int id,AttemptPageViewModel model,CancellationToken token){var user=users.GetUserId(User);if(user is null)return Challenge();var r=await service.SubmitAsync(new(id,user,Inputs(model)),token);if(!r.Succeeded){TempData["ErrorMessage"]=r.Error;return RedirectToAction(nameof(Details),new{id});}return RedirectToAction(nameof(Result),new{id});}
    public async Task<IActionResult> Result(int id,CancellationToken token){var user=users.GetUserId(User);if(user is null)return Challenge();var r=await service.GetResultAsync(id,user,false,token);return r is null?NotFound():View(new AttemptResultViewModel{Result=r});}
    public async Task<IActionResult> History(int? trainingId,bool? passed,AttemptStatus? status,CancellationToken token){var user=users.GetUserId(User);if(user is null)return Challenge();return View(new AttemptHistoryViewModel{TrainingId=trainingId,Passed=passed,Status=status,Items=await service.GetHistoryAsync(user,new(trainingId,passed,status),token)});}
    private static AttemptPageViewModel ToView(AttemptDetailsModel a)=>new(){Attempt=a,Questions=a.Questions.Select(q=>new AttemptQuestionInputViewModel{AttemptQuestionId=q.AttemptQuestionId,SelectedOptionIds=q.Options.Where(o=>o.Selected).Select(o=>o.Id).ToList(),TextAnswer=q.TextAnswer}).ToList()};
    private static IReadOnlyCollection<AttemptAnswerInputModel> Inputs(AttemptPageViewModel m)=>m.Questions.Select(q=>new AttemptAnswerInputModel(q.AttemptQuestionId,q.SelectedOptionIds,q.TextAnswer)).ToArray();
}
