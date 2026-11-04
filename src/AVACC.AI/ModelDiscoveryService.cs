using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using Microsoft.Extensions.Logging;

namespace AVACC.AI;

public class ModelDiscoveryService : IModelDiscoveryService
{
    private readonly ISecureKeyStorage _secureKeyStorage;
    private readonly ILogger<ModelDiscoveryService>? _logger;
    private readonly HttpClient _httpClient;

    public ModelDiscoveryService(ISecureKeyStorage secureKeyStorage, HttpClient? httpClient = null, ILogger<ModelDiscoveryService>? logger = null)
    {
        _secureKeyStorage = secureKeyStorage;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public async Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var models = new List<ModelInfo>();
        var apiKey = await _secureKeyStorage.GetApiKeyAsync(providerName);

        switch (providerName.ToLowerInvariant())
        {
            case "google gemini":
            case "gemini":
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    try
                    {
                        var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
                        using var res = await _httpClient.GetAsync(geminiUrl, cancellationToken);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("models", out var modelsEl) && modelsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var m in modelsEl.EnumerateArray())
                                {
                                    var rawName = m.GetProperty("name").GetString() ?? "";
                                    var id = rawName.StartsWith("models/") ? rawName.Substring(7) : rawName;

                                    // Check supported methods
                                    var supportsGenerate = false;
                                    if (m.TryGetProperty("supportedGenerationMethods", out var methodsEl) && methodsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                                    {
                                        foreach (var method in methodsEl.EnumerateArray())
                                        {
                                            if (string.Equals(method.GetString(), "generateContent", StringComparison.OrdinalIgnoreCase))
                                            {
                                                supportsGenerate = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!supportsGenerate) continue;

                                    var displayName = m.TryGetProperty("displayName", out var dnEl) ? dnEl.GetString() : id;
                                    var description = m.TryGetProperty("description", out var descEl) ? descEl.GetString() : "";

                                    var isAudio = id.Contains("flash", StringComparison.OrdinalIgnoreCase) ||
                                                  id.Contains("pro", StringComparison.OrdinalIgnoreCase);

                                    var info = new ModelInfo
                                    {
                                        Id = id,
                                        DisplayName = $"{displayName} ({id})",
                                        Provider = "Google Gemini",
                                        SupportsAudioInput = isAudio,
                                        SupportsStructuredOutput = true,
                                        Description = description ?? ""
                                    };

                                    // Put flash models (especially 1.5-flash and 2.0-flash) at top of list
                                    if (id.StartsWith("gemini-1.5-flash", StringComparison.OrdinalIgnoreCase) ||
                                        id.StartsWith("gemini-2.0-flash", StringComparison.OrdinalIgnoreCase))
                                    {
                                        models.Insert(0, info);
                                    }
                                    else
                                    {
                                        models.Add(info);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to dynamically discover Gemini models online; using standard models.");
                    }
                }

                // If dynamic discovery returned nothing (e.g. no key yet or offline), provide standard verified Gemini models
                if (models.Count == 0)
                {
                    models.Add(new ModelInfo
                    {
                        Id = "gemini-1.5-flash",
                        DisplayName = "Gemini 1.5 Flash (Recommended - Multimodal Audio)",
                        Provider = "Google Gemini",
                        SupportsAudioInput = true,
                        SupportsStructuredOutput = true,
                        Description = "Fast, efficient multimodal model supporting direct native audio ingestion and structured JSON."
                    });
                    models.Add(new ModelInfo
                    {
                        Id = "gemini-2.0-flash",
                        DisplayName = "Gemini 2.0 Flash (Next-Gen Multimodal Audio)",
                        Provider = "Google Gemini",
                        SupportsAudioInput = true,
                        SupportsStructuredOutput = true,
                        Description = "Next-generation ultra fast multimodal model."
                    });
                    models.Add(new ModelInfo
                    {
                        Id = "gemini-1.5-pro",
                        DisplayName = "Gemini 1.5 Pro (Multimodal Audio & High Reasoning)",
                        Provider = "Google Gemini",
                        SupportsAudioInput = true,
                        SupportsStructuredOutput = true,
                        Description = "High-reasoning multimodal model."
                    });
                }
                break;

            case "openai":
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    try
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                        var res = await _httpClient.SendAsync(req, cancellationToken);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var m in dataEl.EnumerateArray())
                                {
                                    var id = m.GetProperty("id").GetString() ?? "";
                                    if (id.StartsWith("gpt-4o", StringComparison.OrdinalIgnoreCase) ||
                                        id.StartsWith("gpt-4-turbo", StringComparison.OrdinalIgnoreCase) ||
                                        id.StartsWith("whisper", StringComparison.OrdinalIgnoreCase) ||
                                        id.StartsWith("gpt-3.5", StringComparison.OrdinalIgnoreCase))
                                    {
                                        models.Add(new ModelInfo
                                        {
                                            Id = id,
                                            DisplayName = id.StartsWith("whisper") ? $"{id} (Audio STT)" : $"{id} (LLM Interpreter)",
                                            Provider = "OpenAI",
                                            SupportsAudioInput = id.StartsWith("whisper"),
                                            SupportsStructuredOutput = !id.StartsWith("whisper")
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to dynamically discover OpenAI models.");
                    }
                }

                if (models.Count == 0)
                {
                    models.Add(new ModelInfo
                    {
                        Id = "gpt-4o-mini",
                        DisplayName = "GPT-4o Mini (LLM Command Interpreter)",
                        Provider = "OpenAI",
                        SupportsAudioInput = false,
                        SupportsStructuredOutput = true,
                        Description = "Cost-effective, low-latency reasoning model for command mapping."
                    });
                    models.Add(new ModelInfo
                    {
                        Id = "gpt-4o",
                        DisplayName = "GPT-4o (LLM Command Interpreter)",
                        Provider = "OpenAI",
                        SupportsAudioInput = false,
                        SupportsStructuredOutput = true,
                        Description = "Flagship reasoning model."
                    });
                    models.Add(new ModelInfo
                    {
                        Id = "whisper-1",
                        DisplayName = "Whisper-1 (STT Speech to Text)",
                        Provider = "OpenAI",
                        SupportsAudioInput = true,
                        SupportsStructuredOutput = false,
                        Description = "Specialized speech transcription model."
                    });
                }
                break;

            case "groq":
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    try
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models");
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                        var res = await _httpClient.SendAsync(req, cancellationToken);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var m in dataEl.EnumerateArray())
                                {
                                    var id = m.GetProperty("id").GetString() ?? "";
                                    models.Add(new ModelInfo
                                    {
                                        Id = id,
                                        DisplayName = id.Contains("whisper") ? $"{id} (Groq Audio STT)" : $"{id} (Groq LLM)",
                                        Provider = "Groq",
                                        SupportsAudioInput = id.Contains("whisper"),
                                        SupportsStructuredOutput = !id.Contains("whisper")
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to dynamically discover Groq models.");
                    }
                }

                if (models.Count == 0)
                {
                    models.Add(new ModelInfo
                    {
                        Id = "whisper-large-v3",
                        DisplayName = "Whisper Large v3 (Ultra-Fast Groq STT)",
                        Provider = "Groq",
                        SupportsAudioInput = true,
                        SupportsStructuredOutput = false,
                        Description = "Sub-second cloud transcription running on Groq LPUs."
                    });
                    models.Add(new ModelInfo
                    {
                        Id = "whisper-large-v3-turbo",
                        DisplayName = "Whisper Large v3 Turbo (Groq STT)",
                        Provider = "Groq",
                        SupportsAudioInput = true,
                        SupportsStructuredOutput = false,
                        Description = "Optimized fast speech transcription on Groq LPUs."
                    });
                    models.Add(new ModelInfo
                    {
                        Id = "llama-3.3-70b-versatile",
                        DisplayName = "Llama 3.3 70B Versatile (Groq LLM)",
                        Provider = "Groq",
                        SupportsAudioInput = false,
                        SupportsStructuredOutput = true,
                        Description = "Ultra fast inference for natural language command parsing."
                    });
                    models.Add(new ModelInfo
                    {
                        Id = "llama-3.1-8b-instant",
                        DisplayName = "Llama 3.1 8B Instant (Groq LLM)",
                        Provider = "Groq",
                        SupportsAudioInput = false,
                        SupportsStructuredOutput = true,
                        Description = "Lightweight high-speed command mapper."
                    });
                }
                break;
        }

        return models;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(string providerName, string? modelName = null, CancellationToken cancellationToken = default)
    {
        var apiKey = await _secureKeyStorage.GetApiKeyAsync(providerName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "No API key configured. Please enter and save an API key first.");
        }

        try
        {
            switch (providerName.ToLowerInvariant())
            {
                case "google gemini":
                case "gemini":
                    var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
                    using (var req = new HttpRequestMessage(HttpMethod.Get, geminiUrl))
                    {
                        var res = await _httpClient.SendAsync(req, cancellationToken);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            var count = doc.RootElement.TryGetProperty("models", out var mEl) ? mEl.GetArrayLength() : 0;
                            return (true, $"Successfully connected to Google Gemini API ({count} models available).");
                        }
                        var err = await res.Content.ReadAsStringAsync(cancellationToken);
                        return (false, $"Gemini API returned error: {(int)res.StatusCode} {res.ReasonPhrase}. Details: {err}");
                    }

                case "openai":
                    using (var req = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models"))
                    {
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                        var res = await _httpClient.SendAsync(req, cancellationToken);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            var count = doc.RootElement.TryGetProperty("data", out var dEl) ? dEl.GetArrayLength() : 0;
                            return (true, $"Successfully connected to OpenAI API ({count} models available).");
                        }
                        var err = await res.Content.ReadAsStringAsync(cancellationToken);
                        return (false, $"OpenAI API returned error: {(int)res.StatusCode} {res.ReasonPhrase}. Details: {err}");
                    }

                case "groq":
                    using (var req = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models"))
                    {
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                        var res = await _httpClient.SendAsync(req, cancellationToken);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            var count = doc.RootElement.TryGetProperty("data", out var dEl) ? dEl.GetArrayLength() : 0;
                            return (true, $"Successfully connected to Groq API ({count} models available).");
                        }
                        var err = await res.Content.ReadAsStringAsync(cancellationToken);
                        return (false, $"Groq API returned error: {(int)res.StatusCode} {res.ReasonPhrase}. Details: {err}");
                    }

                default:
                    return (false, $"Unknown provider '{providerName}'.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "API Connection test failed for provider {Provider}", providerName);
            return (false, $"Connection test failed: {ex.Message}");
        }
    }
}

