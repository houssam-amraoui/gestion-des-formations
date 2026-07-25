using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Web.ViewModels.Account;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Le prénom est obligatoire."), StringLength(100)]
    [Display(Name = "Prénom")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire."), StringLength(100)]
    [Display(Name = "Nom")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "L’adresse e-mail est obligatoire."), EmailAddress(ErrorMessage = "L’adresse e-mail n’est pas valide.")]
    [Display(Name = "Adresse e-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire."), StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password), Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas.")]
    [Display(Name = "Confirmer le mot de passe")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
