using System.Collections.ObjectModel;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms.Validation;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Views.ProfileLogCodecs;

namespace Genius.Starlog.UI.Views;

public interface IProfileViewModel : ISelectable
{
    void CopyFrom(IProfileViewModel source, string? nameSuffix = null);

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
    private readonly ILogCodecContainer _logCodecContainer;

    private Profile? _profile;

    public ProfileViewModel(
        Profile? profile,
        ICommandBus commandBus,
        IMainController controller,
        IProfileQueryService profileQuery,
        IUserInteraction ui,
        ILogContainer logContainer,
        ILogCodecContainer logCodecContainer,
        IViewModelFactory vmFactory)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _controller = controller.NotNull();
        _profileQuery = profileQuery.NotNull();
        _ui = ui.NotNull();
        _logContainer = logContainer.NotNull();
        _logCodecContainer = logCodecContainer.NotNull();

        // Members initialization:
        _profile = profile;

        AddValidationRule(new StringNotNullOrEmptyValidationRule(nameof(Name)));

        foreach (var logCodec in _logCodecContainer.GetLogCodecs())
        {
            LogCodecs.Add(vmFactory.CreateLogCodec(logCodec, _profile?.LogCodec));
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
            await Task.Delay(10); // TODO: Helps to let the UI to show a 'busy' overlay, find a better way around.
            await Task.Run(async() =>
            {
                Guard.NotNull(_profile);
                await _logContainer.LoadProfileAsync(_profile).ConfigureAwait(false);
                await _commandBus.SendAsync(new SettingsUpdateAutoLoadingProfileCommand(_profile.Id));
                _controller.ShowLogsTab();
            })
            .ContinueWith(_ => _controller.SetBusy(false), TaskContinuationOptions.None)
            .ConfigureAwait(false);
        });
        ResetCommand = new ActionCommand(_ => ResetForm(), _ => _profile is not null);
    }

    public void CopyFrom(IProfileViewModel source, string? nameSuffix = null)
    {
        if (source is not ProfileViewModel sourceProfile)
            return;

        Name = sourceProfile.Name + (nameSuffix ?? string.Empty);
        Path = sourceProfile.Path;
        LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == sourceProfile.LogCodec.ProfileLogCodec.LogCodec.Id);
        LogCodec.CopySettingsFrom(sourceProfile.LogCodec);
        FileArtifactLinesCount = sourceProfile.FileArtifactLinesCount;
    }

    public void Reconcile()
    {
        if (_profile is null)
        {
            return;
        }

        Name = _profile.Name;
        Path = _profile.Path;
        LogCodec = LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == _profile.LogCodec.LogCodec.Id);
        FileArtifactLinesCount = _profile.FileArtifactLinesCount;
    }

    private async Task<bool> CommitProfile()
    {
        if (HasErrors || LogCodec.HasErrors)
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
                LogCodec = LogCodec.ProfileLogCodec,
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
                LogCodec = LogCodec.ProfileLogCodec,
                FileArtifactLinesCount = FileArtifactLinesCount
            });
        }

        return true;
    }

    private void ResetForm()
    {
        Name = _profile?.Name ?? Name;
        Path = _profile?.Path ?? Path;
        LogCodec = _profile is null
            ? LogCodec
            : LogCodecs.First(x => x.ProfileLogCodec.LogCodec.Id == _profile.LogCodec.LogCodec.Id);
        FileArtifactLinesCount = _profile?.FileArtifactLinesCount ?? FileArtifactLinesCount;
    }

    public ObservableCollection<LogCodecViewModel> LogCodecs { get; }
        = new TypedObservableList<LogCodecViewModel, LogCodecViewModel>();

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

    public LogCodecViewModel LogCodec
    {
        get => GetOrDefault(LogCodecs[0]);
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
