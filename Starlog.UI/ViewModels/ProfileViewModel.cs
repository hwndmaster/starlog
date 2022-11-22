using System.Collections.ObjectModel;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;

namespace Genius.Starlog.UI.ViewModels;

public interface IProfileViewModel : ISelectable
{
    Guid? Id { get; }
    string Name { get; }
    IActionCommand CommitProfileCommand { get; }
    IActionCommand LoadProfileCommand { get; }
}

public sealed class ProfileViewModel : ViewModelBase, IProfileViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly IProfileQueryService _profileQuery;
    private readonly IUserInteraction _ui;
    private readonly IMainController _controller;
    private readonly ILogContainer _logContainer;
    private readonly ILogReaderContainer _logReaderContainer;

    private Profile? _profile;

    public ProfileViewModel(
        Profile? profile,
        ICommandBus commandBus,
        IMainController controller,
        IProfileQueryService profileQuery,
        IUserInteraction ui,
        ILogContainer logContainer,
        ILogReaderContainer logReaderContainer)
    {
        _commandBus = commandBus.NotNull();
        _controller = controller.NotNull();
        _profileQuery = profileQuery.NotNull();
        _ui = ui.NotNull();
        _logContainer = logContainer.NotNull();
        _logReaderContainer = logReaderContainer.NotNull();

        _profile = profile;

        foreach (var logReader in _logReaderContainer.GetLogReaders())
        {
            LogReaders.Add(new LogReaderViewModel(_logReaderContainer.CreateProfileLogReader(logReader)));
        }

        InitializeProperties(() =>
        {
            if (_profile is not null)
            {
                ResetForm();
                Reconcile();
            }
        });

        CommitProfileCommand = new ActionCommand(_ => CommitProfile());

        LoadProfileCommand = new ActionCommand(async _ => {
            await _logContainer.LoadProfileAsync(_profile.NotNull());
            _controller.ShowLogsForActiveProfile();
        });

        ResetCommand = new ActionCommand(_ => ResetForm(), _ => _profile is not null);
    }

    public void Reconcile()
    {
        if (_profile is null)
        {
            return;
        }

        Name = _profile.Name;
        Path = _profile.Path;
        SelectedLogReader = LogReaders.First(x => x.LogReader.LogReader.Id == _profile.LogReader.LogReader.Id);
        FileArtifactLinesCount = _profile.FileArtifactLinesCount;
    }

    private async Task CommitProfile()
    {
        if (string.IsNullOrEmpty(Name))
        {
            _ui.ShowWarning("Profile name cannot be empty.");
            return;
        }

        if (_profile is null)
        {
            var profileId = await _commandBus.SendAsync(new ProfileCreateCommand
            {
                Name = Name,
                Path = Path,
                LogReader = SelectedLogReader.LogReader,
                FileArtifactLinesCount = FileArtifactLinesCount
            });
            _profile = await _profileQuery.FindByIdAsync(profileId);
        }
        else
        {
            await _commandBus.SendAsync(new ProfileUpdateCommand(_profile.Id)
            {
                Name = Name,
                Path = Path,
                LogReader = SelectedLogReader.LogReader,
                FileArtifactLinesCount = FileArtifactLinesCount
            });
        }
    }

    private void ResetForm()
    {
        Name = _profile?.Name ?? Name;
        Path = _profile?.Path ?? Path;
        SelectedLogReader = _profile is null
            ? SelectedLogReader
            : LogReaders.First(x => x.LogReader.LogReader.Id == _profile.LogReader.LogReader.Id);
        FileArtifactLinesCount = _profile?.FileArtifactLinesCount ?? FileArtifactLinesCount;
    }

    public ObservableCollection<LogReaderViewModel> LogReaders { get; }
        = new TypedObservableList<LogReaderViewModel, LogReaderViewModel>();

    public Guid? Id => _profile?.Id;

    public string Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Path
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public LogReaderViewModel SelectedLogReader
    {
        get => GetOrDefault(LogReaders[0]);
        set => RaiseAndSetIfChanged(value);
    }

    public int FileArtifactLinesCount
    {
        get => GetOrDefault(0);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsSelected
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitProfileCommand { get; }

    public IActionCommand LoadProfileCommand { get; }

    public IActionCommand ResetCommand { get; }
}
