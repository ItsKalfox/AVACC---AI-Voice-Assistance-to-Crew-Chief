# Build AVACC — AI Voice Assistance to Crew Chief

## 1. Project Overview

Build a lightweight, standalone Windows desktop application named:

**AVACC — AI Voice Assistance to Crew Chief**

AVACC is an AI-powered voice recognition and command interpretation utility designed specifically for sim racers who use Crew Chief V4, particularly with Assetto Corsa Competizione (ACC).

The application's purpose is to replace traditional Windows speech recognition with modern AI-powered speech-to-text and natural-language understanding.

AVACC captures the user's voice through a configurable push-to-talk (PTT) button, transcribes it using an AI provider, interprets the resulting text using an AI language model, and generates a corresponding Crew Chief command.

**Important: AVACC Version 0.1 must NOT integrate with Crew Chief.**

Do not implement:
- Crew Chief IPC.
- Crew Chief API communication.
- Crew Chief source modifications.
- Command execution inside Crew Chief.
- Telemetry integration.
- Race data processing.
- AI-generated TTS.

The application must only generate, display, configure, and manage suggested Crew Chief commands.

Actual Crew Chief integration will be developed in a future version.

---

# 2. Core Product Concept

AVACC acts as an intelligent intermediary between a racing driver's microphone and Crew Chief's command system.

Example:

The user holds their configured PTT button and says:

"Chief, how much fuel do we need for the next stint?"

AVACC performs the following:

1. Detects that the PTT button has been pressed.
2. Starts microphone recording.
3. Detects that the PTT button has been released.
4. Stops microphone recording.
5. Sends the captured audio to the selected AI speech recognition provider.
6. Receives the transcribed text.
7. Sends the transcript to the selected LLM.
8. Identifies the user's intended Crew Chief command.
9. Converts the intent into a configured Crew Chief command identifier.
10. Displays the complete processing result in the application.

Expected result:

```text
PTT: Released

Speech recognition:
"Chief, how much fuel do we need for the next stint?"

STT Provider:
Google Gemini

LLM interpretation:
Fuel requirement for next stint

Suggested Crew Chief command:
HOW_MUCH_FUEL_TO_END_OF_RACE

Confidence:
98%

Status:
Command suggestion generated successfully.
```

The application must never pretend that a command has been sent to Crew Chief.

The command is only generated and displayed.

---

# 3. Technology Stack

Use:

- C#.
- .NET LTS supported at implementation time.
- WPF for the desktop interface.
- MVVM architecture.
- Microsoft.Extensions.DependencyInjection.
- Microsoft.Extensions.Configuration.
- Microsoft.Extensions.Logging.
- NAudio or an appropriate lightweight WASAPI library for microphone capture.
- Async/await for asynchronous operations.
- CancellationToken for cancellation support.

The application must be lightweight and suitable for running alongside Assetto Corsa Competizione and Crew Chief.

Avoid:
- Electron.
- Web-based desktop shells.
- Embedded browser runtimes.
- Local LLMs.
- Heavy background services.
- Unnecessary dependencies.

---

# 4. Application Architecture

Use a modular architecture.

Suggested project structure:

```text
AVACC/
│
├── AVACC.sln
│
├── src/
│   │
│   ├── AVACC.App/
│   │   ├── App.xaml
│   │   ├── MainWindow.xaml
│   │   ├── Views/
│   │   │   ├── VisualCMDView.xaml
│   │   │   ├── CommandEditorView.xaml
│   │   │   └── SettingsView.xaml
│   │   ├── ViewModels/
│   │   └── Resources/
│   │
│   ├── AVACC.Core/
│   │   ├── Models/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   ├── Commands/
│   │   └── Configuration/
│   │
│   ├── AVACC.Audio/
│   │   ├── AudioCaptureService.cs
│   │   ├── MicrophoneManager.cs
│   │   ├── PushToTalkService.cs
│   │   └── AudioDiagnostics.cs
│   │
│   ├── AVACC.AI/
│   │   ├── Providers/
│   │   │   ├── GeminiProvider.cs
│   │   │   ├── OpenAIProvider.cs
│   │   │   └── GroqProvider.cs
│   │   ├── SpeechToTextService.cs
│   │   ├── IntentInterpreter.cs
│   │   ├── ModelDiscoveryService.cs
│   │   └── PromptBuilder.cs
│   │
│   └── AVACC.Infrastructure/
│       ├── SecureKeyStorage.cs
│       ├── SettingsService.cs
│       └── LoggingService.cs
│
├── tests/
│   ├── AVACC.Core.Tests/
│   ├── AVACC.AI.Tests/
│   └── AVACC.Audio.Tests/
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── PROVIDERS.md
│   └── CONTRIBUTING.md
│
└── README.md
```

