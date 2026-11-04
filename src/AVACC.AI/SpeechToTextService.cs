using AVACC.AI.Providers;
using AVACC.Core.Interfaces;

namespace AVACC.AI;

public class SpeechToTextService
{
    private readonly IEnumerable<ISpeechToTextProvider> _providers;

    public SpeechToTextService(IEnumerable<ISpeechToTextProvider> providers)
    {
        _providers = providers;
    }

    public ISpeechToTextProvider? GetProvider(string providerName)
    {
        return _providers.FirstOrDefault(p =>
            string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase) ||
            p.ProviderName.Contains(providerName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string providerName,
        string modelName,
        byte[] audioWav,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(providerName);
        if (provider == null)
        {
            return new TranscriptionResult
            {
                Error = $"No STT provider found matching '{providerName}'.",
                Provider = providerName,
                Model = modelName
            };
        }

        return await provider.TranscribeAsync(audioWav, modelName, cancellationToken);
    }
}

