namespace TrainingManagement.Infrastructure.Identity;

public sealed class SeedTrainerOptions
{
    public const string SectionName = "SeedTrainer";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
