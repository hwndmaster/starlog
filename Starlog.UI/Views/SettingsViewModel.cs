using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Views.Generic;
using ReactiveUI;

namespace Genius.Starlog.UI.Views;

public interface ISettingsViewModel : ITabViewModel
{ }

// TODO: Cover with unit tests
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
        PlainTextLogCodecLineRegexTemplatesBuilder = gridBuilder.NotNull();

        // Members initialization:
        _model = settingsQuery.NotNull().Get();
        Reconcile();

        // Actions:
        AddPlainTextLogCodecLineRegexTemplateCommand = new ActionCommand(_ =>
            AddPlainTextLogCodecLineRegexTemplate(new SettingStringValue("Unnamed", ".*")));

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

        PlainTextLogCodecLineRegexTemplates.Clear();
        foreach (var regex in _model.PlainTextLogCodecLineRegexes)
        {
            AddPlainTextLogCodecLineRegexTemplate(regex);
        }
    }

    private async Task SendUpdate()
    {
        await _commandBus.SendAsync(new SettingsUpdateCommand(_model));
    }

    private void AddPlainTextLogCodecLineRegexTemplate(SettingStringValue stringValue)
    {
        var vm = new RegexValueViewModel(stringValue);
        vm.DeleteCommand.Executed.Subscribe(async _ =>
        {
            if (!_ui.AskForConfirmation($"Confirm removing '{vm.Name}'", "Deletion confirmation"))
                return;
            PlainTextLogCodecLineRegexTemplates.Remove(vm);
            await RebindAndSendAsync();
        });
        vm.WhenAnyChanged().Subscribe(async _ =>
        {
            if (PlainTextLogCodecLineRegexTemplates.Any(x => x.HasErrors))
                return;

            await RebindAndSendAsync();
        });
        PlainTextLogCodecLineRegexTemplates.Add(vm);

        async Task RebindAndSendAsync()
        {
            _model.PlainTextLogCodecLineRegexes = PlainTextLogCodecLineRegexTemplates.Select(x => new SettingStringValue(x.Name, x.Regex)).ToList();
            await SendUpdate();
        }
    }

    public bool AutoLoadPreviouslyOpenedProfile
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value, (_, @new) => _model.AutoLoadPreviouslyOpenedProfile = @new);
    }

    public PlainTextLineRegexTemplatesAutoGridBuilder PlainTextLogCodecLineRegexTemplatesBuilder { get; }
    public ObservableCollection<RegexValueViewModel> PlainTextLogCodecLineRegexTemplates { get; } = new();
    public IActionCommand AddPlainTextLogCodecLineRegexTemplateCommand { get; }
}
