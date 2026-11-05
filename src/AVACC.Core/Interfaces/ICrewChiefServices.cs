using AVACC.Core.Models;

namespace AVACC.Core.Interfaces;

public enum CrewChiefConnectionState
{
    NotRunning,
    Running,
    Active
}

public interface ICrewChiefMonitorService
{
    CrewChiefConnectionState State { get; }
    bool IsRunning { get; }
    string StatusDisplay { get; }
    string? ExecutablePath { get; }

    event EventHandler<CrewChiefConnectionState>? StateChanged;

    void StartMonitoring();
    void StopMonitoring();
    Task<bool> LaunchCrewChiefAsync();
}

public interface ICrewChiefBridgeService
{
    Task<bool> SendCommandAsync(string commandId, string? phraseOverride = null, CancellationToken cancellationToken = default);
    Task<bool> SpeakPhraseAsync(string phrase, CancellationToken cancellationToken = default);
    string GetCanonicalPhrase(string commandId);
    IReadOnlyList<AudioDeviceInfo> GetPlaybackDevices();
}

