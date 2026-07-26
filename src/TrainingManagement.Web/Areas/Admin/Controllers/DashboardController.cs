using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Application.AiTrainer;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class DashboardController(IAnalyticsService analytics, IAiUsageService aiUsage) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    {
        ViewBag.AiUsage = await aiUsage.GetAdminAsync(DateTime.UtcNow.Date, DateTime.UtcNow, token);
        return View(await analytics.GetAdminDashboardAsync(new(), token));
    }
}
