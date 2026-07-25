using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingManagement.Application.Authentication;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Tests;

public sealed class AccountRegistrationServiceTests
{
    [Fact]
    public async Task RegisterLearnerAsync_AssignsOnlyLearnerRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole(AppRoles.Learner));
        var service = scope.ServiceProvider.GetRequiredService<IAccountRegistrationService>();

        var result = await service.RegisterLearnerAsync(
            new RegisterUserRequest("Ada", "Lovelace", "ada@example.test", "Password1"));

        Assert.True(result.Succeeded);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("ada@example.test");
        Assert.NotNull(user);
        Assert.Equal([AppRoles.Learner], await userManager.GetRolesAsync(user));
    }
}
