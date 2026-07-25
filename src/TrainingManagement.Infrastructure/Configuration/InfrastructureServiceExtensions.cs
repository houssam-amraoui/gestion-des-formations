using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TrainingManagement.Application.Authentication;
using TrainingManagement.Application.Categories;
using TrainingManagement.Application.Trainings;
using TrainingManagement.Application.Modules;
using TrainingManagement.Application.Lessons;
using TrainingManagement.Application.LessonContents;
using TrainingManagement.Application.Pedagogy;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Application.Questions;
using TrainingManagement.Application.AnswerOptions;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Infrastructure.Services;

namespace TrainingManagement.Infrastructure.Configuration;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddTrainingManagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (environment.IsDevelopment())
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
                : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        });

        services.AddOptions<SeedAdminOptions>()
            .Bind(configuration.GetSection(SeedAdminOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITrainingService, TrainingService>();
        services.AddScoped<ITrainingModuleService, TrainingModuleService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ILessonContentService, LessonContentService>();
        services.AddScoped<IPedagogyReadService, PedagogyReadService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAnswerOptionService, AnswerOptionService>();
        services.AddSingleton<IExternalMediaUrlService, ExternalMediaUrlService>();
        services.AddScoped<IdentityDataSeeder>();
        services.Configure<SeedTrainerOptions>(configuration.GetSection(SeedTrainerOptions.SectionName));
        services.AddScoped<DevelopmentDataSeeder>();
        return services;
    }
}
