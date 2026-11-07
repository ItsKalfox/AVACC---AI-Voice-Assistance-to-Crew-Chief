# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Crew Chief Process Remote Controller & Execution:**
  - Added launch capability to [`CrewChiefMonitorService`](file:///c:/Projects/AVACC---AI-Voice-Assistance-to-Crew-Chief/src/AVACC.Infrastructure/CrewChiefMonitorService.cs), starting `CrewChiefV4.exe` alongside AVACC.
  - Added `Start Crew Chief` / `Stop Crew Chief` remote toggle button directly in the main top bar, emulating Win32 `BM_CLICK` on Crew Chief's native start button.
  - Added real-time session button tracking to detect whether Crew Chief is actively monitoring telemetry or idle.
  - Added `Auto-launch Crew Chief alongside AVACC on startup` toggle in Settings.
  - Added configurable Crew Chief Executable Path with auto-detection and file browser in Settings.
- **Dynamic Fuel & Amount Parameter Extraction (`CrewChiefPhraseHelper`):**
  - Expanded parameter extraction in [`CrewChiefPhraseHelper`](file:///c:/Projects/AVACC---AI-Voice-Assistance-to-Crew-Chief/src/AVACC.Core/Helpers/CrewChiefPhraseHelper.cs) to recognize volume units (`litres`, `liters`, `l`, `gallons`), standalone amounts (`30`, `50`, `55`, `70`), and compound number words (`fifty-five`, `sixty-four`).
  - Added British English normalization converting any occurrence of `liters` / `liter` to `litres` / `litre`, strictly adhering to Crew Chief's speech recognition dictionary.
- **Comprehensive Crew Chief Commands Dataset (`CrewChief_All_Commands.csv`):**
  - Generated full dataset of 271 Crew Chief commands extracted directly from official `speech_recognition_config.txt` documentation.
  - Complete coverage across 14 categories: `Tyres`, `Brakes`, `Powertrain`, `Damage`, `Fuel`, `Timing`, `Gaps`, `Session`, `Pitstop`, `Comms`, `Display`, `Opponents`, `Rally`, and `General`.
  - Every command includes user-friendly display name, exact `CrewChiefCommandId`, description, canonical `VoiceLine`, natural language examples, aliases, and sensible default active state (`IsEnabled=True` for circuit racing, `IsEnabled=False` for rally/niche telemetry charts to preserve token limits).
  - Configured `RADIO_CHECK` with canonical voice line `"can you hear me"` matching Crew Chief's speech recognition grammar.
- **Dynamic Recognition Phrase Synthesis Engine (`CrewChiefPhraseHelper`):**
  - Added support for dynamic recognition phrase fragments described in Crew Chief documentation (e.g. `WHATS + POSITION_LONG + POSSESSIVE + LAST_LAP`, `CALCULATE_FUEL_FOR + {laps/minutes}`, `WHOS_IN + POSITION`, `WHERE_IS + POSITION`).
  - Added intelligent parameter extraction for race positions (e.g. "P4", "position 4", "third place"), fuel calculation quantities (e.g. "25 laps", "45 minutes"), driver names, and alarm timers.
  - Built-in phonetic number-to-word converter for 0 to 100 adhering to Crew Chief's voice recognizer grammar (e.g. `4` -> `"four"`, `25` -> `"twenty-five"`, `40` -> `"fourty"`).
  - Integrated dynamic phrase resolution into `PromptBuilder.ParseStructuredResponse`, `VisualCMDViewModel`, and `MainViewModel`, ensuring the exact parameterized verbal phrase (e.g. `"what's position four's last lap"`, `"calculate fuel for twenty-five laps"`) is displayed in the UI and spoken over the TTS audio bridge.
- **Idempotent CSV Import & Category Expansion:**
  - Updated `ConfirmImportAsync` in `CommandEditorViewModel` to update existing commands by `CrewChiefCommandId` rather than appending duplicate entries.
  - Expanded category filter dropdown in Command Editor to include all 14 categories (`Tyres`, `Brakes`, `Powertrain`, `Damage`, `Fuel`, `Timing`, `Gaps`, `Session`, `Pitstop`, `Comms`, `Display`, `Opponents`, `Rally`, and `General`).
- **Customizable Voice Line Field for Crew Chief Bridge:**
  - Added `VoiceLine` field to `CommandDefinition`, enabling users to inspect and override the exact verbal phrase sent via TTS into Crew Chief for every command.
  - Added "Voice Line (exact verbal phrase spoken to Crew Chief)" text input to the Command Editor panel and a dedicated column in the Command Registry table.
  - Added `VoiceLine` support to CSV Import & Staging table, as well as the CSV template generator.
  - Connected the Settings "Test Audio Bridge" button dynamically to `RADIO_CHECK`'s configured voice line (`"can you hear me"` by default).
- **Crew Chief Process Monitor & Auto-Launch (`ICrewChiefMonitorService`):**
  - Real-time polling service detecting `CrewChiefV4.exe` execution state (`Running`, `Active`, or `Offline`).
  - Added a status badge in the top navigation header with live indicator light and a one-click "Launch" button to start Crew Chief if it is offline.
- **Crew Chief Zero-Token Offline TTS Bridge (`ICrewChiefBridgeService`):**
  - Integrated local Windows Speech Synthesis (`System.Speech.Synthesis`) routed through NAudio `WasapiOut` directly into a selectable virtual playback device (e.g., VB-Audio Virtual Cable) listened to by Crew Chief.
  - Complete mapping of 180+ Crew Chief commands to their exact canonical voice trigger phrases (e.g., `HOWS_MY_AERO` -> `"how's my aero"`).
  - Optional simulated Win32 PTT key stroke (`keybd_event`) for Crew Chief before and after TTS audio playback.
  - "▶ Send to Crew Chief" action button in the Visual CMD suggested command card with live transmission status and canonical trigger phrase display.
  - Auto-forward toggle (`AutoSendToCrewChief`) to push recognized voice commands automatically upon detection.
- **Crew Chief Integration Settings Panel:**
  - Added dedicated configuration card in Settings tab: Bridge Enable/Disable, Auto-Send toggle, Virtual Audio Output Device picker, Crew Chief Simulated PTT Key selector, TTS Speech Rate slider, and an instant "Test Audio Bridge" playback button.
- **CSV Command Bulk Import with Interactive Staging Table:**
  - Added "Import CSV" button in the Command Editor toolbar to load batch commands from standard `.csv` files.
  - Implemented an interactive preview & staging modal overlay where users can edit rows, modify friendly names, command IDs, categories, descriptions, natural language examples, and aliases before confirming the bulk import.
  - Added "CSV Template" export feature generating `AVACC_Commands_Template.csv` pre-populated with standard schema and example sim racing commands.
- **Command Deletion Confirmation:**
  - Added a confirmation modal warning dialog before deleting commands from the registry to prevent accidental deletions.
- **Voice Command History Clear Action:**
  - Added a "Clear History" button with confirmation prompt to the Recent Voice Command History card in Visual CMD.
- **Structured JSON Response Viewer:**
  - Raw AI response JSON is captured, formatted with indentation, and displayed in an expanded-by-default viewer in Visual CMD.
- **Prompt Payload Optimization Toggle (Compact Lookup Table):**
  - Added `OptimizePromptPayload` setting and checkbox in the Settings tab under AI Configuration.
  - Generates a streamlined `- COMMAND_ID: Description` catalog that reduces prompt size from 15,000+ tokens to ~1,500 tokens (a ~90% reduction), eliminating Groq TPM rate limit errors (413).

### Changed
- **Auto-Scrolling Diagnostic Live Log:**
  - Connected collection change listener to automatically scroll the live diagnostic event log to the latest entry as messages arrive.

### Fixed
- **Command Selection Binding in Command Editor:**
  - Removed internal `!_isEditing` guard from `SelectedCommand` property setter that was permanently blocking `PopulateEditFields` from updating form fields when selecting rows in the command registry table.
  - Added explicit two-way binding (`Mode=TwoWay`) and `IsSynchronizedWithCurrentItem="True"` on the Command Registry `DataGrid`.
- **Dark Theme Styling for History Table:**
  - Added implicit dark theme styles for `GridViewColumnHeader` and `ListViewItem` in `App.xaml` to eliminate white header backgrounds and white-on-white text in the Recent Voice Command History table.
- **Delete Button State Reactivity:**
  - Fixed `DeleteCommand.RaiseCanExecuteChanged()` triggering when selection changes in the Command Registry.

---

## [0.0.1-beta] - 2026-11-04

### Added
- **Core Architecture & Modular Solution:**
  - `AVACC.Core`: Domain models, command definitions, configuration abstractions, and interfaces.
  - `AVACC.Infrastructure`: DPAPI-encrypted credential storage (never storing plaintext keys), JSON settings persistence, event logging service, and default ACC Crew Chief command registry.
  - `AVACC.Audio`: WASAPI low-level audio capture with automatic endpoint resolution (Multimedia, Communications, Console) and peak-level metering.
  - `AVACC.AI`: Comprehensive multi-provider AI subsystem supporting Google Gemini, Groq, and OpenAI with dynamic online model discovery.
  - `AVACC.App`: Modern dark-cockpit themed WPF desktop application built on MVVM architecture with Microsoft Dependency Injection.
- **Dynamic Provider Factory (`IAIProviderFactory`):**
  - Seamless runtime switching between Google Gemini, Groq, and OpenAI based on active user settings.
  - Groq dual-stage chaining: Whisper audio transcription + LLaMA natural-language command interpretation in sub-second response times.
  - Dynamic model discovery pulling available models directly from provider endpoints (`/v1beta/models`, `/v1/models`).
- **Interactive Monitoring & Diagnostic Tools:**
  - Real-time Push-to-Talk (PTT) monitoring with global low-level keyboard hook (`WH_KEYBOARD_LL`) supporting Right Ctrl, Left Ctrl, Space, Alt, and function keys.
  - Interactive "🎙 Hold to Speak (PTT Test Button)" directly in Visual CMD for quick mouse-driven microphone testing.
  - Live audio level meter reacting to 32-bit IEEE float & 16-bit PCM WASAPI audio streams.
  - Manual Command Intent Tester in Command Editor tab for fast, typed intent verification against configured models.
  - Live chronological diagnostic event log with export-to-desktop and clipboard copy support.
- **Unit Testing Suite:**
  - 27 unit tests covering Core commands, AI prompt parsing, anti-hallucination validation, Audio enumeration/hook lifecycle, and AI provider routing.

### Fixed
- Fixed Win32 low-level keyboard hook to properly detect Right Ctrl (`VK_CONTROL` with `LLKHF_EXTENDED` flag).
- Fixed 404 error when calling Google Gemini by updating default model to `gemini-1.5-flash` and adding `CleanModelName` normalization.
- Fixed Groq selection routing that previously routed Groq model names to Gemini endpoints.
- Fixed UI contrast issues with dropdown popups (`ComboBox`) and scrollbars (`ScrollBar`) in dark mode.
- Fixed tab navigation `InvalidCastException` and view visibility bindings.
- Fixed Manual Command Intent Tester button disabled state by adding reactive `CanExecuteChanged` notifications.

