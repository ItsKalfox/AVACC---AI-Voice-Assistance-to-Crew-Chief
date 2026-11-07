using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;

namespace AVACC.Infrastructure;

public class CrewChiefMonitorService : ICrewChiefMonitorService, IDisposable
{
    private readonly IEventLogService _eventLogService;
    private readonly ISettingsService _settingsService;
    private readonly IAudioEndpointService? _audioEndpointService;
    private readonly System.Timers.Timer _timer;
    private CrewChiefConnectionState _state = CrewChiefConnectionState.NotRunning;
    private string? _executablePath;
    private bool _isSessionActive;
    private bool _disposed;

    public CrewChiefConnectionState State => _state;
    public bool IsRunning => _state != CrewChiefConnectionState.NotRunning;
    public bool IsSessionActive => _isSessionActive;
    public string? ExecutablePath => _executablePath;

    public string StatusDisplay => _state switch
    {
        CrewChiefConnectionState.Active => "Crew Chief: Active & Running",
        CrewChiefConnectionState.Running => "Crew Chief: Running (Idle)",
        _ => "Crew Chief: Offline"
    };

    public event EventHandler<CrewChiefConnectionState>? StateChanged;

    #region Win32 Interop
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private delegate bool EnumChildDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildDelegate lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const uint BM_CLICK = 0x00F5;
    private const int SW_RESTORE = 9;
    #endregion

