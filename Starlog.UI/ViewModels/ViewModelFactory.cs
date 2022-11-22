using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;

namespace Genius.Starlog.UI.ViewModels;

public interface IViewModelFactory
{
    IProfileViewModel CreateProfile(Profile? profile);
}

[ExcludeFromCodeCoverage]
internal sealed class ViewModelFactory : IViewModelFactory
{
    private readonly ICommandBus _commandBus;
    private readonly IMainController _mainController;
    private readonly IProfileQueryService _profileQuery;
    private readonly IUserInteraction _ui;
    private readonly ILogContainer _logContainer;
    private readonly ILogReaderContainer _logReaderContainer;

    public ViewModelFactory(
        ICommandBus commandBus,
        IMainController mainController,
        IProfileQueryService profileQuery,
        IUserInteraction ui,
        ILogContainer logContainer,
        ILogReaderContainer logReaderContainer)
    {
        _commandBus = commandBus.NotNull();
        _mainController = mainController.NotNull();
        _profileQuery = profileQuery.NotNull();
        _ui = ui.NotNull();
        _logContainer = logContainer.NotNull();
        _logReaderContainer = logReaderContainer.NotNull();
    }

    public IProfileViewModel CreateProfile(Profile? profile)
    {
        return new ProfileViewModel(profile, _commandBus, _mainController, _profileQuery, _ui, _logContainer, _logReaderContainer);
    }
}
