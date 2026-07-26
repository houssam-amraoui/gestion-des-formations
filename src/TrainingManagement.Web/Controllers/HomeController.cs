using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Web.Infrastructure;
using TrainingManagement.Web.Models;

namespace TrainingManagement.Web.Controllers;

public sealed class HomeController(IWebHostEnvironment environment) : Controller
{
    public IActionResult Index() => View();
    public IActionResult Privacy() => View();
    public IActionResult About() => View();
    public IActionResult Courses() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [Route("Home/Status/{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int statusCode)
    {
        var reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        if (statusCode == StatusCodes.Status400BadRequest &&
            HttpMethods.IsPost(Request.Method) &&
            string.Equals(reExecute?.OriginalPath, "/Account/Login",
                StringComparison.OrdinalIgnoreCase))
        {
            Response.Cookies.Delete(AntiforgeryCookieSettings.Name(environment),
                AntiforgeryCookieSettings.DeleteOptions(environment));
            return RedirectToAction("Login", "Account", new { requestExpired = true });
        }

        Response.StatusCode = statusCode;
        return View(new ErrorViewModel
        {
            StatusCode = statusCode,
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
