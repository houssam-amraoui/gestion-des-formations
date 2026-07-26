using TrainingManagement.Application.AiTrainer;

namespace TrainingManagement.Infrastructure.AiTrainer;

public sealed class AiProviderFactory(IEnumerable<IAiProvider> providers) : IAiProviderFactory
{
    private readonly IReadOnlyDictionary<string, IAiProvider> values = providers
        .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public IAiProvider Get(string provider) => values.TryGetValue(provider, out var value)
        ? value : throw new InvalidOperationException($"Le fournisseur IA « {provider} » n’est pas enregistré.");
    public bool IsConfigured(string provider) => values.TryGetValue(provider, out var value) &&
        value.IsConfigured;
}
