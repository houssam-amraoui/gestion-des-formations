using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Assessments;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class AssessmentsController(IAssessmentService service, IPedagogyReadService pedagogy,
    UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(int lessonId, CancellationToken token)
    {
        var userId=users.GetUserId(User); if(userId is null)return Challenge();
        var list=await service.GetByLessonAsync(lessonId,token); if(list is null)return NotFound();
        if(await pedagogy.GetTrainerTrainingAsync(list.TrainingId,userId,token)is null)return Forbid();
        return View(new AssessmentIndexViewModel{Collection=list});
    }
    public async Task<IActionResult> Details(int id,CancellationToken token)
    {
        var userId=users.GetUserId(User);if(userId is null)return Challenge();
        var preview=await service.GetTrainerPreviewAsync(id,userId,token);
        return preview is null?Forbid():View(new AssessmentPreviewViewModel{Preview=preview,IsTrainer=true});
    }
}
