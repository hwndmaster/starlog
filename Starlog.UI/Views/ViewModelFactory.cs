using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Views.ProfileFilters;
using Genius.Starlog.UI.Views.ProfileLogCodecs;

namespace Genius.Starlog.UI.Views;

public interface IViewModelFactory
{
    LogCodecViewModel CreateLogCodec(LogCodec logCodec, ProfileLogCodecBase? profileLogCodec);
    IMessageParsingViewModel CreateMessageParsing(MessageParsing? messageParsing);
    IProfileViewModel CreateProfile(Profile? profile);
    IProfileFilterViewModel CreateProfileFilter(ProfileFilterBase? profileFilter);
    IProfileFilterSettingsViewModel CreateProfileFilterSettings(LogFilter logFilter, ProfileFilterBase? profileFilter);
    IProfileSettingsViewModel CreateProfileSettings(ProfileSettings? profileSettings);
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
        _profileQuery = profileQuery.NotNull();
        _profileSettingsTemplateQuery = profileSettingsTemplateQuery.NotNull();
        _settingsQuery = settingsQuery.NotNull();
        _quickFilterProvider = quickFilterProvider.NotNull();
        _ui = ui.NotNull();
    }

    public LogCodecViewModel CreateLogCodec(LogCodec logCodec, ProfileLogCodecBase? profileLogCodec)
    {
        profileLogCodec = profileLogCodec is not null && logCodec.Id == profileLogCodec.LogCodec.Id
            ? profileLogCodec
            : _logCodecContainer.CreateProfileLogCodec(logCodec);

        return profileLogCodec switch
        {
            PlainTextProfileLogCodec plainText => new PlainTextLogCodecViewModel(plainText, _settingsQuery),
            XmlProfileLogCodec xml => new XmlLogCodecViewModel(xml),
            _ => throw new InvalidOperationException($"{nameof(profileLogCodec)} is of unexpected type {profileLogCodec.GetType().Name}")
        };
    }

    public IMessageParsingViewModel CreateMessageParsing(MessageParsing? messageParsing)
    {
        return new MessageParsingViewModel(messageParsing, _commandBus, _currentProfile, _quickFilterProvider, _ui);
    }

    public IProfileViewModel CreateProfile(Profile? profile)
    {
        return new ProfileViewModel(profile, _commandBus, _mainController, _profileQuery, this, _ui);
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
            MessageProfileFilter message => new MessageProfileFilterSettingsViewModel(message, _logContainer),
            LoggersProfileFilter loggers => new LoggersProfileFilterSettingsViewModel(loggers, _logContainer),
            LogLevelsProfileFilter logLevels => new LogLevelsProfileFilterSettingsViewModel(logLevels, _logContainer),
            ThreadsProfileFilter threads => new ThreadProfileFilterSettingsViewModel(threads, _logContainer),
            TimeAgoProfileFilter timeAgo => new TimeAgoProfileFilterSettingsViewModel(timeAgo, _logContainer),
            TimeRangeProfileFilter timeRange => new TimeRangeProfileFilterSettingsViewModel(timeRange, _logContainer, isNewFilter),
            _ => throw new InvalidOperationException($"{nameof(profileFilter)} is of unexpected type {profileFilter.GetType().Name}")
        };
    }

    public IProfileSettingsViewModel CreateProfileSettings(ProfileSettings? profileSettings)
    {
        var codecName = "Plain Text";
        var logCodec = _logCodecContainer.GetLogCodecs().First(x => x.Name.Equals(codecName, StringComparison.OrdinalIgnoreCase));
        profileSettings ??= new ProfileSettings
        {
            LogCodec = _logCodecContainer.CreateProfileLogCodec(logCodec)
        };

        return new ProfileSettingsViewModel(profileSettings, _eventBus, _profileSettingsTemplateQuery,
            _logCodecContainer, this, _dispatcher, _ui);
    }
}
