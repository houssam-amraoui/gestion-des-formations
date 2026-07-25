using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.LessonContents;
using TrainingManagement.Application.Lessons;
using TrainingManagement.Application.Modules;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Infrastructure.Services;
using AdminLessonsController = TrainingManagement.Web.Areas.Admin.Controllers.LessonsController;
using AdminModulesController = TrainingManagement.Web.Areas.Admin.Controllers.TrainingModulesController;
using TrainerTrainingsController = TrainingManagement.Web.Areas.Trainer.Controllers.TrainingsController;

namespace TrainingManagement.Tests;

public sealed class ModulesLessonsContentsTests
{
    [Fact]
    public async Task Module_BelongsToTraining()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var training = await AddTrainingAsync(scope);
        var result = await Modules(scope).CreateAsync(new(training.Id, "Module", null, null, null));
        var entity = await Db(scope).TrainingModules.FindAsync(result.Value);
        Assert.Equal(training.Id, entity!.TrainingId);
    }

    [Fact]
    public async Task Module_OrderIsUniqueInsideTraining()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var training = await AddTrainingAsync(scope);
        await Modules(scope).CreateAsync(new(training.Id, "Premier", "premier", null, 1));
        await Modules(scope).CreateAsync(new(training.Id, "Nouveau premier", "nouveau", null, 1));
        var orders = await Db(scope).TrainingModules.Where(x => x.TrainingId == training.Id).Select(x => x.Order).ToListAsync();
        Assert.Equal(orders.Count, orders.Distinct().Count());
        Assert.Equal([1, 2], orders.Order());
    }

    [Fact]
    public async Task SameModuleOrder_CanExistInDifferentTrainings()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var first = await AddTrainingAsync(scope, "first");
        var second = await AddTrainingAsync(scope, "second");
        await Modules(scope).CreateAsync(new(first.Id, "Module", "module", null, 1));
        await Modules(scope).CreateAsync(new(second.Id, "Module", "module", null, 1));
        Assert.Equal(2, await Db(scope).TrainingModules.CountAsync(x => x.Order == 1));
    }

    [Fact]
    public async Task ModuleSlug_IsUniqueInsideTraining()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var training = await AddTrainingAsync(scope);
        Assert.True((await Modules(scope).CreateAsync(new(training.Id, "A", "same", null, null))).Succeeded);
        Assert.False((await Modules(scope).CreateAsync(new(training.Id, "B", "same", null, null))).Succeeded);
    }

    [Fact]
    public void Module_CannotPublishWhenTrainingArchived()
    {
        var module = new TrainingModule { Training = new Training { Status = TrainingStatus.Archived } };
        Assert.Throws<InvalidOperationException>(() => module.Publish(DateTime.UtcNow));
    }

    [Fact]
    public async Task ModuleWithLesson_CannotBeDeleted()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var (_, module) = await AddModuleAsync(scope);
        Db(scope).Lessons.Add(NewLesson(module.Id));
        await Db(scope).SaveChangesAsync();
        Assert.False((await Modules(scope).DeleteAsync(module.Id)).Succeeded);
    }

    [Fact]
    public async Task ModuleMoveUpAndDown_ReordersPositions()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var training = await AddTrainingAsync(scope);
        var first = await Modules(scope).CreateAsync(new(training.Id, "A", "a", null, null));
        var second = await Modules(scope).CreateAsync(new(training.Id, "B", "b", null, null));
        await Modules(scope).MoveUpAsync(second.Value);
        Assert.Equal(1, (await Db(scope).TrainingModules.FindAsync(second.Value))!.Order);
        await Modules(scope).MoveDownAsync(second.Value);
        Assert.Equal(2, (await Db(scope).TrainingModules.FindAsync(second.Value))!.Order);
        Assert.Equal(1, (await Db(scope).TrainingModules.FindAsync(first.Value))!.Order);
    }

    [Fact]
    public async Task Lesson_BelongsToModule()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var (_, module) = await AddModuleAsync(scope);
        var result = await Lessons(scope).CreateAsync(new(module.Id, "Leçon", null, null, null, 10, false));
        Assert.Equal(module.Id, (await Db(scope).Lessons.FindAsync(result.Value))!.TrainingModuleId);
    }

    [Fact]
    public async Task LessonOrder_IsUniqueInsideModule()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var (_, module) = await AddModuleAsync(scope);
        await Lessons(scope).CreateAsync(new(module.Id, "A", "a", null, 1, 0, false));
        await Lessons(scope).CreateAsync(new(module.Id, "B", "b", null, 1, 0, false));
        var orders = await Db(scope).Lessons.Select(x => x.Order).ToListAsync();
        Assert.Equal([1, 2], orders.Order());
    }

    [Fact]
    public void Lesson_CannotPublishWhenModuleUnpublished()
    {
        var lesson = NewLesson(1);
        lesson.TrainingModule = new TrainingModule
        {
            IsPublished = false,
            Training = new Training { Status = TrainingStatus.Published }
        };
        lesson.Contents.Add(new LessonContent());
        Assert.Throws<InvalidOperationException>(() => lesson.Publish(DateTime.UtcNow));
    }

    [Fact]
    public void PublishedLesson_RequiresContent()
    {
        var lesson = NewLesson(1);
        lesson.TrainingModule = new TrainingModule
        {
            IsPublished = true,
            Training = new Training { Status = TrainingStatus.Published }
        };
        Assert.Throws<InvalidOperationException>(() => lesson.Publish(DateTime.UtcNow));
    }

    [Fact]
    public async Task ArchivedLesson_IsNotPublic()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var setup = await AddPublicLessonAsync(scope, preview: true, archived: true);
        var result = await Pedagogy(scope).GetPublicLessonAsync(setup.Training.Slug, setup.Module.Slug, setup.Lesson.Slug);
        Assert.Null(result);
    }

    [Fact]
    public async Task PreviewLesson_ContentIsPublic()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var setup = await AddPublicLessonAsync(scope, preview: true);
        var result = await Pedagogy(scope).GetPublicLessonAsync(setup.Training.Slug, setup.Module.Slug, setup.Lesson.Slug);
        Assert.True(result!.ContentAccessible);
        Assert.Single(result.Contents);
    }

    [Fact]
    public async Task NonPreviewLesson_HidesPublicContent()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var setup = await AddPublicLessonAsync(scope, preview: false);
        var result = await Pedagogy(scope).GetPublicLessonAsync(setup.Training.Slug, setup.Module.Slug, setup.Lesson.Slug);
        Assert.False(result!.ContentAccessible);
        Assert.Empty(result.Contents);
    }

    [Fact]
    public async Task TextContent_RequiresText()
    {
        var result = await CreateInvalidContentAsync(LessonContentType.Text, null, null);
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(LessonContentType.Video)]
    [InlineData(LessonContentType.Audio)]
    [InlineData(LessonContentType.Pdf)]
    [InlineData(LessonContentType.ExternalLink)]
    public async Task ExternalContent_RequiresValidUrl(LessonContentType type)
    {
        var result = await CreateInvalidContentAsync(type, null, "not-a-url");
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,bad")]
    [InlineData("file:///secret.txt")]
    public void DangerousUrl_IsRejected(string url)
    {
        var service = new ExternalMediaUrlService();
        Assert.False(service.IsValidExternalUrl(url, LessonContentType.ExternalLink));
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/76979871", "https://player.vimeo.com/video/76979871")]
    public void VideoUrl_IsConvertedToSafeEmbed(string input, string expected) =>
        Assert.Equal(expected, new ExternalMediaUrlService().GetSafeEmbedUrl(input));

    [Fact]
    public async Task Contents_AreReturnedInOrder()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var (_, _, lesson) = await AddLessonAsync(scope);
        await Contents(scope).CreateAsync(new(lesson.Id, "Second", LessonContentType.Text, "B", null, null, 1));
        await Contents(scope).CreateAsync(new(lesson.Id, "First", LessonContentType.Text, "A", null, null, 1));
        var result = await Contents(scope).GetByLessonAsync(lesson.Id);
        Assert.Equal([1, 2], result!.Items.Select(x => x.Order));
        Assert.Equal("First", result.Items.First().Title);
    }

    [Fact]
    public async Task ContentMoveUpAndDown_ReordersPositions()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var (_, _, lesson) = await AddLessonAsync(scope);
        var first = await Contents(scope).CreateAsync(new(lesson.Id, "A", LessonContentType.Text, "A", null, null, null));
        var second = await Contents(scope).CreateAsync(new(lesson.Id, "B", LessonContentType.Text, "B", null, null, null));
        await Contents(scope).MoveUpAsync(second.Value);
        Assert.Equal(1, (await Db(scope).LessonContents.FindAsync(second.Value))!.Order);
        await Contents(scope).MoveDownAsync(second.Value);
        Assert.Equal(2, (await Db(scope).LessonContents.FindAsync(second.Value))!.Order);
        Assert.Equal(1, (await Db(scope).LessonContents.FindAsync(first.Value))!.Order);
    }

    [Fact]
    public async Task UnpublishedContent_IsNotPublic()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var setup = await AddPublicLessonAsync(scope, preview: true);
        Db(scope).LessonContents.Add(new LessonContent
        {
            LessonId = setup.Lesson.Id, Title = "Hidden", ContentType = LessonContentType.Text,
            TextContent = "Secret", Order = 2, IsPublished = false
        });
        await Db(scope).SaveChangesAsync();
        var result = await Pedagogy(scope).GetPublicLessonAsync(setup.Training.Slug, setup.Module.Slug, setup.Lesson.Slug);
        Assert.DoesNotContain(result!.Contents, x => x.Title == "Hidden");
    }

    [Fact]
    public void Learner_CannotAccessAdminControllers()
    {
        var attribute = typeof(AdminModulesController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal(AppRoles.Admin, attribute.Roles);
    }

    [Fact]
    public void TrainerController_ExposesNoMutationActions()
    {
        var names = typeof(TrainerTrainingsController).GetMethods().Select(method => method.Name).ToHashSet();
        Assert.DoesNotContain("Create", names);
        Assert.DoesNotContain("Edit", names);
        Assert.DoesNotContain("Delete", names);
        Assert.DoesNotContain("Publish", names);
    }

    [Fact]
    public async Task Trainer_CanReadAssignedTraining()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var training = await AddTrainingAsync(scope, trainerId: "trainer-1");
        Assert.NotNull(await Pedagogy(scope).GetTrainerTrainingAsync(training.Id, "trainer-1"));
    }

    [Fact]
    public async Task Trainer_CannotReadAnotherTraining()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var training = await AddTrainingAsync(scope, trainerId: "trainer-1");
        Assert.Null(await Pedagogy(scope).GetTrainerTrainingAsync(training.Id, "trainer-2"));
    }

    [Fact]
    public async Task Admin_CanPreviewUnpublishedLesson()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var (_, _, lesson) = await AddLessonAsync(scope);
        Assert.NotNull(await Pedagogy(scope).GetAdminPreviewAsync(lesson.Id));
        Assert.Equal(AppRoles.Admin, typeof(AdminLessonsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles);
    }

    [Fact]
    public async Task DevelopmentSeed_IsIdempotent()
    {
        await using var provider = CreateProvider(seedOptions: true);
        using var scope = provider.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roles.CreateAsync(new IdentityRole(AppRoles.Trainer));
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        await seeder.SeedAsync();
        var first = (await Db(scope).TrainingModules.CountAsync(), await Db(scope).Lessons.CountAsync(), await Db(scope).LessonContents.CountAsync());
        Db(scope).ChangeTracker.Clear();
        await seeder.SeedAsync();
        var second = (await Db(scope).TrainingModules.CountAsync(), await Db(scope).Lessons.CountAsync(), await Db(scope).LessonContents.CountAsync());
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task DevelopmentSeed_LinksHierarchyCorrectly()
    {
        await using var provider = CreateProvider(seedOptions: true);
        using var scope = provider.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roles.CreateAsync(new IdentityRole(AppRoles.Trainer));
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
        var training = await Db(scope).Trainings.Include(x => x.Modules).ThenInclude(x => x.Lessons)
            .ThenInclude(x => x.Contents).SingleAsync(x => x.Slug == "aspnet-core-mvc-fondamentaux");
        Assert.True(training.Modules.Count >= 3);
        Assert.Contains(training.Modules.SelectMany(x => x.Lessons).SelectMany(x => x.Contents), x => x.ContentType == LessonContentType.Pdf);
    }

    private static ServiceProvider CreateProvider(bool seedOptions = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton<IExternalMediaUrlService, ExternalMediaUrlService>();
        services.AddScoped<ITrainingModuleService, TrainingModuleService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ILessonContentService, LessonContentService>();
        services.AddScoped<IPedagogyReadService, PedagogyReadService>();
        if (seedOptions)
        {
            services.AddSingleton<IOptions<SeedTrainerOptions>>(Options.Create(new SeedTrainerOptions
            {
                Email = "trainer@seed.test", Password = "Trainer123!",
                FirstName = "Seed", LastName = "Trainer"
            }));
            services.AddScoped<DevelopmentDataSeeder>();
        }
        return services.BuildServiceProvider();
    }

    private static ApplicationDbContext Db(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    private static ITrainingModuleService Modules(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ITrainingModuleService>();
    private static ILessonService Lessons(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ILessonService>();
    private static ILessonContentService Contents(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ILessonContentService>();
    private static IPedagogyReadService Pedagogy(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IPedagogyReadService>();

    private static async Task<Training> AddTrainingAsync(IServiceScope scope, string suffix = "training", string? trainerId = null)
    {
        var db = Db(scope);
        var category = new Category { Name = $"Category-{Guid.NewGuid():N}", Slug = $"category-{Guid.NewGuid():N}" };
        var training = new Training
        {
            Title = $"Training {suffix}", Slug = $"{suffix}-{Guid.NewGuid():N}",
            ShortDescription = "Short", Description = "Description", Category = category,
            Level = TrainingLevel.Beginner, Language = "Français", EstimatedDurationHours = 2,
            Status = TrainingStatus.Published, PublishedAt = DateTime.UtcNow, TrainerId = trainerId
        };
        db.Add(training);
        await db.SaveChangesAsync();
        return training;
    }

    private static async Task<(Training Training, TrainingModule Module)> AddModuleAsync(IServiceScope scope)
    {
        var training = await AddTrainingAsync(scope);
        var module = new TrainingModule
        {
            TrainingId = training.Id, Training = training, Title = "Module", Slug = $"module-{Guid.NewGuid():N}",
            Order = 1, IsPublished = true
        };
        Db(scope).Add(module);
        await Db(scope).SaveChangesAsync();
        return (training, module);
    }

    private static async Task<(Training Training, TrainingModule Module, Lesson Lesson)> AddLessonAsync(IServiceScope scope)
    {
        var (training, module) = await AddModuleAsync(scope);
        var lesson = NewLesson(module.Id);
        lesson.TrainingModule = module;
        Db(scope).Add(lesson);
        await Db(scope).SaveChangesAsync();
        return (training, module, lesson);
    }

    private static Lesson NewLesson(int moduleId) => new()
    {
        TrainingModuleId = moduleId, Title = "Lesson", Slug = $"lesson-{Guid.NewGuid():N}",
        Order = 1, EstimatedDurationMinutes = 10
    };

    private static async Task<(Training Training, TrainingModule Module, Lesson Lesson)> AddPublicLessonAsync(
        IServiceScope scope, bool preview, bool archived = false)
    {
        var (training, module, lesson) = await AddLessonAsync(scope);
        lesson.IsPreview = preview;
        lesson.IsPublished = true;
        lesson.IsArchived = archived;
        Db(scope).LessonContents.Add(new LessonContent
        {
            LessonId = lesson.Id, Title = "Visible", ContentType = LessonContentType.Text,
            TextContent = "Visible text", Order = 1, IsPublished = true
        });
        await Db(scope).SaveChangesAsync();
        return (training, module, lesson);
    }

    private static async Task<TrainingManagement.Application.Common.ServiceResult<int>> CreateInvalidContentAsync(
        LessonContentType type, string? text, string? url)
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var (_, _, lesson) = await AddLessonAsync(scope);
        return await Contents(scope).CreateAsync(new(lesson.Id, "Test", type, text, url, null, null));
    }
}