    public CrewChiefMonitorService(
        IEventLogService eventLogService, 
        ISettingsService settingsService,
        IAudioEndpointService? audioEndpointService = null)
    {
        _eventLogService = eventLogService;
        _settingsService = settingsService;
        _audioEndpointService = audioEndpointService;
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
            var proc = GetCrewChiefProcess();
            CrewChiefConnectionState newState;

            if (proc != null)
            {
                try
                {
                    _executablePath = proc.MainModule?.FileName;
                }
                catch
                {
                    // Ignore access exception if elevated
                }

                // Check button state first
                var btn = FindSessionButton(proc.Id, out var btnText);
                if (btn != IntPtr.Zero)
                {
                    if (btnText.IndexOf("Stop", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _isSessionActive = true;
                        newState = CrewChiefConnectionState.Active;
                    }
                    else
                    {
                        _isSessionActive = false;
                        newState = CrewChiefConnectionState.Running;
                    }
                }
                else
                {
                    // Fallback to window title
                    var title = proc.MainWindowTitle;
                    if (!string.IsNullOrWhiteSpace(title) &&
                        (title.Contains("running", StringComparison.OrdinalIgnoreCase) ||
                         title.Contains("started", StringComparison.OrdinalIgnoreCase)))
                    {
                        _isSessionActive = true;
                        newState = CrewChiefConnectionState.Active;
                    }
                    else
                    {
                        _isSessionActive = false;
                        newState = CrewChiefConnectionState.Running;
                    }
                }
            }
            else
            {
                _isSessionActive = false;
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
                else if (_state == CrewChiefConnectionState.Active && previousState == CrewChiefConnectionState.Running)
                {
                    _eventLogService.LogInfo("Crew Chief session started.");
                }
                else if (_state == CrewChiefConnectionState.Running && previousState == CrewChiefConnectionState.Active)
                {
                    _eventLogService.LogInfo("Crew Chief session stopped.");
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
            string? launchPath = _settingsService.CurrentSettings.CustomCrewChiefPath;

            if (string.IsNullOrEmpty(launchPath) || !File.Exists(launchPath))
            {
                launchPath = _executablePath;
            }

            if (string.IsNullOrEmpty(launchPath) || !File.Exists(launchPath))
            {
                // Common default installation locations
                var candidatePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Britton IT Ltd", "CrewChiefV4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Britton IT Ltd", "Crew Chief V4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Britton IT Ltd", "CrewChiefV4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Britton IT Ltd", "Crew Chief V4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CrewChiefV4", "CrewChiefV4.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CrewChiefV4", "CrewChiefV4.exe"),
                    @"C:\Program Files (x86)\Britton IT Ltd\CrewChiefV4\CrewChiefV4.exe",
                    @"C:\Program Files (x86)\Britton IT Ltd\Crew Chief V4\CrewChiefV4.exe"
                };

                launchPath = candidatePaths.FirstOrDefault(File.Exists);
            }

            if (string.IsNullOrEmpty(launchPath))
            {
                _eventLogService.LogWarning("Could not automatically locate CrewChiefV4.exe. Please specify path in Settings.");
                return Task.FromResult(false);
            }

            var settings = _settingsService.CurrentSettings;
            AudioDeviceInfo? originalDefaultDevice = null;
            bool didSwitchDevice = false;

            if (settings.AutoSwitchCrewChiefInputDeviceOnLaunch && _audioEndpointService != null)
            {
                try
                {
                    var activeInputs = _audioEndpointService.GetInputDevices();
                    AudioDeviceInfo? targetDevice = null;

                    if (!string.IsNullOrEmpty(settings.CrewChiefInputDeviceId))
                    {
                        targetDevice = activeInputs.FirstOrDefault(d => string.Equals(d.Id, settings.CrewChiefInputDeviceId, StringComparison.OrdinalIgnoreCase))
                                    ?? activeInputs.FirstOrDefault(d => string.Equals(d.Name, settings.CrewChiefInputDeviceName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetDevice == null)
                    {
                        targetDevice = activeInputs.FirstOrDefault(d => d.Name.Contains("CABLE", StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetDevice != null)
                    {
                        originalDefaultDevice = _audioEndpointService.GetDefaultInputDevice();
                        if (originalDefaultDevice != null && !string.Equals(originalDefaultDevice.Id, targetDevice.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            _eventLogService.LogInfo($"Temporarily setting Windows default recording device to '{targetDevice.Name}' for Crew Chief launch...");
                            if (_audioEndpointService.SetDefaultInputDevice(targetDevice.Id))
                            {
                                didSwitchDevice = true;
                            }
                        }
                    }
                }
                catch (Exception devEx)
                {
                    _eventLogService.LogWarning($"Pre-launch audio switch check failed: {devEx.Message}");
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = launchPath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(launchPath),
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(startInfo);
            _eventLogService.LogInfo($"Launched Crew Chief from: {launchPath}");
            CheckCrewChiefProcess();

            if (didSwitchDevice && originalDefaultDevice != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Give Crew Chief 3 seconds to initialize its Microsoft Speech Recognition engine and latch onto the default device
                        await Task.Delay(3000);
                        _audioEndpointService!.SetDefaultInputDevice(originalDefaultDevice.Id);
                        _eventLogService.LogInfo($"Restored Windows default recording device back to '{originalDefaultDevice.Name}'.");
                    }
                    catch (Exception restoreEx)
                    {
                        _eventLogService.LogWarning($"Failed to restore Windows default recording device: {restoreEx.Message}");
                    }
                });
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _eventLogService.LogError($"Failed to launch Crew Chief: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> StartCrewChiefSessionAsync()
    {
        return Task.Run(() =>
        {
            var proc = GetCrewChiefProcess();
            if (proc == null)
            {
                _eventLogService.LogWarning("Cannot start Crew Chief session: Crew Chief process is not running.");
                return false;
            }

            // If window is minimized, restore it so controls can receive messages
            try
            {
                if (proc.MainWindowHandle != IntPtr.Zero && IsIconic(proc.MainWindowHandle))
                {
                    ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                    Thread.Sleep(200);
                }
            }
            catch { }

            var btn = FindSessionButton(proc.Id, out var btnText);
            if (btn == IntPtr.Zero)
            {
                _eventLogService.LogWarning("Could not locate Crew Chief Start button.");
                return false;
            }

            if (btnText.IndexOf("Start", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SendMessage(btn, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                _eventLogService.LogInfo("Triggered 'Start Crew Chief' session.");
                CheckCrewChiefProcess();
                return true;
            }

            _eventLogService.LogInfo("Crew Chief session is already active.");
            return true;
        });
    }

    public Task<bool> StopCrewChiefSessionAsync()
    {
        return Task.Run(() =>
        {
            var proc = GetCrewChiefProcess();
            if (proc == null) return false;

            // If window is minimized, restore it so controls can receive messages
            try
            {
                if (proc.MainWindowHandle != IntPtr.Zero && IsIconic(proc.MainWindowHandle))
                {
                    ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                    Thread.Sleep(200);
                }
            }
            catch { }

            var btn = FindSessionButton(proc.Id, out var btnText);
            if (btn == IntPtr.Zero) return false;

            if (btnText.IndexOf("Stop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SendMessage(btn, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                _eventLogService.LogInfo("Triggered 'Stop Crew Chief' session.");
                CheckCrewChiefProcess();
                return true;
            }

            return true;
        });
    }

    public async Task<bool> ToggleCrewChiefSessionAsync()
    {
        if (_isSessionActive)
        {
            return await StopCrewChiefSessionAsync();
        }
        else
        {
            return await StartCrewChiefSessionAsync();
        }
    }

    private static Process? GetCrewChiefProcess()
    {
        var processes = Process.GetProcessesByName("CrewChiefV4");
        if (processes.Length == 0)
        {
            processes = Process.GetProcessesByName("CrewChief");
        }
        return processes.FirstOrDefault();
    }

    private static IntPtr FindSessionButton(int pid, out string buttonText)
    {
        IntPtr foundBtn = IntPtr.Zero;
        string foundTxt = string.Empty;

        EnumWindows((h, l) =>
        {
            uint p;
            GetWindowThreadProcessId(h, out p);
            if (p == pid)
            {
                EnumChildWindows(h, (ch, cl) =>
                {
                    var sb = new StringBuilder(256);
                    GetWindowText(ch, sb, 256);
                    var txt = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(txt) &&
                        (txt.IndexOf("Start Crew Chief", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         txt.IndexOf("Stop", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        foundBtn = ch;
                        foundTxt = txt.Trim();
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            return foundBtn == IntPtr.Zero;
        }, IntPtr.Zero);

        buttonText = foundTxt;
        return foundBtn;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }
}

