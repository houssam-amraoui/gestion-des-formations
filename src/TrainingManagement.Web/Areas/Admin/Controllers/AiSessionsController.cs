using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.AiTrainer;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class AiSessionsController(IAiConversationService conversations,
    IAiUsageService usage, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(AiSessionIndexViewModel filter, CancellationToken token)
    {
        filter.Results = await conversations.GetAdminAsync(new(null, filter.User, filter.Provider,
            filter.Status, filter.DateFrom, filter.DateTo, filter.Results.Page), token);
        ViewBag.Usage = await usage.GetAdminAsync(filter.DateFrom ?? DateTime.UtcNow.AddDays(-30),
            (filter.DateTo ?? DateTime.UtcNow).Date.AddDays(1), token);
        return View(filter);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken token)
    {
        var userId = users.GetUserId(User);
        if (userId is null) return Challenge();
        var session = await conversations.GetAsync(id, userId, true, false, token);
        return session is null ? NotFound() : View(session);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken token)
    {
        var result = await conversations.CancelAsync(id, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Session IA annulée." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}
