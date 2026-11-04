using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using Microsoft.Extensions.Logging;

namespace AVACC.AI.Providers;

public class GroqProvider : IMultimodalVoiceProvider, ISpeechToTextProvider, ICommandInterpreter
{
    private readonly ISecureKeyStorage _keyStorage;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqProvider>? _logger;
    private readonly ISettingsService? _settingsService;

    public string ProviderName => "Groq";

    public GroqProvider(ISecureKeyStorage keyStorage, HttpClient? httpClient = null, ILogger<GroqProvider>? logger = null, ISettingsService? settingsService = null)
    {
        _keyStorage = keyStorage;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<VoiceCommandResult> ProcessAudioAsync(
        byte[] audioWav,
        string modelName,
        IReadOnlyList<CommandDefinition> availableCommands,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. Groq Audio STT with Whisper
        var sttModel = "whisper-large-v3-turbo";
        if (!string.IsNullOrEmpty(modelName) && modelName.Contains("whisper", StringComparison.OrdinalIgnoreCase))
        {
            sttModel = modelName;
        }

        var sttResult = await TranscribeAsync(audioWav, sttModel, cancellationToken);
        if (!sttResult.IsSuccess)
        {
            sw.Stop();
            return new VoiceCommandResult
            {
                Error = sttResult.Error ?? "Groq Whisper transcription failed.",
                Provider = ProviderName,
                ModelName = sttModel,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        // 2. Groq LLM interpretation with LLaMA
        var llmModel = "llama-3.3-70b-versatile";
        if (!string.IsNullOrEmpty(modelName) && !modelName.Contains("whisper", StringComparison.OrdinalIgnoreCase))
        {
            llmModel = modelName;
        }

        var result = await InterpretAsync(sttResult.Transcript, llmModel, availableCommands, cancellationToken);
        result.DurationMs = sw.ElapsedMilliseconds;
        return result;
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        byte[] audioWav,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var apiKey = await _keyStorage.GetApiKeyAsync(ProviderName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranscriptionResult
            {
                Error = "Groq API key is missing. Please configure it in Settings.",
                Provider = ProviderName,
                Model = modelName
            };
        }

        try
        {
            var audioModel = string.IsNullOrEmpty(modelName) || !modelName.Contains("whisper", StringComparison.OrdinalIgnoreCase)
                ? "whisper-large-v3-turbo"
                : modelName;

            using var form = new MultipartFormDataContent();
            var audioContent = new ByteArrayContent(audioWav);
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            form.Add(audioContent, "file", "audio.wav");
            form.Add(new StringContent(audioModel), "model");
            form.Add(new StringContent("json"), "response_format");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/audio/transcriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = form;

            var response = await _httpClient.SendAsync(request, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                return new TranscriptionResult
                {
                    Error = $"Groq error ({audioModel}): {(int)response.StatusCode} {err}",
                    Provider = ProviderName,
                    Model = audioModel,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var transcript = doc.RootElement.GetProperty("text").GetString()?.Trim() ?? "";

            return new TranscriptionResult
            {
                Transcript = transcript,
                Confidence = null,
                Provider = ProviderName,
                Model = audioModel,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TranscriptionResult
            {
                Error = ex.Message,
                Provider = ProviderName,
                Model = modelName,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }

    public async Task<VoiceCommandResult> InterpretAsync(
        string transcript,
        string modelName,
        IReadOnlyList<CommandDefinition> availableCommands,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var apiKey = await _keyStorage.GetApiKeyAsync(ProviderName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new VoiceCommandResult
            {
                Transcript = transcript,
                Error = "Groq API key is missing. Please configure it in Settings.",
                Provider = ProviderName,
                ModelName = modelName,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        try
        {
            var textModel = string.IsNullOrEmpty(modelName) || modelName.Contains("whisper", StringComparison.OrdinalIgnoreCase)
                ? "llama-3.3-70b-versatile"
                : modelName;

            var optimizePayload = _settingsService?.CurrentSettings?.OptimizePromptPayload ?? true;
            var systemPrompt = PromptBuilder.BuildSystemPrompt(availableCommands, optimizePayload);
            var userPrompt = PromptBuilder.BuildUserPrompt(transcript);

            var requestBody = new
            {
                model = textModel,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                return new VoiceCommandResult
                {
                    Transcript = transcript,
                    Error = $"Groq error: {(int)response.StatusCode} {err}",
                    Provider = ProviderName,
                    ModelName = modelName,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            var resText = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(resText);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var parsed = PromptBuilder.ParseStructuredResponse(content ?? "{}", transcript, availableCommands);
            parsed.Provider = ProviderName;
            parsed.ModelName = modelName;
            parsed.DurationMs = sw.ElapsedMilliseconds;

            return parsed;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new VoiceCommandResult
            {
                Transcript = transcript,
                Error = ex.Message,
                Provider = ProviderName,
                ModelName = modelName,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }
}