Adjust the structure when justified, but maintain clear separation between UI, audio, AI providers, core logic, and infrastructure.

---

# 5. Main Application Interface

The application must have exactly THREE main tabs.

1. Visual CMD.
2. Command Editor.
3. Settings.

Use a modern, compact, dark-themed Windows desktop design.

The interface should feel like a professional sim-racing utility rather than a generic AI chatbot.

Keep it clean, functional, and lightweight.

## TAB 1 — VISUAL CMD

This is the main monitoring and diagnostic dashboard.

It should display everything happening during a voice command lifecycle.

The user must be able to see the complete process in real time.

### A. PTT Status

Display the current PTT state prominently.

Possible states:

- IDLE.
- BUTTON PRESSED.
- RECORDING.
- BUTTON RELEASED.
- PROCESSING AUDIO.
- TRANSCRIBING.
- INTERPRETING COMMAND.
- COMPLETED.
- ERROR.

Use clear visual indicators.

For example:

```text
PUSH TO TALK

● IDLE

Press and hold Right Ctrl to speak.
```

When the user presses the button:

```text
● RECORDING

Microphone active.
Duration: 00:02.4
```

When released:

```text
● PROCESSING

Audio submitted for recognition.
```

### B. Audio Monitoring

Display:

- Selected microphone.
- Microphone input level.
- Recording duration.
- Current audio capture state.
- Audio sample rate.
- Audio processing status.

Provide a small real-time microphone level meter.

Do not continuously record audio.

### C. Speech-to-Text Output

Display:

- Transcribed text.
- STT provider.
- Selected STT model.
- Processing duration.
- API response status.
- Confidence or probability when genuinely provided by the model.

Important:

Not every provider supplies calibrated transcription confidence.

Do not invent confidence percentages.

If the provider does not return a genuine confidence score, display:

"Confidence unavailable"

Optionally implement a separate clearly labelled transcription quality estimate, but never represent it as the provider's actual probability.

### D. LLM Interpretation Output

Display:

- Original transcript.
- Interpreted intent.
- Suggested Crew Chief command identifier.
- Command description.
- Model provider.
- Model name.
- Processing time.
- Interpretation confidence or quality indicators.

Show the structured LLM response.

Example:

```json
{
  "intent": "fuel_requirement",
  "command_id": "HOW_MUCH_FUEL_TO_END_OF_RACE",
  "confidence": 0.97,
  "reason": "The user is asking about fuel required for the next stint.",
  "requires_clarification": false
}
```

Confidence must be treated as the model's self-reported estimate, not a statistically calibrated probability.

### E. Event Log

Create a live chronological event log.

Examples:

```text
[10:15:20] PTT button pressed.
[10:15:20] Microphone recording started.
[10:15:23] PTT button released.
[10:15:23] Audio recording completed.
[10:15:23] Sending audio to Gemini.
[10:15:24] Speech recognition completed.
[10:15:24] Transcript received.
[10:15:24] Sending transcript to LLM.
[10:15:25] Command interpretation completed.
[10:15:25] Suggested command generated.
```

Display errors, warnings, and API failures in the same event log with appropriate visual distinction.

Allow the user to:

- Clear the log.
- Copy logs.
- Export diagnostic logs.
- View detailed error information.

Never log API keys or sensitive credentials.

### F. Processing History

Display recent voice commands.

Each entry should include:

- Timestamp.
- Transcript.
- Suggested command.
- Provider.
- Processing duration.
- Status.

Allow users to select an entry and inspect its details.

Do not permanently store raw audio by default.

---

# 6. TAB 2 — COMMAND EDITOR

This tab manages the relationship between natural-language requests and Crew Chief command identifiers.

It must be editable by users.

This is an essential part of AVACC.

The AI should not invent command identifiers. Instead, it should choose from the user's configured command registry.

### A. Command Registry

Display a searchable and editable list of commands.

Each command should have:

- Command name.
- Crew Chief command identifier.
- Description.
- Example natural-language requests.
- Enabled/disabled status.
- Optional category.
- Optional user-defined aliases.

Example:

