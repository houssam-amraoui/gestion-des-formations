using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class CertificatesController(ICertificateService service, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    {
        var id = users.GetUserId(User); return id is null ? Challenge() : View(await service.GetForLearnerAsync(id, token));
    }
    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var user = users.GetUserId(User); if (user is null) return Challenge();
        var item = await service.GetForLearnerAsync(id, user, token);
        return item is null ? NotFound() : View(item);
    }
    public async Task<IActionResult> Download(int id, CancellationToken token)
    {
        var user = users.GetUserId(User); if (user is null) return Challenge();
        var file = await service.DownloadAsync(id, user, false, false, token);
        return file is null ? NotFound() : File(file.Content, "application/pdf", file.FileName);
    }
}
