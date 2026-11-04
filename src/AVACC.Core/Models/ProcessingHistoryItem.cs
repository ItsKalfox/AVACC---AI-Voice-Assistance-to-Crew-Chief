namespace AVACC.Core.Models;

public class ProcessingHistoryItem
{
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string Transcript { get; set; } = string.Empty;

    public string? SuggestedCommand { get; set; }

    public string Provider { get; set; } = string.Empty;

    public long DurationMs { get; set; }

    public string Status { get; set; } = string.Empty;

    public double? Confidence { get; set; }

    public string? Intent { get; set; }

    public string? Reason { get; set; }

    public string? RawJsonResponse { get; set; }

    public string FormattedDuration => $"{DurationMs} ms";

    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");
}

