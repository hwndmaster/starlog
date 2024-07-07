using System.Reactive.Linq;
using System.Windows.Data;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public sealed class LogFilterViewModel : DisposableViewModelBase, ILogFilterNodeViewModel,
    IHasModifyCommand, IHasDeleteCommand, ISelectable
{
    public LogFilterViewModel(ProfileFilterBase profileFilter, bool isUserDefined, IEventBus eventBus)
    {
        Guard.NotNull(eventBus);

        // Members initialization:
        Filter = profileFilter.NotNull();
        IsUserDefined = isUserDefined;
        RefreshIcon();

        // Subscriptions:
        eventBus.WhenFired<ProfileFilterUpdatedEvent>()
            .Where(eventArgs => eventArgs.ProfileFilterId == profileFilter.Id)
            .Subscribe(_ =>
            {
                RefreshIcon();
            }).DisposeWith(Disposer);

        // Actions:
        AddChildCommand = new ActionCommand(_ => throw new NotSupportedException());

        if (isUserDefined)
        {
            ModifyCommand = new ActionCommand();
            DeleteCommand = new ActionCommand();
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

    private static string FindBestMatchingIcon(FieldProfileFilter field)
    {
        if (field.Name.StartsWith("logger", StringComparison.InvariantCultureIgnoreCase)
            || field.Name.StartsWith("component", StringComparison.InvariantCultureIgnoreCase))
        {
            return "Logger32";
        }
        if (field.Name.StartsWith("thread", StringComparison.InvariantCultureIgnoreCase))
        {
            return "Thread32";
        }

        return "FolderFavs32";
    }

    private void RefreshIcon()
    {
        Icon = Filter switch
        {
            FilesProfileFilter _ => "LogFile32",
            MessageProfileFilter _ => "Message32",
            FieldProfileFilter field => FindBestMatchingIcon(field),
            LogLevelsProfileFilter _ => "LogLevel32",
            TimeAgoProfileFilter _ => "MinusOneHour32",
            TimeRangeProfileFilter _ => "TimeRange32",
            _ => "FolderFavs32"
        };
    }

    public ProfileFilterBase Filter { get; }
    public string Title => Filter.Name;
    public string Icon
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }
    public bool IsUserDefined { get; }
    public bool CanAddChildren => false;
    public bool CanModifyOrDelete => IsUserDefined;
    public bool CanPin => true;
    public bool IsExpanded { get; set; }

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

    public Disposer DisposerForExternalSubscriptions => base.Disposer;

    public CollectionViewSource CategoryItemsView { get; } = new();

    public IActionCommand AddChildCommand { get; }
    public IActionCommand ModifyCommand { get; }
    public IActionCommand DeleteCommand { get; }
    public IActionCommand PinCommand { get; }
}
