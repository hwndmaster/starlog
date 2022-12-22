using System.Windows.Data;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

public sealed class LogFilterViewModel : ViewModelBase, ILogFilterNodeViewModel,
    IHasModifyCommand, IHasDeleteCommand, ISelectable
{
    public LogFilterViewModel(ProfileFilterBase profileFilter, bool isUserDefined)
    {
        // Members initialization:
        Filter = profileFilter.NotNull();
        IsUserDefined = isUserDefined;

        Icon = profileFilter switch
        {
            LoggersProfileFilter _ => "Logger32",
            LogLevelsProfileFilter _ => "LogLevel32",
            LogSeveritiesProfileFilter _ => "LogSeverity32",
            ThreadsProfileFilter _ => "Thread32",
            _ => "FolderFavs32"
        };

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

    public ProfileFilterBase Filter { get; }
    public string Title => Filter.Name;
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
}
