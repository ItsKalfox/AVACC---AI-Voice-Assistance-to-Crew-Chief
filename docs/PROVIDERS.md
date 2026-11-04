# AVACC AI Providers Guide

AVACC features a Bring Your Own Key (BYOK) provider architecture. This document outlines capabilities, configuration, and recommended models for each provider.

---

## 1. Google Gemini

### Capabilities
- **Direct Multimodal Audio:** Supported via `gemini-2.5-flash` and `gemini-2.5-pro`.
- **Structured JSON Output:** Native support via `generationConfig.response_mime_type: "application/json"`.
- **Recommended Default:** `gemini-2.5-flash` offers ultra-low latency and low token costs, making it ideal as a single multimodal model for sim racing.

### Credentials
- Get an API key from [Google AI Studio](https://aistudio.google.com/).
- Enter the key in AVACC **Settings** > **Google Gemini** > **Save Key**.
- Verified endpoint: `https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}`.

---

## 2. OpenAI

### Capabilities
- **Speech-to-Text (STT):** `whisper-1` via `/v1/audio/transcriptions`.
- **Command Interpretation (LLM):** `gpt-4o-mini`, `gpt-4o` via `/v1/chat/completions` with JSON Object mode.
- **Notes on Confidence:** OpenAI Whisper does not provide a calibrated overall transcription probability percentage. AVACC will display `"Confidence unavailable"` for STT confidence rather than fabricating a percentage.

### Credentials
- Obtain an API key from [OpenAI Platform](https://platform.openai.com/api-keys).
- Enter key in AVACC **Settings** > **OpenAI** > **Save Key**.
- Verified endpoint: `https://api.openai.com/v1/models`.

---

## 3. Groq

### Capabilities
- **Ultra-Fast Cloud Speech-to-Text:** `whisper-large-v3`, `whisper-large-v3-turbo` running on Groq LPU inference engines with typical transcription latencies under 400ms.
- **Fast Reasoning LLM:** `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`.

### Credentials
- Obtain an API key from [Groq Console](https://console.groq.com/keys).
- Enter key in AVACC **Settings** > **Groq** > **Save Key**.
- Verified endpoint: `https://api.groq.com/openai/v1/models`.