| Command name | Crew Chief command ID |
|---|---|
| Fuel to end | HOW_MUCH_FUEL_TO_END_OF_RACE |
| Current fuel | HOWS_MY_FUEL |
| Tyre pressures | WHAT_ARE_MY_TYRE_PRESSURES |
| Gap behind | WHAT_IS_MY_GAP_BEHIND |
| Gap ahead | WHAT_IS_MY_GAP_AHEAD |
| Race update | FULL_UPDATE |

These are example entries only.

The application must clearly distinguish example entries from verified Crew Chief command identifiers.

Create an initial editable example registry.

Users must be able to:

- Add a command.
- Edit a command.
- Delete a command.
- Enable/disable a command.
- Add natural-language examples.
- Change command descriptions.
- Search commands.
- Import command definitions.
- Export command definitions.

### B. AI Command Mapping

When the LLM receives a transcript, it should select one command from the enabled registry.

The prompt sent to the LLM must include the available command definitions and examples.

The LLM must return structured JSON.

Example:

User transcript:

"Can we make it to the finish with the fuel we have?"

Available command:

HOW_MUCH_FUEL_TO_END_OF_RACE

Expected output:

```json
{
  "intent": "fuel_to_end",
  "command_id": "HOW_MUCH_FUEL_TO_END_OF_RACE",
  "confidence": 0.96,
  "requires_clarification": false
}
```

If no suitable command exists:

```json
{
  "intent": "unknown",
  "command_id": null,
  "confidence": 0.2,
  "requires_clarification": true
}
```

Never generate arbitrary command IDs.

### C. Suggested Command Preview

Provide a preview showing:

- Recognised text.
- Selected command.
- Command description.
- Interpretation reason.
- Confidence.
- Validation status.

### D. Command Testing

Allow users to manually enter a sample sentence and test how the configured LLM maps it.

This should work without microphone recording.

Example:

Input:

"How much fuel should I add for the next stint?"

Output:

Suggested command: HOW_MUCH_FUEL_TO_END_OF_RACE

This feature is useful for editing and improving command mappings.

Do not send commands to Crew Chief.

### E. Registry Persistence

Save the user's command registry locally.

Use a structured format such as JSON.

Allow backup and restoration.

Never overwrite the user's command registry without confirmation.

---

# 7. TAB 3 — SETTINGS

This tab contains all application configuration.

Organise settings into clear sections.

## A. AI Provider Settings

Support Bring Your Own Key (BYOK).

Users must be able to configure their own provider credentials.

Initial providers:

- Google Gemini.
- OpenAI.
- Groq, where compatible APIs are available.

Each provider must have:

- Provider selection.
- API key field.
- Show/hide API key.
- Save credentials.
- Remove credentials.
- Test API connection.
- Model selection.
- Model refresh, where supported.
- Manual model-ID entry.

The Test Connection button must make a real API request.

Display actual success and failure results.

Never fake successful API verification.

## B. Processing Architecture

Provide two modes.

### Simple mode — Single multimodal model

One audio-capable model receives microphone audio and returns structured command interpretation.

Pipeline:

```text
Audio
  ↓
Multimodal AI Model
  ↓
Transcript + Intent + Suggested Command
  ↓
Visual CMD
```

This is the recommended default.

Investigate Gemini Flash audio-capable models as the first implementation option.

Do not assume every Flash model supports audio input.

### Advanced mode — Two-model architecture

Allow users to configure separate models.

```text
Audio
  ↓
STT Provider
  ↓
Transcript
  ↓
LLM Provider
  ↓
Intent + Suggested Command
  ↓
Visual CMD
```

Users should independently select:

- STT provider.
- STT model.
- LLM provider.
- LLM model.

Allow different providers for each stage.

For example:

- Gemini STT + OpenAI LLM.
- OpenAI STT + Gemini LLM.
- Groq STT + Gemini LLM.

Only expose combinations supported by actual provider capabilities.

## C. Model Management

Where supported:

- Retrieve available models.
- Display model identifiers.
- Filter models by capability.
- Identify audio input support.
- Identify structured output support.
- Identify text-only models.

Provide manual model-ID entry.

Never hardcode obsolete model identifiers.

Consult current provider documentation when implementing model integrations.

## D. Push-to-Talk Settings

Allow users to:

- Configure the PTT key.
- Change the key binding.
- Enable/disable global PTT.
- Test the key binding.
- Select the microphone.
- Configure maximum recording duration.
- Configure silence handling.
- Enable optional voice activity detection.

Default PTT key: Right Ctrl.

The PTT key must work while the user is playing ACC without switching windows.

