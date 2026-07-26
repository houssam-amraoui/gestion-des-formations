using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingManagement.Application.Exports;
using TrainingManagement.Domain.Constants;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin), EnableRateLimiting("export")]
public sealed class ExportsController(ICsvExportService service) : Controller
{
    public async Task<IActionResult> Enrollments(CancellationToken t) => Csv(await service.ExportAdminEnrollmentsAsync(t));
    public async Task<IActionResult> Progress(CancellationToken t) => Csv(await service.ExportAdminProgressAsync(t));
    public async Task<IActionResult> AssessmentResults(CancellationToken t) => Csv(await service.ExportAdminAssessmentResultsAsync(t));
    public async Task<IActionResult> Certificates(CancellationToken t) => Csv(await service.ExportAdminCertificatesAsync(t));
    [HttpGet("Admin/Exports/TrainingAnalytics/{id:int}")]
    public async Task<IActionResult> TrainingAnalytics(int id, CancellationToken t)
    { var file = await service.ExportTrainingAnalyticsAsync(id, null, t); return file is null ? NotFound() : Csv(file); }
    private FileContentResult Csv(CsvFileResult file) => File(file.Content, "text/csv; charset=utf-8", file.FileName);
}
