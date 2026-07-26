using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Application.Analytics;
using Microsoft.AspNetCore.Identity;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Application.AiTrainer;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class DashboardController(IAnalyticsService analytics, IAiUsageService aiUsage,
    UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    {
        var id = users.GetUserId(User);
        if (id is null) return Challenge();
        ViewBag.AiUsage = await aiUsage.GetTrainerAsync(id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, token);
        return View(await analytics.GetTrainerDashboardAsync(id, new(), token));
    }
}
