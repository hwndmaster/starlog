using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.UI.ViewModels;

public interface ISettingsViewModel : ITabViewModel
{ }

internal sealed class SettingsViewModel : TabViewModelBase, ISettingsViewModel
{
    private Settings _model;

    public SettingsViewModel(ICommandBus commandBus, ISettingsRepository repo, IEventBus eventBus)
    {
        Guard.NotNull(commandBus);

        _model = repo.NotNull().Get();
        Reconcile();

        eventBus.WhenFired<SettingsUpdatedEvent>().Subscribe(@event =>
        {
            _model = @event.Settings;
            Reconcile();
        });

        this.WhenAnyChanged().Subscribe(async _ =>
            await commandBus.SendAsync(new SettingsUpdateCommand(_model)));
    }

    private void Reconcile()
    {
        AutoLoadPreviouslyOpenedProfile = _model.AutoLoadPreviouslyOpenedProfile;
    }

    public bool AutoLoadPreviouslyOpenedProfile
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value, (_, @new) => _model.AutoLoadPreviouslyOpenedProfile = @new);
    }
}
