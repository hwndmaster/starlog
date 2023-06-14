using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views;

public sealed class ProfileSettingsTemplateSelectionViewModel : ViewModelBase
{
    private readonly ProfileSettingsTemplate _model;

    public ProfileSettingsTemplateSelectionViewModel(ProfileSettingsTemplate model)
    {
        _model = model.NotNull();
    }

    public string Name => _model.Name;

    public ProfileSettings Settings => _model.Settings;
}
