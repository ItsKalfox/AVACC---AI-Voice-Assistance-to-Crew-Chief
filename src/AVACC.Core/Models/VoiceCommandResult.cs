namespace AVACC.Core.Models;

public class VoiceCommandResult
{
    public string Transcript { get; set; } = string.Empty;

    public string Intent { get; set; } = string.Empty;

    public string? CommandId { get; set; }

    /// <summary>
    /// The exact synthesized verbal phrase to be forwarded to Crew Chief via TTS audio bridge.
    /// Supports dynamic parameters (e.g. 'what's position four's last lap', 'calculate fuel for twenty-five laps').
    /// </summary>
    public string? SpokenPhrase { get; set; }

    public double? Confidence { get; set; }

    public string? Reason { get; set; }

    public bool RequiresClarification { get; set; }

    public string? Error { get; set; }

    public string? Provider { get; set; }

    public string? ModelName { get; set; }

    public string? RawJsonResponse { get; set; }

    public long DurationMs { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool IsSuccess => string.IsNullOrEmpty(Error) && !string.IsNullOrEmpty(CommandId);
}

