using System.Reactive;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core;

public interface ICurrentProfile
{
    /// <summary>
    ///   Closes down currently loaded profile.
    /// </summary>
    void CloseProfile();

    Profile? Profile { get; }
    IObservable<Unit> ProfileClosed { get; }
    IObservable<Profile?> ProfileChanged { get; }
}
