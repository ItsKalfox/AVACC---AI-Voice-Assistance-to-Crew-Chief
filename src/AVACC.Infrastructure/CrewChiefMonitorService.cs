using System.Diagnostics;
using System.IO;
using AVACC.Core.Interfaces;

namespace AVACC.Infrastructure;

public class CrewChiefMonitorService : ICrewChiefMonitorService, IDisposable
{
    private readonly IEventLogService _eventLogService;
    private readonly System.Timers.Timer _timer;
    private CrewChiefConnectionState _state = CrewChiefConnectionState.NotRunning;
    private string? _executablePath;
    private bool _disposed;

    public CrewChiefConnectionState State => _state;
    public bool IsRunning => _state != CrewChiefConnectionState.NotRunning;
    public string? ExecutablePath => _executablePath;

    public string StatusDisplay => _state switch
    {
        CrewChiefConnectionState.Active => "Crew Chief: Active & Running",
        CrewChiefConnectionState.Running => "Crew Chief: Running (Idle)",
        _ => "Crew Chief: Offline"
    };

    public event EventHandler<CrewChiefConnectionState>? StateChanged;

    public CrewChiefMonitorService(IEventLogService eventLogService)
    {
        _eventLogService = eventLogService;
        _timer = new System.Timers.Timer(2000); // Check every 2 seconds
        _timer.Elapsed += (s, e) => CheckCrewChiefProcess();
        _timer.AutoReset = true;
    }

    public void StartMonitoring()
    {
        CheckCrewChiefProcess();
        _timer.Start();
    }

    public void StopMonitoring()
    {
        _timer.Stop();
    }

    private void CheckCrewChiefProcess()
    {
        try
        {
            var processes = Process.GetProcessesByName("CrewChiefV4");
            if (processes.Length == 0)
            {
                processes = Process.GetProcessesByName("CrewChief");
            }

            CrewChiefConnectionState newState;
            if (processes.Length > 0)
            {
                var proc = processes[0];
                try
                {
                    _executablePath = proc.MainModule?.FileName;
                }
                catch
                {
                    // Ignore access exception if elevated
                }

                // Check if main window title contains "started" or session indicators
                var title = proc.MainWindowTitle;
                if (!string.IsNullOrWhiteSpace(title) && 
                    (title.Contains("running", StringComparison.OrdinalIgnoreCase) || 
                     title.Contains("started", StringComparison.OrdinalIgnoreCase)))
                {
                    newState = CrewChiefConnectionState.Active;
                }
                else
                {
                    // Process is running
                    newState = CrewChiefConnectionState.Running;
                }
            }
            else
            {
                newState = CrewChiefConnectionState.NotRunning;
            }

            if (newState != _state)
            {
                var previousState = _state;
                _state = newState;
                StateChanged?.Invoke(this, _state);

                if (_state == CrewChiefConnectionState.NotRunning && previousState != CrewChiefConnectionState.NotRunning)
                {
                    _eventLogService.LogWarning("Crew Chief process stopped.");
                }
                else if (_state != CrewChiefConnectionState.NotRunning && previousState == CrewChiefConnectionState.NotRunning)
                {
                    _eventLogService.LogInfo($"Crew Chief detected ({StatusDisplay}).");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Crew Chief process check error: {ex.Message}");
        }
    }

    public Task<bool> LaunchCrewChiefAsync()
    {
        try
        {
            string? launchPath = _executablePath;

            if (string.IsNullOrEmpty(launchPath) || !File.Exists(launchPath))
            {
                // Common default installation locations
                var candidatePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Britton IT Ltd", "Crew Chief V4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Britton IT Ltd", "Crew Chief V4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CrewChiefV4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CrewChiefV4", "CrewChiefV4.exe"),
                    @"C:\Program Files (x86)\Britton IT Ltd\Crew Chief V4\CrewChiefV4.exe"
                };

                launchPath = candidatePaths.FirstOrDefault(File.Exists);
            }

            if (string.IsNullOrEmpty(launchPath))
            {
                _eventLogService.LogWarning("Could not automatically locate CrewChiefV4.exe. Please launch Crew Chief manually.");
                return Task.FromResult(false);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = launchPath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(launchPath)
            };

            Process.Start(startInfo);
            _eventLogService.LogInfo($"Launched Crew Chief from: {launchPath}");
            CheckCrewChiefProcess();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _eventLogService.LogError($"Failed to launch Crew Chief: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }
}
