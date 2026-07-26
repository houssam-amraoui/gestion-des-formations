using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.AiTrainer;

namespace TrainingManagement.Infrastructure.AiTrainer;

public sealed class AnamAiProvider(HttpClient client, IOptions<AnamOptions> options) :
    IAiProvider, IAiAvatarProvider
{
    private readonly AnamOptions settings = options.Value;
    public string Name => "Anam";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.ApiKey);

    public async Task<AiProviderSessionResult> StartSessionAsync(AiProviderSessionRequest request,
        CancellationToken token = default)
    {
        var avatar = await CreateAvatarSessionAsync(request, token);
        return new(avatar.Succeeded, avatar.ProviderSessionId, avatar.ClientToken,
            avatar.Status, avatar.Error);
    }

    public async Task<AiAvatarSessionResult> CreateAvatarSessionAsync(AiProviderSessionRequest request,
        CancellationToken token = default)
    {
        if (!IsConfigured)
            return new(false, null, null, "unconfigured",
                new("anam_not_configured", "La clé API Anam n’est pas configurée."));
        if (string.IsNullOrWhiteSpace(request.AvatarId) || string.IsNullOrWhiteSpace(request.VoiceId) ||
            string.IsNullOrWhiteSpace(settings.LlmId))
            return new(false, null, null, "invalid_configuration",
                new("anam_profile_invalid",
                    "Anam exige des identifiants d’avatar, de voix et de modèle de langage."));
        using var message = new HttpRequestMessage(HttpMethod.Post, "auth/session-token");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        message.Content = JsonContent.Create(new AnamSessionTokenRequest(new(
            request.DisplayName, request.AvatarId, request.VoiceId, settings.LlmId,
            request.SystemPrompt, NormalizeLanguageCode(request.LanguageCode))));
        try
        {
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
            if (!response.IsSuccessStatusCode)
                return new(false, null, null, "failed", new($"anam_http_{(int)response.StatusCode}",
                    "Le service avatar Anam est momentanément indisponible.",
                    (int)response.StatusCode >= 500));
            var payload = await response.Content.ReadFromJsonAsync<AnamSessionTokenResponse>(
                cancellationToken: token);
            return string.IsNullOrWhiteSpace(payload?.SessionToken)
                ? new(false, null, null, "failed",
                    new("anam_invalid_response", "La réponse du service avatar est invalide."))
                : new(true, null, payload.SessionToken, "ready");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new(false, null, null, "timeout",
                new("anam_timeout", "Le service avatar n’a pas répondu à temps.", true));
        }
        catch (HttpRequestException)
        {
            return new(false, null, null, "unavailable",
                new("anam_unavailable", "Le service avatar est temporairement indisponible.", true));
        }
    }

    public Task<AiChatResult> SendMessageAsync(AiChatRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(new AiChatResult(false, null,
        Error: new("anam_avatar_only",
            "L’adaptateur Anam actuel fournit l’avatar ; configurez un fournisseur de langage séparé.")));
    public Task<bool> SendAvatarTextAsync(string providerSessionId, string text,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task EndSessionAsync(string? providerSessionId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task EndAvatarSessionAsync(string providerSessionId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return false;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "sessions?page=1&perPage=1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
    }

    private sealed record AnamSessionTokenRequest(
        [property: JsonPropertyName("personaConfig")] AnamPersonaConfig PersonaConfig);
    private sealed record AnamPersonaConfig(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("avatarId")] string AvatarId,
        [property: JsonPropertyName("voiceId")] string VoiceId,
        [property: JsonPropertyName("llmId")] string LlmId,
        [property: JsonPropertyName("systemPrompt")] string SystemPrompt,
        [property: JsonPropertyName("languageCode")] string LanguageCode);
    private sealed record AnamSessionTokenResponse(
        [property: JsonPropertyName("sessionToken")] string SessionToken);

    public static string NormalizeLanguageCode(string? languageCode)
    {
        var primary = languageCode?.Trim().Split(['-', '_'], 2)[0].ToLowerInvariant();
        return primary is { Length: 2 } && primary.All(char.IsLetter) ? primary : "en";
    }
}
