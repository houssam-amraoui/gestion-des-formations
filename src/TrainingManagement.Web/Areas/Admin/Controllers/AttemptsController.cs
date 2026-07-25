using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Attempts;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"),Authorize(Roles=AppRoles.Admin)]
public sealed class AttemptsController(IAssessmentAttemptService service):Controller
{
    public async Task<IActionResult>Index(CancellationToken token)=>View(await service.GetAdminAttemptsAsync(token));
    public async Task<IActionResult>Details(int id,CancellationToken token){var r=await service.GetResultAsync(id,"",true,token);return r is null?NotFound():View(new AttemptResultViewModel{Result=r});}
}
