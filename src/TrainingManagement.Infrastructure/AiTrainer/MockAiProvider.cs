using TrainingManagement.Application.AiTrainer;

namespace TrainingManagement.Infrastructure.AiTrainer;

public sealed class MockAiProvider : IAiProvider, IAiAvatarProvider,
    IAiLanguageModelProvider, IAiSpeechToTextProvider, IAiTextToSpeechProvider
{
    public string Name => "Mock";
    public bool IsConfigured => true;

    public Task<AiProviderSessionResult> StartSessionAsync(AiProviderSessionRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(
        new AiProviderSessionResult(true, $"mock-{request.InternalSessionId:N}", null,
            request.EnableAvatar ? "simulated" : "text-only"));

    public Task<AiChatResult> SendMessageAsync(AiChatRequest request,
        CancellationToken cancellationToken = default) => CompleteAsync(request, cancellationToken);

    public Task<AiChatResult> CompleteAsync(AiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var value = request.UserMessage.Trim().ToLowerInvariant();
        if (value.Contains("[mock-error]"))
            return Task.FromResult(new AiChatResult(false, null, Error:
                new("mock_failure", "Le fournisseur Mock simule une indisponibilité.", true)));
        var answer = value switch
        {
            var x when x.Contains("contrôleur") || x.Contains("controller") =>
                "Un contrôleur reçoit les requêtes, exécute la logique applicative nécessaire et choisit la réponse ou la vue à retourner.",
            var x when x.Contains("mvc") =>
                "MVC sépare l’application en Model pour les données, View pour l’affichage et Controller pour traiter les requêtes.",
            var x when x.Contains("entity framework") =>
                "Entity Framework Core est l’ORM qui traduit les opérations .NET en requêtes vers la base de données.",
            var x when x.Contains("migration") =>
                "Une migration décrit une évolution versionnée du schéma de base de données et permet de l’appliquer de façon contrôlée.",
            var x when x.Contains("razor") || x.Contains("view") || x.Contains("vue") =>
                "Une vue Razor combine HTML et expressions C# encodées afin de produire l’interface envoyée au navigateur.",
            var x when x.Contains("model") || x.Contains("modèle") =>
                "Dans MVC, le modèle représente les données et règles manipulées par l’application.",
            var x when x.Contains("route") =>
                "Une route associe une URL et une méthode HTTP à une action de contrôleur.",
            _ => "Je ne trouve pas cette information dans le contenu actuel de la leçon. Je peux toutefois vous aider à reformuler la question ou vous orienter vers une ressource du cours."
        };
        return Task.FromResult(new AiChatResult(true, answer,
            Math.Max(1, request.UserMessage.Length / 4), Math.Max(1, answer.Length / 4),
            $"mock-message-{Guid.NewGuid():N}"));
    }

    public Task EndSessionAsync(string? providerSessionId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    public Task<AiAvatarSessionResult> CreateAvatarSessionAsync(AiProviderSessionRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(new AiAvatarSessionResult(
            true, $"mock-avatar-{request.InternalSessionId:N}", null, "simulated"));
    public Task<bool> SendAvatarTextAsync(string providerSessionId, string text,
        CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task EndAvatarSessionAsync(string providerSessionId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<AiTranscriptionResult> TranscribeAsync(Stream audio, string contentType,
        string languageCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTranscriptionResult(true,
            "Qu’est-ce qu’un contrôleur MVC ?", 3));
    public Task<AiSpeechResult> SynthesizeAsync(string text, string languageCode, string? voiceId,
        CancellationToken cancellationToken = default) => Task.FromResult(
        new AiSpeechResult(false, Error: new("audio_disabled",
            "La synthèse audio est désactivée en mode Mock.")));
}
