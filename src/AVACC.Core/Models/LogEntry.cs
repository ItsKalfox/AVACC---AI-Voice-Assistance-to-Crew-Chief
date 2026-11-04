namespace AVACC.Core.Models;

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug
}

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public LogLevel Level { get; set; } = LogLevel.Info;

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");
}

