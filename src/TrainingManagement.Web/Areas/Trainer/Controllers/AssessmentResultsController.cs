using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Attempts;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"),Authorize(Roles=AppRoles.Trainer)]
public sealed class AssessmentResultsController(IAssessmentAttemptService service,UserManager<ApplicationUser> users):Controller
{
    public async Task<IActionResult>Index(CancellationToken token){var user=users.GetUserId(User);return user is null?Challenge():View(await service.GetTrainerAttemptsAsync(user,token));}
    public async Task<IActionResult>Details(int id,CancellationToken token){var user=users.GetUserId(User);if(user is null)return Challenge();var r=await service.GetTrainerResultAsync(id,user,token);return r is null?Forbid():View(new AttemptResultViewModel{Result=r});}
}
