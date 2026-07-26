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
using TrainingManagement.Application.Enrollments;
using TrainingManagement.Application.Progress;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Infrastructure.Services;
using TrainingManagement.Application.Completion;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Application.Exports;
using TrainingManagement.Infrastructure.Certificates;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Infrastructure.AiTrainer;

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
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<ILessonProgressService, LessonProgressService>();
        services.AddSingleton<IAttemptScoringService, AttemptScoringService>();
        services.AddScoped<IAssessmentAttemptService, AssessmentAttemptService>();
        services.AddScoped<ITrainingCompletionService, TrainingCompletionService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddSingleton<ICertificateNumberGenerator, CertificateNumberGenerator>();
        services.AddSingleton<ICertificatePdfService, CertificatePdfService>();
        services.AddSingleton<ICertificateStorageService, LocalCertificateStorageService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<IAiTrainerProfileService, AiTrainerProfileService>();
        services.AddScoped<IAiConversationService, AiConversationService>();
        services.AddScoped<ILessonContextService, LessonContextService>();
        services.AddScoped<IAiContentModerationService, AiContentModerationService>();
        services.AddScoped<IAiConsentService, AiConsentService>();
        services.AddScoped<IAiUsageService, AiUsageService>();
        services.AddSingleton<MockAiProvider>();
        services.AddSingleton<IAiProvider>(sp => sp.GetRequiredService<MockAiProvider>());
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IAiAvatarProvider>(sp => sp.GetRequiredService<MockAiProvider>());
            services.AddSingleton<IAiLanguageModelProvider>(sp => sp.GetRequiredService<MockAiProvider>());
            services.AddSingleton<IAiSpeechToTextProvider>(sp => sp.GetRequiredService<MockAiProvider>());
            services.AddSingleton<IAiTextToSpeechProvider>(sp => sp.GetRequiredService<MockAiProvider>());
        }
        else
        {
            services.AddSingleton<UnavailableSpeechProvider>();
            services.AddSingleton<IAiSpeechToTextProvider>(sp =>
                sp.GetRequiredService<UnavailableSpeechProvider>());
            services.AddSingleton<IAiTextToSpeechProvider>(sp =>
                sp.GetRequiredService<UnavailableSpeechProvider>());
        }
        services.AddHttpClient<AnamAiProvider>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AnamOptions>>().Value;
            client.BaseAddress = new Uri(settings.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiTrainerOptions>>()
                    .Value.RequestTimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TrainingManagement/1.0");
        });
        services.AddTransient<IAiProvider>(sp => sp.GetRequiredService<AnamAiProvider>());
        services.AddTransient<IAiAvatarProvider>(sp => sp.GetRequiredService<AnamAiProvider>());
        services.AddScoped<IAiProviderFactory, AiProviderFactory>();
        services.AddSingleton<IExternalMediaUrlService, ExternalMediaUrlService>();
        services.AddScoped<IdentityDataSeeder>();
        services.Configure<SeedTrainerOptions>(configuration.GetSection(SeedTrainerOptions.SectionName));
        services.Configure<SeedLearnerOptions>(configuration.GetSection(SeedLearnerOptions.SectionName));
        services.AddOptions<ApplicationOptions>().Bind(configuration.GetSection(ApplicationOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AiTrainerOptions>().Bind(configuration.GetSection(AiTrainerOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(value => !value.Enabled || environment.IsDevelopment() ||
                (!value.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase) &&
                 !value.LanguageModelProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase) &&
                 !value.AvatarProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase) &&
                 !value.SpeechToTextProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase) &&
                 !value.TextToSpeechProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase)),
                "Le fournisseur Mock est interdit en Production.")
            .ValidateOnStart();
        services.AddOptions<AnamOptions>().Bind(configuration.GetSection(AnamOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.Configure<ExternalAiProviderOptions>("HeyGen", configuration.GetSection("HeyGen"));
        services.Configure<ExternalAiProviderOptions>("LanguageModel", configuration.GetSection("LanguageModel"));
        services.Configure<ExternalAiProviderOptions>("SpeechToText", configuration.GetSection("SpeechToText"));
        services.Configure<ExternalAiProviderOptions>("TextToSpeech", configuration.GetSection("TextToSpeech"));
        services.AddOptions<CertificateStorageOptions>().Bind(configuration.GetSection(CertificateStorageOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<DevelopmentDataSeeder>();
        return services;
    }
}
