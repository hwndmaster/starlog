using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Views.ProfileFilters;
using Genius.Starlog.UI.Views.ProfileSettings;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.Starlog.UI.Views;

public interface IViewModelFactory
{
    AnonymousProfileLoadSettingsViewModel CreateAnonymousProfileLoadSettings(string path,
        IActionCommand closeCommand,
        IActionCommand<ProfileSettingsBase> confirmCommand);
    ProfileSettingsBaseViewModel CreateLogCodec(LogCodec logCodec, ProfileSettingsBase? profileSettings);
    IMessageParsingViewModel CreateMessageParsing(MessageParsing? messageParsing);
    IProfileViewModel CreateProfile(Profile? profile);
    IProfileFilterViewModel CreateProfileFilter(ProfileFilterBase? profileFilter);
    IProfileFilterSettingsViewModel CreateProfileFilterSettings(LogFilter logFilter, ProfileFilterBase? profileFilter);
    IProfileSettingsViewModel CreateProfileSettings(ProfileSettingsBase? profileSettings);
}

[ExcludeFromCodeCoverage]
internal sealed class ViewModelFactory : IViewModelFactory
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly IEventBus _eventBus;
    private readonly ILogContainer _logContainer;
    private readonly ILogFilterContainer _logFilterContainer;
    private readonly ILogCodecContainer _logCodecContainer;
    private readonly IMainController _mainController;
    private readonly IMessageParsingHandler _messageParsingHandler;
    private readonly IProfileLoadingController _profileLoadingController;
    private readonly IProfileQueryService _profileQuery;
    private readonly IProfileSettingsTemplateQueryService _profileSettingsTemplateQuery;
    private readonly ISettingsQueryService _settingsQuery;
    private readonly IQuickFilterProvider _quickFilterProvider;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserInteraction _ui;

    public ViewModelFactory(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        IEventBus eventBus,
        ILogContainer logContainer,
        ILogFilterContainer logFilterContainer,
        ILogCodecContainer logCodecContainer,
        IMainController mainController,
        IMessageParsingHandler messageParsingHandler,
        IProfileLoadingController profileLoadingController,
        IProfileQueryService profileQuery,
        IProfileSettingsTemplateQueryService profileSettingsTemplateQuery,
        ISettingsQueryService settingsQuery,
        IQuickFilterProvider quickFilterProvider,
        IUiDispatcher dispatcher,
        IUserInteraction ui)
    {
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _dispatcher = dispatcher.NotNull();
        _eventBus = eventBus.NotNull();
        _logContainer = logContainer.NotNull();
        _logFilterContainer = logFilterContainer.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _mainController = mainController.NotNull();
        _messageParsingHandler = messageParsingHandler.NotNull();
        _profileLoadingController = profileLoadingController.NotNull();
        _profileQuery = profileQuery.NotNull();
        _profileSettingsTemplateQuery = profileSettingsTemplateQuery.NotNull();
        _settingsQuery = settingsQuery.NotNull();
        _quickFilterProvider = quickFilterProvider.NotNull();
        _ui = ui.NotNull();
    }

    public AnonymousProfileLoadSettingsViewModel CreateAnonymousProfileLoadSettings(string path, IActionCommand closeCommand, IActionCommand<ProfileSettingsBase> confirmCommand)
    {
        return new AnonymousProfileLoadSettingsViewModel(
            _logCodecContainer,
            this,
            path,
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

    public IMessageParsingViewModel CreateMessageParsing(MessageParsing? messageParsing)
    {
        return new MessageParsingViewModel(messageParsing, _commandBus, _currentProfile, _messageParsingHandler,
            _logContainer, _quickFilterProvider, _ui, App.ServiceProvider.GetRequiredService<MessageParsingTestBuilder>());
    }

    public IProfileViewModel CreateProfile(Profile? profile)
    {
        return new ProfileViewModel(profile, _commandBus, _mainController, _profileLoadingController, _profileQuery, this, _ui);
    }

    public IProfileFilterViewModel CreateProfileFilter(ProfileFilterBase? profileFilter)
    {
        return new ProfileFilterViewModel(profileFilter, _commandBus, _currentProfile, _logFilterContainer, _ui, this);
    }

    public IProfileFilterSettingsViewModel CreateProfileFilterSettings(LogFilter logFilter, ProfileFilterBase? profileFilter)
    {
        var isNewFilter = profileFilter is null;
        profileFilter = profileFilter is not null && logFilter.Id == profileFilter.LogFilter.Id
            ? profileFilter
            : _logFilterContainer.CreateProfileFilter(logFilter);

        return profileFilter switch
        {
            FilesProfileFilter files => new FilesProfileFilterSettingsViewModel(files, _logContainer),
            MessageProfileFilter message => new MessageProfileFilterSettingsViewModel(message, _logContainer),
            LoggersProfileFilter loggers => new LoggersProfileFilterSettingsViewModel(loggers, _logContainer),
            LogLevelsProfileFilter logLevels => new LogLevelsProfileFilterSettingsViewModel(logLevels, _logContainer),
            ThreadsProfileFilter threads => new ThreadProfileFilterSettingsViewModel(threads, _logContainer),
            TimeAgoProfileFilter timeAgo => new TimeAgoProfileFilterSettingsViewModel(timeAgo, _logContainer),
            TimeRangeProfileFilter timeRange => new TimeRangeProfileFilterSettingsViewModel(timeRange, _logContainer, isNewFilter),
            _ => throw new InvalidOperationException($"{nameof(profileFilter)} is of unexpected type {profileFilter.GetType().Name}")
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
