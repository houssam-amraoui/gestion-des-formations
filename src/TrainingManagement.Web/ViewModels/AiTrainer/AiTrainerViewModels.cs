using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Web.ViewModels.AiTrainer;

public sealed class AiTrainerProfileFormViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Formation")] public int TrainingId { get; set; }
    [Required, StringLength(150), Display(Name = "Nom affiché")] public string DisplayName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required, StringLength(100)] public string Provider { get; set; } = "Mock";
    [StringLength(250)] public string? AvatarId { get; set; }
    [StringLength(250)] public string? VoiceId { get; set; }
    [Required, StringLength(20), Display(Name = "Langue")] public string LanguageCode { get; set; } = "fr-FR";
    [StringLength(10000), Display(Name = "Prompt pédagogique personnalisé")] public string? SystemPrompt { get; set; }
    [StringLength(2000), Display(Name = "Message de bienvenue")] public string? WelcomeMessage { get; set; }
    [StringLength(1000), Display(Name = "Message de repli")] public string? FallbackMessage { get; set; }
    [Display(Name = "Actif")] public bool IsEnabled { get; set; } = true;
    [Display(Name = "Entrée texte")] public bool AllowTextInput { get; set; } = true;
    [Display(Name = "Entrée audio")] public bool AllowAudioInput { get; set; }
    [Display(Name = "Sortie audio")] public bool AllowAudioOutput { get; set; }
    [Display(Name = "Avatar")] public bool AllowAvatar { get; set; }
    [Range(1, 1000), Display(Name = "Messages maximum")] public int MaximumMessagesPerSession { get; set; } = 20;
    [Range(1, 240), Display(Name = "Durée maximum (minutes)")] public int MaximumSessionMinutes { get; set; } = 30;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Provider.Equals("Anam", StringComparison.OrdinalIgnoreCase) && AllowAvatar &&
            string.IsNullOrWhiteSpace(AvatarId))
            yield return new("AvatarId est obligatoire lorsque l’avatar Anam est activé.", [nameof(AvatarId)]);
        if (Provider.Equals("Anam", StringComparison.OrdinalIgnoreCase) && AllowAudioOutput &&
            string.IsNullOrWhiteSpace(VoiceId))
            yield return new("VoiceId est obligatoire lorsque l’audio Anam est activé.", [nameof(VoiceId)]);
    }

    public AiTrainerProfileInput ToInput() => new(TrainingId, DisplayName, Description, Provider,
        AvatarId, VoiceId, LanguageCode, SystemPrompt, WelcomeMessage, FallbackMessage,
        IsEnabled, AllowTextInput, AllowAudioInput, AllowAudioOutput, AllowAvatar,
        MaximumMessagesPerSession, MaximumSessionMinutes);
}

public sealed class AiProfileTestViewModel
{
    public required AiTrainerProfileDetails Profile { get; init; }
    [Range(1, int.MaxValue), Display(Name = "Identifiant de la leçon")] public int LessonId { get; set; }
    [StringLength(2000), Display(Name = "Question de test")] public string? Question { get; set; }
}

public sealed class AiSendMessageViewModel
{
    public Guid SessionId { get; set; }
    [Required, StringLength(2000), Display(Name = "Votre question")] public string Text { get; set; } = string.Empty;
}

public sealed class AiSessionPageViewModel
{
    public required AiSessionModel Session { get; init; }
    public AiSendMessageViewModel Message { get; init; } = new();
    public AiConsentModel? AudioConsent { get; init; }
}

public sealed class AiSessionIndexViewModel
{
    public string? User { get; set; }
    public string? Provider { get; set; }
    public AiConversationStatus? Status { get; set; }
    [DataType(DataType.Date)] public DateTime? DateFrom { get; set; }
    [DataType(DataType.Date)] public DateTime? DateTo { get; set; }
    public PagedResult<AiSessionListItem> Results { get; set; } =
        new(Array.Empty<AiSessionListItem>(), 1, 20, 0);
}
