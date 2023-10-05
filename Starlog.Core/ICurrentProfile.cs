using System.Reactive;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core;

public interface ICurrentProfile
{
    /// <summary>
    ///   Closes down currently loaded profile.
    /// </summary>
    void CloseProfile();

    /// <summary>
    ///   Loads a specified <paramref name="profile"/>.
    /// </summary>
    /// <param name="profile">The profile to load.</param>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task LoadProfileAsync(Profile profile);

    Profile? Profile { get; }
    IObservable<Unit> ProfileClosed { get; }
    IObservable<Profile?> ProfileChanged { get; }
}
