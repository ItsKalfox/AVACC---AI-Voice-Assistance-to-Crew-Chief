# AVACC System Architecture

## 1. Overview & Principles

AVACC is engineered as a lightweight, modular Windows desktop application built with C# and WPF on .NET.

### Core Architecture Pillars:
1. **Decoupled MVVM:** Strict separation between Views (XAML), ViewModels, Core Models, and Services.
2. **Provider Agnostic:** Common abstractions (`IMultimodalVoiceProvider`, `ISpeechToTextProvider`, `ICommandInterpreter`) insulate the application from specific third-party SDK dependencies.
3. **Low Latency & High Responsiveness:** Asynchronous non-blocking I/O with `CancellationToken` support; audio recording is captured via WASAPI with minimal CPU footprint.
4. **Security by Design:** Windows Data Protection API (DPAPI) ensures credentials are encrypted on disk and never logged or exposed in plaintext.

---

## 2. Solution Structure

```text
AVACC/
│
├── AVACC.sln
│
├── src/
│   ├── AVACC.App/               # WPF Application, Windows, XAML Views, ViewModels, DI Host
│   │   ├── Views/               # VisualCMDView, CommandEditorView, SettingsView
│   │   ├── ViewModels/          # MainViewModel, VisualCMDViewModel, CommandEditorViewModel, SettingsViewModel
│   │   ├── Converters/          # UI state, brush, and equality value converters
│   │   └── App.xaml             # Dark theme resources, palettes, and ServiceCollection DI setup
│   │
│   ├── AVACC.Core/              # Domain models, interfaces, base commands, configuration
│   │   ├── Models/              # CommandDefinition, VoiceCommandResult, ProcessingState, LogEntry
│   │   ├── Interfaces/          # IAudioCaptureService, IPushToTalkService, IAIInterfaces, ISettingsService
│   │   ├── Commands/            # RelayCommand, AsyncRelayCommand, ObservableObject
│   │   └── Configuration/       # AppSettings, ProcessingArchitectureMode
│   │
│   ├── AVACC.Audio/             # WASAPI microphone capture and global PTT keyboard hook
│   │   ├── AudioCaptureService.cs  # WASAPI capture with RMS peak meter and WAV stream buffering
│   │   ├── MicrophoneManager.cs    # WASAPI endpoint enumeration
│   │   ├── PushToTalkService.cs    # Win32 WH_KEYBOARD_LL global hook
│   │   └── AudioDiagnostics.cs     # System audio endpoint report generator
│   │
│   ├── AVACC.AI/                # AI provider implementations, prompt building, and model discovery
│   │   ├── Providers/           # GeminiProvider, OpenAIProvider, GroqProvider
│   │   ├── PromptBuilder.cs     # System prompt formatting with few-shot examples & schema validation
│   │   ├── SpeechToTextService.cs
│   │   ├── IntentInterpreter.cs
│   │   └── ModelDiscoveryService.cs
│   │
│   └── AVACC.Infrastructure/    # Persistence and OS-level security
│       ├── SecureKeyStorage.cs     # DPAPI encrypted key storage in %AppData%/AVACC/keys
│       ├── SettingsService.cs      # JSON configuration management
│       ├── CommandRegistryService.cs # Default & custom Crew Chief command registry
│       └── EventLogService.cs      # Thread-safe observable chronological diagnostic logger
│
└── tests/
    ├── AVACC.Core.Tests/        # Core models, DPAPI, settings, and command registry unit tests
    ├── AVACC.AI.Tests/          # Natural language variation mapping and JSON schema tests
    └── AVACC.Audio.Tests/       # Audio manager and PTT service configuration tests
```

---

## 3. Processing Lifecycle

```text
User holds PTT Key (Right Ctrl)
  │
  ├─► PushToTalkService detects KeyDown via WH_KEYBOARD_LL hook
  │     └─► Visual CMD state changes to RECORDING
  │     └─► AudioCaptureService starts WasapiCapture (16kHz 16-bit Mono)
  │     └─► Real-time RMS level meter updates UI
  │
User releases PTT Key
  │
  ├─► PushToTalkService detects KeyUp
  │     └─► Visual CMD state changes to PROCESSING
  │     └─► AudioCaptureService flushes & returns in-memory WAV byte array
  │
AI Processing (Selected Mode)
  │
  ├─► [Simple Mode]: GeminiProvider.ProcessAudioAsync
  │     └─► Single HTTP POST to Gemini multimodal endpoint with inline audio/wav
  │     └─► Model returns transcript, intent, suggested command, and confidence
  │
  └─► [Advanced Mode]:
        ├─► STT Provider (e.g. Whisper) transcribes audio bytes -> transcript string
        └─► LLM Provider (e.g. GPT-4o-mini / Groq Llama) maps transcript -> command ID
  │
Validation & Verification
  │
  ├─► PromptBuilder.ParseStructuredResponse validates command ID against registry
  ├─► Reject hallucinated, non-existent, or disabled command IDs
  │
UI Dashboard Update
  │
  └─► Visual CMD renders suggested command, confidence, latency, JSON, and history entry
```

---

## 4. Security & Privacy Model

- **Windows DPAPI:** `System.Security.Cryptography.ProtectedData.Protect` is used with `DataProtectionScope.CurrentUser`. Keys cannot be read by another user account on the machine.
- **Audio Privacy:** Audio is held strictly in volatile `MemoryStream` buffers and freed after processing.
- **Log Sanitation:** API keys are scrubbed and never output to `EventLogService` or diagnostic files.

