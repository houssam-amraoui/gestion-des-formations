using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Authentication;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Account;

namespace TrainingManagement.Web.Controllers;

public sealed class AccountController(
    IAccountRegistrationService registrationService,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IDashboardRedirectService redirectService) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await registrationService.RegisterLearnerAsync(
            new(model.FirstName, model.LastName, model.Email, model.Password),
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is not null) await signInManager.SignInAsync(user, false);
        TempData["SuccessMessage"] = "Votre compte apprenant a été créé.";
        return RedirectToAction(nameof(Dashboard));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Adresse e-mail ou mot de passe incorrect.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "Ce compte est temporairement verrouillé."
                : "Adresse e-mail ou mot de passe incorrect.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction(nameof(Dashboard));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var destination = redirectService.GetDestination(await userManager.GetRolesAsync(user));
        return LocalRedirect($"/{destination.Area}/Dashboard");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccessDenied() => View();
}
