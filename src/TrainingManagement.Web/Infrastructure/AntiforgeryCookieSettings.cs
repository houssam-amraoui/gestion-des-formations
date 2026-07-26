using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace TrainingManagement.Web.Infrastructure;

public static class AntiforgeryCookieSettings
{
    public const string DevelopmentName = "TrainingManagement.Antiforgery";
    public const string ProductionName = "__Host-TrainingManagement.Antiforgery";

    public static string Name(IHostEnvironment environment) =>
        environment.IsDevelopment() ? DevelopmentName : ProductionName;

    public static CookieOptions DeleteOptions(IHostEnvironment environment) => new()
    {
        HttpOnly = true,
        IsEssential = true,
        Path = "/",
        SameSite = SameSiteMode.Lax,
        Secure = !environment.IsDevelopment()
    };
}
