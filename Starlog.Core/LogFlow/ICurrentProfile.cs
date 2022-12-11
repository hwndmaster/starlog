using System.Reactive;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public interface ICurrentProfile
{
    Profile? Profile { get; }
    IObservable<Unit> ProfileChanging { get; }
    IObservable<Profile?> ProfileChanged { get; }
}
