using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Analytics;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class AnalyticsController(IAnalyticsService service, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(AnalyticsFilterViewModel filter, CancellationToken token)
    { var id = users.GetUserId(User); if (id is null) return Challenge(); if (!ModelState.IsValid) filter = new(); ViewBag.Filter = filter; return View(await service.GetTrainerDashboardAsync(id, filter.ToModel(), token)); }
    [HttpGet("Trainer/Analytics/Trainings/{trainingId:int}")]
    public async Task<IActionResult> Training(int trainingId, AnalyticsFilterViewModel filter, CancellationToken token)
    { var id = users.GetUserId(User); if (id is null) return Challenge(); if (!ModelState.IsValid) filter = new(); var result = await service.GetTrainingAsync(trainingId, filter.ToModel(), id, token); return result is null ? Forbid() : View(result); }
}
