using System.Reactive;
using System.Reactive.Subjects;

namespace Genius.Starlog.Core.Tests;

internal sealed class TestDirectoryMonitor : IDirectoryMonitor
{
    public bool MonitoringStarted { get; private set; }
    private readonly Subject<long> _pulse = new();

    public void TriggerPulse(long newValue)
    {
        _pulse.OnNext(newValue);
    }

    public void StartMonitoring(string path)
    {
        MonitoringStarted = true;
    }

    public void StopMonitoring()
    {
        MonitoringStarted = false;
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    public IObservable<long> Pulse => _pulse;
}
