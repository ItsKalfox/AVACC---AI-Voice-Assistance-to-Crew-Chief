using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;

namespace AVACC.Infrastructure;

public class EventLogService : IEventLogService
{
    private readonly object _lock = new();
    private readonly int _maxEntries;
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public EventLogService(int maxEntries = 1000)
    {
        _maxEntries = maxEntries;
    }

    public void LogInfo(string message, string? details = null) => AddEntry(LogLevel.Info, message, details);

    public void LogWarning(string message, string? details = null) => AddEntry(LogLevel.Warning, message, details);

    public void LogError(string message, string? details = null) => AddEntry(LogLevel.Error, message, details);

    public void LogDebug(string message, string? details = null) => AddEntry(LogLevel.Debug, message, details);

    private void AddEntry(LogLevel level, string message, string? details)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Details = details
        };

        // If running in WPF context, dispatch to UI thread
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.BeginInvoke(() => AddEntryInternal(entry));
        }
        else
        {
            AddEntryInternal(entry);
        }
    }

    private void AddEntryInternal(LogEntry entry)
    {
        lock (_lock)
        {
            Entries.Add(entry);
            while (Entries.Count > _maxEntries)
            {
                Entries.RemoveAt(0);
            }
        }
    }

    public void Clear()
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => Entries.Clear());
        }
        else
        {
            lock (_lock)
            {
                Entries.Clear();
            }
        }
    }

    public Task<string> ExportLogsAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# AVACC Diagnostic Event Log - Exported {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('-', 80));

        lock (_lock)
        {
            foreach (var entry in Entries)
            {
                sb.AppendLine($"[{entry.FormattedTimestamp}] [{entry.Level.ToString().ToUpperInvariant()}] {entry.Message}");
                if (!string.IsNullOrWhiteSpace(entry.Details))
                {
                    sb.AppendLine($"    Details: {entry.Details}");
                }
            }
        }

        return Task.FromResult(sb.ToString());
    }
}

