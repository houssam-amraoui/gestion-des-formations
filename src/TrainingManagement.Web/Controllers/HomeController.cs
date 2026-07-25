using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Web.Models;

namespace TrainingManagement.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Privacy() => View();
    public IActionResult About() => View();
    public IActionResult Courses() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
