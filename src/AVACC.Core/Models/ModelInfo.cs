namespace AVACC.Core.Models;

public class ModelInfo
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public bool SupportsAudioInput { get; set; }

    public bool SupportsStructuredOutput { get; set; }

    public bool SupportsTextOnly { get; set; }

    public string? Description { get; set; }

    public override string ToString() => DisplayName;
}

