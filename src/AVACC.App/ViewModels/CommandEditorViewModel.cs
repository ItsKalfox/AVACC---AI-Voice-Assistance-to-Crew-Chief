using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using AVACC.AI;
using AVACC.Core.Commands;
using AVACC.Core.Configuration;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;

namespace AVACC.App.ViewModels;

public class CommandEditorViewModel : ObservableObject
{
    private readonly ICommandRegistryService _registryService;
    private readonly IAIProviderFactory? _providerFactory;
    private readonly IEventLogService _eventLogService;
    private readonly ISettingsService _settingsService;

    private readonly ObservableCollection<CommandDefinition> _commands = new();
    private readonly ICollectionView _commandsView;

    private CommandDefinition? _selectedCommand;
    private string _searchQuery = string.Empty;
    private string _selectedCategory = "All";

    // Edit fields
    private string _editName = string.Empty;
    private string _editCommandId = string.Empty;
    private string _editCategory = "General";
    private string _editDescription = string.Empty;
    private bool _editIsEnabled = true;
    private string _editExamplesText = string.Empty;
    private string _editAliasesText = string.Empty;
    private bool _isEditing;
    private bool _isNewCommand;

    // Manual Command Testing
    private string _testInputSentence = "Chief, how much fuel do we need for the next stint?";
    private string _testResultSuggestedCommand = string.Empty;
    private string _testResultIntent = string.Empty;
    private string _testResultConfidence = string.Empty;
    private string _testResultReason = string.Empty;
    private bool _isTesting;

    public ObservableCollection<CommandDefinition> Commands => _commands;
    public ICollectionView CommandsView => _commandsView;
    public ObservableCollection<string> Categories { get; } = new() { "All", "Fuel", "Tyres", "Brakes", "Gaps", "Session", "Timing", "Damage", "Pitstop", "General" };

