using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingManagement.Application.Categories;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Trainings;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Infrastructure.Services;
using TrainingManagement.Web.Areas.Admin.Controllers;
using TrainingManagement.Web.ViewModels.Trainings;

namespace TrainingManagement.Tests;

public sealed class CategoriesAndTrainingsTests
{
    [Fact]
    public void SlugGenerator_NormalizesAccentsSpacesAndCase() =>
        Assert.Equal("developpement-web-avance", SlugGenerator.Generate("  Développement Web Avancé! "));

    [Fact]
    public async Task CategoryService_RejectsDuplicateSlug()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        Assert.True((await service.CreateAsync(new("Web", null, null, null))).Succeeded);
        var duplicate = await service.CreateAsync(new("Autre", "web", null, null));
        Assert.False(duplicate.Succeeded);
        Assert.Contains("slug", duplicate.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CategoryService_DoesNotDeleteUsedCategory()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new Category { Name = "Web", Slug = "web" };
        db.Add(category);
        await db.SaveChangesAsync();
        db.Trainings.Add(CreateTraining(category.Id));
        await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<ICategoryService>().DeleteAsync(category.Id);
        Assert.False(result.Succeeded);
        Assert.True(await db.Categories.AnyAsync(item => item.Id == category.Id));
    }

    [Fact]
    public async Task TrainingService_ForcesFreeTrainingPriceToZero()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var category = await AddCategoryAsync(scope, true);
        var service = scope.ServiceProvider.GetRequiredService<ITrainingService>();
        var result = await service.CreateAsync(Input(category.Id, price: 999, isFree: true));
        var training = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Trainings.FindAsync(result.Value);
        Assert.Equal(0, training!.Price);
    }

    [Fact]
    public void Training_CannotPublishWithInactiveCategory()
    {
        var training = CreateTraining(1);
        training.Category = new Category { Id = 1, IsActive = false };
        Assert.Throws<InvalidOperationException>(() => training.Publish(DateTime.UtcNow));
    }

    [Fact]
    public void Training_PublishSetsStatusAndUtcDate()
    {
        var now = DateTime.UtcNow;
        var training = CreateTraining(1);
        training.Category = new Category { Id = 1, IsActive = true };
        training.Publish(now);
        Assert.Equal(TrainingStatus.Published, training.Status);
        Assert.Equal(now, training.PublishedAt);
    }

    [Fact]
    public async Task TrainingService_RejectsUserWithoutTrainerRole()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var category = await AddCategoryAsync(scope, true);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "learner@test.local", Email = "learner@test.local", FirstName = "Test", LastName = "User" };
        await users.CreateAsync(user, "Password1");
        var input = Input(category.Id) with { TrainerId = user.Id };
        var result = await scope.ServiceProvider.GetRequiredService<ITrainingService>().CreateAsync(input);
        Assert.False(result.Succeeded);
        Assert.Contains("formateur", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicQuery_ReturnsOnlyPublishedTrainings()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = await AddCategoryAsync(scope, true);
        var published = CreateTraining(category.Id, "publiee");
        published.Category = category;
        published.Publish(DateTime.UtcNow);
        db.AddRange(published, CreateTraining(category.Id, "brouillon"));
        await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<ITrainingService>()
            .GetPublishedPagedAsync(new());
        Assert.Single(result.Items);
        Assert.Equal(TrainingStatus.Published, result.Items.Single().Status);
    }

    [Theory]
    [InlineData(typeof(CategoriesController))]
    [InlineData(typeof(TrainingsController))]
    public void AdminControllers_RequireAdminRole(Type controllerType)
    {
        var attribute = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal(AppRoles.Admin, attribute.Roles);
    }

    [Fact]
    public void TrainingViewModel_ValidatesRequiredFieldsAndPositiveDuration()
    {
        var model = new TrainingCreateViewModel { EstimatedDurationHours = 0 };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Title)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.ShortDescription)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.EstimatedDurationHours)));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITrainingService, TrainingService>();
        return services.BuildServiceProvider();
    }

    private static async Task<Category> AddCategoryAsync(IServiceScope scope, bool active)
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new Category { Name = $"Category {Guid.NewGuid():N}", Slug = $"category-{Guid.NewGuid():N}", IsActive = active };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static Training CreateTraining(int categoryId, string suffix = "test") => new()
    {
        Title = $"Formation {suffix}", Slug = $"formation-{suffix}-{Guid.NewGuid():N}",
        ShortDescription = "Description courte", Description = "Description complète",
        CategoryId = categoryId, Level = TrainingLevel.Beginner, Language = "Français",
        EstimatedDurationHours = 5, Price = 100, Status = TrainingStatus.Draft
    };

    private static TrainingInput Input(int categoryId, decimal price = 100, bool isFree = false) =>
        new("Formation test", $"formation-{Guid.NewGuid():N}", "Description courte",
            "Description complète", null, categoryId, null, TrainingLevel.Beginner,
            "Français", 5, price, isFree);
}
