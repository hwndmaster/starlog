using System.Collections.ObjectModel;
using System.Diagnostics;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public sealed class WindowsEventProfileSettingsViewModel : ProfileSettingsBaseViewModel
{
    private readonly WindowsEventProfileSettings _windowsEventProfileSettings;
    private readonly Lazy<ObservableCollection<string>> _sources;

    public WindowsEventProfileSettingsViewModel(WindowsEventProfileSettings profileSettings)
        : base(profileSettings)
    {
        // Dependencies:
        _windowsEventProfileSettings = profileSettings.NotNull();

        // Members initialization:
        _sources = new Lazy<ObservableCollection<string>>(() =>
            new ObservableCollection<string>(EventLog.GetEventLogs().Select(x => x.Log))
        );
        SelectedSources = new ObservableCollection<string>(_windowsEventProfileSettings.Sources);

        ResetForm();
    }

    public override bool CommitChanges()
    {
        Validate();
        if (HasErrors)
            return false;

        _windowsEventProfileSettings.Sources = SelectedSources.ToArray();
        _windowsEventProfileSettings.SelectCount = SelectCount;

        return true;
    }

    public override void ResetForm()
    {
        SelectedSources.ReplaceItems(_windowsEventProfileSettings.Sources);
        SelectCount = _windowsEventProfileSettings.SelectCount;
    }

    internal override void CopySettingsFrom(ProfileSettingsBaseViewModel otherProfileSettings)
    {
        if (otherProfileSettings is not WindowsEventProfileSettingsViewModel settings)
            return;

        SelectedSources.ReplaceItems(settings.Sources);
        SelectCount = settings.SelectCount;
    }

    public ObservableCollection<string> Sources => _sources.Value;
    public ObservableCollection<string> SelectedSources { get; }
    public int SelectCount
    {
        get => GetOrDefault(100);
        set => RaiseAndSetIfChanged(value);
    }
}
