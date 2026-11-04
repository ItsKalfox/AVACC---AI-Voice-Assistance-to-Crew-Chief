using AVACC.Core.Models;

namespace AVACC.Core.Interfaces;

public interface ICommandRegistryService
{
    IReadOnlyList<CommandDefinition> Commands { get; }
    Task<IReadOnlyList<CommandDefinition>> LoadCommandsAsync();
    Task SaveCommandsAsync(IEnumerable<CommandDefinition> commands);
    Task AddCommandAsync(CommandDefinition command);
    Task UpdateCommandAsync(CommandDefinition command);
    Task DeleteCommandAsync(string commandId);
    Task<int> ImportCommandsAsync(string jsonContent, bool overwriteExisting);
    Task<string> ExportCommandsAsync();
    Task ResetToDefaultCommandsAsync();
    event EventHandler? CommandsChanged;
}

