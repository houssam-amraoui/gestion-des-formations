using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Domain.Constants;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class DashboardController : Controller
{
    public IActionResult Index() => View();
}
