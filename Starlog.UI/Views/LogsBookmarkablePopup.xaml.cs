using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using Genius.Starlog.UI.Behaviors;

namespace Genius.Starlog.UI.Views;

[ExcludeFromCodeCoverage]
public partial class LogsBookmarkablePopup : UserControl
{
    public LogsBookmarkablePopup(LogsBookmarkableBehavior dataContext)
    {
        InitializeComponent();

        DataContext = dataContext;
    }
}
