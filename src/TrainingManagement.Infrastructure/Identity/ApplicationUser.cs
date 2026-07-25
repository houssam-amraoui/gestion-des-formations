using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TrainingManagement.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? ProfilePictureUrl { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
