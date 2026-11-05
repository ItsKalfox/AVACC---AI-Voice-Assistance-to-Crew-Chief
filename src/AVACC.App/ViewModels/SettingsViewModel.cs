using System.Collections.ObjectModel;
using System.Windows;
using AVACC.Core.Commands;
using AVACC.Core.Configuration;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;

namespace AVACC.App.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ISecureKeyStorage _secureKeyStorage;
    private readonly IModelDiscoveryService _modelDiscoveryService;
    private readonly IAudioCaptureService _audioCaptureService;
    private readonly IEventLogService _eventLogService;
    private readonly ICrewChiefBridgeService _crewChiefBridge;
    private readonly ICrewChiefMonitorService _crewChiefMonitor;
    private readonly ICommandRegistryService _registryService;
    private readonly IPushToTalkService _pttService;
    private readonly ISoundEffectService? _soundEffectService;

    // Architecture
    private bool _isSimpleMode = true;
    private bool _isAdvancedMode = false;

    // Providers
    public ObservableCollection<string> AvailableProviders { get; } = new()
    {
        "Google Gemini",
        "OpenAI",
        "Groq"
    };

    private string _selectedProvider = "Google Gemini";
    private string _apiKeyInput = string.Empty;
    private bool _hasSavedKey;
    private bool _isTestingConnection;
    private string _connectionTestStatus = string.Empty;
    private bool? _connectionTestSuccess;

    // Models
    public ObservableCollection<ModelInfo> AvailableModels { get; } = new();
    public ObservableCollection<ModelInfo> AvailableSttModels { get; } = new();
    public ObservableCollection<ModelInfo> AvailableLlmModels { get; } = new();

    private string _selectedModel = "gemini-1.5-flash";
    private string _manualModelOverride = string.Empty;

    // Advanced Mode properties
    private string _selectedSttProvider = "Google Gemini";
    private string _selectedSttModel = "gemini-1.5-flash";
    private string _selectedLlmProvider = "Google Gemini";
    private string _selectedLlmModel = "gemini-1.5-flash";

    // PTT settings
    private string _pttKey = "RightCtrl";
    private bool _isListeningForPttKey;
    private bool _isGlobalPtt = true;
    private bool _disablePttWhenInactive;
    private int _maxRecordingDuration = 10;
    private bool _voiceActivityDetection;
    private bool _noiseSuppression;
    private bool _optimizePromptPayload = true;

    // PTT Sound Effects
    private bool _enablePttStartSound = true;
    private bool _enablePttEndSound = true;
    private AudioDeviceInfo? _selectedPttSoundDevice;
    private float _pttSoundVolume = 0.8f;

    // Audio settings
    public ObservableCollection<AudioDeviceInfo> AvailableAudioDevices { get; } = new();
    public ObservableCollection<int> AvailableSampleRates { get; } = new() { 16000, 44100, 48000 };
    private AudioDeviceInfo? _selectedAudioDevice;
    private int _sampleRate = 16000;
    private float _inputBoost = 1.0f;
    private string _audioTestStatus = string.Empty;

    // Crew Chief Integration settings
    public ObservableCollection<AudioDeviceInfo> AvailablePlaybackDevices { get; } = new();
    public ObservableCollection<string> AvailableCrewChiefPttKeys { get; } = new()
    {
        "None", "Insert", "Delete", "Home", "End", "PageUp", "PageDown",
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        "F13", "F14", "F15", "F16", "ScrollLock", "Pause"
    };

    private bool _enableCrewChiefBridge = true;
    private bool _autoSendToCrewChief = true;
    private AudioDeviceInfo? _selectedPlaybackDevice;
    private string _crewChiefPttKey = "None";
    private int _ttsSpeechRate = 2;
    private string _crewChiefTestStatus = string.Empty;
    private bool _isTestingBridge;

    public bool IsSimpleMode
    {
        get => _isSimpleMode;
        set
        {
            if (SetProperty(ref _isSimpleMode, value))
            {
                _isAdvancedMode = !value;
                OnPropertyChanged(nameof(IsAdvancedMode));
            }
        }
    }

    public bool IsAdvancedMode
    {
        get => _isAdvancedMode;
        set
        {
            if (SetProperty(ref _isAdvancedMode, value))
            {
                _isSimpleMode = !value;
                OnPropertyChanged(nameof(IsSimpleMode));
            }
        }
    }

    public string SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetProperty(ref _selectedProvider, value))
            {
                _ = CheckExistingKeyAsync();
                _ = RefreshModelsAsync();
            }
        }
    }

    public string ApiKeyInput
    {
        get => _apiKeyInput;
        set => SetProperty(ref _apiKeyInput, value);
    }

    public bool HasSavedKey
    {
        get => _hasSavedKey;
        set => SetProperty(ref _hasSavedKey, value);
    }

    public bool IsTestingConnection
    {
        get => _isTestingConnection;
        set => SetProperty(ref _isTestingConnection, value);
    }

    public string ConnectionTestStatus
    {
        get => _connectionTestStatus;
        set => SetProperty(ref _connectionTestStatus, value);
    }

    public bool? ConnectionTestSuccess
    {
        get => _connectionTestSuccess;
        set => SetProperty(ref _connectionTestSuccess, value);
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
    }

    public string ManualModelOverride
    {
        get => _manualModelOverride;
        set => SetProperty(ref _manualModelOverride, value);
    }

    public string SelectedSttProvider
    {
        get => _selectedSttProvider;
        set
        {
            if (SetProperty(ref _selectedSttProvider, value))
            {
                _ = RefreshSttModelsAsync();
            }
        }
    }

    public string SelectedSttModel
    {
        get => _selectedSttModel;
        set => SetProperty(ref _selectedSttModel, value);
    }

    public string SelectedLlmProvider
    {
        get => _selectedLlmProvider;
        set
        {
            if (SetProperty(ref _selectedLlmProvider, value))
            {
                _ = RefreshLlmModelsAsync();
            }
        }
    }

    public string SelectedLlmModel
    {
        get => _selectedLlmModel;
        set => SetProperty(ref _selectedLlmModel, value);
    }

    public string PttKey
    {
        get => _pttKey;
        set
        {
            if (SetProperty(ref _pttKey, value))
            {
                OnPropertyChanged(nameof(PttKeyButtonText));
            }
        }
    }

    public bool IsListeningForPttKey
    {
        get => _isListeningForPttKey;
        set
        {
            if (SetProperty(ref _isListeningForPttKey, value))
            {
                OnPropertyChanged(nameof(PttKeyButtonText));
            }
        }
    }

    public string PttKeyButtonText => IsListeningForPttKey
        ? "🔴 Listening... Press any key"
        : $"⌨ Bound Key: {PttKey} (Click to Record)";

    public bool IsGlobalPtt
    {
        get => _isGlobalPtt;
        set => SetProperty(ref _isGlobalPtt, value);
    }

    public bool DisablePttWhenInactive
    {
        get => _disablePttWhenInactive;
        set => SetProperty(ref _disablePttWhenInactive, value);
    }

    public int MaxRecordingDuration
    {
        get => _maxRecordingDuration;
        set => SetProperty(ref _maxRecordingDuration, value);
    }

    public bool VoiceActivityDetection
    {
        get => _voiceActivityDetection;
        set => SetProperty(ref _voiceActivityDetection, value);
    }

    public bool NoiseSuppression
    {
        get => _noiseSuppression;
        set => SetProperty(ref _noiseSuppression, value);
    }

    public bool OptimizePromptPayload
    {
        get => _optimizePromptPayload;
        set => SetProperty(ref _optimizePromptPayload, value);
    }

    public bool EnablePttStartSound
    {
        get => _enablePttStartSound;
        set => SetProperty(ref _enablePttStartSound, value);
    }

    public bool EnablePttEndSound
    {
        get => _enablePttEndSound;
        set => SetProperty(ref _enablePttEndSound, value);
    }

    public AudioDeviceInfo? SelectedPttSoundDevice
    {
        get => _selectedPttSoundDevice;
        set => SetProperty(ref _selectedPttSoundDevice, value);
    }

    public float PttSoundVolume
    {
        get => _pttSoundVolume;
        set
        {
            if (SetProperty(ref _pttSoundVolume, value))
            {
                OnPropertyChanged(nameof(PttSoundVolumePercent));
            }
        }
    }

    public int PttSoundVolumePercent
    {
        get => (int)Math.Round(_pttSoundVolume * 100);
        set => PttSoundVolume = Math.Clamp(value / 100f, 0f, 1f);
    }

    public AudioDeviceInfo? SelectedAudioDevice
    {
        get => _selectedAudioDevice;
        set => SetProperty(ref _selectedAudioDevice, value);
    }

    public int SampleRate
    {
        get => _sampleRate;
        set => SetProperty(ref _sampleRate, value);
    }

    public float InputBoost
    {
        get => _inputBoost;
        set => SetProperty(ref _inputBoost, value);
    }

    public string AudioTestStatus
    {
        get => _audioTestStatus;
        set => SetProperty(ref _audioTestStatus, value);
    }

    public bool EnableCrewChiefBridge
    {
        get => _enableCrewChiefBridge;
        set => SetProperty(ref _enableCrewChiefBridge, value);
    }

    public bool AutoSendToCrewChief
    {
        get => _autoSendToCrewChief;
        set => SetProperty(ref _autoSendToCrewChief, value);
    }

    public AudioDeviceInfo? SelectedPlaybackDevice
    {
        get => _selectedPlaybackDevice;
        set => SetProperty(ref _selectedPlaybackDevice, value);
    }

    public string CrewChiefPttKey
    {
        get => _crewChiefPttKey;
        set => SetProperty(ref _crewChiefPttKey, value);
    }

    public int TtsSpeechRate
    {
        get => _ttsSpeechRate;
        set => SetProperty(ref _ttsSpeechRate, value);
    }

    public string CrewChiefTestStatus
    {
        get => _crewChiefTestStatus;
        set => SetProperty(ref _crewChiefTestStatus, value);
    }

    public bool IsTestingBridge
    {
        get => _isTestingBridge;
        set => SetProperty(ref _isTestingBridge, value);
    }

    public string TestAudioBridgeButtonText
    {
        get
        {
            var phrase = _crewChiefBridge.GetCanonicalPhrase("RADIO_CHECK");
            if (string.IsNullOrWhiteSpace(phrase))
            {
                phrase = "can you hear me";
            }
            return $"Test Audio Bridge (Play '{phrase}')";
        }
    }

    // Commands
    public AsyncRelayCommand SaveApiKeyCommand { get; }
    public AsyncRelayCommand RemoveApiKeyCommand { get; }
    public AsyncRelayCommand TestConnectionCommand { get; }
    public AsyncRelayCommand RefreshModelsCommand { get; }
    public AsyncRelayCommand RefreshAudioDevicesCommand { get; }
    public AsyncRelayCommand RefreshPlaybackDevicesCommand { get; }
    public AsyncRelayCommand TestCrewChiefBridgeCommand { get; }
    public AsyncRelayCommand SaveSettingsCommand { get; }
    public RelayCommand RecordPttKeyCommand { get; }
    public RelayCommand ResetPttKeyToDefaultCommand { get; }
    public RelayCommand TestPttStartSoundCommand { get; }
    public RelayCommand TestPttEndSoundCommand { get; }

    public SettingsViewModel(
        ISettingsService settingsService,
        ISecureKeyStorage secureKeyStorage,
        IModelDiscoveryService modelDiscoveryService,
        IAudioCaptureService audioCaptureService,
        IEventLogService eventLogService,
        ICrewChiefBridgeService crewChiefBridge,
        ICrewChiefMonitorService crewChiefMonitor,
        ICommandRegistryService registryService,
        IPushToTalkService pttService,
        ISoundEffectService? soundEffectService = null)
    {
        _settingsService = settingsService;
        _secureKeyStorage = secureKeyStorage;
        _modelDiscoveryService = modelDiscoveryService;
        _audioCaptureService = audioCaptureService;
        _eventLogService = eventLogService;
        _crewChiefBridge = crewChiefBridge;
        _crewChiefMonitor = crewChiefMonitor;
        _registryService = registryService;
        _pttService = pttService;
        _soundEffectService = soundEffectService;

        _pttService.KeyDetectedForBinding += (s, detectedKey) =>
        {
            Application.Current?.Dispatcher?.Invoke(async () =>
            {
                PttKey = detectedKey;
                IsListeningForPttKey = false;
                _pttService.UpdateConfiguration(PttKey, IsGlobalPtt, DisablePttWhenInactive);
                await SaveAllSettingsAsync();
                _eventLogService.LogInfo($"PTT Trigger Key recorded: '{detectedKey}' and saved.");
            });
        };

        _registryService.CommandsChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TestAudioBridgeButtonText));
        };

        RecordPttKeyCommand = new RelayCommand(RecordPttKey);
        ResetPttKeyToDefaultCommand = new RelayCommand(ResetPttKeyToDefault);
        TestPttStartSoundCommand = new RelayCommand(() => _soundEffectService?.PlaySound("start_ptt.wav", PttSoundVolume, SelectedPttSoundDevice?.Id));
        TestPttEndSoundCommand = new RelayCommand(() => _soundEffectService?.PlaySound("end_ptt.wav", PttSoundVolume, SelectedPttSoundDevice?.Id));
        SaveApiKeyCommand = new AsyncRelayCommand(SaveApiKeyAsync);
        RemoveApiKeyCommand = new AsyncRelayCommand(RemoveApiKeyAsync);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        RefreshModelsCommand = new AsyncRelayCommand(RefreshModelsAsync);
        RefreshAudioDevicesCommand = new AsyncRelayCommand(RefreshAudioDevicesAsync);
        RefreshPlaybackDevicesCommand = new AsyncRelayCommand(RefreshPlaybackDevicesAsync);
        TestCrewChiefBridgeCommand = new AsyncRelayCommand(TestCrewChiefBridgeAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveAllSettingsAsync);

        LoadInitialSettings();
    }

    private void LoadInitialSettings()
    {
        var s = _settingsService.CurrentSettings;
        IsSimpleMode = s.ArchitectureMode == ProcessingArchitectureMode.Simple;
        SelectedProvider = s.SimpleProvider;
        SelectedModel = s.SimpleModel;

        SelectedSttProvider = s.SttProvider;
        SelectedSttModel = s.SttModel;
        SelectedLlmProvider = s.LlmProvider;
        SelectedLlmModel = s.LlmModel;

        PttKey = s.PttKey;
        IsGlobalPtt = s.IsGlobalPttEnabled;
        DisablePttWhenInactive = s.DisablePttWhenInactive;
        MaxRecordingDuration = s.MaxRecordingDurationSeconds;
        VoiceActivityDetection = s.VoiceActivityDetectionEnabled;
        NoiseSuppression = s.NoiseSuppressionEnabled;
        OptimizePromptPayload = s.OptimizePromptPayload;

        EnablePttStartSound = s.EnablePttStartSound;
        EnablePttEndSound = s.EnablePttEndSound;
        PttSoundVolume = s.PttSoundVolume;

        SampleRate = s.AudioSampleRate;
        InputBoost = s.InputLevelBoost;

        EnableCrewChiefBridge = s.EnableCrewChiefBridge;
        AutoSendToCrewChief = s.AutoSendToCrewChief;
        CrewChiefPttKey = s.CrewChiefPttKey;
        TtsSpeechRate = s.TtsSpeechRate;

        _ = CheckExistingKeyAsync();
        _ = RefreshModelsAsync();
        _ = RefreshSttModelsAsync();
        _ = RefreshLlmModelsAsync();
        _ = RefreshAudioDevicesAsync();
        _ = RefreshPlaybackDevicesAsync();
    }

    public async Task CheckExistingKeyAsync()
    {
        HasSavedKey = await _secureKeyStorage.HasApiKeyAsync(SelectedProvider);
        if (HasSavedKey)
        {
            ApiKeyInput = "••••••••••••••••••••••••••••••••";
        }
        else
        {
            ApiKeyInput = string.Empty;
        }
    }

    private async Task SaveApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyInput) || ApiKeyInput.Contains("•••")) return;

        await _secureKeyStorage.SetApiKeyAsync(SelectedProvider, ApiKeyInput);
        HasSavedKey = true;
        ApiKeyInput = "••••••••••••••••••••••••••••••••";
        _eventLogService.LogInfo($"API key saved securely for {SelectedProvider} (DPAPI encrypted).");

        ConnectionTestStatus = "Discovering models from provider...";
        await RefreshModelsAsync();
        ConnectionTestStatus = $"Key saved successfully. Discovered {AvailableModels.Count} models for {SelectedProvider}.";
    }

    private async Task RemoveApiKeyAsync()
    {
        await _secureKeyStorage.RemoveApiKeyAsync(SelectedProvider);
        HasSavedKey = false;
        ApiKeyInput = string.Empty;
        ConnectionTestStatus = "API key removed.";
        _eventLogService.LogInfo($"API key removed for {SelectedProvider}.");
        await RefreshModelsAsync();
    }

    private async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        ConnectionTestStatus = "Connecting to provider API...";
        ConnectionTestSuccess = null;

        try
        {
            var result = await _modelDiscoveryService.TestConnectionAsync(SelectedProvider, SelectedModel);
            ConnectionTestSuccess = result.Success;
            ConnectionTestStatus = result.Message;

            if (result.Success)
            {
                _eventLogService.LogInfo($"API connection test passed for {SelectedProvider}.");
                await RefreshModelsAsync();
            }
            else
            {
                _eventLogService.LogWarning($"API connection test failed for {SelectedProvider}: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            ConnectionTestSuccess = false;
            ConnectionTestStatus = $"Error: {ex.Message}";
            _eventLogService.LogError($"Connection test error: {ex.Message}");
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    private async Task RefreshModelsAsync()
    {
        AvailableModels.Clear();
        var models = await _modelDiscoveryService.GetAvailableModelsAsync(SelectedProvider);
        foreach (var m in models)
        {
            AvailableModels.Add(m);
        }

        if (AvailableModels.Count > 0 && !AvailableModels.Any(m => m.Id == SelectedModel))
        {
            SelectedModel = AvailableModels[0].Id;
        }

        // Also update settings with the validated model
        var s = _settingsService.CurrentSettings;
        s.SimpleModel = SelectedModel;
    }

    private async Task RefreshSttModelsAsync()
    {
        AvailableSttModels.Clear();
        var models = await _modelDiscoveryService.GetAvailableModelsAsync(SelectedSttProvider);
        foreach (var m in models)
        {
            AvailableSttModels.Add(m);
        }

        if (AvailableSttModels.Count > 0 && !AvailableSttModels.Any(m => m.Id == SelectedSttModel))
        {
            SelectedSttModel = AvailableSttModels[0].Id;
        }
    }

    private async Task RefreshLlmModelsAsync()
    {
        AvailableLlmModels.Clear();
        var models = await _modelDiscoveryService.GetAvailableModelsAsync(SelectedLlmProvider);
        foreach (var m in models)
        {
            AvailableLlmModels.Add(m);
        }

        if (AvailableLlmModels.Count > 0 && !AvailableLlmModels.Any(m => m.Id == SelectedLlmModel))
        {
            SelectedLlmModel = AvailableLlmModels[0].Id;
        }
    }

    private async Task RefreshAudioDevicesAsync()
    {
        AvailableAudioDevices.Clear();
        var devices = await _audioCaptureService.GetAvailableDevicesAsync();
        foreach (var dev in devices)
        {
            AvailableAudioDevices.Add(dev);
        }

        SelectedAudioDevice = AvailableAudioDevices.FirstOrDefault(d => d.IsDefault)
            ?? AvailableAudioDevices.FirstOrDefault();
    }

    private Task RefreshPlaybackDevicesAsync()
    {
        AvailablePlaybackDevices.Clear();
        var devices = _crewChiefBridge.GetPlaybackDevices();
        foreach (var dev in devices)
        {
            AvailablePlaybackDevices.Add(dev);
        }

        var s = _settingsService.CurrentSettings;
        if (!string.IsNullOrEmpty(s.CrewChiefAudioOutputDeviceId))
        {
            SelectedPlaybackDevice = AvailablePlaybackDevices.FirstOrDefault(d => d.Id == s.CrewChiefAudioOutputDeviceId)
                ?? AvailablePlaybackDevices.FirstOrDefault(d => d.Name == s.CrewChiefAudioOutputDeviceName);
        }

        if (SelectedPlaybackDevice == null)
        {
            // Suggest Virtual Cable if present, otherwise default
            SelectedPlaybackDevice = AvailablePlaybackDevices.FirstOrDefault(d => d.Name.Contains("CABLE", StringComparison.OrdinalIgnoreCase))
                ?? AvailablePlaybackDevices.FirstOrDefault(d => d.IsDefault)
                ?? AvailablePlaybackDevices.FirstOrDefault();
        }

        // PTT sound output device (default to standard speakers/headset, not virtual cable)
        if (!string.IsNullOrEmpty(s.PttSoundOutputDeviceId))
        {
            SelectedPttSoundDevice = AvailablePlaybackDevices.FirstOrDefault(d => d.Id == s.PttSoundOutputDeviceId)
                ?? AvailablePlaybackDevices.FirstOrDefault(d => d.Name == s.PttSoundOutputDeviceName);
        }

        if (SelectedPttSoundDevice == null)
        {
            SelectedPttSoundDevice = AvailablePlaybackDevices.FirstOrDefault(d => d.IsDefault)
                ?? AvailablePlaybackDevices.FirstOrDefault(d => !d.Name.Contains("CABLE", StringComparison.OrdinalIgnoreCase))
                ?? AvailablePlaybackDevices.FirstOrDefault();
        }

        return Task.CompletedTask;
    }

    private async Task TestCrewChiefBridgeAsync()
    {
        IsTestingBridge = true;
        var phrase = _crewChiefBridge.GetCanonicalPhrase("RADIO_CHECK");
        if (string.IsNullOrWhiteSpace(phrase))
        {
            phrase = "can you hear me";
        }

        CrewChiefTestStatus = $"Playing test phrase '{phrase}' through selected audio device...";
        _eventLogService.LogInfo($"Testing Crew Chief TTS bridge on device: {SelectedPlaybackDevice?.Name ?? "Default"} with phrase: '{phrase}'");

        try
        {
            var success = await _crewChiefBridge.SpeakPhraseAsync(phrase);
            if (success)
            {
                CrewChiefTestStatus = $"Test audio successfully played to device! (Phrase: \"{phrase}\")";
                _eventLogService.LogInfo($"Crew Chief audio bridge test succeeded. Trigger phrase '{phrase}' synthesized.");
            }
            else
            {
                CrewChiefTestStatus = "Test playback failed. Check device selection and Windows audio settings.";
                _eventLogService.LogError("Crew Chief audio bridge test playback failed.");
            }
        }
        catch (Exception ex)
        {
            CrewChiefTestStatus = $"Error: {ex.Message}";
            _eventLogService.LogError($"Crew Chief test error: {ex.Message}");
        }
        finally
        {
            IsTestingBridge = false;
        }
    }

    private void RecordPttKey()
    {
        if (IsListeningForPttKey)
        {
            IsListeningForPttKey = false;
            _pttService.StopCapturingBinding();
            _eventLogService.LogInfo("PTT key recording cancelled.");
        }
        else
        {
            IsListeningForPttKey = true;
            _pttService.StartCapturingBinding();
            _eventLogService.LogInfo("Listening for PTT key... Press any key on your keyboard.");
        }
    }

    private async void ResetPttKeyToDefault()
    {
        PttKey = "RightCtrl";
        IsListeningForPttKey = false;
        _pttService.StopCapturingBinding();
        _pttService.UpdateConfiguration(PttKey, IsGlobalPtt, DisablePttWhenInactive);
        await SaveAllSettingsAsync();
        _eventLogService.LogInfo("PTT Trigger Key reset to default (RightCtrl).");
    }

    public async Task SaveAllSettingsAsync()
    {
        var s = _settingsService.CurrentSettings;
        s.ArchitectureMode = IsSimpleMode ? ProcessingArchitectureMode.Simple : ProcessingArchitectureMode.Advanced;
        s.SimpleProvider = SelectedProvider;
        s.SimpleModel = !string.IsNullOrWhiteSpace(ManualModelOverride) ? ManualModelOverride : SelectedModel;

        s.SttProvider = SelectedSttProvider;
        s.SttModel = SelectedSttModel;
        s.LlmProvider = SelectedLlmProvider;
        s.LlmModel = SelectedLlmModel;

        s.PttKey = PttKey;
        s.IsGlobalPttEnabled = IsGlobalPtt;
        s.DisablePttWhenInactive = DisablePttWhenInactive;
        s.MaxRecordingDurationSeconds = MaxRecordingDuration;
        s.VoiceActivityDetectionEnabled = VoiceActivityDetection;
        s.NoiseSuppressionEnabled = NoiseSuppression;
        s.OptimizePromptPayload = OptimizePromptPayload;

        // PTT Sound Effects
        s.EnablePttStartSound = EnablePttStartSound;
        s.EnablePttEndSound = EnablePttEndSound;
        s.PttSoundVolume = PttSoundVolume;
        s.PttSoundOutputDeviceId = SelectedPttSoundDevice?.Id;
        s.PttSoundOutputDeviceName = SelectedPttSoundDevice?.Name;

        if (SelectedAudioDevice != null)
        {
            s.SelectedAudioDeviceId = SelectedAudioDevice.Id;
            s.SelectedAudioDeviceName = SelectedAudioDevice.Name;
        }

        s.AudioSampleRate = SampleRate;
        s.InputLevelBoost = InputBoost;

        // Crew Chief settings
        s.EnableCrewChiefBridge = EnableCrewChiefBridge;
        s.AutoSendToCrewChief = AutoSendToCrewChief;
        s.CrewChiefPttKey = CrewChiefPttKey;
        s.TtsSpeechRate = TtsSpeechRate;

        if (SelectedPlaybackDevice != null)
        {
            s.CrewChiefAudioOutputDeviceId = SelectedPlaybackDevice.Id;
            s.CrewChiefAudioOutputDeviceName = SelectedPlaybackDevice.Name;
        }

        _pttService.UpdateConfiguration(PttKey, IsGlobalPtt, DisablePttWhenInactive);
        await _settingsService.SaveSettingsAsync(s);
        _eventLogService.LogInfo("All configuration settings saved.");
    }
}

