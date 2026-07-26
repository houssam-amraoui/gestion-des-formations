using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TrainingManagement.Domain.Constants;

namespace TrainingManagement.Infrastructure.Identity;

public sealed class IdentityDataSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<SeedAdminOptions> options)
{
    public async Task SeedAsync()
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await EnsureSucceededAsync(roleManager.CreateAsync(new IdentityRole(role)));
        }

        var settings = options.Value;
        if (!settings.Enabled) return;
        var admin = await userManager.FindByEmailAsync(settings.Email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FirstName = settings.FirstName,
                LastName = settings.LastName,
                Email = settings.Email,
                UserName = settings.Email,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await EnsureSucceededAsync(userManager.CreateAsync(admin, settings.Password));
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
            await EnsureSucceededAsync(userManager.AddToRoleAsync(admin, AppRoles.Admin));
    }

    private static async Task EnsureSucceededAsync(Task<IdentityResult> operation)
    {
        var result = await operation;
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
