using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Enrollments;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"),Authorize(Roles=AppRoles.Admin)]
public sealed class EnrollmentsController(IEnrollmentService service,UserManager<ApplicationUser> users):Controller
{
    public async Task<IActionResult> Index(string? search,int? trainingId,EnrollmentStatus? status,int page=1,CancellationToken token=default)
    {
        var trainings=(await service.GetTrainingOptionsAsync(token)).Select(x=>new SelectListItem(x.Label,x.Value)).ToArray();
        return View(new EnrollmentIndexViewModel{Search=search,TrainingId=trainingId,Status=status,Page=page,
            Results=await service.GetAdminListAsync(new(search,trainingId,status,Page:page),token),Trainings=trainings});
    }
    public async Task<IActionResult> Details(int id,CancellationToken token){var x=await service.GetByIdAsync(id,token);return x is null?NotFound():View(new EnrollmentDetailsViewModel{Enrollment=x});}
    [HttpGet]public async Task<IActionResult>Create(CancellationToken token)=>View(await Form(new EnrollmentCreateViewModel(),token));
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Create(EnrollmentCreateViewModel m,CancellationToken token){if(!ModelState.IsValid)return View(await Form(m,token));var admin=users.GetUserId(User);if(admin is null)return Challenge();var r=await service.CreateByAdminAsync(new(m.LearnerId,m.TrainingId,m.ActivateImmediately,admin),token);if(!r.Succeeded){ModelState.AddModelError("",r.Error!);return View(await Form(m,token));}TempData["SuccessMessage"]="Inscription créée.";return RedirectToAction(nameof(Details),new{id=r.Value});}
    [HttpPost,ValidateAntiForgeryToken]public Task<IActionResult>Activate(int id,CancellationToken t)=>Apply(id,()=>service.ActivateAsync(id,t),"Inscription activée.",t);
    [HttpPost,ValidateAntiForgeryToken]public Task<IActionResult>Suspend(int id,CancellationToken t)=>Apply(id,()=>service.SuspendAsync(id,t),"Inscription suspendue.",t);
    [HttpPost,ValidateAntiForgeryToken]public Task<IActionResult>Reactivate(int id,CancellationToken t)=>Apply(id,()=>service.ReactivateAsync(id,t),"Inscription réactivée.",t);
    [HttpPost,ValidateAntiForgeryToken]public Task<IActionResult>Cancel(int id,CancellationToken t)=>Apply(id,()=>service.CancelAsync(id,t),"Inscription annulée.",t);
    private async Task<IActionResult>Apply(int id,Func<Task<ServiceResult>> a,string success,CancellationToken t){if(await service.GetByIdAsync(id,t)is null)return NotFound();var r=await a();TempData[r.Succeeded?"SuccessMessage":"ErrorMessage"]=r.Succeeded?success:r.Error;return RedirectToAction(nameof(Details),new{id});}
    private async Task<EnrollmentCreateViewModel>Form(EnrollmentCreateViewModel m,CancellationToken t){m.Learners=(await service.GetLearnerOptionsAsync(t)).Select(x=>new SelectListItem(x.Label,x.Value)).ToArray();m.Trainings=(await service.GetTrainingOptionsAsync(t)).Select(x=>new SelectListItem(x.Label,x.Value)).ToArray();return m;}
}
