using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.AiTrainer;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class AiTrainerController(IAiConversationService conversations,
    IAiConsentService consents, UserManager<ApplicationUser> users) : Controller
{
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-session-start")]
    public async Task<IActionResult> Start(int lessonId, CancellationToken token)
    {
        var userId = UserId();
        var result = await conversations.StartAsync(lessonId, userId, false, false, token);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction("Details", "Lessons", new { id = lessonId });
        }
        return RedirectToAction(nameof(Session), new { id = result.Value!.Id });
    }

    [HttpGet, EnableRateLimiting("ai-status")]
    public async Task<IActionResult> Session(Guid id, CancellationToken token)
    {
        var userId = UserId();
        var session = await conversations.GetAsync(id, userId, false, false, token);
        if (session is null) return NotFound();
        return View(new AiSessionPageViewModel
        {
            Session = session, Message = new() { SessionId = id },
            AudioConsent = await consents.GetAsync(userId, session.Provider, token)
        });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-message")]
    public async Task<IActionResult> SendMessage(AiSendMessageViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "La question est invalide.";
            return RedirectToAction(nameof(Session), new { id = model.SessionId });
        }
        var result = await conversations.SendTextAsync(model.SessionId, UserId(), model.Text,
            false, false, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Réponse reçue." : result.Error;
        return RedirectToAction(nameof(Session), new { id = model.SessionId });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-audio"),
     RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadAudio(Guid id, IFormFile? audio, CancellationToken token)
    {
        if (audio is null)
        {
            TempData["ErrorMessage"] = "Aucun fichier audio reçu.";
            return RedirectToAction(nameof(Session), new { id });
        }
        await using var stream = audio.OpenReadStream();
        var result = await conversations.SendAudioAsync(id, UserId(), stream, audio.ContentType,
            audio.Length, false, false, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Audio transcrit et réponse reçue." : result.Error;
        return RedirectToAction(nameof(Session), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptAudioConsent(Guid id, string provider, CancellationToken token)
    {
        await consents.AcceptAsync(UserId(), provider, token);
        TempData["SuccessMessage"] = "Consentement audio enregistré.";
        return RedirectToAction(nameof(Session), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAudioConsent(Guid id, string provider, CancellationToken token)
    {
        await consents.RevokeAsync(UserId(), provider, token);
        TempData["SuccessMessage"] = "Consentement audio révoqué.";
        return RedirectToAction(nameof(Session), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> End(Guid id, CancellationToken token)
    {
        var result = await conversations.EndAsync(id, UserId(), false, false, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Session IA terminée." : result.Error;
        return RedirectToAction(nameof(Session), new { id });
    }

    public async Task<IActionResult> History(int? lessonId, CancellationToken token) =>
        View(await conversations.GetHistoryAsync(UserId(), lessonId, token));

    private string UserId() => users.GetUserId(User) ?? throw new UnauthorizedAccessException();
}
