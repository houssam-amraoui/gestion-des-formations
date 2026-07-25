using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"),Authorize(Roles=AppRoles.Trainer)]
public sealed class LearnerProgressController(IAssessmentAttemptService service,UserManager<ApplicationUser> users):Controller
{
    public async Task<IActionResult>Index(CancellationToken token){var id=users.GetUserId(User);return id is null?Challenge():View(await service.GetTrainerProgressAsync(id,token));}
}
