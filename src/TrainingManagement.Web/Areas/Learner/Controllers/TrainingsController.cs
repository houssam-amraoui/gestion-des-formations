using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class TrainingsController(IEnrollmentService service, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token){var id=users.GetUserId(User);return id is null?Challenge():View(await service.GetLearnerTrainingsAsync(id,token));}
    public async Task<IActionResult> Details(int id,CancellationToken token){var user=users.GetUserId(User);if(user is null)return Challenge();var model=await service.GetLearnerTrainingAsync(id,user,token);return model is null?Forbid():View(model);}
}
