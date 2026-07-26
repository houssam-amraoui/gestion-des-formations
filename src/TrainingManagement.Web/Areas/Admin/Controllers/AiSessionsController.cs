using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-session-start"),
     ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> AvatarAccess(Guid id, CancellationToken token)
    {
        var userId = users.GetUserId(User);
        if (userId is null) return Challenge();

        var result = await conversations.CreateAvatarAccessAsync(id, userId, true, false, token);
        return result.Succeeded
            ? Json(new
            {
                provider = result.Value!.Provider,
                clientToken = result.Value.ClientToken,
                status = result.Value.Status
            })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-audio"),
     RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadAudio(Guid id, IFormFile? audio, CancellationToken token)
    {
        var userId = users.GetUserId(User);
        if (userId is null) return Challenge();
        if (audio is null)
        {
            TempData["ErrorMessage"] = "Aucun enregistrement audio reçu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var stream = audio.OpenReadStream();
        var result = await conversations.SendAudioAsync(id, userId, stream, audio.ContentType,
            audio.Length, true, false, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Question transcrite et réponse reçue." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
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
