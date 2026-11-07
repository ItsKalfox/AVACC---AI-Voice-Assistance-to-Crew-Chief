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

    // PTT Sound Effects & Audio Cues
    public bool EnablePttStartSound { get; set; } = true;
    public bool EnablePttEndSound { get; set; } = true;
    public bool EnableDidntUnderstandSound { get; set; } = true;
    public string SelectedChiefVoicePersona { get; set; } = "Auto-Detect";
    public bool EnableAiErrorSound { get; set; } = true;
    public string? PttSoundOutputDeviceId { get; set; }
    public string? PttSoundOutputDeviceName { get; set; }
    public float PttSoundVolume { get; set; } = 0.8f;

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

    // Crew Chief Integration Bridge
    public bool EnableCrewChiefBridge { get; set; } = true;
    public bool AutoSendToCrewChief { get; set; } = true;
    public bool AutoLaunchCrewChiefOnStartup { get; set; } = false;
    public bool AutoSwitchCrewChiefInputDeviceOnLaunch { get; set; } = true;
    public string? CrewChiefInputDeviceId { get; set; }
    public string? CrewChiefInputDeviceName { get; set; }
    public string? CustomCrewChiefPath { get; set; }
    public string? CrewChiefAudioOutputDeviceId { get; set; }
    public string? CrewChiefAudioOutputDeviceName { get; set; }
    public string CrewChiefPttKey { get; set; } = "None";
    public int TtsSpeechRate { get; set; } = 2;

    // Diagnostics & Privacy
    public bool EnableDiagnosticLogging { get; set; } = true;
    public bool SaveAudioRecordingsToDisk { get; set; } = false; // Never retain raw audio by default

    // UI Preferences
    public string Theme { get; set; } = "Dark";
    public bool AutoScrollLogs { get; set; } = true;
}

