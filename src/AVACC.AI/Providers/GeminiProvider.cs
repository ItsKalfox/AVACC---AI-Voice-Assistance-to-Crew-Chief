using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using Microsoft.Extensions.Logging;

namespace AVACC.AI.Providers;

public class GeminiProvider : IMultimodalVoiceProvider, ISpeechToTextProvider, ICommandInterpreter
{
    private readonly ISecureKeyStorage _keyStorage;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiProvider>? _logger;
    private readonly ISettingsService? _settingsService;

    public string ProviderName => "Google Gemini";

    public static string CleanModelName(string? model)
    {
        if (string.IsNullOrWhiteSpace(model)) return "gemini-1.5-flash";
        var trimmed = model.Trim();
        if (trimmed.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(7);
        }
        if (trimmed.Equals("gemini-2.5-flash", StringComparison.OrdinalIgnoreCase))
        {
            return "gemini-1.5-flash";
        }
        if (trimmed.Equals("gemini-2.5-pro", StringComparison.OrdinalIgnoreCase))
        {
            return "gemini-1.5-pro";
        }
        return trimmed;
    }

    public GeminiProvider(ISecureKeyStorage keyStorage, HttpClient? httpClient = null, ILogger<GeminiProvider>? logger = null, ISettingsService? settingsService = null)
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
        var apiKey = await _keyStorage.GetApiKeyAsync(ProviderName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new VoiceCommandResult
            {
                Error = "Google Gemini API key is missing. Please configure it in Settings.",
                Provider = ProviderName,
                ModelName = modelName,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        try
        {
            var base64Audio = Convert.ToBase64String(audioWav);
            var optimizePayload = _settingsService?.CurrentSettings?.OptimizePromptPayload ?? true;
            var systemInstruction = PromptBuilder.BuildSystemPrompt(availableCommands, optimizePayload);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new object[]
                    {
                        new { text = systemInstruction }
                    }
                },
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "audio/wav",
                                    data = base64Audio
                                }
                            },
                            new
                            {
                                text = "Transcribe the driver's audio and identify the corresponding Crew Chief command JSON."
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1
                }
            };

            var cleanModel = CleanModelName(modelName);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent?key={apiKey}";
            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogError("Gemini multimodal request failed: {StatusCode} {Error}", response.StatusCode, errorBody);
                return new VoiceCommandResult
                {
                    Error = $"Gemini error ({cleanModel}): {(int)response.StatusCode} {response.ReasonPhrase}. Details: {errorBody}",
                    Provider = ProviderName,
                    ModelName = cleanModel,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            var textCandidate = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            var parsed = PromptBuilder.ParseStructuredResponse(textCandidate ?? "{}", "(Audio Spoken by Driver)", availableCommands);
            parsed.Provider = ProviderName;
            parsed.ModelName = modelName;
            parsed.DurationMs = sw.ElapsedMilliseconds;

            return parsed;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogError(ex, "Gemini multimodal processing error");
            return new VoiceCommandResult
            {
                Error = $"Gemini error: {ex.Message}",
                Provider = ProviderName,
                ModelName = modelName,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
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
                Error = "Gemini API key is not configured.",
                Provider = ProviderName,
                Model = modelName
            };
        }

        try
        {
            var base64Audio = Convert.ToBase64String(audioWav);
            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "audio/wav",
                                    data = base64Audio
                                }
                            },
                            new
                            {
                                text = "Accurately transcribe the spoken voice in this audio into plain text. Return only the transcription."
                            }
                        }
                    }
                }
            };

            var cleanModel = CleanModelName(modelName);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent?key={apiKey}";
            using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new TranscriptionResult
                {
                    Error = $"Gemini STT error ({cleanModel}): {response.StatusCode}. Details: {errorBody}",
                    Provider = ProviderName,
                    Model = cleanModel,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            var resText = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(resText);
            var transcript = doc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()?.Trim() ?? "";

            return new TranscriptionResult
            {
                Transcript = transcript,
                Confidence = null, // Gemini does not return calibrated probabilities on this endpoint
                Provider = ProviderName,
                Model = modelName,
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
                Error = "Gemini API key is not configured.",
                Provider = ProviderName,
                ModelName = modelName,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        try
        {
            var optimizePayload = _settingsService?.CurrentSettings?.OptimizePromptPayload ?? true;
            var systemInstruction = PromptBuilder.BuildSystemPrompt(availableCommands, optimizePayload);
            var userPrompt = PromptBuilder.BuildUserPrompt(transcript);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new object[]
                    {
                        new { text = systemInstruction }
                    }
                },
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = userPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1
                }
            };

            var cleanModel = CleanModelName(modelName);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent?key={apiKey}";
            using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogError("Gemini LLM interpretation failed: {StatusCode} {Error}", response.StatusCode, errorBody);
                return new VoiceCommandResult
                {
                    Transcript = transcript,
                    Error = $"Gemini LLM error ({cleanModel}): {(int)response.StatusCode} {response.ReasonPhrase}. Details: {errorBody}",
                    Provider = ProviderName,
                    ModelName = cleanModel,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            var resText = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(resText);
            var text = doc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            var parsed = PromptBuilder.ParseStructuredResponse(text ?? "{}", transcript, availableCommands);
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

