using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views.ProfileFilters;

namespace Genius.Starlog.UI.Views.LogSearchAndFiltering;

[ExcludeFromCodeCoverage]
public partial class LogsFilteringView
{
    public LogsFilteringView()
    {
        InitializeComponent();

        this.WhenLoadedOneTime().Subscribe(_ =>
        {
            WpfHelpers.AddFlyout<AddEditProfileFilterFlyout>(this, nameof(LogsFilteringViewModel.IsAddEditProfileFilterVisible), nameof(LogsFilteringViewModel.EditingProfileFilter));
            WpfHelpers.AddFlyout<AddEditMessageParsingFlyout>(this, nameof(LogsFilteringViewModel.IsAddEditMessageParsingVisible), nameof(LogsFilteringViewModel.EditingMessageParsing));
        });
    }
}
