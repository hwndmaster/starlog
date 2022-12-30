using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Views.ProfileFilters;
using Genius.Starlog.UI.Views.ProfileLogReaders;

namespace Genius.Starlog.UI.Views;

public interface IViewModelFactory
{
    LogReaderViewModel CreateLogReader(LogReader logReader, ProfileLogReadBase? profileLogReader);
    IProfileViewModel CreateProfile(Profile? profile);
    IProfileFilterViewModel CreateProfileFilter(ProfileFilterBase? profileFilter);
    IProfileFilterSettingsViewModel CreateProfileFilterSettings(LogFilter logFilter, ProfileFilterBase? profileFilter);
}

[ExcludeFromCodeCoverage]
internal sealed class ViewModelFactory : IViewModelFactory
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogFilterContainer _logFilterContainer;
    private readonly ILogReaderContainer _logReaderContainer;
    private readonly IMainController _mainController;
    private readonly IProfileQueryService _profileQuery;
    private readonly ISettingsQueryService _settingsQuery;
    private readonly IUserInteraction _ui;

    public ViewModelFactory(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        ILogFilterContainer logFilterContainer,
        ILogReaderContainer logReaderContainer,
        IMainController mainController,
        IProfileQueryService profileQuery,
        ISettingsQueryService settingsQuery,
        IUserInteraction ui)
    {
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        _logFilterContainer = logFilterContainer.NotNull();
        _logReaderContainer = logReaderContainer.NotNull();
        _mainController = mainController.NotNull();
        _profileQuery = profileQuery.NotNull();
        _settingsQuery = settingsQuery.NotNull();
        _ui = ui.NotNull();
    }

    public LogReaderViewModel CreateLogReader(LogReader logReader, ProfileLogReadBase? profileLogReader)
    {
        profileLogReader = profileLogReader is not null && logReader.Id == profileLogReader.LogReader.Id
            ? profileLogReader
            : _logReaderContainer.CreateProfileLogReader(logReader);

        return profileLogReader switch
        {
            PlainTextProfileLogRead plainText => new PlainTextLogReaderViewModel(plainText, _settingsQuery),
            XmlProfileLogRead xml => new XmlLogReaderViewModel(xml),
            _ => throw new InvalidOperationException($"{nameof(profileLogReader)} is of unexpected type {profileLogReader.GetType().Name}")
        };
    }

    public IProfileViewModel CreateProfile(Profile? profile)
    {
        return new ProfileViewModel(profile, _commandBus, _mainController, _profileQuery, _ui, _logContainer, _logReaderContainer, this);
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
}
