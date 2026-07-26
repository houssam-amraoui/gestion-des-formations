using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Application.Analytics;
using Microsoft.AspNetCore.Identity;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class DashboardController(IAnalyticsService analytics, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    { var id = users.GetUserId(User); return id is null ? Challenge() : View(await analytics.GetTrainerDashboardAsync(id, new(), token)); }
}
