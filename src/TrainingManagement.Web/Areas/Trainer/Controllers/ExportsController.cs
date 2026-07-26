using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Exports;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;

namespace TrainingManagement.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = AppRoles.Trainer)]
public sealed class ExportsController(ICsvExportService service, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Learners(CancellationToken t) => Csv(await service.ExportTrainerLearnersAsync(UserId(), t));
    public async Task<IActionResult> Progress(CancellationToken t) => Csv(await service.ExportTrainerProgressAsync(UserId(), t));
    public async Task<IActionResult> AssessmentResults(CancellationToken t) => Csv(await service.ExportTrainerAssessmentResultsAsync(UserId(), t));
    [HttpGet("Trainer/Exports/TrainingAnalytics/{id:int}")]
    public async Task<IActionResult> TrainingAnalytics(int id, CancellationToken t)
    { var file = await service.ExportTrainingAnalyticsAsync(id, UserId(), t); return file is null ? Forbid() : Csv(file); }
    private string UserId() => users.GetUserId(User) ?? throw new UnauthorizedAccessException();
    private FileContentResult Csv(CsvFileResult file) => File(file.Content, "text/csv; charset=utf-8", file.FileName);
}
