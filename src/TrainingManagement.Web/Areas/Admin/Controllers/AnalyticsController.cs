using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Analytics;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class AnalyticsController(IAnalyticsService service) : Controller
{
    public async Task<IActionResult> Index(AnalyticsFilterViewModel filter, CancellationToken token)
    {
        if (!ModelState.IsValid) filter = new();
        ViewBag.Filter = filter;
        return View(await service.GetAdminDashboardAsync(filter.ToModel(), token));
    }
    [HttpGet("Admin/Analytics/Trainings/{trainingId:int}")]
    public async Task<IActionResult> Training(int trainingId, AnalyticsFilterViewModel filter, CancellationToken token)
    {
        if (!ModelState.IsValid) filter = new();
        ViewBag.Filter = filter;
        var result = await service.GetTrainingAsync(trainingId, filter.ToModel(), null, token);
        return result is null ? NotFound() : View(result);
    }
}
