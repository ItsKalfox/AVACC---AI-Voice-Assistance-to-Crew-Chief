namespace AVACC.Core.Models;

public class AudioDeviceInfo
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public int Channels { get; set; } = 1;

    public int SampleRate { get; set; } = 16000;

    public override string ToString() => IsDefault ? $"{Name} (Default)" : Name;
}

