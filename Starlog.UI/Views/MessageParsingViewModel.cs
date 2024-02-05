using System.Collections.ObjectModel;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.UI.Forms.Controls.AutoGrid;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.AutoGridBuilders;

namespace Genius.Starlog.UI.Views;

public interface IMessageParsingViewModel
{
    MessageParsing? MessageParsing { get; }
    IActionCommand CommitCommand { get; }
}

public sealed class MessageParsingTestViewModel : ViewModelBase
{
    public MessageParsingTestViewModel(LogRecord logRecord)
    {
        LogRecord = logRecord.NotNull();
    }

    public LogRecord LogRecord { get; }
    public DynamicColumnEntriesViewModel? Entries { get; set; }
}

// TODO: Cover with unit tests
public sealed class MessageParsingViewModel : ViewModelBase, IMessageParsingViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly IMessageParsingHandler _messageParsingHandler;
    private readonly IUserInteraction _ui;
    private MessageParsing? _messageParsing;

    public MessageParsingViewModel(
        MessageParsing? messageParsing,
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IMessageParsingHandler messageParsingHandler,
        ILogContainer logContainer,
        IQuickFilterProvider quickFilterProvider,
        IUserInteraction ui,
        MessageParsingTestBuilder testAutoGridBuilder)
    {
        Guard.NotNull(quickFilterProvider);

        // Dependencies:
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _messageParsingHandler = messageParsingHandler.NotNull();
        _logContainer = logContainer.NotNull();
        _ui = ui.NotNull();
        TestAutoGridBuilder = testAutoGridBuilder.NotNull();

        // Members initialization:
        _messageParsing = messageParsing;
        Methods = Enum.GetNames<PatternType>();
        Method = Methods[0];
        IsRegex = Method == PatternType.RegularExpression.ToString();
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Pattern)));
        AddValidationRule(new IsRegexValidationRule(nameof(Pattern)), shouldValidatePropertyName: nameof(IsRegex));
        Filters = quickFilterProvider.GetQuickFilters()
            .Concat(_currentProfile.Profile.Filters)
            .Select(x => new ReferenceDto(x.Id, x.Name)).ToList();
        SelectedFilters = new ObservableCollection<ReferenceDto>();

        InitializeProperties(() =>
        {
            if (_messageParsing is not null)
            {
                Reconcile();
                ShowTestEntries();
            }
        });

        // Subscriptions:
        this.WhenChanged(x => x.Method)
            .Subscribe(_ =>
            {
                IsRegex = Method == PatternType.RegularExpression.ToString();
            });
        this.WhenAnyChanged(x => x.Pattern, x => x.Method).Subscribe(_ => ShowTestEntries());
        SelectedFilters.WhenCollectionChanged().Subscribe(_ => ShowTestEntries());

        // Actions:
        CommitCommand = new ActionCommand(_ => Commit());
        ResetCommand = new ActionCommand(_ => Reconcile(), _ => _messageParsing is not null);
    }

    public void Reconcile()
    {
        if (_messageParsing is null)
        {
            return;
        }

        Name = _messageParsing.Name;
        Method = _messageParsing.Method.ToString();
        Pattern = _messageParsing.Pattern;

        SelectedFilters.Clear();
        if (_messageParsing.Filters is not null)
        {
            foreach (var filter in _messageParsing.Filters)
            {
                var foundFilter = Filters.FirstOrDefault(x => x.Id == filter);
                if (foundFilter != default)
                {
                    SelectedFilters.Add(foundFilter);
                }
            }
        }
    }

    private async Task<bool> Commit()
    {
        Guard.NotNull(_currentProfile.Profile);

        Validate();

        if (HasErrors)
        {
            _ui.ShowWarning(StringResources.ValidationError);
            return false;
        }

        var messageParsing = _messageParsing ?? new MessageParsing { Name = default!, Method = default, Pattern = default! };
        messageParsing.Name = Name;
        messageParsing.Method = Enum.Parse<PatternType>(Method);
        messageParsing.Pattern = Pattern;
        messageParsing.Filters = SelectedFilters.Select(x => x.Id).ToArray();

        var commandResult = await _commandBus.SendAsync(new MessageParsingCreateOrUpdateCommand
        {
            ProfileId = _currentProfile.Profile.Id,
            MessageParsing = messageParsing
        });

        var addedOrUpdatedId = commandResult.MessageParsingAdded ?? commandResult.MessageParsingUpdated;
        _messageParsing = _currentProfile.Profile.MessageParsings.First(x => x.Id == addedOrUpdatedId);

        return true;
    }

    private void ShowTestEntries()
    {
        if (PropertyHasErrors(nameof(Pattern)))
            return;

        var dummyModel = new MessageParsing {
            Name = string.Empty,
            Pattern = Pattern,
            Method = Enum.Parse<PatternType>(Method),
            Filters = SelectedFilters.Select(x => x.Id).ToArray()
        };
        var logs = _logContainer.GetLogs();

        var foundColumns = _messageParsingHandler.RetrieveColumns(dummyModel);
        if (foundColumns.Length == 0)
        {
            TestingError = "No columns have been determined by the pattern.";
            return;
        }

        TestEntries.Clear();
        foreach (var log in logs)
        {
            var parsed = _messageParsingHandler.ParseMessage(dummyModel, log, testingMode: true).ToArray();
            if (parsed.Length > 0)
            {
                TestEntries.Add(new MessageParsingTestViewModel(log)
                {
                    Entries = new DynamicColumnEntriesViewModel(() => parsed)
                });
            }

            if (TestEntries.Count == 5)
                break;
        }

        if (TestEntries.Count == 0)
        {
            TestingError = "No records have been found by the pattern.";
            return;
        }

        TestColumns = new DynamicColumnsViewModel(foundColumns);
        TestingError = null;
    }

    public IAutoGridBuilder TestAutoGridBuilder { get; }
    public MessageParsing? MessageParsing => _messageParsing;
    public string PageTitle => _messageParsing is null ? "Add message parsing" : "Edit message parsing";

    public string Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Method
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string[] Methods
    {
        get => GetOrDefault<string[]>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Pattern
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public ICollection<ReferenceDto> Filters { get; }
    public ObservableCollection<ReferenceDto> SelectedFilters { get; }

    public DynamicColumnsViewModel? TestColumns
    {
        get => GetOrDefault<DynamicColumnsViewModel?>();
        set => RaiseAndSetIfChanged(value);
    }

    public DelayedObservableCollection<MessageParsingTestViewModel> TestEntries { get; }
        = new TypedObservableCollection<MessageParsingTestViewModel, MessageParsingTestViewModel>();

    public bool IsRegex
    {
        get => GetOrDefault<bool>();
        private set => RaiseAndSetIfChanged(value);
    }

    public string? TestingError
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitCommand { get; }
    public IActionCommand ResetCommand { get; }
}
