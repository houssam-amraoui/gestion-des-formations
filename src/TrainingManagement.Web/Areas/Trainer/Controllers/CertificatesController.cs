using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class CertificatesController(ICertificateService service, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    { var id = users.GetUserId(User); return id is null ? Challenge() : View(await service.GetForTrainerAsync(id, token)); }
    public async Task<IActionResult> Details(int id, CancellationToken token)
    { var user = users.GetUserId(User); if (user is null) return Challenge(); var item = await service.GetForTrainerAsync(id, user, token); return item is null ? Forbid() : View(item); }
}
