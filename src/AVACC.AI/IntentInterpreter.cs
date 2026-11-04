using AVACC.Core.Interfaces;
using AVACC.Core.Models;

namespace AVACC.AI;

public class IntentInterpreter
{
    private readonly IEnumerable<ICommandInterpreter> _interpreters;

    public IntentInterpreter(IEnumerable<ICommandInterpreter> interpreters)
    {
        _interpreters = interpreters;
    }

    public ICommandInterpreter? GetInterpreter(string providerName)
    {
        return _interpreters.FirstOrDefault(p =>
            string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase) ||
            p.ProviderName.Contains(providerName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<VoiceCommandResult> InterpretAsync(
        string providerName,
        string modelName,
        string transcript,
        IReadOnlyList<CommandDefinition> availableCommands,
        CancellationToken cancellationToken = default)
    {
        var interpreter = GetInterpreter(providerName);
        if (interpreter == null)
        {
            return new VoiceCommandResult
            {
                Transcript = transcript,
                Error = $"No command interpreter found matching '{providerName}'.",
                Provider = providerName,
                ModelName = modelName
            };
        }

        return await interpreter.InterpretAsync(transcript, modelName, availableCommands, cancellationToken);
    }
}