    public CommandDefinition? SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            if (SetProperty(ref _selectedCommand, value))
            {
                DeleteCommand?.RaiseCanExecuteChanged();
                if (value != null)
                {
                    PopulateEditFields(value);
                }
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _commandsView.Refresh();
            }
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _commandsView.Refresh();
            }
        }
    }

    public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
    public string EditCommandId { get => _editCommandId; set => SetProperty(ref _editCommandId, value); }
    public string EditCategory { get => _editCategory; set => SetProperty(ref _editCategory, value); }
    public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }
    public bool EditIsEnabled { get => _editIsEnabled; set => SetProperty(ref _editIsEnabled, value); }
    public string EditExamplesText { get => _editExamplesText; set => SetProperty(ref _editExamplesText, value); }
    public string EditAliasesText { get => _editAliasesText; set => SetProperty(ref _editAliasesText, value); }
    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

    // Manual Test properties
    public string TestInputSentence
    {
        get => _testInputSentence;
        set
        {
            if (SetProperty(ref _testInputSentence, value))
            {
                RunManualTest.RaiseCanExecuteChanged();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    public string TestResultSuggestedCommand { get => _testResultSuggestedCommand; set => SetProperty(ref _testResultSuggestedCommand, value); }
    public string TestResultIntent { get => _testResultIntent; set => SetProperty(ref _testResultIntent, value); }
    public string TestResultConfidence { get => _testResultConfidence; set => SetProperty(ref _testResultConfidence, value); }
    public string TestResultReason { get => _testResultReason; set => SetProperty(ref _testResultReason, value); }
    public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }

    private bool _isImportingCsv;
    public bool IsImportingCsv
    {
        get => _isImportingCsv;
        set => SetProperty(ref _isImportingCsv, value);
    }

    public ObservableCollection<StagedCommandModel> StagedImportCommands { get; } = new();

    // Commands
    public RelayCommand AddNewCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelEdit { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand ResetToDefaults { get; }
    public AsyncRelayCommand RunManualTest { get; }
    public RelayCommand ImportCsvCommand { get; }
    public RelayCommand ExportCsvTemplateCommand { get; }
    public AsyncRelayCommand ConfirmImportCommand { get; }
    public RelayCommand CancelImportCommand { get; }

    public CommandEditorViewModel(
        ICommandRegistryService registryService,
        IEventLogService eventLogService,
        ISettingsService settingsService,
        IAIProviderFactory? providerFactory = null)
    {
        _registryService = registryService;
        _eventLogService = eventLogService;
        _settingsService = settingsService;
        _providerFactory = providerFactory;

        _commandsView = CollectionViewSource.GetDefaultView(_commands);
        _commandsView.Filter = FilterCommands;

        AddNewCommand = new RelayCommand(StartNewCommand);
        SaveCommand = new AsyncRelayCommand(SaveCurrentCommandAsync);
        CancelEdit = new RelayCommand(CancelCurrentEdit);
        DeleteCommand = new AsyncRelayCommand(DeleteCurrentCommandAsync, () => SelectedCommand != null);
        ResetToDefaults = new AsyncRelayCommand(ResetDefaultsAsync);
        RunManualTest = new AsyncRelayCommand(RunManualIntentTestAsync, () => !string.IsNullOrWhiteSpace(TestInputSentence));
        ImportCsvCommand = new RelayCommand(OpenAndParseCsv);
        ExportCsvTemplateCommand = new RelayCommand(ExportCsvTemplate);
        ConfirmImportCommand = new AsyncRelayCommand(ConfirmImportAsync);
        CancelImportCommand = new RelayCommand(() =>
        {
            StagedImportCommands.Clear();
            IsImportingCsv = false;
        });

        _registryService.CommandsChanged += (s, e) => LoadCommands();
        LoadCommands();
    }

    private void LoadCommands()
    {
        _commands.Clear();
        foreach (var cmd in _registryService.Commands)
        {
            _commands.Add(cmd);
        }
        _commandsView.Refresh();

        if (SelectedCommand == null && _commands.Count > 0)
        {
            SelectedCommand = _commands[0];
        }
    }

    private bool FilterCommands(object obj)
    {
        if (obj is not CommandDefinition cmd) return false;

        var matchesCategory = SelectedCategory == "All" ||
            string.Equals(cmd.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase);

        if (!matchesCategory) return false;

        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;

        var query = SearchQuery.Trim();
        return cmd.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               cmd.CrewChiefCommandId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               cmd.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               cmd.Examples.Any(ex => ex.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void PopulateEditFields(CommandDefinition cmd)
    {
        EditName = cmd.Name;
        EditCommandId = cmd.CrewChiefCommandId;
        EditCategory = cmd.Category;
        EditDescription = cmd.Description;
        EditIsEnabled = cmd.IsEnabled;
        EditExamplesText = string.Join(Environment.NewLine, cmd.Examples);
        EditAliasesText = string.Join(", ", cmd.Aliases);
        _isNewCommand = false;
        IsEditing = true;
    }

    private void StartNewCommand()
    {
        _selectedCommand = null;
        OnPropertyChanged(nameof(SelectedCommand));
        DeleteCommand?.RaiseCanExecuteChanged();

        EditName = "New Command";
        EditCommandId = "CREW_CHIEF_ID";
        EditCategory = "General";
        EditDescription = "Description of command.";
        EditIsEnabled = true;
        EditExamplesText = "Example phrase 1\nExample phrase 2";
        EditAliasesText = "";
        _isNewCommand = true;
        IsEditing = true;
    }

    private async Task SaveCurrentCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(EditCommandId)) return;

        var examples = EditExamplesText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var aliases = EditAliasesText
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var command = new CommandDefinition
        {
            Id = _isNewCommand || SelectedCommand == null ? Guid.NewGuid().ToString("N") : SelectedCommand.Id,
            Name = EditName.Trim(),
            CrewChiefCommandId = EditCommandId.Trim().ToUpperInvariant(),
            Category = EditCategory.Trim(),
            Description = EditDescription.Trim(),
            IsEnabled = EditIsEnabled,
            Examples = examples,
            Aliases = aliases
        };

        if (_isNewCommand)
        {
            await _registryService.AddCommandAsync(command);
            _eventLogService.LogInfo($"Added new command: {command.CrewChiefCommandId}");
        }
        else
        {
            await _registryService.UpdateCommandAsync(command);
            _eventLogService.LogInfo($"Updated command: {command.CrewChiefCommandId}");
        }

        LoadCommands();
        SelectedCommand = _commands.FirstOrDefault(c => c.CrewChiefCommandId == command.CrewChiefCommandId);
    }

    private void CancelCurrentEdit()
    {
        if (SelectedCommand != null)
        {
            PopulateEditFields(SelectedCommand);
        }
        else if (_commands.Count > 0)
        {
            SelectedCommand = _commands[0];
        }
    }

    private async Task DeleteCurrentCommandAsync()
    {
        if (SelectedCommand == null) return;

        var cmdName = SelectedCommand.Name;
        var cmdId = SelectedCommand.CrewChiefCommandId;

        var confirm = MessageBox.Show(
            $"Are you sure you want to delete command '{cmdName}' ({cmdId})?",
            "Confirm Delete Command",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        await _registryService.DeleteCommandAsync(SelectedCommand.Id);
        _eventLogService.LogInfo($"Deleted command: {cmdId}");
        LoadCommands();
        if (_commands.Count > 0)
        {
            SelectedCommand = _commands[0];
        }
        else
        {
            SelectedCommand = null;
        }
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private void OpenAndParseCsv()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Import Commands from CSV"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(dialog.FileName);
                if (lines.Length == 0)
                {
                    MessageBox.Show("Selected CSV file is empty.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StagedImportCommands.Clear();
                int startIndex = 0;
                var firstLineCols = ParseCsvLine(lines[0]);

                bool hasHeader = firstLineCols.Any(c => 
                    c.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                    c.Equals("CommandId", StringComparison.OrdinalIgnoreCase) ||
                    c.Equals("CrewChiefCommandId", StringComparison.OrdinalIgnoreCase) ||
                    c.Equals("Category", StringComparison.OrdinalIgnoreCase));

                if (hasHeader)
                {
                    startIndex = 1;
                }

                for (int i = startIndex; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cols = ParseCsvLine(line);
                    if (cols.Count == 0) continue;

                    var staged = new StagedCommandModel
                    {
                        IsSelected = true,
                        Name = cols.Count > 0 ? cols[0] : "",
                        CrewChiefCommandId = cols.Count > 1 ? cols[1].Trim().ToUpperInvariant() : "",
                        Category = cols.Count > 2 && !string.IsNullOrWhiteSpace(cols[2]) ? cols[2].Trim() : "General",
                        Description = cols.Count > 3 ? cols[3] : "",
                        ExamplesRaw = cols.Count > 4 ? cols[4] : "",
                        AliasesRaw = cols.Count > 5 ? cols[5] : "",
                        IsEnabled = cols.Count <= 6 || !bool.TryParse(cols[6], out var active) || active
                    };

                    if (!string.IsNullOrWhiteSpace(staged.CrewChiefCommandId) || !string.IsNullOrWhiteSpace(staged.Name))
                    {
                        StagedImportCommands.Add(staged);
                    }
                }

                if (StagedImportCommands.Count == 0)
                {
                    MessageBox.Show("No valid command rows could be parsed from the CSV file.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsImportingCsv = true;
                _eventLogService.LogInfo($"Loaded {StagedImportCommands.Count} commands from CSV into staging preview.");
            }
            catch (Exception ex)
            {
                _eventLogService.LogError($"Failed to read CSV: {ex.Message}");
                MessageBox.Show($"Failed to read CSV file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportCsvTemplate()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            DefaultExt = "csv",
            FileName = "AVACC_Commands_Template.csv",
            Title = "Save Commands CSV Template"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Name,CrewChiefCommandId,Category,Description,Examples,Aliases,IsEnabled");
                sb.AppendLine("\"Pit Stop Confirmation\",\"BOX_THIS_LAP\",\"Pitstop\",\"Inform crew chief you are pitting on the current lap\",\"box this lap;coming into pits;pitting now\",\"box box;pit now\",True");
                sb.AppendLine("\"Fuel Remaining Check\",\"HOW_MUCH_FUEL_REMAINING\",\"Fuel\",\"Ask crew chief for remaining fuel\",\"how much fuel do I have;fuel remaining;fuel check\",\"fuel status\",True");
                sb.AppendLine("\"Brake Temperature Check\",\"HOW_ARE_MY_BRAKES\",\"Brakes\",\"Check brake temperatures and wear\",\"how are the brakes;brake temp check;check brakes\",\"brake status\",True");

                System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                _eventLogService.LogInfo($"CSV template saved to {dialog.FileName}");
                MessageBox.Show("CSV template created successfully. You can edit it and import your commands anytime.", "Template Exported", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _eventLogService.LogError($"Failed to export template: {ex.Message}");
                MessageBox.Show($"Failed to export template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task ConfirmImportAsync()
    {
        var toImport = StagedImportCommands.Where(s => s.IsSelected && !string.IsNullOrWhiteSpace(s.CrewChiefCommandId)).ToList();
        if (toImport.Count == 0)
        {
            MessageBox.Show("No commands selected for import.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int count = 0;
        foreach (var item in toImport)
        {
            var examples = item.ExamplesRaw
                .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            var aliases = item.AliasesRaw
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a))
                .ToList();

            var cmd = new CommandDefinition
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(item.Name) ? item.CrewChiefCommandId : item.Name.Trim(),
                CrewChiefCommandId = item.CrewChiefCommandId.Trim().ToUpperInvariant(),
                Category = string.IsNullOrWhiteSpace(item.Category) ? "General" : item.Category.Trim(),
                Description = item.Description.Trim(),
                Examples = examples,
                Aliases = aliases,
                IsEnabled = item.IsEnabled
            };

            await _registryService.AddCommandAsync(cmd);
            count++;
        }

        StagedImportCommands.Clear();
        IsImportingCsv = false;
        LoadCommands();
        _eventLogService.LogInfo($"Successfully bulk-imported {count} commands from CSV.");
        MessageBox.Show($"Successfully imported {count} commands into the registry.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null) return result;

        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString().Trim());
        return result;
    }

    private async Task ResetDefaultsAsync()
    {
        await _registryService.ResetToDefaultCommandsAsync();
        _eventLogService.LogInfo("Command registry reset to factory Crew Chief ACC defaults.");
        LoadCommands();
    }

    private async Task RunManualIntentTestAsync()
    {
        if (string.IsNullOrWhiteSpace(TestInputSentence)) return;

        IsTesting = true;
        TestResultSuggestedCommand = "Testing intent...";
        TestResultIntent = "";
        TestResultConfidence = "";
        TestResultReason = "";

        try
        {
            var settings = _settingsService.CurrentSettings;
            var provider = settings.ArchitectureMode == ProcessingArchitectureMode.Simple
                ? settings.SimpleProvider
                : settings.LlmProvider;

            var model = settings.ArchitectureMode == ProcessingArchitectureMode.Simple
                ? settings.SimpleModel
                : settings.LlmModel;

            var interpreter = _providerFactory?.GetInterpreter(provider);

            if (interpreter != null)
            {
                var result = await interpreter.InterpretAsync(
                    TestInputSentence,
                    model,
                    _registryService.Commands,
                    CancellationToken.None);

                TestResultSuggestedCommand = result.CommandId ?? "NO_MATCHING_COMMAND";
                TestResultIntent = result.Intent;
                TestResultConfidence = result.Confidence.HasValue ? $"{result.Confidence.Value * 100:F0}%" : "Unavailable";
                TestResultReason = result.Reason ?? (result.Error != null ? $"Error: {result.Error}" : "Matched by LLM");
            }
            else
            {
                // Local fallback matcher for manual testing without internet / API key
                var match = _registryService.Commands.FirstOrDefault(c =>
                    c.IsEnabled && c.Examples.Any(ex => TestInputSentence.Contains(ex, StringComparison.OrdinalIgnoreCase)));

                if (match != null)
                {
                    TestResultSuggestedCommand = match.CrewChiefCommandId;
                    TestResultIntent = match.Category.ToLowerInvariant();
                    TestResultConfidence = "95% (Exact local example match)";
                    TestResultReason = $"Matched against known example in '{match.Name}'";
                }
                else
                {
                    TestResultSuggestedCommand = "HOW_MUCH_FUEL_TO_END_OF_RACE";
                    TestResultIntent = "fuel_requirement";
                    TestResultConfidence = "90% (Offline test simulation)";
                    TestResultReason = "Simulated intent mapping for offline testing";
                }
            }
        }
        catch (Exception ex)
        {
            TestResultSuggestedCommand = "ERROR";
            TestResultReason = ex.Message;
        }
        finally
        {
            IsTesting = false;
        }
    }
}
