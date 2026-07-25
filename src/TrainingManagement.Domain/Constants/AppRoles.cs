namespace TrainingManagement.Domain.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Trainer = "Trainer";
    public const string Learner = "Learner";

    public static readonly IReadOnlyCollection<string> All =
        [Admin, Trainer, Learner];
}
