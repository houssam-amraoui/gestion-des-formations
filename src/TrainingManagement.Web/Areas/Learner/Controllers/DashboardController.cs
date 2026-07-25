using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Domain.Constants;

namespace TrainingManagement.Web.Areas.Learner.Controllers;

[Area("Learner"), Authorize(Roles = AppRoles.Learner)]
public sealed class DashboardController : Controller
{
    public IActionResult Index() => View();
}