Use an appropriate Windows global keyboard hook.

Avoid conflicts with game controls.

Provide an option to disable PTT when the application is not active if required by the user.

## E. Audio Settings

Allow users to configure:

- Input device.
- Audio sample rate.
- Audio channels.
- Microphone input level.
- Optional noise suppression.
- Optional voice activity detection.
- Test recording.

The application must gracefully handle disconnected or unavailable microphones.

## F. Privacy and Security

Display clear information explaining:

- When microphone recording happens.
- Which provider receives audio.
- Whether audio is retained.
- Where API keys are stored.
- What information is written to diagnostic logs.

Store API keys securely using Windows DPAPI or Windows Credential Manager.

Never save credentials in plaintext JSON configuration files.

---

# 8. AI Provider Architecture

Use provider-independent interfaces.

Example:

```csharp
public interface IMultimodalVoiceProvider
{
    Task<VoiceCommandResult> ProcessAudioAsync(
        byte[] audio,
        IReadOnlyList<CommandDefinition> commands,
        CancellationToken cancellationToken);
}
```

```csharp
public interface ISpeechToTextProvider
{
    Task<TranscriptionResult> TranscribeAsync(
        byte[] audio,
        CancellationToken cancellationToken);
}
```

```csharp
public interface ICommandInterpreter
{
    Task<VoiceCommandResult> InterpretAsync(
        string transcript,
        IReadOnlyList<CommandDefinition> commands,
        CancellationToken cancellationToken);
}
```

Each provider must implement only the capabilities it genuinely supports.

The core application must not depend directly on a specific provider SDK.

Implement:

- Provider selection.
- Model selection.
- Capability validation.
- API authentication.
- Error handling.
- Timeouts.
- Cancellation.
- Retry handling where appropriate.

Do not automatically retry requests that could create unexpected duplicate costs without a sensible retry policy.

---

# 9. Structured AI Output

Use a strict internal result model.

Example:

```csharp
public class VoiceCommandResult
{
    public string Transcript { get; set; }

    public string Intent { get; set; }

    public string? CommandId { get; set; }

    public double? Confidence { get; set; }

    public string? Reason { get; set; }

    public bool RequiresClarification { get; set; }

    public string? Error { get; set; }
}
```

The application must validate all model outputs.

Reject:

- Invalid JSON.
- Unknown command IDs.
- Disabled commands.
- Null or empty transcripts.
- Malformed confidence values.
- Arbitrary generated command identifiers.

If the LLM cannot determine a suitable command, display that clearly.

Do not automatically select the closest command if confidence is too low.

---

# 10. Audio Processing and Performance

The application must be suitable for running alongside a demanding racing simulator.

Requirements:

- Capture only during PTT.
- Use efficient audio buffering.
- Avoid unnecessary allocations.
- Dispose audio resources correctly.
- Do not continuously poll microphones.
- Avoid unnecessary CPU-intensive processing.
- Perform network requests asynchronously.
- Provide cancellation.
- Prevent duplicate submissions.
- Handle device changes.

The application should remain responsive during long ACC sessions.

Measure actual resource usage.

Do not claim that the application is lightweight without testing.

---

# 11. Error Handling and Diagnostics

Every major processing stage must produce useful diagnostic information.

Handle:

- Microphone unavailable.
- Recording failure.
- Invalid audio.
- API authentication failure.
- Provider rate limits.
- Network errors.
- Model unavailable.
- Empty transcription.
- Invalid structured output.
- Unknown command.
- Disabled command.
- Low confidence.
- Request timeout.
- User cancellation.

The Visual CMD tab must display these errors.

Errors must be understandable to normal users.

Provide detailed technical information in expandable diagnostic sections.

Never expose API keys in error messages.

---

# 12. Testing Requirements

Implement unit and integration tests.

### Audio tests

- PTT press detection.
- PTT release detection.
- Recording lifecycle.
- Maximum recording duration.
- Cancellation.
- Microphone disconnection.
- Invalid audio handling.

### AI tests

- Speech transcription.
- Different natural-language variations.
- Command mapping.
- Unknown intents.
- Low confidence.
- Malformed JSON.
- Invalid command IDs.
- Disabled commands.
- API authentication errors.
- Network failures.

Example variations:

```text
How much fuel do we need?

What's our fuel requirement?

Can we make it to the finish?

How much petrol should I add?

How are the tyres looking?

What's the gap behind?

Give me a quick race update.
```

Test both multimodal and two-model processing.

### UI tests

