using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Web.ViewModels.Certificates;

namespace TrainingManagement.Web.Controllers;

[Route("Certificates/Verify"), EnableRateLimiting("certificate-verification")]
public sealed class CertificatesController(ICertificateService service) : Controller
{
    [HttpGet("")]
    public IActionResult Verify() => View(new CertificateVerifyViewModel());

    [HttpPost(""), ValidateAntiForgeryToken]
    public IActionResult Verify(CertificateVerifyViewModel model) =>
        !ModelState.IsValid ? View(model) : RedirectToAction(nameof(Result), new { verificationCode = model.Query.Trim() });

    [HttpGet("{verificationCode}", Name = "VerifyCertificate")]
    public async Task<IActionResult> Result(string verificationCode, CancellationToken token) =>
        View("Verify", new CertificateVerifyViewModel
        {
            Query = verificationCode,
            Result = await service.VerifyAsync(verificationCode, token)
        });
}
