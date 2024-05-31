using System.Collections.ObjectModel;
using System.Diagnostics;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public sealed class WindowsEventProfileSettingsViewModel : ProfileSettingsBaseViewModel
{
    private readonly WindowsEventProfileSettings _windowsEventLogCodec;
    private readonly Lazy<ObservableCollection<string>> _sources;

    public WindowsEventProfileSettingsViewModel(WindowsEventProfileSettings logCodec)
        : base(logCodec)
    {
        // Dependencies:
        _windowsEventLogCodec = logCodec.NotNull();

        // Members initialization:
        _sources = new Lazy<ObservableCollection<string>>(() =>
            new ObservableCollection<string>(EventLog.GetEventLogs().Select(x => x.LogDisplayName))
        );

        // Actions:
        PickSourceCommand = new ActionCommand(arg =>
        {
            // TODO: ...
        });

        // Subscriptions:
        // TODO: ...
    }

    public override bool CommitChanges()
    {
        // TODO: Do nothing for now
        return true;
    }

    public override void ResetForm()
    {
        // TODO: Do nothing for now
    }

    internal override void CopySettingsFrom(ProfileSettingsBaseViewModel logCodec)
    {
        // TODO: Do nothing for now
    }

    public ObservableCollection<string> Sources => _sources.Value;
    public IActionCommand PickSourceCommand { get; }
}
