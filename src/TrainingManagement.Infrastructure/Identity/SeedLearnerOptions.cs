namespace TrainingManagement.Infrastructure.Identity;

public sealed class SeedLearnerOptions
{
    public const string SectionName = "SeedLearner";
    public string Email { get; set; } = "learner@training.local";
    public string Password { get; set; } = "Learner123!";
    public string FirstName { get; set; } = "Apprenant";
    public string LastName { get; set; } = "Démo";
}
