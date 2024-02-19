using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.Starlog.UI.Views;

public interface IViewModelFactory
{
    IMessageParsingViewModel CreateMessageParsing(MessageParsing? messageParsing);
    IProfileViewModel CreateProfile(Profile? profile);
}

[ExcludeFromCodeCoverage]
internal sealed class ViewModelFactory : IViewModelFactory
{
    private readonly ICommandBus _commandBus;
    private readonly ICurrentProfile _currentProfile;
    private readonly ILogContainer _logContainer;
    private readonly ILogCodecContainer _logCodecContainer;
    private readonly IMainController _mainController;
    private readonly IMessageParsingHandler _messageParsingHandler;
    private readonly IProfileLoadingController _profileLoadingController;
    private readonly IProfileQueryService _profileQuery;
    private readonly IProfileSettingsViewModelFactory _profileSettingsViewModelFactory;
    private readonly IQuickFilterProvider _quickFilterProvider;
    private readonly IUserInteraction _ui;

    public ViewModelFactory(
        ICommandBus commandBus,
        ICurrentProfile currentProfile,
        ILogContainer logContainer,
        ILogCodecContainer logCodecContainer,
        IMainController mainController,
        IMessageParsingHandler messageParsingHandler,
        IProfileLoadingController profileLoadingController,
        IProfileQueryService profileQuery,
        IProfileSettingsViewModelFactory profileSettingsViewModelFactory,
        IQuickFilterProvider quickFilterProvider,
        IUserInteraction ui)
    {
        _commandBus = commandBus.NotNull();
        _currentProfile = currentProfile.NotNull();
        _logContainer = logContainer.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();
        _mainController = mainController.NotNull();
        _messageParsingHandler = messageParsingHandler.NotNull();
        _profileLoadingController = profileLoadingController.NotNull();
        _profileQuery = profileQuery.NotNull();
        _profileSettingsViewModelFactory = profileSettingsViewModelFactory.NotNull();
        _quickFilterProvider = quickFilterProvider.NotNull();
        _ui = ui.NotNull();
    }

    public IMessageParsingViewModel CreateMessageParsing(MessageParsing? messageParsing)
    {
        return new MessageParsingViewModel(messageParsing, _commandBus, _currentProfile,
            _messageParsingHandler, _logContainer, _quickFilterProvider, _ui,
            App.ServiceProvider.GetRequiredService<MessageParsingTestBuilder>());
    }

    public IProfileViewModel CreateProfile(Profile? profile)
    {
        return new ProfileViewModel(profile, _commandBus, _mainController, _profileLoadingController,
            _profileQuery, _profileSettingsViewModelFactory, _ui);
    }
}
