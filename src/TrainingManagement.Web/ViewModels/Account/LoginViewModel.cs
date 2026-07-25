using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Web.ViewModels.Account;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "L’adresse e-mail est obligatoire."), EmailAddress]
    [Display(Name = "Adresse e-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire."), DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Se souvenir de moi")]
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
