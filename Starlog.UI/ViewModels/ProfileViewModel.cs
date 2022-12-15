using System.Collections.ObjectModel;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms.Validation;
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
    string Name { get; set; }
    string Path { get; set; }
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
        ILogReaderContainer logReaderContainer,
        IViewModelFactory vmFactory)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _controller = controller.NotNull();
        _profileQuery = profileQuery.NotNull();
        _ui = ui.NotNull();
        _logContainer = logContainer.NotNull();
        _logReaderContainer = logReaderContainer.NotNull();

        // Members initialization:
        _profile = profile;

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));

        foreach (var logReader in _logReaderContainer.GetLogReaders())
        {
            LogReaders.Add(vmFactory.CreateLogReader(logReader, _profile?.LogReader));
        }

        InitializeProperties(() =>
        {
            if (_profile is not null)
            {
                ResetForm();
                Reconcile();
            }
        });

        // Actions:
        CommitProfileCommand = new ActionCommand(_ => CommitProfile());
        LoadProfileCommand = new ActionCommand(async _ => {
            _controller.SetBusy(true);
            try
            {
                Guard.NotNull(_profile);
                await _logContainer.LoadProfileAsync(_profile).ConfigureAwait(false);
                await _commandBus.SendAsync(new SettingsUpdateAutoLoadingProfileCommand(_profile.Id));
                _controller.ShowLogsTab();
            }
            finally
            {
                _controller.SetBusy(false);
            }
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
        LogReader = LogReaders.First(x => x.LogReader.LogReader.Id == _profile.LogReader.LogReader.Id);
        FileArtifactLinesCount = _profile.FileArtifactLinesCount;
    }

    private async Task<bool> CommitProfile()
    {
        if (HasErrors || LogReader.HasErrors)
        {
            _ui.ShowWarning("Cannot proceed while there are errors in the form.");
            return false;
        }

        if (_profile is null)
        {
            var profileId = await _commandBus.SendAsync(new ProfileCreateCommand
            {
                Name = Name,
                Path = Path,
                LogReader = LogReader.LogReader,
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
                LogReader = LogReader.LogReader,
                FileArtifactLinesCount = FileArtifactLinesCount
            });
        }

        return true;
    }

    private void ResetForm()
    {
        Name = _profile?.Name ?? Name;
        Path = _profile?.Path ?? Path;
        LogReader = _profile is null
            ? LogReader
            : LogReaders.First(x => x.LogReader.LogReader.Id == _profile.LogReader.LogReader.Id);
        FileArtifactLinesCount = _profile?.FileArtifactLinesCount ?? FileArtifactLinesCount;
    }

    public ObservableCollection<LogReaderViewModel> LogReaders { get; }
        = new TypedObservableList<LogReaderViewModel, LogReaderViewModel>();

    public Guid? Id => _profile?.Id;

    public string PageTitle => _profile is null ? "Add profile" : "Edit profile";

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

    public LogReaderViewModel LogReader
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
