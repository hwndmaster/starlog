using System.Reactive;
using System.Reactive.Subjects;
using System.Windows.Data;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public sealed class LogFilterMessageParsingViewModel : DisposableViewModelBase, ILogFilterNodeViewModel,
    IHasModifyCommand, IHasDeleteCommand, ISelectable
{
    private readonly Subject<Unit> _committed = new();
    private readonly Subject<LogFilterMessageParsingViewModel> _deleted = new();
    private readonly Subject<IMessageParsingViewModel> _modifying = new();

    public LogFilterMessageParsingViewModel(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IUserInteraction ui,
        IViewModelFactory vmFactory,
        MessageParsing messageParsing, bool isUserDefined)
    {
        Guard.NotNull(commandBus);
        Guard.NotNull(currentProfile);
        Guard.NotNull(ui);
        Guard.NotNull(vmFactory);

        // Members initialization:
        MessageParsing = messageParsing.NotNull();
        IsUserDefined = isUserDefined;
        Icon = "MessageParsing32";
        _committed.DisposeWith(Disposer);
        _deleted.DisposeWith(Disposer);
        _modifying.DisposeWith(Disposer);

        // Actions:
        AddChildCommand = new ActionCommand(_ => throw new NotSupportedException());

        if (isUserDefined)
        {
            ModifyCommand = new ActionCommand(_ =>
            {
                var editingMessageParsing = vmFactory.CreateMessageParsing(MessageParsing);
                editingMessageParsing.CommitCommand
                    .OnOneTimeExecutedBooleanAction()
                    .Subscribe(_ =>
                    {
                        Reconcile();
                        _committed.OnNext(Unit.Default);
                    }).DisposeWith(Disposer);
                    _modifying.OnNext(editingMessageParsing);
            });
            DeleteCommand = new ActionCommand(async _ =>
            {
                if (ui.AskForConfirmation($"You're about to delete a filter named '{Title}'. Proceed?", "Deletion confirmation"))
                {
                    await commandBus.SendAsync(new MessageParsingDeleteCommand(currentProfile.Profile.NotNull().Id, MessageParsing.Id));
                    Dispose();

                    _deleted.OnNext(this);
                }
            });
        }
        else
        {
            ModifyCommand = new ActionCommand(_ => throw new NotSupportedException());
            DeleteCommand = new ActionCommand(_ => throw new NotSupportedException());
        }

        PinCommand = new ActionCommand(_ => IsPinned = !IsPinned);
    }

    internal void Reconcile()
    {
        OnPropertyChanged(nameof(Title));
    }

    public Disposer DisposerExposed => Disposer;

    public MessageParsing MessageParsing { get; }
    public string Title => MessageParsing.Name;
    public string Icon { get; }
    public bool IsUserDefined { get; }
    public bool CanAddChildren => false;
    public bool CanModifyOrDelete => IsUserDefined;
    public bool CanPin => true;
    public bool IsExpanded { get; set; } = false;

    public bool IsPinned
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsSelected
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public CollectionViewSource CategoryItemsView { get; } = new();

    public IActionCommand AddChildCommand { get; }
    public IActionCommand ModifyCommand { get; }
    public IActionCommand DeleteCommand { get; }
    public IActionCommand PinCommand { get; }
    public IObservable<Unit> Committed => _committed;
    public IObservable<LogFilterMessageParsingViewModel> Deleted => _deleted;
    public IObservable<IMessageParsingViewModel> Modifying => _modifying;
}
