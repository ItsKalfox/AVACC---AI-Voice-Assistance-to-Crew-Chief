using System.Collections.ObjectModel;
using System.Windows;
using AVACC.Core.Commands;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;

namespace AVACC.App.ViewModels;

public class VisualCMDViewModel : ObservableObject
{
    private readonly IEventLogService _eventLogService;
    private readonly ISettingsService _settingsService;
    private readonly IPushToTalkService _pttService;

    private ProcessingState _currentState = ProcessingState.Idle;
    private string _stateMessage = "Press and hold Right Ctrl to speak.";
    private string _selectedMicrophoneName = "Default Microphone";
    private float _micInputLevel;
    private string _recordingDurationText = "00:00.0";
    private string _sampleRateDisplay = "16,000 Hz • 16-bit Mono";
    private string _audioStatus = "Idle";

    private string _transcribedText = string.Empty;
    private string _sttProvider = "Google Gemini";
    private string _sttModel = "gemini-1.5-flash";
    private long _sttLatencyMs;
    private string _sttConfidenceDisplay = "Confidence unavailable";

    private string _interpretedIntent = string.Empty;
    private string? _suggestedCommandId;
    private string _suggestedCommandDescription = string.Empty;
    private string? _llmReason;
    private double? _llmConfidence;
    private long _llmLatencyMs;
    private string _llmModel = "gemini-1.5-flash";
    private string _llmProvider = "Google Gemini";
    private string? _rawJsonResponse;
    private bool _requiresClarification;
    private string? _errorMessage;

    private ProcessingHistoryItem? _selectedHistoryItem;

    public ProcessingState CurrentState
    {
        get => _currentState;
        set => SetProperty(ref _currentState, value);
    }

    public string StateMessage
    {
        get => _stateMessage;
        set => SetProperty(ref _stateMessage, value);
    }

    public string SelectedMicrophoneName
    {
        get => _selectedMicrophoneName;
        set => SetProperty(ref _selectedMicrophoneName, value);
    }

    public float MicInputLevel
    {
        get => _micInputLevel;
        set => SetProperty(ref _micInputLevel, value);
    }

    public string RecordingDurationText
    {
        get => _recordingDurationText;
        set => SetProperty(ref _recordingDurationText, value);
    }

    public string SampleRateDisplay
    {
        get => _sampleRateDisplay;
        set => SetProperty(ref _sampleRateDisplay, value);
    }

    public string AudioStatus
    {
        get => _audioStatus;
        set => SetProperty(ref _audioStatus, value);
    }

    public string TranscribedText
    {
        get => _transcribedText;
        set => SetProperty(ref _transcribedText, value);
    }

    public string SttProvider
    {
        get => _sttProvider;
        set => SetProperty(ref _sttProvider, value);
    }

    public string SttModel
    {
        get => _sttModel;
        set => SetProperty(ref _sttModel, value);
    }

    public long SttLatencyMs
    {
        get => _sttLatencyMs;
        set => SetProperty(ref _sttLatencyMs, value);
    }

    public string SttConfidenceDisplay
    {
        get => _sttConfidenceDisplay;
        set => SetProperty(ref _sttConfidenceDisplay, value);
    }

    public string InterpretedIntent
    {
        get => _interpretedIntent;
        set => SetProperty(ref _interpretedIntent, value);
    }

    public string? SuggestedCommandId
    {
        get => _suggestedCommandId;
        set => SetProperty(ref _suggestedCommandId, value);
    }

    public string SuggestedCommandDescription
    {
        get => _suggestedCommandDescription;
        set => SetProperty(ref _suggestedCommandDescription, value);
    }

    public string? LlmReason
    {
        get => _llmReason;
        set => SetProperty(ref _llmReason, value);
    }

    public double? LlmConfidence
    {
        get => _llmConfidence;
        set => SetProperty(ref _llmConfidence, value);
    }

    public long LlmLatencyMs
    {
        get => _llmLatencyMs;
        set => SetProperty(ref _llmLatencyMs, value);
    }

