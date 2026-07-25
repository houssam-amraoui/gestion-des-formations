using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Identity;

public sealed class DevelopmentDataSeeder(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<SeedTrainerOptions> options)
{
    public async Task SeedAsync()
    {
        var trainer = await SeedTrainerAsync();
        var categories = await SeedCategoriesAsync();
        await SeedTrainingsAsync(categories, trainer.Id);
    }

    private async Task<ApplicationUser> SeedTrainerAsync()
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
            throw new InvalidOperationException("La configuration SeedTrainer est requise en Development.");
        var trainer = await userManager.FindByEmailAsync(settings.Email);
        if (trainer is null)
        {
            trainer = new ApplicationUser
            {
                UserName = settings.Email, Email = settings.Email, EmailConfirmed = true,
                FirstName = settings.FirstName, LastName = settings.LastName,
                IsActive = true, CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(trainer, settings.Password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
        if (!await userManager.IsInRoleAsync(trainer, AppRoles.Trainer))
            await userManager.AddToRoleAsync(trainer, AppRoles.Trainer);
        return trainer;
    }

    private async Task<Dictionary<string, Category>> SeedCategoriesAsync()
    {
        var seeds = new[]
        {
            ("Développement Web", "developpement-web", "Créez des applications web modernes."),
            ("Développement Mobile", "developpement-mobile", "Concevez des applications mobiles performantes."),
            ("Intelligence Artificielle", "intelligence-artificielle", "Découvrez les usages modernes de l’IA."),
            ("Bases de données", "bases-de-donnees", "Maîtrisez la conception et l’exploitation des données.")
        };
        foreach (var (name, slug, description) in seeds)
        {
            if (!await dbContext.Categories.AnyAsync(category => category.Slug == slug))
                dbContext.Categories.Add(new Category { Name = name, Slug = slug, Description = description });
        }
        await dbContext.SaveChangesAsync();
        return await dbContext.Categories.Where(category => seeds.Select(seed => seed.Item2).Contains(category.Slug))
            .ToDictionaryAsync(category => category.Slug);
    }

    private async Task SeedTrainingsAsync(IReadOnlyDictionary<string, Category> categories, string trainerId)
    {
        if (!await dbContext.Trainings.AnyAsync(training => training.Slug == "aspnet-core-mvc-fondamentaux"))
        {
            var training = new Training
            {
                Title = "ASP.NET Core MVC — Fondamentaux", Slug = "aspnet-core-mvc-fondamentaux",
                ShortDescription = "Construisez une application web robuste avec MVC et Entity Framework Core.",
                Description = "Un parcours pratique pour comprendre MVC, Razor, Entity Framework Core et les bonnes pratiques de sécurité.",
                CategoryId = categories["developpement-web"].Id, Category = categories["developpement-web"],
                TrainerId = trainerId, Level = TrainingLevel.Beginner, Language = "Français",
                EstimatedDurationHours = 18, Price = 499, IsFree = false
            };
            training.Publish(DateTime.UtcNow);
            dbContext.Trainings.Add(training);
        }
        if (!await dbContext.Trainings.AnyAsync(training => training.Slug == "flutter-premiers-pas"))
        {
            dbContext.Trainings.Add(new Training
            {
                Title = "Flutter — Premiers pas", Slug = "flutter-premiers-pas",
                ShortDescription = "Découvrez le développement mobile multiplateforme avec Flutter.",
                Description = "Une introduction progressive à Dart, aux widgets et à la création d’interfaces mobiles.",
                CategoryId = categories["developpement-mobile"].Id, TrainerId = trainerId,
                Level = TrainingLevel.Beginner, Language = "Français",
                EstimatedDurationHours = 12, Price = 0, IsFree = true, Status = TrainingStatus.Draft
            });
        }
        if (!await dbContext.Trainings.AnyAsync(training => training.Slug == "comprendre-intelligence-artificielle"))
        {
            var training = new Training
            {
                Title = "Comprendre l’intelligence artificielle", Slug = "comprendre-intelligence-artificielle",
                ShortDescription = "Les concepts essentiels de l’IA expliqués simplement.",
                Description = "Découvrez les concepts, opportunités et limites de l’intelligence artificielle moderne.",
                CategoryId = categories["intelligence-artificielle"].Id, Category = categories["intelligence-artificielle"],
                TrainerId = trainerId, Level = TrainingLevel.AllLevels, Language = "Français",
                EstimatedDurationHours = 6, Price = 0, IsFree = true
            };
            training.Publish(DateTime.UtcNow);
            dbContext.Trainings.Add(training);
        }
        await dbContext.SaveChangesAsync();
    }
}
