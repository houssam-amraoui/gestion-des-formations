using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Infrastructure.Identity;

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";
    public bool Enabled { get; set; }
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public bool IsValid(out string error)
    {
        error = string.Empty;
        if (!Enabled) return true;
        if (string.IsNullOrWhiteSpace(Email) || !new EmailAddressAttribute().IsValid(Email) ||
            string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            error = "Le seed administrateur activé exige une identité complète et une adresse e-mail valide.";
        else if (Password.Length < 12 || !Password.Any(char.IsUpper) || !Password.Any(char.IsLower) ||
                 !Password.Any(char.IsDigit) || !Password.Any(ch => !char.IsLetterOrDigit(ch)))
            error = "Le seed administrateur activé exige un mot de passe fort d’au moins 12 caractères.";
        return error.Length == 0;
    }
}