    public string LlmModel
    {
        get => _llmModel;
        set => SetProperty(ref _llmModel, value);
    }

    public string LlmProvider
    {
        get => _llmProvider;
        set => SetProperty(ref _llmProvider, value);
    }

    public string? RawJsonResponse
    {
        get => _rawJsonResponse;
        set => SetProperty(ref _rawJsonResponse, value);
    }

    public bool RequiresClarification
    {
        get => _requiresClarification;
        set => SetProperty(ref _requiresClarification, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<LogEntry> EventLogs => _eventLogService.Entries;
    public ObservableCollection<ProcessingHistoryItem> HistoryItems { get; } = new();

    public ProcessingHistoryItem? SelectedHistoryItem
    {
        get => _selectedHistoryItem;
        set
        {
            if (SetProperty(ref _selectedHistoryItem, value) && value != null)
            {
                ApplyHistoryItem(value);
            }
        }
    }

    public RelayCommand ClearLogsCommand { get; }
    public RelayCommand CopyLogsCommand { get; }
    public AsyncRelayCommand ExportLogsCommand { get; }
    public RelayCommand ClearHistoryCommand { get; }
    public RelayCommand TriggerPttDownCommand { get; }
    public RelayCommand TriggerPttUpCommand { get; }

    public VisualCMDViewModel(
        IEventLogService eventLogService, 
        ISettingsService settingsService,
        IPushToTalkService pttService)
    {
        _eventLogService = eventLogService;
        _settingsService = settingsService;
        _pttService = pttService;

        TriggerPttDownCommand = new RelayCommand(() => _pttService.TriggerPttDown());
        TriggerPttUpCommand = new RelayCommand(() => _pttService.TriggerPttUp());
        ClearLogsCommand = new RelayCommand(() => _eventLogService.Clear());
        ClearHistoryCommand = new RelayCommand(ClearHistoryWithConfirmation);
        CopyLogsCommand = new RelayCommand(CopyLogsToClipboard);
        ExportLogsCommand = new AsyncRelayCommand(ExportLogsToFileAsync);

        _eventLogService.LogInfo("AVACC Subsystem initialized. Ready for PTT capture.");
        _eventLogService.LogInfo("Note: Version 0.1 generates and displays command suggestions only (Crew Chief IPC inactive).");
    }

    private void ClearHistoryWithConfirmation()
    {
        if (HistoryItems.Count == 0) return;

        var confirm = System.Windows.MessageBox.Show(
            "Are you sure you want to clear all recent voice command history entries?",
            "Clear Command History",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm == System.Windows.MessageBoxResult.Yes)
        {
            HistoryItems.Clear();
            SelectedHistoryItem = null;
            _eventLogService.LogInfo("Voice command history cleared.");
        }
    }

    private void CopyLogsToClipboard()
    {
        var logs = string.Join(Environment.NewLine, EventLogs.Select(l => $"[{l.FormattedTimestamp}] [{l.Level}] {l.Message}"));
        Clipboard.SetText(logs);
        _eventLogService.LogInfo("Logs copied to clipboard.");
    }

    private async Task ExportLogsToFileAsync()
    {
        try
        {
            var content = await _eventLogService.ExportLogsAsync();
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"AVACC_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await System.IO.File.WriteAllTextAsync(path, content);
            _eventLogService.LogInfo($"Logs exported to Desktop: {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            _eventLogService.LogError($"Failed to export logs: {ex.Message}");
        }
    }

    public void AddHistoryItem(ProcessingHistoryItem item)
    {
        HistoryItems.Insert(0, item);
        while (HistoryItems.Count > 100)
        {
            HistoryItems.RemoveAt(HistoryItems.Count - 1);
        }
    }

    private void ApplyHistoryItem(ProcessingHistoryItem item)
    {
        TranscribedText = item.Transcript;
        SuggestedCommandId = item.SuggestedCommand;
        InterpretedIntent = item.Intent ?? string.Empty;
        LlmReason = item.Reason;
        LlmConfidence = item.Confidence;
        RawJsonResponse = item.RawJsonResponse;
    }
}

