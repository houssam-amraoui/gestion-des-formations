using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Application.Analytics;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class DashboardController(IAnalyticsService analytics) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token) =>
        View(await analytics.GetAdminDashboardAsync(new(), token));
}
