using System.Reactive.Subjects;
using Genius.Atom.Infrastructure;

namespace Genius.Starlog.Core.Tests;

internal sealed class TestDirectoryMonitor : IDirectoryMonitor
{
    public bool MonitoringStarted { get; private set; }
    private readonly Subject<long> _pulse = new();

    public void TriggerPulse(long newValue)
    {
        _pulse.OnNext(newValue);
    }

    public IDisposable StartMonitoring(string path, string searchPattern)
    {
        MonitoringStarted = true;

        return new DisposableAction(() => StopMonitoring());
    }

    public void StopMonitoring()
    {
        MonitoringStarted = false;
    }

    public void Dispose()
    {
        StopMonitoring();
        _pulse.Dispose();
    }

    public IObservable<long> Pulse => _pulse;
}
