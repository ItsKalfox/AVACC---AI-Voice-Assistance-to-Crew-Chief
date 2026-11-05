namespace AVACC.Core.Models;

public class CommandDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string CrewChiefCommandId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The exact verbal phrase spoken by the offline TTS bridge to Crew Chief.
    /// If null or empty, falls back to the default canonical Crew Chief trigger phrase.
    /// </summary>
    public string? VoiceLine { get; set; }

    public List<string> Examples { get; set; } = new();

    public List<string> Aliases { get; set; } = new();

    public CommandDefinition Clone()
    {
        return new CommandDefinition
        {
            Id = Id,
            Name = Name,
            CrewChiefCommandId = CrewChiefCommandId,
            Description = Description,
            Category = Category,
            IsEnabled = IsEnabled,
            VoiceLine = VoiceLine,
            Examples = new List<string>(Examples),
            Aliases = new List<string>(Aliases)
        };
    }
}

