using System.Windows;
using AVACC.Core.Commands;
using AVACC.Core.Configuration;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;

namespace AVACC.App.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IPushToTalkService _pttService;
    private readonly IAudioCaptureService _audioCaptureService;
    private readonly IEventLogService _eventLogService;
    private readonly ICommandRegistryService _registryService;
    private readonly IAIProviderFactory _providerFactory;
    private readonly ICrewChiefMonitorService _crewChiefMonitor;
    private readonly ICrewChiefBridgeService _crewChiefBridge;
    private readonly ISoundEffectService _soundEffectService;

    private int _selectedTabIndex;
    private string _overallStatus = "IDLE";
    private bool _isPttActive;
    private string _activeProviderInfo = "Google Gemini Flash • Simple Mode";
    private bool _isProcessingVoice;
    private string _crewChiefStatusDisplay = "Crew Chief: Offline";
    private string _crewChiefStateColor = "#FF1744";
    private string _crewChiefStateBackground = "#1F1215";
    private bool _isCrewChiefRunning;
    private bool _canLaunchCrewChief = true;
    private bool _isCrewChiefSessionActive;
    private bool _canToggleCrewChiefSession;
    private bool _canStartCrewChiefSession;
    private bool _canStopCrewChiefSession;
    private string _sessionButtonText = "START CREW CHIEF";
    private string _sessionButtonBackground = "#E50914";
    private string _sessionButtonBorderBrush = "#E50914";
    private string _sessionButtonBorderThickness = "0";
    private string _sessionButtonForeground = "#FFFFFF";
    private string _crewChiefBridgeStatusText = "Crew Chief Audio Bridge: Inactive";

    public VisualCMDViewModel VisualCMD { get; }
    public CommandEditorViewModel CommandEditor { get; }
    public SettingsViewModel Settings { get; }

    public string PttTriggerStatusText => $"PTT Trigger: {_pttService.ConfiguredKey}";

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public string OverallStatus
    {
        get => _overallStatus;
        set => SetProperty(ref _overallStatus, value);
    }

    public bool IsPttActive
    {
        get => _isPttActive;
        set => SetProperty(ref _isPttActive, value);
    }

    public string ActiveProviderInfo
    {
        get => _activeProviderInfo;
        set => SetProperty(ref _activeProviderInfo, value);
    }

    public string CrewChiefStatusDisplay
    {
        get => _crewChiefStatusDisplay;
        set => SetProperty(ref _crewChiefStatusDisplay, value);
    }

    public string CrewChiefStateColor
    {
        get => _crewChiefStateColor;
        set => SetProperty(ref _crewChiefStateColor, value);
    }

    public string CrewChiefStateBackground
    {
        get => _crewChiefStateBackground;
        set => SetProperty(ref _crewChiefStateBackground, value);
    }

    public bool IsCrewChiefRunning
    {
        get => _isCrewChiefRunning;
        set => SetProperty(ref _isCrewChiefRunning, value);
    }

    public bool CanLaunchCrewChief
    {
        get => _canLaunchCrewChief;
        set => SetProperty(ref _canLaunchCrewChief, value);
    }

    public bool IsCrewChiefSessionActive
    {
        get => _isCrewChiefSessionActive;
        set => SetProperty(ref _isCrewChiefSessionActive, value);
    }

    public bool CanToggleCrewChiefSession
    {
        get => _canToggleCrewChiefSession;
        set => SetProperty(ref _canToggleCrewChiefSession, value);
    }

    public bool CanStartCrewChiefSession
    {
        get => _canStartCrewChiefSession;
        set => SetProperty(ref _canStartCrewChiefSession, value);
    }

    public bool CanStopCrewChiefSession
    {
        get => _canStopCrewChiefSession;
        set => SetProperty(ref _canStopCrewChiefSession, value);
    }

    public string SessionButtonText
    {
        get => _sessionButtonText;
        set => SetProperty(ref _sessionButtonText, value);
    }

    public string SessionButtonBackground
    {
        get => _sessionButtonBackground;
        set => SetProperty(ref _sessionButtonBackground, value);
    }

    public string SessionButtonBorderBrush
    {
        get => _sessionButtonBorderBrush;
        set => SetProperty(ref _sessionButtonBorderBrush, value);
    }

    public string SessionButtonBorderThickness
    {
        get => _sessionButtonBorderThickness;
        set => SetProperty(ref _sessionButtonBorderThickness, value);
    }

    public string SessionButtonForeground
    {
        get => _sessionButtonForeground;
        set => SetProperty(ref _sessionButtonForeground, value);
    }

    public string CrewChiefBridgeStatusText
    {
        get => _crewChiefBridgeStatusText;
        set => SetProperty(ref _crewChiefBridgeStatusText, value);
    }

    public RelayCommand<object> SelectTabCommand { get; }
    public AsyncRelayCommand LaunchCrewChiefCommand { get; }
    public AsyncRelayCommand ToggleCrewChiefSessionCommand { get; }

    public MainViewModel(
        VisualCMDViewModel visualCmdVm,
        CommandEditorViewModel commandEditorVm,
        SettingsViewModel settingsVm,
        ISettingsService settingsService,
        IPushToTalkService pttService,
        IAudioCaptureService audioCaptureService,
        IEventLogService eventLogService,
        ICommandRegistryService registryService,
        IAIProviderFactory providerFactory,
        ICrewChiefMonitorService crewChiefMonitor,
        ICrewChiefBridgeService crewChiefBridge,
        ISoundEffectService soundEffectService)
    {
        VisualCMD = visualCmdVm;
        CommandEditor = commandEditorVm;
        Settings = settingsVm;
        _settingsService = settingsService;
        _pttService = pttService;
        _audioCaptureService = audioCaptureService;
        _eventLogService = eventLogService;
        _registryService = registryService;
        _providerFactory = providerFactory;
        _crewChiefMonitor = crewChiefMonitor;
        _crewChiefBridge = crewChiefBridge;
        _soundEffectService = soundEffectService;

        SelectTabCommand = new RelayCommand<object>(param =>
        {
            if (param != null && int.TryParse(param.ToString(), out var index))
            {
                SelectedTabIndex = index;
            }
        });

        LaunchCrewChiefCommand = new AsyncRelayCommand(async () =>
        {
            _eventLogService.LogInfo("Attempting to launch Crew Chief process...");
            var launched = await _crewChiefMonitor.LaunchCrewChiefAsync();
            if (!launched)
            {
                MessageBox.Show(
                    "Could not automatically locate CrewChiefV4.exe in default Program Files folders.\nPlease check or configure your Crew Chief path in Settings.",
                    "Crew Chief Launch",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        });

        ToggleCrewChiefSessionCommand = new AsyncRelayCommand(async () =>
        {
            _eventLogService.LogInfo("Toggling Crew Chief session state...");
            var success = await _crewChiefMonitor.ToggleCrewChiefSessionAsync();
            if (!success)
            {
                MessageBox.Show(
                    "Could not interact with Crew Chief window. Make sure Crew Chief is running.",
                    "Crew Chief Session",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        });

        WireEvents();
        UpdateProviderInfo();
        UpdateCrewChiefDisplay(_crewChiefMonitor.State);
        VisualCMD.StateMessage = $"Press and hold {_pttService.ConfiguredKey} to speak.";
    }

    private void WireEvents()
    {
        _settingsService.SettingsChanged += (s, settings) =>
        {
            UpdateProviderInfo();
            UpdateCrewChiefDisplay(_crewChiefMonitor.State);
            OnPropertyChanged(nameof(PttTriggerStatusText));
            if (VisualCMD.CurrentState == ProcessingState.Idle)
            {
                VisualCMD.StateMessage = $"Press and hold {_pttService.ConfiguredKey} to speak.";
            }
        };

        _crewChiefMonitor.StateChanged += (s, state) => UpdateCrewChiefDisplay(state);

        _pttService.PttPressed += async (s, e) =>
        {
            if (_audioCaptureService.IsRecording || _isProcessingVoice) return;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsPttActive = true;
                OverallStatus = "RECORDING";
                VisualCMD.CurrentState = ProcessingState.Recording;
                VisualCMD.StateMessage = "Microphone active. Recording speech...";
                VisualCMD.AudioStatus = "Capturing audio (WASAPI)...";
            });

            _eventLogService.LogInfo($"PTT pressed ({_pttService.ConfiguredKey}). Microphone recording started.");

            try
            {
                var settings = _settingsService.CurrentSettings;
                await _audioCaptureService.StartRecordingAsync(
                    settings.SelectedAudioDeviceId,
                    settings.AudioSampleRate,
                    settings.AudioChannels);
            }
            catch (Exception ex)
            {
                _eventLogService.LogError($"Microphone error: {ex.Message}");
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    VisualCMD.CurrentState = ProcessingState.Error;
                    VisualCMD.StateMessage = $"Mic error: {ex.Message}";
                    VisualCMD.AudioStatus = "Error";
                    IsPttActive = false;
                    OverallStatus = "ERROR";
                });
            }
        };

        _pttService.PttReleased += async (s, e) =>
        {
            if (!_audioCaptureService.IsRecording) return;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsPttActive = false;
                OverallStatus = "PROCESSING";
                VisualCMD.CurrentState = ProcessingState.ProcessingAudio;
                VisualCMD.StateMessage = "Audio captured. Processing voice command...";
                VisualCMD.AudioStatus = "Finalizing WAV stream...";
            });

            byte[] audioBytes = Array.Empty<byte>();
            try
            {
                audioBytes = await _audioCaptureService.StopRecordingAsync();
            }
            catch (Exception ex)
            {
                _eventLogService.LogError($"Audio stop error: {ex.Message}");
            }

            _eventLogService.LogInfo($"PTT released. Audio recording completed ({audioBytes.Length:N0} bytes captured).");

            if (audioBytes.Length < 1000)
            {
                _eventLogService.LogWarning("Audio was too short or silent. Command ignored.");
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    VisualCMD.CurrentState = ProcessingState.Idle;
                    VisualCMD.StateMessage = $"Press and hold {_pttService.ConfiguredKey} to speak.";
                    VisualCMD.AudioStatus = "Idle";
                    OverallStatus = "IDLE";
                });
                return;
            }

            _ = ProcessCapturedAudioAsync(audioBytes);
        };

        _audioCaptureService.InputLevelChanged += (s, level) =>
        {
            Application.Current?.Dispatcher?.BeginInvoke(() =>
            {
                VisualCMD.MicInputLevel = level;
            });
        };

        _audioCaptureService.RecordingDurationChanged += (s, duration) =>
        {
            Application.Current?.Dispatcher?.BeginInvoke(() =>
            {
                VisualCMD.RecordingDurationText = $"{duration:mm\\:ss\\.f}";
            });
        };
    }

    private async Task ProcessCapturedAudioAsync(byte[] audioBytes)
    {
        _isProcessingVoice = true;
        try
        {
            var settings = _settingsService.CurrentSettings;
            var commands = _registryService.Commands;
            var activeCount = commands.Count(c => c.IsEnabled);
            _eventLogService.LogInfo($"AI prompt schema prepared with {activeCount} active commands (of {commands.Count} total registered).");

            VoiceCommandResult result;

            if (settings.ArchitectureMode == ProcessingArchitectureMode.Simple)
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    VisualCMD.CurrentState = ProcessingState.Transcribing;
                    VisualCMD.StateMessage = $"Submitting voice audio to {settings.SimpleProvider}...";
                    VisualCMD.AudioStatus = "AI Ingestion...";
                });

                _eventLogService.LogInfo($"Sending audio to {settings.SimpleProvider} ({settings.SimpleModel})...");

                var provider = _providerFactory.GetMultimodalProvider(settings.SimpleProvider);
                result = await provider.ProcessAudioAsync(
                    audioBytes,
                    settings.SimpleModel,
                    commands,
                    CancellationToken.None);
            }
            else
            {
                // Advanced Mode: STT first, then LLM
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    VisualCMD.CurrentState = ProcessingState.Transcribing;
                    VisualCMD.StateMessage = $"Transcribing voice audio via {settings.SttProvider}...";
                    VisualCMD.AudioStatus = "STT Ingestion...";
                });

                _eventLogService.LogInfo($"Sending audio to STT: {settings.SttProvider} ({settings.SttModel})...");
                var stt = _providerFactory.GetSttProvider(settings.SttProvider);
                var sttResult = await stt.TranscribeAsync(audioBytes, settings.SttModel, CancellationToken.None);

                if (!sttResult.IsSuccess)
                {
                    result = new VoiceCommandResult
                    {
                        Error = sttResult.Error ?? "Transcription failed.",
                        Provider = settings.SttProvider,
                        ModelName = settings.SttModel,
                        DurationMs = sttResult.DurationMs
                    };
                }
                else
                {
                    _eventLogService.LogInfo($"Transcript received: \"{sttResult.Transcript}\"");
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        VisualCMD.TranscribedText = sttResult.Transcript;
                        VisualCMD.SttLatencyMs = sttResult.DurationMs;
                        VisualCMD.CurrentState = ProcessingState.InterpretingCommand;
                        VisualCMD.StateMessage = $"Interpreting intent via {settings.LlmProvider}...";
                    });

                    _eventLogService.LogInfo($"Sending transcript to LLM: {settings.LlmProvider} ({settings.LlmModel})...");
                    var interpreter = _providerFactory.GetInterpreter(settings.LlmProvider);
                    result = await interpreter.InterpretAsync(
                        sttResult.Transcript,
                        settings.LlmModel,
                        commands,
                        CancellationToken.None);
                }
            }

            // Display results in UI
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                VisualCMD.AudioStatus = "Idle";
                VisualCMD.RawJsonResponse = result.RawJsonResponse ?? (!string.IsNullOrEmpty(result.Error) ? $"{{\"error\": \"{result.Error}\"}}" : null);

                if (!string.IsNullOrEmpty(result.Error))
                {
                    VisualCMD.CurrentState = ProcessingState.Error;
                    VisualCMD.StateMessage = $"AI Error: {result.Error}";
                    VisualCMD.ErrorMessage = result.Error;
                    OverallStatus = "ERROR";
                    _eventLogService.LogError($"AI processing error: {result.Error}");
                    _soundEffectService.PlayAiError();
                }
                else
                {
                    VisualCMD.TranscribedText = result.Transcript;
                    VisualCMD.SetSuggestedCommand(result.CommandId ?? "NO_MATCHING_COMMAND", result.SpokenPhrase);
                    VisualCMD.InterpretedIntent = result.Intent;
                    VisualCMD.LlmReason = result.Reason;
                    VisualCMD.LlmConfidence = result.Confidence;
                    VisualCMD.LlmLatencyMs = result.DurationMs;
                    VisualCMD.SttLatencyMs = result.DurationMs;

                    var matched = commands.FirstOrDefault(c => string.Equals(c.CrewChiefCommandId, result.CommandId, StringComparison.OrdinalIgnoreCase));
                    VisualCMD.SuggestedCommandDescription = matched?.Description ?? (result.CommandId != null ? "Matched command." : "No matching Crew Chief command identified.");

                    VisualCMD.CurrentState = ProcessingState.Completed;
                    VisualCMD.StateMessage = result.CommandId != null
                        ? $"Suggested command: {result.CommandId}"
                        : "Command suggestion generated (No matching ID).";
                    OverallStatus = "COMPLETED";

                    _eventLogService.LogInfo($"Speech recognition completed: \"{result.Transcript}\"");
                    _eventLogService.LogInfo($"Suggested command: {result.CommandId ?? "None"} (Confidence: {(result.Confidence.HasValue ? $"{result.Confidence.Value * 100:F0}%" : "Unavailable")})");

                    VisualCMD.AddHistoryItem(new ProcessingHistoryItem
                    {
                        Timestamp = DateTime.Now,
                        Transcript = result.Transcript,
                        SuggestedCommand = result.CommandId,
                        Intent = result.Intent,
                        Confidence = result.Confidence,
                        Reason = result.Reason,
                        Provider = result.Provider ?? settings.SimpleProvider,
                        DurationMs = result.DurationMs,
                        Status = result.IsSuccess ? "Success" : "Unmatched",
                        RawJsonResponse = result.RawJsonResponse
                    });

                    // Auto-forward to Crew Chief if enabled, otherwise play native didn't understand response
                    if (settings.EnableCrewChiefBridge && settings.AutoSendToCrewChief && !string.IsNullOrEmpty(result.CommandId) && result.IsSuccess && result.CommandId != "NO_MATCHING_COMMAND")
                    {
                        var phraseToSpeak = !string.IsNullOrWhiteSpace(result.SpokenPhrase)
                            ? result.SpokenPhrase
                            : _crewChiefBridge.GetCanonicalPhrase(result.CommandId);

                        _eventLogService.LogInfo($"Auto-forwarding '{result.CommandId}' to Crew Chief via TTS audio bridge (Phrase: \"{phraseToSpeak}\")...");
                        _ = Task.Run(async () =>
                        {
                            var ok = await _crewChiefBridge.SendCommandAsync(result.CommandId, phraseToSpeak);
                            if (ok)
                            {
                                _eventLogService.LogInfo($"Command '{result.CommandId}' successfully dispatched to Crew Chief.");
                            }
                        });
                    }
                    else
                    {
                        // Play native Crew Chief didn't understand voice clip for unmatched speech
                        _soundEffectService.PlayDidntUnderstand();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _eventLogService.LogError($"Pipeline error: {ex.Message}");
            _soundEffectService.PlayAiError();
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                VisualCMD.CurrentState = ProcessingState.Error;
                VisualCMD.StateMessage = $"Error: {ex.Message}";
                OverallStatus = "ERROR";
            });
        }
        finally
        {
            _isProcessingVoice = false;
        }
    }

    private void UpdateProviderInfo()
    {
        var s = _settingsService.CurrentSettings;
        ActiveProviderInfo = s.ArchitectureMode == ProcessingArchitectureMode.Simple
            ? $"{s.SimpleProvider} ({s.SimpleModel}) • Single Multimodal Model"
            : $"STT: {s.SttProvider} | LLM: {s.LlmProvider} • Two-Model Mode";

        VisualCMD.SttProvider = s.ArchitectureMode == ProcessingArchitectureMode.Simple ? s.SimpleProvider : s.SttProvider;
        VisualCMD.SttModel = s.ArchitectureMode == ProcessingArchitectureMode.Simple ? s.SimpleModel : s.SttModel;
        VisualCMD.LlmProvider = s.ArchitectureMode == ProcessingArchitectureMode.Simple ? s.SimpleProvider : s.LlmProvider;
        VisualCMD.LlmModel = s.ArchitectureMode == ProcessingArchitectureMode.Simple ? s.SimpleModel : s.LlmModel;
    }

    private void UpdateCrewChiefDisplay(CrewChiefConnectionState state)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            var s = _settingsService.CurrentSettings;
            IsCrewChiefRunning = state != CrewChiefConnectionState.NotRunning;
            CanLaunchCrewChief = !IsCrewChiefRunning;
            CanToggleCrewChiefSession = IsCrewChiefRunning;
            IsCrewChiefSessionActive = state == CrewChiefConnectionState.Active;
            CanStartCrewChiefSession = IsCrewChiefRunning && !IsCrewChiefSessionActive;
            CanStopCrewChiefSession = IsCrewChiefRunning && IsCrewChiefSessionActive;

            if (IsCrewChiefSessionActive)
            {
                SessionButtonText = "STOP CREW CHIEF";
                SessionButtonBackground = "Transparent";
                SessionButtonBorderBrush = "#767D8E"; // Gray outline
                SessionButtonBorderThickness = "1";
                SessionButtonForeground = "#FFFFFF";
            }
            else
            {
                SessionButtonText = "START CREW CHIEF";
                SessionButtonBackground = "#E50914"; // Solid motorsport red
                SessionButtonBorderBrush = "#E50914";
                SessionButtonBorderThickness = "0";
                SessionButtonForeground = "#FFFFFF";
            }

            switch (state)
            {
                case CrewChiefConnectionState.Active:
                    CrewChiefStatusDisplay = "Crew Chief: Active";
                    CrewChiefStateColor = "#00E676";
                    CrewChiefStateBackground = "#0D2518";
                    break;
                case CrewChiefConnectionState.Running:
                    CrewChiefStatusDisplay = "Crew Chief: Running";
                    CrewChiefStateColor = "#00E5FF";
                    CrewChiefStateBackground = "#0B2228";
                    break;
                default:
                    CrewChiefStatusDisplay = "Crew Chief: Offline";
                    CrewChiefStateColor = "#FF1744";
                    CrewChiefStateBackground = "#221114";
                    break;
            }

            CrewChiefBridgeStatusText = s.EnableCrewChiefBridge
                ? "Crew Chief Audio Bridge: Active"
                : "Crew Chief Audio Bridge: Inactive";
        });
    }
}
