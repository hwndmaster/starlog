using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views;

public interface IMessageParsingViewModel
{
    MessageParsing? MessageParsing { get; }
    IActionCommand CommitCommand { get; }
}

// TODO: Cover with unit tests
public sealed class MessageParsingViewModel : ViewModelBase, IMessageParsingViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly IUserInteraction _ui;
    private MessageParsing? _messageParsing;

    public MessageParsingViewModel(
        MessageParsing? messageParsing,
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IUserInteraction ui)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _ui = ui.NotNull();

        // Members initialization:
        _messageParsing = messageParsing;
        Methods = Enum.GetNames<MessageParsingMethod>();
        Method = Methods[0];
        IsRegex = Method == MessageParsingMethod.RegEx.ToString();
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));
        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Pattern)));
        AddValidationRule(new IsRegexValidationRule(nameof(Pattern)), shouldValidatePropertyName: nameof(IsRegex));

        InitializeProperties(() =>
        {
            if (_messageParsing is not null)
            {
                Reconcile();
            }
        });

        // Subscriptions:
        this.WhenChanged(x => x.Method)
            .Subscribe(_ =>
            {
                IsRegex = Method == MessageParsingMethod.RegEx.ToString();
            });

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
    }

    private async Task<bool> Commit()
    {
        Guard.NotNull(_currentProfile.Profile);

        Validate();

        if (HasErrors)
        {
            _ui.ShowWarning("Cannot proceed while there are errors in the form.");
            return false;
        }

        var messageParsing = _messageParsing ?? new MessageParsing();
        messageParsing.Name = Name;
        messageParsing.Method = Enum.Parse<MessageParsingMethod>(Method);
        messageParsing.Pattern = Pattern;

        var commandResult = await _commandBus.SendAsync(new MessageParsingCreateOrUpdateCommand
        {
            ProfileId = _currentProfile.Profile.Id,
            MessageParsing = messageParsing
        });

        var addedOrUpdatedId = commandResult.MessageParsingAdded ?? commandResult.MessageParsingUpdated;
        _messageParsing = _currentProfile.Profile.MessageParsings.First(x => x.Id == addedOrUpdatedId);

        return true;
    }

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

    public bool IsRegex
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitCommand { get; }
    public IActionCommand ResetCommand { get; }
}
