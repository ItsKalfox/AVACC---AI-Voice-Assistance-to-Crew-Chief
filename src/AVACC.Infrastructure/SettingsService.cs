using System.IO;
using System.Text.Json;
using AVACC.Core.Configuration;
using AVACC.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AVACC.Infrastructure;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly ILogger<SettingsService>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _currentSettings;

    public AppSettings CurrentSettings => _currentSettings;

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService(string? customFilePath = null, ILogger<SettingsService>? logger = null)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        var directory = customFilePath != null
            ? Path.GetDirectoryName(customFilePath) ?? AppDomain.CurrentDomain.BaseDirectory
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AVACC");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _settingsFilePath = customFilePath ?? Path.Combine(directory, "settings.json");
        _currentSettings = new AppSettings();
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (loaded != null)
                {
                    // Migrate deprecated / non-existent default model names
                    if (loaded.SimpleModel == "gemini-2.5-flash") loaded.SimpleModel = "gemini-1.5-flash";
                    if (loaded.SttModel == "gemini-2.5-flash") loaded.SttModel = "gemini-1.5-flash";
                    if (loaded.LlmModel == "gemini-2.5-flash") loaded.LlmModel = "gemini-1.5-flash";

                    _currentSettings = loaded;
                    _logger?.LogInformation("Loaded application settings from {FilePath}", _settingsFilePath);
                    SettingsChanged?.Invoke(this, _currentSettings);
                    return _currentSettings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load settings file from {FilePath}, using defaults", _settingsFilePath);
        }

        _currentSettings = new AppSettings();
        await SaveSettingsAsync(_currentSettings);
        return _currentSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            _currentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            var json = JsonSerializer.Serialize(_currentSettings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger?.LogInformation("Saved application settings to {FilePath}", _settingsFilePath);
            SettingsChanged?.Invoke(this, _currentSettings);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings to {FilePath}", _settingsFilePath);
            throw;
        }
    }
}
