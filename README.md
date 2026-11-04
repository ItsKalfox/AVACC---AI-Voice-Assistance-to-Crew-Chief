# AVACC — AI Voice Assistance to Crew Chief

> **Important Scope Notice:** AVACC Version 0.1 is strictly an AI speech recognition and natural-language command interpretation utility. **Version 0.1 does NOT integrate with Crew Chief, send commands to Crew Chief, modify Crew Chief code, or read game telemetry.** The application generates, displays, configures, and manages suggested Crew Chief commands.

---

## 1. Overview

**AVACC** (AI Voice Assistance to Crew Chief) is a lightweight Windows desktop utility designed specifically for sim racers using Crew Chief V4, particularly in Assetto Corsa Competizione (ACC).

Its mission is to replace legacy Windows Speech Recognition with state-of-the-art AI-powered speech-to-text (STT) and natural language understanding (NLU). With AVACC, sim racers can speak naturally during intense race sessions—without memorizing rigid keyword commands.

### How It Works:
```text
[ Push-to-Talk (Right Ctrl) ]
              ↓
[ WASAPI 16kHz Audio Capture ]
              ↓
[ AI Provider (Multimodal / STT + LLM) ]
              ↓
[ Strict Command Registry Matching ]
              ↓
[ Visual CMD Dashboard & Suggested Command Output ]
```

---

## 2. Key Features

- **Modern Three-Tab Sim-Racing UI:**
  - **Tab 1 — Visual CMD:** Real-time PTT state badge, audio peak meter, speech transcription card, suggested Crew Chief command badge with self-reported confidence, chronological event logs, and processing history.
  - **Tab 2 — Command Editor:** Full CRUD command registry with verified Crew Chief command identifiers, categories, descriptions, natural language examples, and an offline/online manual command tester.
  - **Tab 3 — Settings:** Bring Your Own Key (BYOK) AI provider setup, architecture mode selection, model picker, global Win32 PTT key binding, and WASAPI microphone selection.
- **Dual AI Architecture Modes:**
  - **Simple Mode (Default):** Single audio-capable multimodal model (e.g., Gemini 2.5 Flash) directly processes audio bytes into structured JSON transcription and command mapping in a single round-trip.
  - **Advanced Mode:** Decoupled two-model pipeline allowing independent STT (e.g., OpenAI Whisper, Groq Whisper) and LLM reasoning (e.g., GPT-4o-mini, Llama 3.3).
- **Enterprise-Grade Local Security:**
  - API keys are encrypted on disk using **Windows Data Protection API (DPAPI)** tied to the current Windows user account.
  - No credentials are ever saved in plaintext configuration files.
  - No persistent raw audio recordings by default.
- **Strict Hallucination Prevention:**
  - AI models are strictly constrained via structured JSON schemas and few-shot registry prompts to only return command identifiers that actually exist and are enabled in your command registry.

---

## 3. Supported AI Providers (BYOK)

| Provider | Simple Multimodal Mode | Advanced STT Mode | Advanced LLM Mode |
|---|---|---|---|
| **Google Gemini** | `gemini-2.5-flash`, `gemini-2.5-pro` | Direct audio transcription | `gemini-2.5-flash` |
| **OpenAI** | — | `whisper-1` | `gpt-4o-mini`, `gpt-4o` |
| **Groq** | — | `whisper-large-v3`, `turbo` | `llama-3.3-70b-versatile` |

---

## 4. Getting Started & Development Setup

### Prerequisites
- Windows 10/11 x64
- [.NET 8 LTS or .NET 10 SDK](https://dotnet.microsoft.com/download)
- Working microphone (WASAPI compatible)
- API key from [Google AI Studio](https://aistudio.google.com/), [OpenAI Platform](https://platform.openai.com/), or [Groq Console](https://console.groq.com/)

### Building and Running
```powershell
# Clone repository
git clone https://github.com/your-repo/AVACC.git
cd AVACC

# Build solution
dotnet build AVACC.sln

# Run unit tests
dotnet test AVACC.sln

# Launch application
dotnet run --project src/AVACC.App/AVACC.App.csproj
```

---

## 5. Configuration & Usage Guide

### Push-to-Talk (PTT)
- **Default Key:** `Right Ctrl`
- The Win32 Low-Level Keyboard Hook (`WH_KEYBOARD_LL`) detects PTT presses and releases globally—even when Assetto Corsa Competizione is running in full screen.
- Configurable maximum recording duration (default 10s) prevents runaway recordings.

### Command Registry
AVACC comes preloaded with verified Crew Chief command identifiers categorized for sim-racing:
- `HOW_MUCH_FUEL_TO_END_OF_RACE` ("How much fuel do we need?", "Can we make it to the finish?")
- `HOWS_MY_FUEL` ("How is my fuel?")
- `WHAT_ARE_MY_TYRE_PRESSURES` ("What are my tyre pressures?")
- `WHAT_IS_MY_GAP_BEHIND` ("What is my gap behind?", "Who is behind me?")
- `WHAT_IS_MY_GAP_AHEAD` ("What is my gap ahead?")
- `FULL_UPDATE` ("Give me a quick race update")
- `BOX_THIS_LAP` ("Box this lap")
- `DAMAGE_REPORT` ("Do we have any damage?")

You can add custom commands, edit descriptions, and add new training examples via the **Command Editor** tab.

---

## 6. Privacy & Security

- **Microphone Capture:** Audio is only captured while holding the configured PTT button. There is no passive background listening.
- **Credentials:** API keys are encrypted at rest using Windows DPAPI (`DataProtectionScope.CurrentUser`).
- **Audio Retention:** Raw WAV audio is processed in-memory and discarded upon completion.
- **Crew Chief Isolation:** Version 0.1 makes no IPC or socket connections to Crew Chief or racing games.

---

## 7. Known Limitations (v0.1)

1. **No Direct Crew Chief Dispatch:** Commands are generated and displayed on screen only. Sending commands to Crew Chief via simulated keypresses, speech synthesis, or IPC will be implemented in future milestones.
2. **Confidence Scores:** Speech-to-text models like Whisper do not provide a single calibrated percentage; confidence is reported as "Confidence unavailable" rather than an invented number. LLM interpretation confidence is the model's self-reported estimate.

---

## 8. License
MIT License.

