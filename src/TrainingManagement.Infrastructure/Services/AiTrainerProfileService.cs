using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AiTrainerProfileService(ApplicationDbContext db, IAiProviderFactory providers) :
    IAiTrainerProfileService
{
    public async Task<IReadOnlyCollection<AiTrainerProfileListItem>> GetAllAsync(string? trainerId = null,
        CancellationToken token = default)
    {
        var rows = await db.AiTrainerProfiles.AsNoTracking()
            .Where(x => trainerId == null || x.Training.TrainerId == trainerId)
            .OrderBy(x => x.Training.Title)
            .Select(x => new { x.Id, x.TrainingId, TrainingTitle = x.Training.Title,
                x.DisplayName, x.Provider, x.LanguageCode, x.IsEnabled })
            .ToListAsync(token);
        return rows.Select(x => new AiTrainerProfileListItem(x.Id, x.TrainingId, x.TrainingTitle,
            x.DisplayName, x.Provider, x.LanguageCode, x.IsEnabled,
            providers.IsConfigured(x.Provider))).ToArray();
    }

    public async Task<AiTrainerProfileDetails?> GetAsync(int id, string? trainerId = null,
        CancellationToken token = default)
    {
        var row = await db.AiTrainerProfiles.AsNoTracking()
            .Where(x => x.Id == id && (trainerId == null || x.Training.TrainerId == trainerId))
            .Select(x => new { x.Id, x.TrainingId, TrainingTitle = x.Training.Title, x.DisplayName,
                x.Description, x.Provider, x.AvatarId, x.VoiceId, x.LanguageCode, x.WelcomeMessage,
                x.FallbackMessage, x.IsEnabled, x.AllowTextInput, x.AllowAudioInput,
                x.AllowAudioOutput, x.AllowAvatar, x.MaximumMessagesPerSession,
                x.MaximumSessionMinutes, x.SystemPrompt })
            .SingleOrDefaultAsync(token);
        return row is null ? null : new(row.Id, row.TrainingId, row.TrainingTitle, row.DisplayName,
            row.Description, row.Provider, row.AvatarId, row.VoiceId, row.LanguageCode,
            row.WelcomeMessage, row.FallbackMessage, row.IsEnabled, row.AllowTextInput,
            row.AllowAudioInput, row.AllowAudioOutput, row.AllowAvatar,
            row.MaximumMessagesPerSession, row.MaximumSessionMinutes,
            providers.IsConfigured(row.Provider), SummarizePrompt(row.SystemPrompt));
    }

    public async Task<AiTrainerProfileEditModel?> GetEditAsync(int id,
        CancellationToken token = default)
    {
        var x = await db.AiTrainerProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
        return x is null ? null : new(x.Id, new(x.TrainingId, x.DisplayName, x.Description,
            x.Provider, x.AvatarId, x.VoiceId, x.LanguageCode, x.SystemPrompt, x.WelcomeMessage,
            x.FallbackMessage, x.IsEnabled, x.AllowTextInput, x.AllowAudioInput,
            x.AllowAudioOutput, x.AllowAvatar, x.MaximumMessagesPerSession,
            x.MaximumSessionMinutes));
    }

    public async Task<ServiceResult<int>> CreateAsync(AiTrainerProfileInput input,
        CancellationToken token = default)
    {
        if (!await db.Trainings.AnyAsync(x => x.Id == input.TrainingId, token))
            return ServiceResult<int>.Failure("Formation introuvable.");
        if (await db.AiTrainerProfiles.AnyAsync(x => x.TrainingId == input.TrainingId, token))
            return ServiceResult<int>.Failure("Cette formation possède déjà un profil IA.");
        var profile = Map(new AiTrainerProfile { TrainingId = input.TrainingId }, input);
        var validation = Validate(profile);
        if (validation is not null) return ServiceResult<int>.Failure(validation);
        db.AiTrainerProfiles.Add(profile);
        await db.SaveChangesAsync(token);
        return ServiceResult<int>.Success(profile.Id);
    }

    public async Task<ServiceResult> UpdateAsync(int id, AiTrainerProfileInput input,
        CancellationToken token = default)
    {
        var profile = await db.AiTrainerProfiles.SingleOrDefaultAsync(x => x.Id == id, token);
        if (profile is null) return ServiceResult.Failure("Profil IA introuvable.");
        if (input.TrainingId != profile.TrainingId &&
            await db.AiTrainerProfiles.AnyAsync(x => x.TrainingId == input.TrainingId && x.Id != id, token))
            return ServiceResult.Failure("Cette formation possède déjà un profil IA.");
        Map(profile, input);
        profile.UpdatedAt = DateTime.UtcNow;
        var validation = Validate(profile);
        if (validation is not null) return ServiceResult.Failure(validation);
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetEnabledAsync(int id, bool enabled,
        CancellationToken token = default)
    {
        var profile = await db.AiTrainerProfiles.SingleOrDefaultAsync(x => x.Id == id, token);
        if (profile is null) return ServiceResult.Failure("Profil IA introuvable.");
        profile.IsEnabled = enabled;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    public async Task<LessonAiAvailability> GetAvailabilityAsync(int lessonId, string userId,
        bool isAdmin, bool isTrainer, CancellationToken token = default)
    {
        var row = await db.Lessons.AsNoTracking().Where(x => x.Id == lessonId)
            .Select(x => new
            {
                Lesson = x,
                TrainingId = x.TrainingModule.TrainingId,
                TrainingStatus = x.TrainingModule.Training.Status,
                TrainerId = x.TrainingModule.Training.TrainerId,
                ModulePublished = x.TrainingModule.IsPublished,
                ModuleArchived = x.TrainingModule.IsArchived,
                Profile = x.TrainingModule.Training.AiTrainerProfile
            }).SingleOrDefaultAsync(token);
        if (row is null) return new(lessonId, false, "Leçon introuvable.", null, null, null, null, false, false, false);
        if (row.TrainingStatus == TrainingStatus.Archived)
            return Unavailable(lessonId, row.Profile, "La formation est archivée.");
        if (row.Lesson.IsArchived || row.ModuleArchived)
            return Unavailable(lessonId, row.Profile, "La leçon ou son module est archivé.");
        if (row.Profile is null || !row.Profile.IsEnabled)
            return Unavailable(lessonId, row.Profile, "Le formateur IA n’est pas disponible.");
        if (!providers.IsConfigured(row.Profile.Provider))
            return Unavailable(lessonId, row.Profile, "Le fournisseur IA n’est pas configuré.");
        if (isTrainer && row.TrainerId != userId)
            return Unavailable(lessonId, row.Profile, "Cette formation n’est pas assignée à ce formateur.");
        if (!isAdmin && !isTrainer)
        {
            if (row.TrainingStatus != TrainingStatus.Published || !row.Lesson.IsPublished ||
                row.Lesson.IsArchived || !row.ModulePublished || row.ModuleArchived)
                return Unavailable(lessonId, row.Profile, "Cette leçon n’est pas accessible.");
            var enrollment = await db.Enrollments.AsNoTracking().AnyAsync(x =>
                x.LearnerId == userId && x.TrainingId == row.TrainingId &&
                (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Completed), token);
            if (!enrollment) return Unavailable(lessonId, row.Profile,
                "Une inscription active est nécessaire pour utiliser le formateur IA.");
        }
        return new(lessonId, true, null, row.Profile.Id, row.Profile.DisplayName,
            row.Profile.Description, row.Profile.LanguageCode, row.Profile.AllowAudioInput,
            row.Profile.AllowAudioOutput, row.Profile.AllowAvatar);
    }

    private static LessonAiAvailability Unavailable(int lessonId, AiTrainerProfile? profile, string reason) =>
        new(lessonId, false, reason, profile?.Id, profile?.DisplayName, profile?.Description,
            profile?.LanguageCode, profile?.AllowAudioInput ?? false,
            profile?.AllowAudioOutput ?? false, profile?.AllowAvatar ?? false);

    private string? Validate(AiTrainerProfile profile)
    {
        try
        {
            _ = providers.Get(profile.Provider);
            var isAnam = profile.Provider.Equals("Anam", StringComparison.OrdinalIgnoreCase);
            profile.Validate(isAnam, isAnam);
            return null;
        }
        catch (InvalidOperationException ex) { return ex.Message; }
    }

    private static AiTrainerProfile Map(AiTrainerProfile profile, AiTrainerProfileInput input)
    {
        profile.TrainingId = input.TrainingId;
        profile.DisplayName = input.DisplayName.Trim();
        profile.Description = input.Description?.Trim();
        profile.Provider = input.Provider.Trim();
        profile.AvatarId = input.AvatarId?.Trim();
        profile.VoiceId = input.VoiceId?.Trim();
        profile.LanguageCode = input.LanguageCode.Trim();
        profile.SystemPrompt = input.SystemPrompt?.Trim();
        profile.WelcomeMessage = input.WelcomeMessage?.Trim();
        profile.FallbackMessage = input.FallbackMessage?.Trim();
        profile.IsEnabled = input.IsEnabled;
        profile.AllowTextInput = input.AllowTextInput;
        profile.AllowAudioInput = input.AllowAudioInput;
        profile.AllowAudioOutput = input.AllowAudioOutput;
        profile.AllowAvatar = input.AllowAvatar;
        profile.MaximumMessagesPerSession = input.MaximumMessagesPerSession;
        profile.MaximumSessionMinutes = input.MaximumSessionMinutes;
        return profile;
    }

    private static string SummarizePrompt(string? prompt) => string.IsNullOrWhiteSpace(prompt)
        ? "Prompt pédagogique par défaut" : $"Prompt personnalisé configuré ({prompt.Length} caractères)";
}
