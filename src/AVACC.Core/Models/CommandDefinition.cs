namespace AVACC.Core.Models;

public class CommandDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string CrewChiefCommandId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public bool IsEnabled { get; set; } = true;

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
            Examples = new List<string>(Examples),
            Aliases = new List<string>(Aliases)
        };
    }
}

