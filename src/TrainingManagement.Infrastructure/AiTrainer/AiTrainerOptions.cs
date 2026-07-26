using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Infrastructure.AiTrainer;

public sealed class AiTrainerOptions
{
    public const string SectionName = "AiTrainer";
    public bool Enabled { get; set; } = true;
    [Required] public string Provider { get; set; } = "Mock";
    [Required] public string LanguageModelProvider { get; set; } = "Mock";
    [Required] public string AvatarProvider { get; set; } = "Mock";
    [Required] public string SpeechToTextProvider { get; set; } = "Mock";
    [Required] public string TextToSpeechProvider { get; set; } = "Mock";
    [Required, MaxLength(20)] public string DefaultLanguage { get; set; } = "fr-FR";
    [MaxLength(250)] public string? DefaultVoice { get; set; }
    [Range(1, 10000)] public int MaximumQuestionLength { get; set; } = 2000;
    [Range(1, 1000)] public int MaximumMessagesPerSession { get; set; } = 20;
    [Range(1, 240)] public int MaximumSessionDurationMinutes { get; set; } = 30;
    [Range(1, 100)] public int MaximumSessionsPerUserPerDay { get; set; } = 10;
    [Range(100, 100000)] public int MaximumContextCharacters { get; set; } = 12000;
    [Range(1024, 25_000_000)] public long MaximumAudioBytes { get; set; } = 5_000_000;
    [Range(1, 600)] public int MaximumAudioSeconds { get; set; } = 120;
    [Range(1, 3650)] public int ConversationRetentionDays { get; set; } = 365;
    [Range(1, 300)] public int RequestTimeoutSeconds { get; set; } = 30;
    [Required, MaxLength(50)] public string ConsentPolicyVersion { get; set; } = "2026-01";
    public string SystemPrompt { get; set; } =
        "Tu es un formateur pédagogique. Utilise d’abord le contexte fourni, reste concis et n’invente pas.";
    public bool EnableAudioInput { get; set; } = true;
    public bool EnableAudioOutput { get; set; }
    public bool EnableAvatar { get; set; }
    public bool SaveConversationHistory { get; set; } = true;
    public bool AllowPublicPreview { get; set; }
}

public sealed class AnamOptions
{
    public const string SectionName = "Anam";
    [Url] public string BaseAddress { get; set; } = "https://api.anam.ai/v1/";
    public string? ApiKey { get; set; }
    public string? LlmId { get; set; }
}

public sealed class ExternalAiProviderOptions
{
    public string? ApiKey { get; set; }
    public string? BaseAddress { get; set; }
}
