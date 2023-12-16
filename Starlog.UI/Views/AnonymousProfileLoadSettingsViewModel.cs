using Genius.Starlog.Core.Models;

namespace Genius.Starlog.UI.Views;

// TODO: Cover with unit tests
public sealed class AnonymousProfileLoadSettingsViewModel : ViewModelBase
{
    public AnonymousProfileLoadSettingsViewModel(IViewModelFactory vmFactory, IActionCommand closeCommand,
        IActionCommand<ProfileSettings> confirmCommand)
    {
        // Dependencies:
        Guard.NotNull(vmFactory);

        // Members initialization:
        ProfileSettings = vmFactory.CreateProfileSettings(null);

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

    public ProfileSettingsViewModel ProfileSettings { get; set; }
    public bool Confirmed { get; set; }
    public IActionCommand CloseCommand { get; }
    public IActionCommand ConfirmCommand { get; }
}
