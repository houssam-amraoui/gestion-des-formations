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
        await SeedPedagogicalContentAsync();
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

    private async Task SeedPedagogicalContentAsync()
    {
        var training = await dbContext.Trainings.SingleAsync(item => item.Slug == "aspnet-core-mvc-fondamentaux");
        var introduction = await GetOrCreateModuleAsync(training, "Introduction à ASP.NET Core",
            "introduction-aspnet-core", 1, true);
        var firstApp = await GetOrCreateModuleAsync(training, "Première application MVC",
            "premiere-application-mvc", 2, true);
        await GetOrCreateModuleAsync(training, "Approfondissement",
            "approfondissement", 3, false);

        var dotnetLesson = await GetOrCreateLessonAsync(introduction,
            "Présentation de .NET et ASP.NET Core", "presentation-dotnet-aspnet-core", 1, 15, true);
        var architectureLesson = await GetOrCreateLessonAsync(introduction,
            "Architecture d’une application MVC", "architecture-application-mvc", 2, 25, false);
        var createProjectLesson = await GetOrCreateLessonAsync(firstApp,
            "Créer un projet ASP.NET Core MVC", "creer-projet-aspnet-core-mvc", 1, 20, true);
        await GetOrCreateLessonAsync(firstApp,
            "Configurer son environnement", "configurer-environnement", 2, 10, false, published: false);

        await SeedContentAsync(dotnetLesson, "Bienvenue", LessonContentType.Text, 1,
            "ASP.NET Core est un framework moderne, multiplateforme et performant pour construire des applications web.", null, true);
        await SeedContentAsync(dotnetLesson, "Découvrir ASP.NET Core", LessonContentType.Video, 2,
            null, "https://www.youtube.com/watch?v=dQw4w9WgXcQ", true);
        await SeedContentAsync(dotnetLesson, "Documentation officielle", LessonContentType.ExternalLink, 3,
            null, "https://learn.microsoft.com/aspnet/core/", true);
        await SeedContentAsync(dotnetLesson, "Guide PDF", LessonContentType.Pdf, 4,
            null, "https://example.com/aspnet-core-guide.pdf", true);

        await SeedContentAsync(architectureLesson, "Le modèle MVC", LessonContentType.Text, 1,
            "MVC sépare les responsabilités entre le modèle, les vues et les contrôleurs.", null, true);
        await SeedContentAsync(architectureLesson, "Ressource MVC", LessonContentType.ExternalLink, 2,
            null, "https://learn.microsoft.com/aspnet/core/mvc/overview", true);
        await SeedContentAsync(architectureLesson, "Note interne", LessonContentType.Text, 3,
            "Ce bloc non publié ne doit jamais apparaître sur la page publique.", null, false);

        await SeedContentAsync(createProjectLesson, "Créer le projet", LessonContentType.Text, 1,
            "Utilisez la commande dotnet new mvc pour créer une première application MVC.", null, true);
        await SeedContentAsync(createProjectLesson, "Démonstration vidéo", LessonContentType.Video, 2,
            null, "https://vimeo.com/76979871", true);
        await SeedContentAsync(createProjectLesson, "Résumé audio", LessonContentType.Audio, 3,
            null, "https://example.com/audio/introduction.mp3", true);

        await PublishSeedLessonAsync(dotnetLesson);
        await PublishSeedLessonAsync(architectureLesson);
        await PublishSeedLessonAsync(createProjectLesson);
        await dbContext.SaveChangesAsync();
    }

    private async Task<TrainingModule> GetOrCreateModuleAsync(
        Training training, string title, string slug, int order, bool published)
    {
        var module = await dbContext.TrainingModules
            .Include(item => item.Training)
            .SingleOrDefaultAsync(item => item.TrainingId == training.Id && item.Slug == slug);
        if (module is null)
        {
            module = new TrainingModule
            {
                TrainingId = training.Id, Training = training, Title = title, Slug = slug,
                Order = order, Description = $"Module : {title}", CreatedAt = DateTime.UtcNow
            };
            dbContext.TrainingModules.Add(module);
            await dbContext.SaveChangesAsync();
        }
        if (published && !module.IsPublished) module.Publish(DateTime.UtcNow);
        return module;
    }

    private async Task<Lesson> GetOrCreateLessonAsync(
        TrainingModule module, string title, string slug, int order,
        int duration, bool preview, bool published = true)
    {
        var lesson = await dbContext.Lessons
            .Include(item => item.TrainingModule).ThenInclude(item => item.Training)
            .Include(item => item.Contents)
            .SingleOrDefaultAsync(item => item.TrainingModuleId == module.Id && item.Slug == slug);
        if (lesson is null)
        {
            lesson = new Lesson
            {
                TrainingModuleId = module.Id, TrainingModule = module, Title = title, Slug = slug,
                Summary = $"Résumé de la leçon : {title}", Order = order,
                EstimatedDurationMinutes = duration, IsPreview = preview, CreatedAt = DateTime.UtcNow
            };
            dbContext.Lessons.Add(lesson);
            await dbContext.SaveChangesAsync();
        }
        if (!published) lesson.IsPublished = false;
        return lesson;
    }

    private async Task SeedContentAsync(Lesson lesson, string title, LessonContentType type,
        int order, string? text, string? url, bool published)
    {
        if (await dbContext.LessonContents.AnyAsync(item => item.LessonId == lesson.Id && item.Order == order))
            return;
        var content = new LessonContent
        {
            LessonId = lesson.Id, Lesson = lesson, Title = title, ContentType = type,
            TextContent = text, ExternalUrl = url, Order = order,
            IsPublished = published, CreatedAt = DateTime.UtcNow
        };
        dbContext.LessonContents.Add(content);
        lesson.Contents.Add(content);
        await dbContext.SaveChangesAsync();
    }

    private static Task PublishSeedLessonAsync(Lesson lesson)
    {
        if (!lesson.IsPublished) lesson.Publish(DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
