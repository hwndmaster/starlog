using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Views.ProfileSettings;

namespace Genius.Starlog.UI.Views;

public interface IProfileSettingsViewModelFactory
{
    AnonymousProfileLoadSettingsViewModel CreateAnonymousProfileLoadSettings(string[] paths,
        IActionCommand closeCommand,
        IActionCommand<ProfileSettingsBase> confirmCommand);
    ProfileSettingsBaseViewModel CreateLogCodec(LogCodec logCodec, ProfileSettingsBase? profileSettings);
    IProfileSettingsViewModel CreateProfileSettings(ProfileSettingsBase? profileSettings);
}

[ExcludeFromCodeCoverage]
internal sealed class ProfileSettingsViewModelFactory : IProfileSettingsViewModelFactory
{
    private readonly IEventBus _eventBus;
    private readonly ILogCodecContainer _logCodecContainer;
    private readonly ISettingsQueryService _settingsQuery;
    private readonly IProfileSettingsTemplateQueryService _profileSettingsTemplateQuery;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserInteraction _ui;

    public ProfileSettingsViewModelFactory(
        IEventBus eventBus,
        ILogCodecContainer logCodecContainer,
        IProfileSettingsTemplateQueryService profileSettingsTemplateQuery,
        ISettingsQueryService settingsQuery,
        IUiDispatcher dispatcher,
        IUserInteraction ui)
    {
        _dispatcher = dispatcher.NotNull();
        _eventBus = eventBus.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _profileSettingsTemplateQuery = profileSettingsTemplateQuery.NotNull();
        _settingsQuery = settingsQuery.NotNull();
        _ui = ui.NotNull();
    }

    public AnonymousProfileLoadSettingsViewModel CreateAnonymousProfileLoadSettings(string[] paths, IActionCommand closeCommand, IActionCommand<ProfileSettingsBase> confirmCommand)
    {
        return new AnonymousProfileLoadSettingsViewModel(
            _logCodecContainer,
            this,
            paths,
            closeCommand,
            confirmCommand);
    }

    public ProfileSettingsBaseViewModel CreateLogCodec(LogCodec logCodec, ProfileSettingsBase? profileSettings)
    {
        profileSettings = profileSettings is not null && logCodec.Id == profileSettings.LogCodec.Id
            ? profileSettings
            : _logCodecContainer.CreateProfileSettings(logCodec);

        return profileSettings switch
        {
            PlainTextProfileSettings plainText => new PlainTextProfileSettingsViewModel(plainText, _settingsQuery),
            XmlProfileSettings xml => new XmlProfileSettingsViewModel(xml),
            WindowsEventProfileSettings windowsEvent => new WindowsEventProfileSettingsViewModel(windowsEvent),
            _ => throw new InvalidOperationException($"{nameof(profileSettings)} is of unexpected type {profileSettings.GetType().Name}")
        };
    }

    public IProfileSettingsViewModel CreateProfileSettings(ProfileSettingsBase? profileSettings)
    {
        var defaultCodecName = PlainTextProfileSettings.CodecName;

        if (profileSettings is null)
        {
            var logCodec = _logCodecContainer.GetLogCodecs().First(x => x.Name.Equals(defaultCodecName, StringComparison.OrdinalIgnoreCase));
            profileSettings = _logCodecContainer.CreateProfileSettings(logCodec);
        }

        return new ProfileSettingsViewModel(profileSettings, _eventBus, _profileSettingsTemplateQuery,
            _logCodecContainer, this, _dispatcher, _ui);
    }
}
