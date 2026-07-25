namespace TrainingManagement.Application.Authentication;

public sealed record RegistrationResult(bool Succeeded, IReadOnlyCollection<string> Errors)
{
    public static RegistrationResult Success() => new(true, []);
    public static RegistrationResult Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}
