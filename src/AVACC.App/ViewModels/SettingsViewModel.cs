using System.Collections.ObjectModel;
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
    private bool _isGlobalPtt = true;
    private bool _disablePttWhenInactive;
    private int _maxRecordingDuration = 10;
    private bool _voiceActivityDetection;
    private bool _noiseSuppression;
    private bool _optimizePromptPayload = true;

    // Audio settings
    public ObservableCollection<AudioDeviceInfo> AvailableAudioDevices { get; } = new();
    public ObservableCollection<int> AvailableSampleRates { get; } = new() { 16000, 44100, 48000 };
    private AudioDeviceInfo? _selectedAudioDevice;
    private int _sampleRate = 16000;
    private float _inputBoost = 1.0f;
    private string _audioTestStatus = string.Empty;

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
        set => SetProperty(ref _pttKey, value);
    }

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

    // Commands
    public AsyncRelayCommand SaveApiKeyCommand { get; }
    public AsyncRelayCommand RemoveApiKeyCommand { get; }
    public AsyncRelayCommand TestConnectionCommand { get; }
    public AsyncRelayCommand RefreshModelsCommand { get; }
    public AsyncRelayCommand RefreshAudioDevicesCommand { get; }
    public AsyncRelayCommand SaveSettingsCommand { get; }

    public SettingsViewModel(
        ISettingsService settingsService,
        ISecureKeyStorage secureKeyStorage,
        IModelDiscoveryService modelDiscoveryService,
        IAudioCaptureService audioCaptureService,
        IEventLogService eventLogService)
    {
        _settingsService = settingsService;
        _secureKeyStorage = secureKeyStorage;
        _modelDiscoveryService = modelDiscoveryService;
        _audioCaptureService = audioCaptureService;
        _eventLogService = eventLogService;

        SaveApiKeyCommand = new AsyncRelayCommand(SaveApiKeyAsync);
        RemoveApiKeyCommand = new AsyncRelayCommand(RemoveApiKeyAsync);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        RefreshModelsCommand = new AsyncRelayCommand(RefreshModelsAsync);
        RefreshAudioDevicesCommand = new AsyncRelayCommand(RefreshAudioDevicesAsync);
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

        SampleRate = s.AudioSampleRate;
        InputBoost = s.InputLevelBoost;

        _ = CheckExistingKeyAsync();
        _ = RefreshModelsAsync();
        _ = RefreshSttModelsAsync();
        _ = RefreshLlmModelsAsync();
        _ = RefreshAudioDevicesAsync();
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

        if (SelectedAudioDevice != null)
        {
            s.SelectedAudioDeviceId = SelectedAudioDevice.Id;
            s.SelectedAudioDeviceName = SelectedAudioDevice.Name;
        }

        s.AudioSampleRate = SampleRate;
        s.InputLevelBoost = InputBoost;

        await _settingsService.SaveSettingsAsync(s);
        _eventLogService.LogInfo("All configuration settings saved.");
    }
}

