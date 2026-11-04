using System.IO;
using System.Text.Json;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using Microsoft.Extensions.Logging;

namespace AVACC.Infrastructure;

public class CommandRegistryService : ICommandRegistryService
{
    private readonly string _registryFilePath;
    private readonly ILogger<CommandRegistryService>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<CommandDefinition> _commands = new();

    public IReadOnlyList<CommandDefinition> Commands
    {
        get
        {
            lock (_commands)
            {
                return _commands.Select(c => c.Clone()).ToList();
            }
        }
    }

    public event EventHandler? CommandsChanged;

    public CommandRegistryService(string? customFilePath = null, ILogger<CommandRegistryService>? logger = null)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        var directory = customFilePath != null
            ? Path.GetDirectoryName(customFilePath) ?? AppDomain.CurrentDomain.BaseDirectory
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AVACC");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _registryFilePath = customFilePath ?? Path.Combine(directory, "commands.json");
    }

    public async Task<IReadOnlyList<CommandDefinition>> LoadCommandsAsync()
    {
        try
        {
            if (File.Exists(_registryFilePath))
            {
                var json = await File.ReadAllTextAsync(_registryFilePath);
                var loaded = JsonSerializer.Deserialize<List<CommandDefinition>>(json, _jsonOptions);
                if (loaded != null && loaded.Count > 0)
                {
                    lock (_commands)
                    {
                        _commands.Clear();
                        _commands.AddRange(loaded);
                    }
                    _logger?.LogInformation("Loaded {Count} commands from {FilePath}", loaded.Count, _registryFilePath);
                    CommandsChanged?.Invoke(this, EventArgs.Empty);
                    return Commands;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load command registry from {FilePath}, loading defaults", _registryFilePath);
        }

        await ResetToDefaultCommandsAsync();
        return Commands;
    }

    public async Task SaveCommandsAsync(IEnumerable<CommandDefinition> commands)
    {
        try
        {
            var list = commands.Select(c => c.Clone()).ToList();
            lock (_commands)
            {
                _commands.Clear();
                _commands.AddRange(list);
            }

            var json = JsonSerializer.Serialize(list, _jsonOptions);
            await File.WriteAllTextAsync(_registryFilePath, json);
            _logger?.LogInformation("Saved {Count} commands to {FilePath}", list.Count, _registryFilePath);
            CommandsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save commands to {FilePath}", _registryFilePath);
            throw;
        }
    }

    public async Task AddCommandAsync(CommandDefinition command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        lock (_commands)
        {
            _commands.Add(command.Clone());
        }
        await SaveCommandsAsync(_commands);
    }

    public async Task UpdateCommandAsync(CommandDefinition command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        lock (_commands)
        {
            var index = _commands.FindIndex(c => c.Id == command.Id);
            if (index >= 0)
            {
                _commands[index] = command.Clone();
            }
            else
            {
                _commands.Add(command.Clone());
            }
        }
        await SaveCommandsAsync(_commands);
    }

    public async Task DeleteCommandAsync(string commandId)
    {
        lock (_commands)
        {
            _commands.RemoveAll(c => c.Id == commandId);
        }
        await SaveCommandsAsync(_commands);
    }

    public async Task<int> ImportCommandsAsync(string jsonContent, bool overwriteExisting)
    {
        var imported = JsonSerializer.Deserialize<List<CommandDefinition>>(jsonContent, _jsonOptions);
        if (imported == null || imported.Count == 0)
        {
            throw new InvalidOperationException("No valid commands found in import data.");
        }

        lock (_commands)
        {
            if (overwriteExisting)
            {
                _commands.Clear();
                _commands.AddRange(imported);
            }
            else
            {
                foreach (var cmd in imported)
                {
                    var existingIdx = _commands.FindIndex(c => c.CrewChiefCommandId == cmd.CrewChiefCommandId);
                    if (existingIdx >= 0)
                    {
                        _commands[existingIdx] = cmd;
                    }
                    else
                    {
                        _commands.Add(cmd);
                    }
                }
            }
        }

        await SaveCommandsAsync(_commands);
        return imported.Count;
    }

    public Task<string> ExportCommandsAsync()
    {
        lock (_commands)
        {
            var json = JsonSerializer.Serialize(_commands, _jsonOptions);
            return Task.FromResult(json);
        }
    }

    public async Task ResetToDefaultCommandsAsync()
    {
        var defaults = GetDefaultCommands();
        await SaveCommandsAsync(defaults);
    }

    public static List<CommandDefinition> GetDefaultCommands()
    {
        return new List<CommandDefinition>
        {
            new()
            {
                Name = "Fuel to finish",
                CrewChiefCommandId = "HOW_MUCH_FUEL_TO_END_OF_RACE",
                Category = "Fuel",
                Description = "Asks the crew chief how much fuel is required to reach the end of the race or stint.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "How much fuel do we need?",
                    "Can we make it to the finish?",
                    "What's our fuel requirement for the next stint?",
                    "How much fuel to the end?",
                    "How much petrol should I add?"
                },
                Aliases = new List<string> { "fuel to end", "fuel required", "finish fuel" }
            },
            new()
            {
                Name = "Current fuel level",
                CrewChiefCommandId = "HOWS_MY_FUEL",
                Category = "Fuel",
                Description = "Checks the current fuel remaining in liters or laps.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "How is my fuel?",
                    "How much fuel do I have left?",
                    "What's my fuel status?",
                    "Fuel level check"
                },
                Aliases = new List<string> { "check fuel", "fuel status" }
            },
            new()
            {
                Name = "Tyre pressures",
                CrewChiefCommandId = "WHAT_ARE_MY_TYRE_PRESSURES",
                Category = "Tyres",
                Description = "Reports the current pressures across all four tyres.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "What are my tyre pressures?",
                    "How are my tyre pressures?",
                    "Check tyre pressures",
                    "Pressures please"
                },
                Aliases = new List<string> { "pressures", "psi check" }
            },
            new()
            {
                Name = "Tyre temperatures",
                CrewChiefCommandId = "HOW_ARE_MY_TYRE_TEMPS",
                Category = "Tyres",
                Description = "Reports tyre surface or carcass temperatures.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "How are the tyre temperatures?",
                    "What are my tyre temps?",
                    "Are the tyres getting hot?",
                    "Tyre temp check"
                },
                Aliases = new List<string> { "tyre temps", "temps check" }
            },
            new()
            {
                Name = "Tyre condition & wear",
                CrewChiefCommandId = "HOW_ARE_MY_TYRES",
                Category = "Tyres",
                Description = "General summary of tyre wear and health.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "How are the tyres looking?",
                    "What's our tyre wear?",
                    "How are my tyres holding up?",
                    "Tyre status"
                },
                Aliases = new List<string> { "tyre health", "tyres check" }
            },
            new()
            {
                Name = "Brake temperatures",
                CrewChiefCommandId = "HOW_ARE_MY_BRAKE_TEMPS",
                Category = "Brakes",
                Description = "Reports brake rotor and pad temperature status.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "How are my brake temps?",
                    "What are my brake temperatures?",
                    "Are the brakes overheating?",
                    "Brake check"
                },
                Aliases = new List<string> { "brakes", "brake temperatures" }
            },
            new()
            {
                Name = "Gap behind",
                CrewChiefCommandId = "WHAT_IS_MY_GAP_BEHIND",
                Category = "Gaps",
                Description = "Reports the time gap to the car following directly behind.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "What's the gap behind?",
                    "What is my gap behind?",
                    "Who is behind me?",
                    "How far is the car behind?",
                    "Is anyone catching me?"
                },
                Aliases = new List<string> { "gap back", "car behind" }
            },
            new()
            {
                Name = "Gap ahead",
                CrewChiefCommandId = "WHAT_IS_MY_GAP_AHEAD",
                Category = "Gaps",
                Description = "Reports the time gap to the car directly ahead or the leader.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "What's the gap ahead?",
                    "What is my gap ahead?",
                    "How far to the car in front?",
                    "Gap to the leader",
                    "Are we closing on the car ahead?"
                },
                Aliases = new List<string> { "gap front", "car ahead" }
            },
            new()
            {
                Name = "Full race update",
                CrewChiefCommandId = "FULL_UPDATE",
                Category = "Session",
                Description = "Provides a complete race briefing including position, gaps, fuel, and remaining laps.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "Give me a quick race update",
                    "Give me a full update",
                    "Race update please",
                    "What's the situation?"
                },
                Aliases = new List<string> { "status report", "race update" }
            },
            new()
            {
                Name = "Time remaining",
                CrewChiefCommandId = "WHATS_THE_TIME_REMAINING",
                Category = "Session",
                Description = "Reports remaining session time or remaining laps.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "How much time is left?",
                    "What's the time remaining?",
                    "How many laps left?",
                    "How long to go in the race?"
                },
                Aliases = new List<string> { "time left", "session time" }
            },
            new()
            {
                Name = "Current position",
                CrewChiefCommandId = "WHAT_POSITION_AM_I_IN",
                Category = "Session",
                Description = "Reports the driver's current race or class position.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "What position am I in?",
                    "What is my position?",
                    "Where are we running?"
                },
                Aliases = new List<string> { "my position", "standings" }
            },
            new()
            {
                Name = "Last lap time",
                CrewChiefCommandId = "WHATS_MY_LAST_LAP_TIME",
                Category = "Timing",
                Description = "Reports the lap time achieved on the previous lap.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "What was my last lap time?",
                    "What was my last lap?",
                    "How fast was that lap?"
                },
                Aliases = new List<string> { "last lap", "previous lap" }
            },
            new()
            {
                Name = "Best lap time",
                CrewChiefCommandId = "WHATS_MY_BEST_LAP_TIME",
                Category = "Timing",
                Description = "Reports the driver's personal best lap time in this session.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "What's my best lap time?",
                    "What is my fastest lap?",
                    "Fastest lap please"
                },
                Aliases = new List<string> { "best lap", "fastest lap" }
            },
            new()
            {
                Name = "Damage report",
                CrewChiefCommandId = "DAMAGE_REPORT",
                Category = "Damage",
                Description = "Reports aerodynamic, bodywork, or suspension damage to the car.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "Do we have any damage?",
                    "Damage report",
                    "How is the car looking?",
                    "Did that touch cause damage?"
                },
                Aliases = new List<string> { "car damage", "damage check" }
            },
            new()
            {
                Name = "Pit box this lap",
                CrewChiefCommandId = "BOX_THIS_LAP",
                Category = "Pitstop",
                Description = "Notifies the crew that the car will pit on this lap.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "Box this lap",
                    "I am pitting this lap",
                    "Coming into the pits",
                    "Box box box"
                },
                Aliases = new List<string> { "box", "pit in" }
            },
            new()
            {
                Name = "Radio check",
                CrewChiefCommandId = "RADIO_CHECK",
                Category = "General",
                Description = "Performs a radio communication confirmation test.",
                IsEnabled = true,
                Examples = new List<string>
                {
                    "Radio check",
                    "Chief can you hear me?",
                    "Comms check"
                },
                Aliases = new List<string> { "comms check", "test radio" }
            }
        };
    }
}