- Tab navigation.
- Settings persistence.
- API key configuration.
- Command registry editing.
- Live processing updates.
- Error display.
- Event log updates.

Use mocks for unit tests and real provider requests only in explicitly configured integration tests.

Never include real API keys in the repository.

---

# 13. Documentation

Create:

- README.md
- ARCHITECTURE.md
- PROVIDERS.md
- CONTRIBUTING.md

README must explain:

- What AVACC is.
- Features.
- Installation.
- Supported AI providers.
- API key configuration.
- Single-model and two-model modes.
- PTT configuration.
- Command registry editing.
- Privacy.
- Troubleshooting.
- Known limitations.
- Development setup.

Clearly state that Version 0.1 does not execute Crew Chief commands or connect to Crew Chief.

---

# 14. Development Milestones

Implement the project incrementally.

## Milestone 1 — Foundation

- Create .NET solution.
- Implement WPF application.
- Create three-tab interface.
- Implement MVVM.
- Implement dependency injection.
- Implement basic settings persistence.

Deliverable: Working application shell.

## Milestone 2 — Audio and PTT

- Implement microphone selection.
- Implement WASAPI capture.
- Implement global PTT.
- Implement recording lifecycle.
- Implement microphone diagnostics.

Deliverable: Working push-to-talk audio capture.

## Milestone 3 — Speech Recognition

- Implement first STT provider.
- Implement Gemini multimodal processing.
- Implement OpenAI transcription.
- Implement provider selection.
- Implement model configuration.
- Implement real API connection testing.

Deliverable: Working AI speech recognition.

## Milestone 4 — LLM Command Interpretation

- Implement command registry.
- Implement LLM provider.
- Implement structured output.
- Implement validation.
- Implement command preview.
- Implement command testing.

Deliverable: Accurate natural-language-to-command conversion.

## Milestone 5 — Visual CMD

- Implement live processing states.
- Implement microphone monitoring.
- Implement transcript display.
- Implement LLM output display.
- Implement event log.
- Implement processing history.
- Implement diagnostics.

Deliverable: Complete monitoring dashboard.

## Milestone 6 — Settings and Polish

- Complete API key management.
- Complete provider selection.
- Complete model discovery.
- Complete PTT settings.
- Complete audio settings.
- Complete privacy settings.
- Improve visual design.

Deliverable: Complete AVACC MVP.

## Milestone 7 — Testing and Release

- Run automated tests.
- Perform real microphone tests.
- Test with ACC and Crew Chief running simultaneously.
- Measure CPU and memory usage.
- Measure processing latency.
- Fix discovered problems.
- Create a Windows release build.
- Prepare installation documentation.

Deliverable: AVACC Version 0.1.

---

# 15. Strict Scope Boundaries

For this version:

DO:
- Build a native Windows application.
- Implement AI speech recognition.
- Implement natural-language command interpretation.
- Support multiple AI providers.
- Support BYOK.
- Support single-model and two-model architectures.
- Implement the three requested tabs.
- Implement editable command mappings.
- Display processing diagnostics.
- Generate suggested Crew Chief commands.

DO NOT:
- Integrate with Crew Chief.
- Send commands to Crew Chief.
- Modify Crew Chief source code.
- Read ACC telemetry.
- Calculate actual fuel requirements.
- Generate AI speech responses.
- Implement a local LLM.
- Build an unnecessary web dashboard.
- Implement unrelated features.

AVACC Version 0.1 is strictly an AI speech-processing and command-suggestion application.

---

# 16. Final Product Vision

AVACC should be a small, reliable AI companion for Crew Chief users.

A sim racer should be able to:

1. Configure their preferred AI provider.
2. Enter their own API key.
3. Select their preferred models.
4. Configure their PTT key.
5. Speak naturally while racing.
6. See their speech transcribed.
7. See the AI interpret their request.
8. See the suggested Crew Chief command.
9. Edit and customise command mappings.

The software should be simple enough for any Crew Chief user to install and configure.

It should be extensible enough to support additional AI providers and a future Crew Chief integration module.

**Product principle: AVACC gives Crew Chief the ability to understand natural human speech, without replacing Crew Chief itself.**

Begin with Milestone 1.

Create the solution, implement the three-tab application shell, and establish the architecture before moving into audio capture and AI integration.

At the end of every milestone:

- Compile the application.
- Run relevant tests.
- Report completed features.
- Report known limitations.
- Identify the next implementation milestone.

Build a real, working application rather than a UI prototype.