using System.Reactive;
using System.Reactive.Subjects;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.TestingUtil;

public sealed class TestCurrentProfile : ICurrentProfile
{
    private readonly Subject<Unit> _profileClosedSubject = new();
    private readonly Subject<Profile> _profileChangedSubject = new();

    public void CloseProfile()
    {
        Profile = null;
        _profileClosedSubject.OnNext(Unit.Default);
    }

    public Task LoadProfileAsync(Profile profile)
    {
        Profile = profile;
        _profileChangedSubject.OnNext(profile);
        return Task.CompletedTask;
    }

    public Profile? Profile { get; private set; }
    public IObservable<Unit> ProfileClosed => _profileClosedSubject;
    public IObservable<Profile?> ProfileChanged => _profileChangedSubject;
    public IObservable<Unit> UnknownChangesDetected => throw new NotImplementedException();
}
