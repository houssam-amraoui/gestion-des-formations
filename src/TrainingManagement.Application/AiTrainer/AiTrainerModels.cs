using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.AiTrainer;

public sealed record AiTrainerProfileInput(int TrainingId, string DisplayName, string? Description,
    string Provider, string? AvatarId, string? VoiceId, string LanguageCode, string? SystemPrompt,
    string? WelcomeMessage, string? FallbackMessage, bool IsEnabled, bool AllowTextInput,
    bool AllowAudioInput, bool AllowAudioOutput, bool AllowAvatar, int MaximumMessagesPerSession,
    int MaximumSessionMinutes);

public sealed record AiTrainerProfileListItem(int Id, int TrainingId, string TrainingTitle,
    string DisplayName, string Provider, string LanguageCode, bool IsEnabled, bool ProviderConfigured);

public sealed record AiTrainerProfileDetails(int Id, int TrainingId, string TrainingTitle,
    string DisplayName, string? Description, string Provider, string? AvatarId, string? VoiceId,
    string LanguageCode, string? WelcomeMessage, string? FallbackMessage, bool IsEnabled,
    bool AllowTextInput, bool AllowAudioInput, bool AllowAudioOutput, bool AllowAvatar,
    int MaximumMessagesPerSession, int MaximumSessionMinutes, bool ProviderConfigured,
    string SystemPromptSummary);

public sealed record AiTrainerProfileEditModel(int Id, AiTrainerProfileInput Input);

public sealed record AiMessageModel(long Id, AiMessageRole Role, string Text, string? Transcription,
    int SequenceNumber, DateTime CreatedAt, bool IsModerated, bool HasAudio);

public sealed record AiSessionModel(Guid Id, int ProfileId, int TrainingId, string TrainingTitle,
    int LessonId, string LessonTitle, string TrainerDisplayName, string Provider,
    AiConversationStatus Status, DateTime StartedAt, DateTime LastActivityAt, DateTime ExpiresAt,
    int MessageCount, int MaximumMessages, int RemainingMessages, int RemainingSeconds,
    bool AllowTextInput, bool AllowAudioInput, bool AllowAudioOutput, bool AllowAvatar,
    string? AvatarClientToken, string? AvatarStatus, IReadOnlyCollection<AiMessageModel> Messages);

public sealed record AiSessionListItem(Guid Id, int TrainingId, string TrainingTitle, int LessonId,
    string LessonTitle, string UserId, string Provider, AiConversationStatus Status,
    DateTime StartedAt, DateTime LastActivityAt, int MessageCount, bool HasFailure);

public sealed record AiSessionFilter(int? TrainingId = null, string? User = null, string? Provider = null,
    AiConversationStatus? Status = null, DateTime? DateFrom = null, DateTime? DateTo = null,
    int Page = 1, int PageSize = 20);

public sealed record AiConversationReply(AiSessionModel Session, string AssistantText,
    bool AudioAvailable, string? AvatarStatus, long ElapsedMilliseconds);

public sealed record AiAvatarAccess(string Provider, string? ClientToken, string Status);

public sealed record LessonAiAvailability(int LessonId, bool Available, string? Reason,
    int? ProfileId, string? DisplayName, string? Description, string? LanguageCode,
    bool AllowAudioInput, bool AllowAudioOutput, bool AllowAvatar);

public sealed record LessonContextModel(int LessonId, string TrainingTitle, string ModuleTitle,
    string LessonTitle, string Content, bool WasTruncated);

public sealed record AiModerationResult(bool Allowed, string? SafeMessage = null,
    string? ReasonCode = null, bool Suspicious = false);

public sealed record AiUsageSummary(int Sessions, int Users, int Messages, int AudioSeconds,
    int Failures, decimal ErrorRate, decimal? EstimatedCost,
    IReadOnlyCollection<AiUsageBreakdown> Providers,
    IReadOnlyCollection<AiUsageBreakdown> Trainings);

public sealed record AiUsageBreakdown(string Label, int Sessions, int Messages, decimal? EstimatedCost);

public sealed record AiConsentModel(bool IsValid, string Provider, string PolicyVersion,
    DateTime? AcceptedAt);

public interface IAiTrainerProfileService
{
    Task<IReadOnlyCollection<AiTrainerProfileListItem>> GetAllAsync(string? trainerId = null,
        CancellationToken cancellationToken = default);
    Task<AiTrainerProfileDetails?> GetAsync(int id, string? trainerId = null,
        CancellationToken cancellationToken = default);
    Task<AiTrainerProfileEditModel?> GetEditAsync(int id,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(AiTrainerProfileInput input,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(int id, AiTrainerProfileInput input,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> SetEnabledAsync(int id, bool enabled,
        CancellationToken cancellationToken = default);
    Task<LessonAiAvailability> GetAvailabilityAsync(int lessonId, string userId, bool isAdmin,
        bool isTrainer, CancellationToken cancellationToken = default);
}

public interface IAiConversationService
{
    Task<ServiceResult<AiSessionModel>> StartAsync(int lessonId, string userId, bool isAdmin,
        bool isTrainer, CancellationToken cancellationToken = default);
    Task<AiSessionModel?> GetAsync(Guid id, string userId, bool isAdmin, bool isTrainer,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<AiAvatarAccess>> CreateAvatarAccessAsync(Guid id, string userId,
        bool isAdmin, bool isTrainer, CancellationToken cancellationToken = default);
    Task<ServiceResult<AiConversationReply>> SendTextAsync(Guid id, string userId, string text,
        bool isAdmin, bool isTrainer, CancellationToken cancellationToken = default);
    Task<ServiceResult<AiConversationReply>> SendAudioAsync(Guid id, string userId, Stream audio,
        string contentType, long length, bool isAdmin, bool isTrainer,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> EndAsync(Guid id, string userId, bool isAdmin, bool isTrainer,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AiSessionListItem>> GetHistoryAsync(string userId, int? lessonId = null,
        CancellationToken cancellationToken = default);
    Task<PagedResult<AiSessionListItem>> GetAdminAsync(AiSessionFilter filter,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AiSessionListItem>> GetTrainerAsync(string trainerId,
        CancellationToken cancellationToken = default);
    Task<int> ExpireSessionsAsync(CancellationToken cancellationToken = default);
    Task<int> AnonymizeExpiredHistoryAsync(CancellationToken cancellationToken = default);
}

public interface ILessonContextService
{
    Task<LessonContextModel?> BuildAsync(int lessonId, CancellationToken cancellationToken = default);
}

public interface IAiContentModerationService
{
    AiModerationResult ModerateText(string text);
    AiModerationResult ModerateAudio(string contentType, long length);
}

public interface IAiUsageService
{
    Task<AiUsageSummary> GetAdminAsync(DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
    Task<AiUsageSummary> GetTrainerAsync(string trainerId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
    Task<AiUsageSummary> GetLearnerAsync(string userId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}

public interface IAiConsentService
{
    Task<AiConsentModel> GetAsync(string userId, string provider,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> AcceptAsync(string userId, string provider,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> RevokeAsync(string userId, string provider,
        CancellationToken cancellationToken = default);
}
