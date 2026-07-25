using System.ComponentModel.DataAnnotations;
using TrainingManagement.Web.ViewModels.Account;

namespace TrainingManagement.Tests;

public sealed class RegisterViewModelTests
{
    [Fact]
    public void InvalidModel_ReportsRequiredEmailAndNames()
    {
        var model = new RegisterViewModel
        {
            Password = "Password1",
            ConfirmPassword = "Password1"
        };

        var errors = Validate(model);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.FirstName)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.LastName)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.Email)));
    }

    [Fact]
    public void InvalidModel_RejectsShortAndMismatchedPasswords()
    {
        var model = new RegisterViewModel
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            Password = "Short1",
            ConfirmPassword = "Different1"
        };

        var errors = Validate(model);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.Password)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.ConfirmPassword)));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }
}
