using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsBookmarkableBehavior : Behavior<DataGrid>
{
    private readonly MenuItem _menuItemBookmark;
    private readonly Popup _popup;

    public LogsBookmarkableBehavior()
    {
        _menuItemBookmark = new MenuItem { Header = "Bookmark",
            InputGestureText = "F8",
            Command = new ActionCommand(_ => BookmarkItem()) };

        _popup = new Popup
        {
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true,
            Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = new CustomPopupPlacementCallback(OnCustomPopupPlacementCallback),
            Child = new LogsBookmarkablePopup(this)
        };
        NextBookmark = new ActionCommand(_ => GoToBookmark(1));
        PreviousBookmark = new ActionCommand(_ => GoToBookmark(-1));
    }

    protected override void OnAttached()
    {
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        AssociatedObject.PreviewKeyUp += OnPreviewKeyUp;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Add(_menuItemBookmark);

        _popup.PlacementTarget = AssociatedObject;

        base.OnAttached();
    }

    private CustomPopupPlacement[] OnCustomPopupPlacementCallback(Size popupSize, Size targetSize, Point offset)
    {
        var placement1 = new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width - 30, targetSize.Height - popupSize.Height - 30), PopupPrimaryAxis.Vertical);
        //var placement2 = new CustomPopupPlacement(new Point(0, 0), PopupPrimaryAxis.Horizontal);

        return [placement1]; //, placement2];
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        AssociatedObject.PreviewKeyUp -= OnPreviewKeyUp;

        var contextMenu = WpfHelpers.EnsureDataGridRowContextMenu(AssociatedObject);
        contextMenu.Items.Remove(_menuItemBookmark);

        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl)
        {
            ShowPopup();
            return;
        }
        if (e.Key == Key.F8)
        {
            BookmarkItem();
            e.Handled = true;
        }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        _popup.IsOpen = false;
    }

    private void ShowPopup()
    {
        _popup.IsOpen = true;
    }

    private void BookmarkItem()
    {
        if (AssociatedObject.DataContext is not ILogsViewModel vm)
            return;
        var firstSelected = vm.SelectedLogItems.FirstOrDefault();
        if (firstSelected is null)
            return;

        var newValue = !firstSelected.IsBookmarked;

        foreach (var item in vm.SelectedLogItems)
        {
            item.IsBookmarked = newValue;
        }
    }

    private void GoToBookmark(int direction)
    {
        if (AssociatedObject.DataContext is not ILogsViewModel vm)
            return;
        var firstSelected = vm.SelectedLogItems.FirstOrDefault();
        if (firstSelected is null)
            return;
        var index = vm.LogItems.IndexOf(firstSelected);
        for (var i = index + direction; i >= 0 && i < vm.LogItems.Count; i += direction)
        {
            if (vm.LogItems[i].IsBookmarked)
            {
                vm.SelectedLogItems.ReplaceItems([vm.LogItems[i]]);
                DataGridNavigation.ScrollToItem(AssociatedObject, index, vm.LogItems.Count);
                break;
            }
        }
    }

    public IActionCommand PreviousBookmark { get; }
    public IActionCommand NextBookmark { get; }
}
