using AVACC.Audio;
using AVACC.Core.Configuration;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using System.Collections.ObjectModel;
using Xunit;

namespace AVACC.Audio.Tests;

public class CrewChiefBridgeTests
{
    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new();
        public event EventHandler<AppSettings>? SettingsChanged;
        public Task<AppSettings> LoadSettingsAsync() => Task.FromResult(CurrentSettings);
        public Task SaveSettingsAsync(AppSettings settings) => Task.CompletedTask;
    }

    private class FakeCommandRegistryService : ICommandRegistryService
    {
        public List<CommandDefinition> CommandsList { get; } = new();
        public IReadOnlyList<CommandDefinition> Commands => CommandsList;
        public event EventHandler? CommandsChanged;
        public Task<IReadOnlyList<CommandDefinition>> LoadCommandsAsync() => Task.FromResult(Commands);
        public Task SaveCommandsAsync(IEnumerable<CommandDefinition> commands) => Task.CompletedTask;
        public Task AddCommandAsync(CommandDefinition command) => Task.CompletedTask;
        public Task UpdateCommandAsync(CommandDefinition command) => Task.CompletedTask;
        public Task DeleteCommandAsync(string commandId) => Task.CompletedTask;
        public Task<int> ImportCommandsAsync(string jsonContent, bool overwriteExisting) => Task.FromResult(0);
        public Task<string> ExportCommandsAsync() => Task.FromResult("[]");
        public Task ResetToDefaultCommandsAsync() => Task.CompletedTask;
    }

    private class FakeEventLogService : IEventLogService
    {
        public ObservableCollection<LogEntry> Entries { get; } = new();
        public event EventHandler<LogEntry>? LogEntryAdded;
        public void Log(LogLevel level, string message, string? details = null) { }
        public void LogInfo(string message, string? details = null) { }
        public void LogWarning(string message, string? details = null) { }
        public void LogError(string message, string? details = null) { }
        public void LogDebug(string message, string? details = null) { }
        public void Clear() { }
        public Task<string> ExportLogsAsync() => Task.FromResult(string.Empty);
    }

    private readonly FakeSettingsService _settings = new();
    private readonly FakeCommandRegistryService _registry = new();
    private readonly FakeEventLogService _log = new();
    private readonly MicrophoneManager _micManager = new();

    [Theory]
    [InlineData("HOWS_MY_TYRE_WEAR", "how's my tire wear")]
    [InlineData("HOWS_MY_AERO", "how's my aero")]
    [InlineData("DAMAGE_REPORT", "damage report")]
    [InlineData("WHATS_MY_FUEL_LEVEL", "what's my fuel level")]
    [InlineData("RADIO_CHECK", "can you hear me")]
    [InlineData("PIT_STOP", "pit stop")]
    [InlineData("WHATS_MY_GAP_BEHIND", "what's the gap behind")]
    public void GetCanonicalPhrase_ReturnsAccurateCrewChiefPhrases(string commandId, string expectedPhrase)
    {
        var bridge = new CrewChiefBridgeService(_settings, _registry, _log, _micManager);
        var phrase = bridge.GetCanonicalPhrase(commandId);

        Assert.Equal(expectedPhrase, phrase);
    }

    [Fact]
    public void GetCanonicalPhrase_PrefersCustomVoiceLine_WhenDefinedInRegistry()
    {
        var registry = new FakeCommandRegistryService();
        registry.CommandsList.Add(new CommandDefinition
        {
            CrewChiefCommandId = "RADIO_CHECK",
            Name = "Radio Check",
            VoiceLine = "test mic check chief"
        });

        var bridge = new CrewChiefBridgeService(_settings, registry, _log, _micManager);
        var phrase = bridge.GetCanonicalPhrase("RADIO_CHECK");

        Assert.Equal("test mic check chief", phrase);
    }

    [Fact]
    public void GetCanonicalPhrase_FallsBackToHumanizedId_WhenNotExplicitlyMapped()
    {
        var bridge = new CrewChiefBridgeService(_settings, _registry, _log, _micManager);
        var phrase = bridge.GetCanonicalPhrase("CUSTOM_TELEMETRY_CHECK");

        Assert.Equal("custom telemetry check", phrase);
    }

    [Fact]
    public void MicrophoneManager_GetPlaybackDevices_ReturnsValidEndpoints()
    {
        var devices = _micManager.GetPlaybackDevices();

        Assert.NotNull(devices);
        Assert.NotEmpty(devices);
        Assert.All(devices, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Id));
            Assert.False(string.IsNullOrWhiteSpace(d.Name));
        });
    }
}
