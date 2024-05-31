using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views.ProfileSettings;

public sealed class AnonymousProfileLoadSettingsViewModel : ViewModelBase
{
    public AnonymousProfileLoadSettingsViewModel(
        ILogCodecContainer logCodecContainer,
        IProfileSettingsViewModelFactory vmFactory,
        string path,
        IActionCommand closeCommand,
        IActionCommand<ProfileSettingsBase> confirmCommand)
    {
        // Dependencies:
        Guard.NotNull(logCodecContainer);
        Guard.NotNull(vmFactory);

        // Members initialization:
        var logCodec = logCodecContainer.GetLogCodecs().First(x => x.Name.Equals(PlainTextProfileSettings.CodecName, StringComparison.OrdinalIgnoreCase));
        var profileSettings = logCodecContainer.CreateProfileSettings(logCodec);
        ((PlainTextProfileSettings)profileSettings).Path = path;
        ProfileSettings = vmFactory.CreateProfileSettings(profileSettings);

        // Actions:
        CloseCommand = closeCommand;
        ConfirmCommand = new ActionCommand(_ =>
        {
            var profileSettings = ProfileSettings.CommitChanges();
            if (profileSettings is not null)
            {
                CloseCommand.Execute(null);
                confirmCommand.Execute(profileSettings);
            }
        });
    }

    public IProfileSettingsViewModel ProfileSettings { get; set; }
    public bool Confirmed { get; set; }
    public IActionCommand CloseCommand { get; }
    public IActionCommand ConfirmCommand { get; }
}
