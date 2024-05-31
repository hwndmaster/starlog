using System.Reactive;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.ProfileLoading;

namespace Genius.Starlog.Core.LogFlow;

internal sealed class CurrentProfileLogContainer : LogContainer, ICurrentProfile
{
    private readonly IEventBus _eventBus;
    private readonly IProfileLoaderFactory _profileLoaderFactory;
    private readonly Subject<Unit> _profileClosed = new();
    private readonly Subject<Profile> _profileChanged = new();
    private readonly Subject<Unit> _unknownChangesDetected = new();
    private IDisposable? _profileDisposable;

    public CurrentProfileLogContainer(
        IEventBus eventBus,
        IProfileLoaderFactory profileLoaderFactory)
    {
        _eventBus = eventBus.NotNull();
        _profileLoaderFactory = profileLoaderFactory.NotNull();
    }

    public async Task LoadProfileAsync(Profile profile)
    {
        CloseProfile();

        var profileLoader = _profileLoaderFactory.Create(profile);
        if (profileLoader is null)
        {
            _eventBus.Publish(new ProfileLoadingErrorEvent(profile, $"Couldn't load profile."));
            return;
        }

        try
        {
            var profileState = await profileLoader.LoadProfileAsync(profile, this).ConfigureAwait(false);
            Profile = profile.NotNull();
            _profileDisposable = profileLoader.StartProfileMonitoring(profileState, this, _unknownChangesDetected);
            _profileChanged.OnNext(Profile);
        }
        catch (Exception ex)
        {
            _eventBus.Publish(new ProfileLoadingErrorEvent(profile, $"Couldn't load profile: " + ex.Message));
        }
    }

    public void CloseProfile()
    {
        _profileDisposable?.Dispose();
        Profile = null;
        Clear();
        _profileClosed.OnNext(Unit.Default);
    }

    protected override void Dispose(bool disposing)
    {
        _profileDisposable?.Dispose();
        _profileClosed.Dispose();
        _profileChanged.Dispose();
        _unknownChangesDetected.Dispose();

        base.Dispose(disposing);
    }

    public Profile? Profile { get; private set; }
    public IObservable<Unit> ProfileClosed => _profileClosed;
    public IObservable<Profile> ProfileChanged => _profileChanged;
    public IObservable<Unit> UnknownChangesDetected => _unknownChangesDetected;
}
