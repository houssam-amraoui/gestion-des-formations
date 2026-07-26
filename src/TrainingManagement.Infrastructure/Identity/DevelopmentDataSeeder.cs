using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Application.Completion;
using TrainingManagement.Application.Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace TrainingManagement.Infrastructure.Identity;

public sealed class DevelopmentDataSeeder(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<SeedTrainerOptions> options,
    IOptions<SeedLearnerOptions> learnerOptions,
    IServiceProvider services)
{
    public async Task SeedAsync()
    {
        var trainer = await SeedTrainerAsync();
        var categories = await SeedCategoriesAsync();
        await SeedTrainingsAsync(categories, trainer.Id);
        await SeedPedagogicalContentAsync();
        await SeedAssessmentsAsync();
        await SeedAiTrainerProfileAsync();
        await SeedLearnerEnrollmentAsync();
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
        var configured = await dbContext.Trainings.SingleAsync(x => x.Slug == "aspnet-core-mvc-fondamentaux");
        configured.RequireAllLessonsCompleted = true;
        configured.RequireAllMandatoryAssessmentsPassed = true;
        configured.MinimumAverageScore = 60;
        configured.CertificateEnabled = true;
        configured.CertificateValidityMonths = null;
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

    private async Task SeedAssessmentsAsync()
    {
        var lesson = await dbContext.Lessons
            .Include(x => x.TrainingModule).ThenInclude(x => x.Training)
            .SingleAsync(x => x.Slug == "presentation-dotnet-aspnet-core");

        var quiz = await dbContext.Assessments.Include(x => x.Questions).ThenInclude(x => x.AnswerOptions)
            .SingleOrDefaultAsync(x => x.LessonId == lesson.Id && x.Slug == "quiz-introduction-aspnet-core");
        if (quiz is null)
        {
            quiz = new Assessment
            {
                LessonId = lesson.Id, Lesson = lesson, Title = "Quiz d’introduction à ASP.NET Core",
                Slug = "quiz-introduction-aspnet-core", Description = "Validez les notions essentielles de cette introduction.",
                AssessmentType = AssessmentType.Quiz, Order = 1, PassingScore = 70,
                TimeLimitMinutes = 10, MaximumAttempts = 3, ShowCorrectAnswers = true
            };
            dbContext.Assessments.Add(quiz);
            await dbContext.SaveChangesAsync();
        }
        quiz.IsMandatory = true;

        await SeedQuestionAsync(quiz, 1, QuestionType.SingleChoice,
            "Quel composant reçoit principalement les requêtes HTTP dans une application ASP.NET Core MVC ?",
            1, null, [("Controller", true), ("View", false), ("Model", false), ("Migration", false)]);
        await SeedQuestionAsync(quiz, 2, QuestionType.MultipleChoice,
            "Quels éléments appartiennent au modèle MVC ?", 3, null,
            [("Model", true), ("View", true), ("Controller", true), ("Repository Git", false)]);
        await SeedQuestionAsync(quiz, 3, QuestionType.TrueFalse,
            "Razor est utilisé pour générer des vues dynamiques.", 1, null,
            [("Vrai", true), ("Faux", false)]);
        await SeedQuestionAsync(quiz, 4, QuestionType.ShortAnswer,
            "Quel fichier contient généralement le point d’entrée d’une application ASP.NET Core moderne ?",
            1, "Program.cs", []);

        quiz = await dbContext.Assessments.Include(x => x.Lesson).ThenInclude(x => x.TrainingModule)
            .ThenInclude(x => x.Training).Include(x => x.Questions).SingleAsync(x => x.Id == quiz.Id);
        quiz.Publish(DateTime.UtcNow);

        var practice = await dbContext.Assessments.SingleOrDefaultAsync(x =>
            x.LessonId == lesson.Id && x.Slug == "exercice-pratique-mvc");
        if (practice is null)
        {
            practice = new Assessment
            {
                LessonId = lesson.Id, Title = "Exercice pratique MVC", Slug = "exercice-pratique-mvc",
                Description = "Exercice d’entraînement non noté.", AssessmentType = AssessmentType.Practice,
                Order = 2, PassingScore = 0
            };
            dbContext.Assessments.Add(practice);
            await dbContext.SaveChangesAsync();
        }
        await SeedQuestionAsync(practice, 1, QuestionType.ShortAnswer,
            "Nommez les trois responsabilités principales de MVC.", 0, "Model, View et Controller", [], false);
        await SeedQuestionAsync(practice, 2, QuestionType.ShortAnswer,
            "Quel moteur de vues est utilisé par ASP.NET Core MVC ?", 0, "Razor", [], false);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedQuestionAsync(Assessment assessment, int order, QuestionType type,
        string statement, decimal points, string? expectedAnswer,
        IReadOnlyCollection<(string Text, bool Correct)> options, bool published = true)
    {
        var question = await dbContext.Questions.Include(x => x.AnswerOptions)
            .SingleOrDefaultAsync(x => x.AssessmentId == assessment.Id && x.Order == order);
        if (question is null)
        {
            question = new Question
            {
                AssessmentId = assessment.Id, Assessment = assessment, QuestionType = type,
                Statement = statement, Order = order, Points = points,
                ExpectedAnswer = expectedAnswer, Explanation = "Explication de référence pour le formateur."
            };
            dbContext.Questions.Add(question);
            await dbContext.SaveChangesAsync();
        }
        var optionOrder = 1;
        foreach (var option in options)
        {
            if (!question.AnswerOptions.Any(x => x.Text == option.Text))
            {
                var entity = new AnswerOption
                {
                    QuestionId = question.Id, Question = question, Text = option.Text,
                    Order = optionOrder, IsCorrect = option.Correct
                };
                dbContext.AnswerOptions.Add(entity);
            }
            optionOrder++;
        }
        await dbContext.SaveChangesAsync();
        if (published && !question.IsPublished)
        {
            question.Assessment = assessment;
            question.Publish(DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedLearnerEnrollmentAsync()
    {
        var settings = learnerOptions.Value;
        var learner = await userManager.FindByEmailAsync(settings.Email);
        if (learner is null)
        {
            learner = new ApplicationUser
            {
                UserName = settings.Email, Email = settings.Email, EmailConfirmed = true,
                FirstName = settings.FirstName, LastName = settings.LastName,
                IsActive = true, CreatedAt = DateTime.UtcNow
            };
            var created = await userManager.CreateAsync(learner, settings.Password);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
        }
        if (!await userManager.IsInRoleAsync(learner, AppRoles.Learner))
        {
            if (!await roleManager.RoleExistsAsync(AppRoles.Learner))
                await roleManager.CreateAsync(new IdentityRole(AppRoles.Learner));
            await userManager.AddToRoleAsync(learner, AppRoles.Learner);
        }

        var training = await dbContext.Trainings.SingleAsync(x => x.Slug == "aspnet-core-mvc-fondamentaux");
        var enrollment = await dbContext.Enrollments.SingleOrDefaultAsync(x =>
            x.LearnerId == learner.Id && x.TrainingId == training.Id &&
            x.Status != EnrollmentStatus.Cancelled);
        if (enrollment is null)
        {
            enrollment = new Enrollment
            {
                LearnerId = learner.Id, TrainingId = training.Id, Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow, StartedAt = DateTime.UtcNow
            };
            dbContext.Enrollments.Add(enrollment);
            await dbContext.SaveChangesAsync();
        }
        var lessons = await dbContext.Lessons.Where(x => x.TrainingModule.TrainingId == training.Id &&
            x.IsPublished && !x.IsArchived && x.TrainingModule.IsPublished && !x.TrainingModule.IsArchived)
            .OrderBy(x => x.TrainingModule.Order).ThenBy(x => x.Order).ToListAsync();
        var completionService = services.GetService<ITrainingCompletionService>();
        var certificateService = services.GetService<ICertificateService>();
        if (completionService is null || certificateService is null)
        {
            foreach (var lesson in lessons.Take(2))
            {
                var progress = await dbContext.LessonProgresses.SingleOrDefaultAsync(x =>
                    x.EnrollmentId == enrollment.Id && x.LessonId == lesson.Id);
                if (progress is null)
                {
                    progress = new LessonProgress { EnrollmentId = enrollment.Id, LessonId = lesson.Id };
                    dbContext.LessonProgresses.Add(progress);
                }
                if (lesson == lessons.First()) progress.Complete(DateTime.UtcNow.AddDays(-1));
                else progress.RecordAccess(DateTime.UtcNow.AddHours(-1));
            }
            await dbContext.SaveChangesAsync();
            var completedCount = await dbContext.LessonProgresses.CountAsync(x =>
                x.EnrollmentId == enrollment.Id && x.Status == LessonProgressStatus.Completed);
            enrollment.SetProgress(lessons.Count == 0 ? 0 : completedCount * 100m / lessons.Count, DateTime.UtcNow);
            enrollment.LastAccessedAt ??= DateTime.UtcNow.AddHours(-1);
            await dbContext.SaveChangesAsync();
            return;
        }
        foreach (var lesson in lessons)
        {
            var progress = await dbContext.LessonProgresses.SingleOrDefaultAsync(x =>
                x.EnrollmentId == enrollment.Id && x.LessonId == lesson.Id);
            if (progress is null)
            {
                progress = new LessonProgress { EnrollmentId = enrollment.Id, LessonId = lesson.Id };
                dbContext.LessonProgresses.Add(progress);
            }
            progress.Complete(DateTime.UtcNow.AddDays(-1));
            progress.TimeSpentSeconds = Math.Max(progress.TimeSpentSeconds, lesson.EstimatedDurationMinutes * 60);
        }
        await dbContext.SaveChangesAsync();
        var mandatory = await dbContext.Assessments.SingleAsync(x =>
            x.Lesson.TrainingModule.TrainingId == training.Id && x.IsMandatory);
        if (!await dbContext.AssessmentAttempts.AnyAsync(x => x.EnrollmentId == enrollment.Id &&
            x.AssessmentId == mandatory.Id && x.Passed == true))
        {
            dbContext.AssessmentAttempts.Add(new AssessmentAttempt
            {
                EnrollmentId = enrollment.Id, AssessmentId = mandatory.Id, AttemptNumber =
                    (await dbContext.AssessmentAttempts.Where(x => x.EnrollmentId == enrollment.Id &&
                        x.AssessmentId == mandatory.Id).MaxAsync(x => (int?)x.AttemptNumber) ?? 0) + 1,
                Status = AttemptStatus.Submitted, StartedAt = DateTime.UtcNow.AddHours(-1),
                SubmittedAt = DateTime.UtcNow.AddMinutes(-45), Score = 100, MaximumScore = 100,
                PercentageScore = 100, Passed = true, DurationSeconds = 900
            });
        }
        enrollment.LastAccessedAt ??= DateTime.UtcNow.AddHours(-1);
        await dbContext.SaveChangesAsync();
        await completionService.FinalizeAsync(enrollment.Id);
        await certificateService.GenerateAsync(enrollment.Id);
    }

    private async Task SeedAiTrainerProfileAsync()
    {
        var training = await dbContext.Trainings
            .SingleAsync(x => x.Slug == "aspnet-core-mvc-fondamentaux");
        var profile = await dbContext.AiTrainerProfiles
            .SingleOrDefaultAsync(x => x.TrainingId == training.Id);
        if (profile is not null)
            return;

        dbContext.AiTrainerProfiles.Add(new AiTrainerProfile
        {
            TrainingId = training.Id,
            DisplayName = "Coach ASP.NET Core",
            Description = "Un assistant pédagogique de démonstration pour réviser les notions de la formation.",
            Provider = "Mock",
            LanguageCode = "fr-FR",
            SystemPrompt = "Tu es un formateur bienveillant spécialisé en ASP.NET Core MVC. Réponds uniquement à partir du contexte pédagogique fourni. Si l’information manque, indique-le clairement.",
            WelcomeMessage = "Bonjour ! Je suis votre coach ASP.NET Core. Posez-moi une question sur cette leçon.",
            FallbackMessage = "Je n’ai pas trouvé cette information dans le contenu de la leçon. Reformulez votre question ou consultez les ressources du cours.",
            IsEnabled = true,
            AllowTextInput = true,
            AllowAudioInput = true,
            AllowAudioOutput = false,
            AllowAvatar = false,
            MaximumMessagesPerSession = 20,
            MaximumSessionMinutes = 30,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}
