using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Infrastructure.Identity;

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}
