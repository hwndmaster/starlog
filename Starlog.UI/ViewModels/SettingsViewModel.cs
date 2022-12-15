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

namespace Genius.Starlog.UI.ViewModels;

public interface ISettingsViewModel : ITabViewModel
{ }

internal sealed class SettingsViewModel : TabViewModelBase, ISettingsViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly IUserInteraction _ui;
    private Settings _model;

    public SettingsViewModel(
        ICommandBus commandBus,
        ISettingsQueryService settingsQuery,
        IEventBus eventBus,
        IUserInteraction ui,
        PlainTextLineRegexTemplatesAutoGridBuilder gridBuilder)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _ui = ui.NotNull();
        PlainTextLogReaderLineRegexTemplatesBuilder = gridBuilder.NotNull();

        // Members initialization:
        _model = settingsQuery.NotNull().Get();
        Reconcile();

        // Actions:
        AddPlainTextLogReaderLineRegexTemplateCommand = new ActionCommand(_ =>
            AddPlainTextLogReaderLineRegexTemplate(new SettingStringValue("Unnamed", ".*")));

        // Subscriptions:
        eventBus.WhenFired<SettingsUpdatedEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(@event =>
            {
                _model = @event.Settings;
                Reconcile();
            });
        this.WhenAnyChanged().Subscribe(async _ => await SendUpdate());
    }

    private void Reconcile()
    {
        AutoLoadPreviouslyOpenedProfile = _model.AutoLoadPreviouslyOpenedProfile;

        PlainTextLogReaderLineRegexTemplates.Clear();
        foreach (var regex in _model.PlainTextLogReaderLineRegexes)
        {
            AddPlainTextLogReaderLineRegexTemplate(regex);
        }
    }

    private async Task SendUpdate()
    {
        await _commandBus.SendAsync(new SettingsUpdateCommand(_model));
    }

    private void AddPlainTextLogReaderLineRegexTemplate(SettingStringValue stringValue)
    {
        var vm = new RegexValueViewModel(stringValue);
        vm.DeleteCommand.Executed.Subscribe(async _ =>
        {
            if (!_ui.AskForConfirmation($"Confirm removing '{vm.Name}'", "Deletion confirmation"))
                return;
            PlainTextLogReaderLineRegexTemplates.Remove(vm);
            await RebindAndSendAsync();
        });
        vm.WhenAnyChanged().Subscribe(async _ =>
        {
            if (PlainTextLogReaderLineRegexTemplates.Any(x => x.HasErrors))
                return;

            await RebindAndSendAsync();
        });
        PlainTextLogReaderLineRegexTemplates.Add(vm);

        async Task RebindAndSendAsync()
        {
            _model.PlainTextLogReaderLineRegexes = PlainTextLogReaderLineRegexTemplates.Select(x => new SettingStringValue(x.Name, x.Regex)).ToList();
            await SendUpdate();
        }
    }

    public bool AutoLoadPreviouslyOpenedProfile
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value, (_, @new) => _model.AutoLoadPreviouslyOpenedProfile = @new);
    }

    public PlainTextLineRegexTemplatesAutoGridBuilder PlainTextLogReaderLineRegexTemplatesBuilder { get; }
    public ObservableCollection<RegexValueViewModel> PlainTextLogReaderLineRegexTemplates { get; } = new();
    public IActionCommand AddPlainTextLogReaderLineRegexTemplateCommand { get; }
}
