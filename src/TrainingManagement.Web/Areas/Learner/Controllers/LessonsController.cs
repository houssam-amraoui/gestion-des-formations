using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Application.Progress;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Lessons;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class LessonsController(IPedagogyReadService pedagogy, IAssessmentService assessments,
    ILessonProgressService progress, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Details(int id,CancellationToken token)
    {
        var user=users.GetUserId(User);if(user is null)return Challenge();
        var lesson=await pedagogy.GetEnrolledLessonByIdAsync(id,user,token);if(lesson is null)return Forbid();
        await progress.RecordAccessAsync(id,user,token);
        return View(new LessonPageViewModel{Lesson=lesson,IsEnrolledAccess=true,
            Assessments=await assessments.GetAccessibleByLessonAsync(id,user,token)});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id,CancellationToken token)
    {
        var user=users.GetUserId(User);if(user is null)return Challenge();
        var result=await progress.CompleteAsync(id,user,token);
        TempData[result.Succeeded?"SuccessMessage":"ErrorMessage"]=result.Succeeded
            ?$"Leçon terminée. Progression : {result.Value!.ProgressPercentage:N2} %.":result.Error;
        return RedirectToAction(nameof(Details),new{id});
    }
}
