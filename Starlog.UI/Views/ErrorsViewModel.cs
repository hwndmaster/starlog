using System.Reactive.Linq;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core;
using Genius.Starlog.Core.Messages;

namespace Genius.Starlog.UI.Views;

public interface IErrorsViewModel : IViewModel
{
    bool HasAnyError { get; }
    bool IsErrorsFlyoutVisible { get; set; }
}

public sealed class ErrorsViewModel : ViewModelBase, IErrorsViewModel, IDisposable
{
    private readonly Disposer _disposer;

    public ErrorsViewModel(ICurrentProfile currentProfile, IEventBus eventBus)
    {
        Guard.NotNull(currentProfile);
        Guard.NotNull(eventBus);

        // Member initialization:
        _disposer = new Disposer();

        // Actions:
        ClearCommand = new ActionCommand(_ =>
        {
            Errors = string.Empty;
            IsErrorsFlyoutVisible = false;
        });

        // Subscriptions:
        currentProfile.ProfileClosed.Subscribe(x =>
            ClearCommand.Execute(null))
            .DisposeWith(_disposer);
        eventBus.WhenFired<ProfileLoadingErrorEvent>()
            .Subscribe(args =>
            {
                IsErrorsFlyoutVisible = true;

                Errors += (Errors.Length > 0 ? Environment.NewLine : string.Empty) + args.Reason;
            })
            .DisposeWith(_disposer);
        this.WhenChanged(x => x.Errors)
            .Subscribe(_ => HasAnyError = !string.IsNullOrEmpty(Errors));
    }

    public void Dispose()
    {
        _disposer.Dispose();
    }

    public string Errors
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    public bool HasAnyError
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsErrorsFlyoutVisible
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand ClearCommand { get; }
}
