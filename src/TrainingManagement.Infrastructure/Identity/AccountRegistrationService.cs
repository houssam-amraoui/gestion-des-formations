using Microsoft.AspNetCore.Identity;
using TrainingManagement.Application.Authentication;
using TrainingManagement.Domain.Constants;

namespace TrainingManagement.Infrastructure.Identity;

public sealed class AccountRegistrationService(
    UserManager<ApplicationUser> userManager) : IAccountRegistrationService
{
    public async Task<RegistrationResult> RegisterLearnerAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var creation = await userManager.CreateAsync(user, request.Password);
        if (!creation.Succeeded)
            return RegistrationResult.Failure(creation.Errors.Select(error => error.Description));

        var roleAssignment = await userManager.AddToRoleAsync(user, AppRoles.Learner);
        if (roleAssignment.Succeeded)
            return RegistrationResult.Success();

        await userManager.DeleteAsync(user);
        return RegistrationResult.Failure(roleAssignment.Errors.Select(error => error.Description));
    }
}
