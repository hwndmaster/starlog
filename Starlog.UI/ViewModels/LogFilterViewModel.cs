using System.Windows.Data;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.ViewModels;

public sealed class LogFilterViewModel : ViewModelBase, ILogFilterNodeViewModel,
    IHasModifyCommand, IHasDeleteCommand, ISelectable
{
    public LogFilterViewModel(ProfileFilterBase profileFilter, bool isUserDefined)
    {
        Filter = profileFilter.NotNull();
        IsUserDefined = isUserDefined;

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
    }

    internal void Reconcile()
    {
        OnPropertyChanged(nameof(Title));
    }

    public ProfileFilterBase Filter { get; }
    public string Title => Filter.Name;
    public string Icon => "FolderFavs32";
    public bool CanAddChildren => false;
    public bool IsExpanded { get; set; } = false;
    public bool IsUserDefined { get; }
    public bool CanModifyOrDelete => IsUserDefined;
    public CollectionViewSource CategoryItemsView { get; } = new();
    public bool IsSelected
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand AddChildCommand { get; }
    public IActionCommand ModifyCommand { get; }
    public IActionCommand DeleteCommand { get; }
}
