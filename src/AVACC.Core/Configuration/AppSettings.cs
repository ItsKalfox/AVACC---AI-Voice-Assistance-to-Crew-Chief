namespace AVACC.Core.Configuration;

public enum ProcessingArchitectureMode
{
    Simple,   // Single multimodal model
    Advanced  // Two-model architecture (STT + LLM)
}

public class AppSettings
{
    // Architecture
    public ProcessingArchitectureMode ArchitectureMode { get; set; } = ProcessingArchitectureMode.Simple;

    // Simple Mode Configuration (Default: Gemini Flash audio-capable)
    public string SimpleProvider { get; set; } = "Google Gemini";
    public string SimpleModel { get; set; } = "gemini-1.5-flash";

    // Advanced Mode Configuration
    public string SttProvider { get; set; } = "Google Gemini";
    public string SttModel { get; set; } = "gemini-1.5-flash";

    public string LlmProvider { get; set; } = "Google Gemini";
    public string LlmModel { get; set; } = "gemini-1.5-flash";

    // Push to Talk Settings
    public string PttKey { get; set; } = "RightCtrl";
    public bool IsGlobalPttEnabled { get; set; } = true;
    public bool DisablePttWhenInactive { get; set; } = false;
    public int MaxRecordingDurationSeconds { get; set; } = 10;
    public bool VoiceActivityDetectionEnabled { get; set; } = false;
    public bool NoiseSuppressionEnabled { get; set; } = false;

    // Audio Capture Settings
    public string? SelectedAudioDeviceId { get; set; }
    public string? SelectedAudioDeviceName { get; set; }
    public int AudioSampleRate { get; set; } = 16000;
    public int AudioChannels { get; set; } = 1;
    public float InputLevelBoost { get; set; } = 1.0f;

    // AI Confidence & Reasoning
    public double MinConfidenceThreshold { get; set; } = 0.50;

    // AI Prompt Optimization
    public bool OptimizePromptPayload { get; set; } = true;

    // Diagnostics & Privacy
    public bool EnableDiagnosticLogging { get; set; } = true;
    public bool SaveAudioRecordingsToDisk { get; set; } = false; // Never retain raw audio by default

    // UI Preferences
    public string Theme { get; set; } = "Dark";
    public bool AutoScrollLogs { get; set; } = true;
}

