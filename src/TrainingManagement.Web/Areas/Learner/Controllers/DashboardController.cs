using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Application.AiTrainer;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class DashboardController(IEnrollmentService service, IAnalyticsService analytics, IAiUsageService aiUsage,
    UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    {
        var id=users.GetUserId(User); if(id is null)return Challenge();
        ViewBag.Analytics = await analytics.GetLearnerDashboardAsync(id, token);
        ViewBag.AiUsage = await aiUsage.GetLearnerAsync(id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, token);
        return View(await service.GetLearnerDashboardAsync(id,token));
    }
}
