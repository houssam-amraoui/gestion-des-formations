namespace TrainingManagement.Application.Authentication;

public interface IAccountRegistrationService
{
    Task<RegistrationResult> RegisterLearnerAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);
}
