using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.AutoGridBuilders;
using ReactiveUI;

namespace Genius.Starlog.UI.Views;

public interface ISettingsViewModel : ITabViewModel
{ }

// TODO: Cover with unit tests
internal sealed class SettingsViewModel : TabViewModelBase, ISettingsViewModel, IDisposable
{
    private readonly ICommandBus _commandBus;
    private readonly Disposer _disposer = new();
    private readonly IUserInteraction _ui;
    private Settings _model;

    public SettingsViewModel(
        ICommandBus commandBus,
        ISettingsQueryService settingsQuery,
        IEventBus eventBus,
        IUserInteraction ui,
        PlainTextLinePatternsAutoGridBuilder plainTextLinePatternsGridBuilder)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _ui = ui.NotNull();
        PlainTextLogCodecLinePatternsBuilder = plainTextLinePatternsGridBuilder.NotNull();

        // Members initialization:
        _model = settingsQuery.NotNull().Get();
        Reconcile();

        // Actions:
        AddPlainTextLogCodecLinePatternCommand = new ActionCommand(_ =>
            AddPlainTextLogCodecLinePattern(new PatternValue{ Name = "Unnamed", Type = PatternType.RegularExpression, Pattern = ".*" }));

        // Subscriptions:
        eventBus.WhenFired<SettingsUpdatedEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(@event =>
            {
                _model = @event.Settings;
                Reconcile();
            }).DisposeWith(_disposer);
        WhenAnyChangedNoDispose([], SendUpdateAsync);
    }

    private void Reconcile()
    {
        AutoLoadPreviouslyOpenedProfile = _model.AutoLoadPreviouslyOpenedProfile;

        PlainTextLogCodecLinePatterns.Clear();
        foreach (var pattern in _model.PlainTextLogCodecLinePatterns)
        {
            AddPlainTextLogCodecLinePattern(pattern);
        }
    }

    private async Task SendUpdateAsync()
    {
        await _commandBus.SendAsync(new SettingsUpdateCommand(_model));
    }

    private void AddPlainTextLogCodecLinePattern(PatternValue patternValue)
    {
        var vm = new PatternValueViewModel(patternValue);
        vm.DeleteCommand.Executed.SubscribeOnUiThread(async _ =>
        {
            if (!_ui.AskForConfirmation($"Confirm removing '{vm.Name}'", "Deletion confirmation"))
                return;
            PlainTextLogCodecLinePatterns.Remove(vm);
            await RebindAndSendAsync();
        }).DisposeWith(_disposer);
        vm.WhenAnyChanged().SubscribeOnUiThread(async _ =>
        {
            if (PlainTextLogCodecLinePatterns.Any(x => x.HasErrors))
                return;

            await RebindAndSendAsync();
        }).DisposeWith(_disposer);
        PlainTextLogCodecLinePatterns.Add(vm);

        async Task RebindAndSendAsync()
        {
            _model.PlainTextLogCodecLinePatterns = PlainTextLogCodecLinePatterns.Select(x => x.Commit()).ToList();
            await SendUpdateAsync();
        }
    }

    public void Dispose()
    {
        _disposer.Dispose();
    }

    public bool AutoLoadPreviouslyOpenedProfile
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value, (_, @new) => _model.AutoLoadPreviouslyOpenedProfile = @new);
    }

    public PlainTextLinePatternsAutoGridBuilder PlainTextLogCodecLinePatternsBuilder { get; }
    public ObservableCollection<PatternValueViewModel> PlainTextLogCodecLinePatterns { get; } = new();
    public IActionCommand AddPlainTextLogCodecLinePatternCommand { get; }
}
