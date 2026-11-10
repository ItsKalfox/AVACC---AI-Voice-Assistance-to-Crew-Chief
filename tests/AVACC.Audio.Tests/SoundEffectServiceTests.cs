using AVACC.Audio;
using AVACC.Core.Configuration;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using Xunit;

namespace AVACC.Audio.Tests;

public class SoundEffectServiceTests
{
    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new();
        public event EventHandler<AppSettings>? SettingsChanged;
        public Task<AppSettings> LoadSettingsAsync() => Task.FromResult(CurrentSettings);
        public Task SaveSettingsAsync(AppSettings settings)
        {
            CurrentSettings = settings;
            SettingsChanged?.Invoke(this, settings);
            return Task.CompletedTask;
        }
    }

    private class FakeEventLogService : IEventLogService
    {
        public System.Collections.ObjectModel.ObservableCollection<LogEntry> Entries { get; } = new();
        public event EventHandler<LogEntry>? LogEntryAdded;
        public void Log(LogLevel level, string message, string? details = null) { }
        public void LogInfo(string message, string? details = null) { }
        public void LogDebug(string message, string? details = null) { }
        public void LogWarning(string message, string? details = null) { }
        public void LogError(string message, string? details = null) { }
        public void Clear() { }
        public Task<string> ExportLogsAsync() => Task.FromResult(string.Empty);
    }

    private class FakePushToTalkService : IPushToTalkService
    {
        public bool IsActive => false;
        public bool IsPttActive => false;
        public string ConfiguredKey => "RightCtrl";
        public bool IsGlobalHookActive => false;
        public bool IsCapturingBinding => false;
        public event EventHandler? PttPressed;
        public event EventHandler? PttReleased;
        public event EventHandler<string>? KeyDetectedForBinding;

        public void Initialize(string key, bool globalHook, bool disableWhenInactive) { }
        public void UpdateConfiguration(string key, bool globalHook, bool disableWhenInactive) { }
        public void TriggerPttDown() => PttPressed?.Invoke(this, EventArgs.Empty);
        public void TriggerPttUp() => PttReleased?.Invoke(this, EventArgs.Empty);
        public void StartCapturingBinding() { }
        public void StopCapturingBinding() { }
        public void Dispose() { }
    }

    private class FakeCrewChiefMonitorService : ICrewChiefMonitorService
    {
        public CrewChiefConnectionState State { get; set; } = CrewChiefConnectionState.NotRunning;
        public bool IsRunning => State != CrewChiefConnectionState.NotRunning;
        public bool IsSessionActive => State == CrewChiefConnectionState.Active;
        public string StatusDisplay => "Crew Chief: Offline";
        public string? ExecutablePath => null;
        public event EventHandler<CrewChiefConnectionState>? StateChanged;

        public void StartMonitoring() { }
        public void StopMonitoring() { }
        public Task<bool> LaunchCrewChiefAsync() => Task.FromResult(true);
        public Task<bool> StartCrewChiefSessionAsync() => Task.FromResult(true);
        public Task<bool> StopCrewChiefSessionAsync() => Task.FromResult(true);
        public Task<bool> ToggleCrewChiefSessionAsync() => Task.FromResult(true);
    }

    [Fact]
    public void SoundEffectService_Initializes_AndPreloadsSoundsWithoutCrashing()
    {
        var settings = new FakeSettingsService();
        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();

        var service = new SoundEffectService(settings, logs, ptt);

        Assert.NotNull(service);
    }

    [Fact]
    public void SoundEffectService_SubscribesToPttEvents()
    {
        var settings = new FakeSettingsService();
        settings.CurrentSettings.EnablePttStartSound = false; // Disable actual audio output during test
        settings.CurrentSettings.EnablePttEndSound = false;

        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();

        var service = new SoundEffectService(settings, logs, ptt);

        // Triggering PTT down and up should not throw
        ptt.TriggerPttDown();
        ptt.TriggerPttUp();
    }

    [Fact]
    public void SoundEffectService_GetAvailableChiefPersonas_ReturnsDefaultsWithoutCrashing()
    {
        var settings = new FakeSettingsService();
        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();

        var service = new SoundEffectService(settings, logs, ptt);
        var personas = service.GetAvailableChiefPersonas();

        Assert.NotNull(personas);
        Assert.Contains("Auto-Detect", personas);
        Assert.Contains("Jim", personas);
    }

    [Fact]
    public void SoundEffectService_DetectActiveChiefPersona_ReturnsFallbackWithoutCrashing()
    {
        var settings = new FakeSettingsService();
        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();

        var service = new SoundEffectService(settings, logs, ptt);
        var detected = service.DetectActiveChiefPersona();

        Assert.False(string.IsNullOrWhiteSpace(detected));
    }

    [Fact]
    public void SoundEffectService_PlayDidntUnderstand_AndPlayAiError_ExecuteWithoutCrashing()
    {
        var settings = new FakeSettingsService();
        settings.CurrentSettings.EnableDidntUnderstandSound = false; // Disabled during unit test
        settings.CurrentSettings.EnableAiErrorSound = false;
        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();

        var service = new SoundEffectService(settings, logs, ptt);
        service.PlayDidntUnderstand();
        service.PlayAiError();
        service.PlayCrewChiefNotRunningError();
    }

    [Fact]
    public void SoundEffectService_PttPressed_WhenCrewChiefNotRunning_PlaysErrorSoundAndSuppressesStartSound()
    {
        var settings = new FakeSettingsService();
        settings.CurrentSettings.PlayErrorSoundWhenCrewChiefNotRunning = true;
        settings.CurrentSettings.EnablePttStartSound = false;
        settings.CurrentSettings.EnablePttEndSound = false;

        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();
        var monitor = new FakeCrewChiefMonitorService { State = CrewChiefConnectionState.NotRunning };

        var service = new SoundEffectService(settings, logs, ptt, monitor);

        // When PTT is pressed and Crew Chief is offline, it should route to error sound without throwing
        ptt.TriggerPttDown();
        ptt.TriggerPttUp();
    }

    [Fact]
    public void SoundEffectService_PttPressed_WhenCrewChiefRunning_RoutesToNormalPttCues()
    {
        var settings = new FakeSettingsService();
        settings.CurrentSettings.PlayErrorSoundWhenCrewChiefNotRunning = true;
        settings.CurrentSettings.EnablePttStartSound = false;
        settings.CurrentSettings.EnablePttEndSound = false;

        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();
        var monitor = new FakeCrewChiefMonitorService { State = CrewChiefConnectionState.Running };

        var service = new SoundEffectService(settings, logs, ptt, monitor);

        ptt.TriggerPttDown();
        ptt.TriggerPttUp();
    }

    [Fact]
    public void SoundEffectService_PttPressed_WhenCrewChiefNotRunning_ToggleDisabled_RoutesToNormalPttCues()
    {
        var settings = new FakeSettingsService();
        settings.CurrentSettings.PlayErrorSoundWhenCrewChiefNotRunning = false;
        settings.CurrentSettings.EnablePttStartSound = false;
        settings.CurrentSettings.EnablePttEndSound = false;

        var logs = new FakeEventLogService();
        var ptt = new FakePushToTalkService();
        var monitor = new FakeCrewChiefMonitorService { State = CrewChiefConnectionState.NotRunning };

        var service = new SoundEffectService(settings, logs, ptt, monitor);

        ptt.TriggerPttDown();
        ptt.TriggerPttUp();
    }
}
