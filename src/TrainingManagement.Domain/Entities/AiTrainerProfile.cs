using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Domain.Entities;

public sealed class AiTrainerProfile
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    [Required, MaxLength(150)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [Required, MaxLength(100)] public string Provider { get; set; } = "Mock";
    [MaxLength(250)] public string? AvatarId { get; set; }
    [MaxLength(250)] public string? VoiceId { get; set; }
    [Required, MaxLength(20)] public string LanguageCode { get; set; } = "fr-FR";
    [MaxLength(10000)] public string? SystemPrompt { get; set; }
    [MaxLength(2000)] public string? WelcomeMessage { get; set; }
    [MaxLength(1000)] public string? FallbackMessage { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool AllowTextInput { get; set; } = true;
    public bool AllowAudioInput { get; set; }
    public bool AllowAudioOutput { get; set; }
    public bool AllowAvatar { get; set; }
    public int MaximumMessagesPerSession { get; set; } = 20;
    public int MaximumSessionMinutes { get; set; } = 30;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<AiConversationSession> Sessions { get; set; } = new List<AiConversationSession>();

    public void Validate(bool providerNeedsAvatar = false, bool providerNeedsVoice = false)
    {
        if (string.IsNullOrWhiteSpace(DisplayName)) throw new InvalidOperationException("Le nom du formateur IA est obligatoire.");
        if (string.IsNullOrWhiteSpace(LanguageCode)) throw new InvalidOperationException("La langue est obligatoire.");
        if (MaximumMessagesPerSession <= 0 || MaximumSessionMinutes <= 0)
            throw new InvalidOperationException("Les limites de session doivent être strictement positives.");
        if (AllowAvatar && providerNeedsAvatar && string.IsNullOrWhiteSpace(AvatarId))
            throw new InvalidOperationException("Un identifiant d’avatar est obligatoire pour ce fournisseur.");
        if (AllowAudioOutput && providerNeedsVoice && string.IsNullOrWhiteSpace(VoiceId))
            throw new InvalidOperationException("Un identifiant de voix est obligatoire pour ce fournisseur.");
    }
}
