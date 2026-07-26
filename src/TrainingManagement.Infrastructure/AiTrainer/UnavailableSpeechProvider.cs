using TrainingManagement.Application.AiTrainer;

namespace TrainingManagement.Infrastructure.AiTrainer;

public sealed class UnavailableSpeechProvider : IAiSpeechToTextProvider, IAiTextToSpeechProvider
{
    public Task<AiTranscriptionResult> TranscribeAsync(Stream audio, string contentType,
        string languageCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTranscriptionResult(false, null, Error: new(
            "speech_to_text_not_configured",
            "Le fournisseur de transcription audio n’est pas configuré.")));

    public Task<AiSpeechResult> SynthesizeAsync(string text, string languageCode, string? voiceId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiSpeechResult(false, Error: new(
            "text_to_speech_not_configured",
            "Le fournisseur de synthèse vocale n’est pas configuré.")));
}
