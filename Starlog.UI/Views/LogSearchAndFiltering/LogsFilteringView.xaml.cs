using System.Diagnostics.CodeAnalysis;
using Genius.Starlog.UI.Views.ProfileFilters;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

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
