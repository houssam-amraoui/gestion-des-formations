using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class TrainingsController(
    IPedagogyReadService service,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null) return Challenge();
        return View(await service.GetTrainerTrainingsAsync(userId, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null) return Challenge();
        var training = await service.GetTrainerTrainingAsync(id, userId, cancellationToken);
        return training is null ? Forbid() : View(training);
    }
}
