using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Assessments;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class AssessmentsController(IAssessmentService assessments, IEnrollmentService enrollments,
    IAssessmentAttemptService attempts, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Details(int id,CancellationToken token)
    {
        var user=users.GetUserId(User);if(user is null)return Challenge();
        var item=await assessments.GetByIdAsync(id,token);if(item is null||!item.IsPublished||item.IsArchived)return NotFound();
        var training=await enrollments.GetLearnerTrainingAsync(item.TrainingId,user,token);if(training is null)return Forbid();
        var a=training.Modules.SelectMany(m=>m.Lessons).SelectMany(l=>l.Assessments).FirstOrDefault(x=>x.Id==id);
        if(a is null)return Forbid();
        return View(new AssessmentDetailsViewModel{Assessment=item,InProgressAttemptId=a.InProgressAttemptId,
            AttemptsRemaining=a.MaximumAttempts is null?null:Math.Max(0,a.MaximumAttempts.Value-a.AttemptsUsed),
            LearnerCanStart=a.MaximumAttempts is null||a.AttemptsUsed<a.MaximumAttempts});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id,CancellationToken token)
    {
        var user=users.GetUserId(User);if(user is null)return Challenge();
        var result=await attempts.StartAsync(new(id,user),token);
        if(!result.Succeeded){TempData["ErrorMessage"]=result.Error;return RedirectToAction(nameof(Details),new{id});}
        return RedirectToAction("Details","Attempts",new{id=result.Value});
    }
}
