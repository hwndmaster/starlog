using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.Comparison;

namespace Genius.Starlog.UI.Controllers;

public interface IComparisonController
{
    Task OpenProfilesForComparisonAsync(Profile profile1, Profile profile2);
}

internal sealed class ComparisonController : IComparisonController
{
    private readonly IComparisonService _comparisonService;
    private readonly IUserInteraction _ui;
    private readonly Lazy<IMainViewModel> _mainViewModel;
    private readonly IMainController _mainController;

    public ComparisonController(
        IComparisonService comparisonService,
        IUserInteraction ui,
        IMainController mainController,
        Lazy<IMainViewModel> mainViewModel)
    {
        _comparisonService = comparisonService.NotNull();
        _mainController = mainController.NotNull();
        _ui = ui.NotNull();
        _mainViewModel = mainViewModel.NotNull();
    }

    // TODO: To cover with unit tests
    public async Task OpenProfilesForComparisonAsync(Profile profile1, Profile profile2)
    {
        var compareTab = _mainViewModel.Value.Tabs.OfType<IComparisonViewModel>().First();
        _mainViewModel.Value.IsComparisonAvailable = true;
        _mainViewModel.Value.SelectedTabIndex = _mainViewModel.Value.Tabs.IndexOf(compareTab);

        _mainController.SetBusy(true);

        try
        {
            var context = await _comparisonService.LoadProfilesAsync(profile1, profile2).ConfigureAwait(false);
            if (context is not null)
            {
                compareTab.PopulateProfiles(context);
            }
            else
            {
                _ui.ShowWarning("Couldn't load one of the selected profiles. Try opening each profile individually to see more details when any if them is failing.");
            }
        }
        finally
        {
            _mainController.SetBusy(false);
        }
    }
}
