using System.Diagnostics.CodeAnalysis;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.Views;

[ExcludeFromCodeCoverage]
public partial class LogsFilteringView
{
    public LogsFilteringView()
    {
        InitializeComponent();

        this.Loaded += (sender, args) =>
            WpfHelpers.AddFlyout<AddEditProfileFilterFlyout>(this, nameof(LogsFilteringViewModel.IsAddEditProfileFilterVisible), nameof(LogsFilteringViewModel.EditingProfileFilter));
    }
}
