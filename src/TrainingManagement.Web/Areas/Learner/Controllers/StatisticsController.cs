using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class StatisticsController(IAnalyticsService service, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    { var id = users.GetUserId(User); return id is null ? Challenge() : View(await service.GetLearnerStatisticsAsync(id, token)); }
}
