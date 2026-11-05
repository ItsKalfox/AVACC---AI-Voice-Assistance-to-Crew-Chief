using System.ComponentModel;

namespace AVACC.Core.Models;

public class CommandDefinition : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private string _name = string.Empty;
    private string _crewChiefCommandId = string.Empty;
    private string _description = string.Empty;
    private string _category = "General";
    private string? _voiceLine;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string CrewChiefCommandId
    {
        get => _crewChiefCommandId;
        set
        {
            if (_crewChiefCommandId != value)
            {
                _crewChiefCommandId = value;
                OnPropertyChanged(nameof(CrewChiefCommandId));
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
    }

    /// <summary>
    /// The exact verbal phrase spoken by the offline TTS bridge to Crew Chief.
    /// If null or empty, falls back to the default canonical Crew Chief trigger phrase.
    /// </summary>
    public string? VoiceLine
    {
        get => _voiceLine;
        set
        {
            if (_voiceLine != value)
            {
                _voiceLine = value;
                OnPropertyChanged(nameof(VoiceLine));
            }
        }
    }

    public List<string> Examples { get; set; } = new();

    public List<string> Aliases { get; set; } = new();

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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
