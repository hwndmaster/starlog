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

    /// <summary>
    ///   Gets the currently loaded profile.
    /// </summary>
    /// <value></value>
    Profile? Profile { get; }

    /// <summary>
    ///   Raised when the current profile has been closed.
    /// </summary>
    IObservable<Unit> ProfileClosed { get; }

    /// <summary>
    ///   Raised when the current profile has been changed.
    /// </summary>
    IObservable<Profile?> ProfileChanged { get; }

    /// <summary>
    ///   Raised when an unknown type of changes were detected by the path of the current profile.
    /// </summary>
    IObservable<Unit> UnknownChangesDetected { get; }
}
