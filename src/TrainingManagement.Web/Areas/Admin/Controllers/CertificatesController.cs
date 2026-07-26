using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Web.ViewModels.Certificates;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class CertificatesController(ICertificateService service, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(string? search, CertificateStatus? status, DateTime? dateFrom,
        DateTime? dateTo, int page = 1, CancellationToken token = default) =>
        View(new CertificateIndexViewModel { Search = search, Status = status, DateFrom = dateFrom, DateTo = dateTo,
            Results = await service.GetAdminAsync(new(search, status, dateFrom, dateTo, page), token) });
    public async Task<IActionResult> Details(int id, CancellationToken token)
    { var item = await service.GetAsync(id, token); return item is null ? NotFound() : View(item); }
    [EnableRateLimiting("download")]
    public async Task<IActionResult> Download(int id, CancellationToken token)
    { var user = users.GetUserId(User); if (user is null) return Challenge(); var file = await service.DownloadAsync(id, user, true, false, token); return file is null ? NotFound() : File(file.Content, "application/pdf", file.FileName); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int enrollmentId, CancellationToken token)
    { var result = await service.GenerateAsync(enrollmentId, token); TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Certificat généré." : result.Error; return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(int id, CancellationToken token)
    { var result = await service.RegeneratePdfAsync(id, token); TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "PDF régénéré." : result.Error; return RedirectToAction(nameof(Details), new { id }); }
    [HttpGet] public async Task<IActionResult> Revoke(int id, CancellationToken token)
    { var item = await service.GetAsync(id, token); return item is null ? NotFound() : View(new CertificateRevokeViewModel { Id = id, CertificateNumber = item.CertificateNumber }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(CertificateRevokeViewModel model, CancellationToken token)
    {
        var user = users.GetUserId(User); if (user is null) return Challenge();
        if (!ModelState.IsValid) return View(model);
        var result = await service.RevokeAsync(new(model.Id, model.Reason, user), token);
        if (!result.Succeeded) { ModelState.AddModelError("", result.Error!); return View(model); }
        TempData["SuccessMessage"] = "Certificat révoqué."; return RedirectToAction(nameof(Details), new { id = model.Id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id, CancellationToken token)
    { var user = users.GetUserId(User); if (user is null) return Challenge(); var result = await service.ReactivateAsync(id, user, token); TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Certificat réactivé." : result.Error; return RedirectToAction(nameof(Details), new { id }); }
}
