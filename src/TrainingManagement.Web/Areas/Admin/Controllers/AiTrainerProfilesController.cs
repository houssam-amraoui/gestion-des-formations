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
public sealed class AiTrainerProfilesController(
    IAiTrainerProfileService profiles,
    IAiConversationService conversations,
    UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token) =>
        View(await profiles.GetAllAsync(null, token));

    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var profile = await profiles.GetAsync(id, null, token);
        return profile is null ? NotFound() : View(profile);
    }

    [HttpGet]
    public IActionResult Create(int trainingId) => View(new AiTrainerProfileFormViewModel
    {
        TrainingId = trainingId, DisplayName = "Alex, formateur IA",
        WelcomeMessage = "Bonjour, je suis Alex. Je peux vous aider à comprendre cette leçon et répondre à vos questions."
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AiTrainerProfileFormViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await profiles.CreateAsync(model.ToInput(), token);
        if (!result.Succeeded) { ModelState.AddModelError("", result.Error!); return View(model); }
        TempData["SuccessMessage"] = "Profil IA créé.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken token)
    {
        var edit = await profiles.GetEditAsync(id, token);
        return edit is null ? NotFound() : View(Form(edit));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AiTrainerProfileFormViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await profiles.UpdateAsync(model.Id, model.ToInput(), token);
        if (!result.Succeeded) { ModelState.AddModelError("", result.Error!); return View(model); }
        TempData["SuccessMessage"] = "Profil IA mis à jour.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(int id, CancellationToken token) =>
        await Toggle(id, true, token);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(int id, CancellationToken token) =>
        await Toggle(id, false, token);

    [HttpGet]
    public async Task<IActionResult> Test(int id, CancellationToken token)
    {
        var profile = await profiles.GetAsync(id, null, token);
        return profile is null ? NotFound() : View(new AiProfileTestViewModel { Profile = profile });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai-session-start")]
    public async Task<IActionResult> Test(int id, int lessonId, string? question, CancellationToken token)
    {
        var userId = users.GetUserId(User);
        if (userId is null) return Challenge();
        var started = await conversations.StartAsync(lessonId, userId, true, false, token);
        if (!started.Succeeded)
        {
            TempData["ErrorMessage"] = started.Error;
            return RedirectToAction(nameof(Test), new { id });
        }
        if (!string.IsNullOrWhiteSpace(question))
        {
            var reply = await conversations.SendTextAsync(started.Value!.Id, userId, question,
                true, false, token);
            if (!reply.Succeeded) TempData["ErrorMessage"] = reply.Error;
        }
        return RedirectToAction("Details", "AiSessions", new { id = started.Value!.Id });
    }

    private async Task<IActionResult> Toggle(int id, bool enabled, CancellationToken token)
    {
        var result = await profiles.SetEnabledAsync(id, enabled, token);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? enabled ? "Profil IA activé." : "Profil IA désactivé." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    private static AiTrainerProfileFormViewModel Form(AiTrainerProfileEditModel edit) => new()
    {
        Id = edit.Id, TrainingId = edit.Input.TrainingId, DisplayName = edit.Input.DisplayName,
        Description = edit.Input.Description, Provider = edit.Input.Provider,
        AvatarId = edit.Input.AvatarId, VoiceId = edit.Input.VoiceId,
        LanguageCode = edit.Input.LanguageCode, SystemPrompt = edit.Input.SystemPrompt,
        WelcomeMessage = edit.Input.WelcomeMessage, FallbackMessage = edit.Input.FallbackMessage,
        IsEnabled = edit.Input.IsEnabled, AllowTextInput = edit.Input.AllowTextInput,
        AllowAudioInput = edit.Input.AllowAudioInput, AllowAudioOutput = edit.Input.AllowAudioOutput,
        AllowAvatar = edit.Input.AllowAvatar,
        MaximumMessagesPerSession = edit.Input.MaximumMessagesPerSession,
        MaximumSessionMinutes = edit.Input.MaximumSessionMinutes
    };
}
