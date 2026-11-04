using AVACC.Core.Models;

namespace AVACC.Core.Interfaces;

public class TranscriptionResult
{
    public string Transcript { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string? Error { get; set; }
    public bool IsSuccess => string.IsNullOrEmpty(Error) && !string.IsNullOrWhiteSpace(Transcript);
}

public interface ISpeechToTextProvider
{
    string ProviderName { get; }
    Task<TranscriptionResult> TranscribeAsync(
        byte[] audioWav,
        string modelName,
        CancellationToken cancellationToken = default);
}

public interface ICommandInterpreter
{
    string ProviderName { get; }
    Task<VoiceCommandResult> InterpretAsync(
        string transcript,
        string modelName,
        IReadOnlyList<CommandDefinition> availableCommands,
        CancellationToken cancellationToken = default);
}

public interface IMultimodalVoiceProvider
{
    string ProviderName { get; }
    Task<VoiceCommandResult> ProcessAudioAsync(
        byte[] audioWav,
        string modelName,
        IReadOnlyList<CommandDefinition> availableCommands,
        CancellationToken cancellationToken = default);
}

public interface IModelDiscoveryService
{
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(string providerName, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> TestConnectionAsync(string providerName, string? modelName = null, CancellationToken cancellationToken = default);
}

public interface IAIProviderFactory
{
    IMultimodalVoiceProvider GetMultimodalProvider(string providerName);
    ISpeechToTextProvider GetSttProvider(string providerName);
    ICommandInterpreter GetInterpreter(string providerName);
}

