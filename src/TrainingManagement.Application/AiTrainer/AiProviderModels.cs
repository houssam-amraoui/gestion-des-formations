namespace TrainingManagement.Application.AiTrainer;

public sealed record AiProviderSessionRequest(Guid InternalSessionId, string DisplayName,
    string LanguageCode, string? AvatarId, string? VoiceId, string SystemPrompt,
    string WelcomeMessage, bool EnableAvatar, bool EnableAudioOutput);
public sealed record AiProviderSessionResult(bool Succeeded, string? ProviderSessionId,
    string? ClientToken, string? AvatarStatus, AiProviderError? Error = null);
public sealed record AiChatRequest(Guid InternalSessionId, string? ProviderSessionId,
    string SystemPrompt, string Context, IReadOnlyCollection<AiProviderMessage> History,
    string UserMessage, string LanguageCode);
public sealed record AiProviderMessage(string Role, string Text);
public sealed record AiChatResult(bool Succeeded, string? Text, int? InputUnits = null,
    int? OutputUnits = null, string? ProviderMessageId = null, AiProviderError? Error = null);
public sealed record AiTranscriptionResult(bool Succeeded, string? Text,
    int? AudioDurationSeconds = null, AiProviderError? Error = null);
public sealed record AiSpeechResult(bool Succeeded, byte[]? Audio = null, string? ContentType = null,
    int? AudioDurationSeconds = null, AiProviderError? Error = null);
public sealed record AiAvatarSessionResult(bool Succeeded, string? ProviderSessionId,
    string? ClientToken, string? Status, AiProviderError? Error = null);
public sealed record AiProviderError(string Code, string UserMessage, bool IsTransient = false);

public interface IAiProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<AiProviderSessionResult> StartSessionAsync(AiProviderSessionRequest request,
        CancellationToken cancellationToken = default);
    Task<AiChatResult> SendMessageAsync(AiChatRequest request,
        CancellationToken cancellationToken = default);
    Task EndSessionAsync(string? providerSessionId, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

public interface IAiAvatarProvider
{
    string Name { get; }
    Task<AiAvatarSessionResult> CreateAvatarSessionAsync(AiProviderSessionRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> SendAvatarTextAsync(string providerSessionId, string text,
        CancellationToken cancellationToken = default);
    Task EndAvatarSessionAsync(string providerSessionId, CancellationToken cancellationToken = default);
}

public interface IAiLanguageModelProvider
{
    Task<AiChatResult> CompleteAsync(AiChatRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiSpeechToTextProvider
{
    Task<AiTranscriptionResult> TranscribeAsync(Stream audio, string contentType, string languageCode,
        CancellationToken cancellationToken = default);
}

public interface IAiTextToSpeechProvider
{
    Task<AiSpeechResult> SynthesizeAsync(string text, string languageCode, string? voiceId,
        CancellationToken cancellationToken = default);
}

public interface IAiProviderFactory
{
    IAiProvider Get(string provider);
    bool IsConfigured(string provider);
}
