using Microsoft.Extensions.Options;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Infrastructure.AiTrainer;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AiContentModerationService(IOptions<AiTrainerOptions> options) :
    IAiContentModerationService
{
    private static readonly string[] SecretPatterns =
        ["api key", "apikey", "clé api", "secret fournisseur", "token permanent"];
    private static readonly string[] PromptPatterns =
        ["révèle le prompt", "affiche le prompt système", "ignore les instructions précédentes",
         "ignore previous instructions", "change ton rôle", "system prompt"];
    private static readonly HashSet<string> AudioTypes = new(StringComparer.OrdinalIgnoreCase)
        { "audio/webm", "audio/wav", "audio/x-wav", "audio/mpeg", "audio/mp3" };

    public AiModerationResult ModerateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new(false, "Veuillez saisir une question.", "empty");
        if (text.Length > options.Value.MaximumQuestionLength)
            return new(false, $"La question dépasse la limite de {options.Value.MaximumQuestionLength} caractères.",
                "too_long");
        var normalized = text.ToLowerInvariant();
        if (SecretPatterns.Any(normalized.Contains))
            return new(false, "Je ne peux pas fournir de clés, tokens ou secrets internes.",
                "secret_extraction", true);
        if (PromptPatterns.Any(normalized.Contains))
            return new(false,
                "Je ne peux pas révéler ni modifier mes instructions internes. Posez une question sur la leçon.",
                "prompt_injection", true);
        if (normalized.Contains("<script") || normalized.Contains("javascript:") ||
            normalized.Contains("../") || normalized.Contains("powershell -") ||
            normalized.Contains("cmd.exe"))
            return new(false, "Cette demande contient un motif non autorisé.", "malicious_pattern", true);
        return new(true);
    }

    public AiModerationResult ModerateAudio(string contentType, long length)
    {
        var mime = contentType.Split(';', 2)[0].Trim();
        if (!AudioTypes.Contains(mime))
            return new(false, "Format audio non pris en charge. Utilisez WebM, WAV ou MP3.", "invalid_audio_type");
        if (length <= 0 || length > options.Value.MaximumAudioBytes)
            return new(false, $"Le fichier audio dépasse la limite de {options.Value.MaximumAudioBytes} octets.",
                "audio_too_large");
        return new(true);
    }
}
