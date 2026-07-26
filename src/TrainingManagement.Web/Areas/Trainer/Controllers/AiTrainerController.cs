using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.AiTrainer;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class AiTrainerController(IAiTrainerProfileService profiles,
    IAiConversationService conversations, IAiUsageService usage,
    UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    {
        var userId = UserId();
        ViewBag.Usage = await usage.GetTrainerAsync(userId, DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(1), token);
        return View(await profiles.GetAllAsync(userId, token));
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-session-start")]
    public async Task<IActionResult> Start(int lessonId, CancellationToken token)
    {
        var result = await conversations.StartAsync(lessonId, UserId(), false, true, token);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Session), new { id = result.Value!.Id });
    }

    public async Task<IActionResult> Session(Guid id, CancellationToken token)
    {
        var session = await conversations.GetAsync(id, UserId(), false, true, token);
        return session is null ? Forbid() : View(new AiSessionPageViewModel
        { Session = session, Message = new() { SessionId = id } });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-session-start"),
     ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> AvatarAccess(Guid id, CancellationToken token)
    {
        var result = await conversations.CreateAvatarAccessAsync(id, UserId(), false, true, token);
        return result.Succeeded
            ? Json(new
            {
                provider = result.Value!.Provider,
                clientToken = result.Value.ClientToken,
                status = result.Value.Status
            })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-message")]
    public async Task<IActionResult> SendMessage(
        [Bind(Prefix = "Message")] AiSendMessageViewModel model,
        CancellationToken token)
    {
        var result = await conversations.SendTextAsync(model.SessionId, UserId(), model.Text,
            false, true, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Réponse reçue." : result.Error;
        return RedirectToAction(nameof(Session), new { id = model.SessionId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> End(Guid id, CancellationToken token)
    {
        await conversations.EndAsync(id, UserId(), false, true, token);
        return RedirectToAction(nameof(Session), new { id });
    }

    public async Task<IActionResult> History(CancellationToken token) =>
        View(await conversations.GetTrainerAsync(UserId(), token));

    private string UserId() => users.GetUserId(User) ?? throw new UnauthorizedAccessException();
}
