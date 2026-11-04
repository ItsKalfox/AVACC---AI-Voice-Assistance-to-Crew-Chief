using AVACC.Core.Commands;

namespace AVACC.App.ViewModels;

public class StagedCommandModel : ObservableObject
{
    private bool _isSelected = true;
    private string _name = string.Empty;
    private string _crewChiefCommandId = string.Empty;
    private string _category = "General";
    private string _description = string.Empty;
    private string _examplesRaw = string.Empty;
    private string _aliasesRaw = string.Empty;
    private bool _isEnabled = true;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string CrewChiefCommandId
    {
        get => _crewChiefCommandId;
        set => SetProperty(ref _crewChiefCommandId, value);
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string ExamplesRaw
    {
        get => _examplesRaw;
        set => SetProperty(ref _examplesRaw, value);
    }

    public string AliasesRaw
    {
        get => _aliasesRaw;
        set => SetProperty(ref _aliasesRaw, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}
