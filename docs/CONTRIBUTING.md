# Contributing to AVACC

Thank you for your interest in contributing to AVACC!

---

## Code of Conduct & Scope Discipline

AVACC follows strict milestone-based development.

### Crucial Architectural Rules
1. **No Direct Crew Chief Hooks in v0.1:** Do not submit PRs implementing Crew Chief IPC, memory reading, or telemetry integration. v0.1 is solely focused on AI voice-to-command interpretation.
2. **Never Log Sensitive Information:** API keys, authorization headers, and personal credentials must never be written to logs, console, or Git commits.
3. **No Fabricated Confidence Values:** If an STT provider does not supply a calibrated probability, always report `null` ("Confidence unavailable").
4. **Strict Command Validation:** AI models must never be permitted to generate arbitrary command identifiers. All suggested commands must exist in the active user registry.

---

## Development Workflow

1. Fork the repository and create your feature branch (`git checkout -b feature/amazing-feature`).
2. Build the solution:
   ```powershell
   dotnet build AVACC.sln
   ```
3. Run the automated test suite:
   ```powershell
   dotnet test AVACC.sln
   ```
4. Verify that code formatting adheres to C# conventions with nullable reference types enabled.
5. Commit your changes with concise, descriptive commit messages.
6. Open a Pull Request detailing your changes and test verification results.

