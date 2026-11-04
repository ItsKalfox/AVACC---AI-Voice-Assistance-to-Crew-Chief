using AVACC.Core.Configuration;

namespace AVACC.Core.Interfaces;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    event EventHandler<AppSettings>? SettingsChanged;
}

