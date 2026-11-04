using System.Collections.ObjectModel;
using AVACC.Core.Models;

namespace AVACC.Core.Interfaces;

public interface IEventLogService
{
    ObservableCollection<LogEntry> Entries { get; }
    void LogInfo(string message, string? details = null);
    void LogWarning(string message, string? details = null);
    void LogError(string message, string? details = null);
    void LogDebug(string message, string? details = null);
    void Clear();
    Task<string> ExportLogsAsync();
}

