using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Io;

namespace Genius.Starlog.Core;

public interface IDirectoryMonitor : IDisposable
{
    void StartMonitoring(string path);
    void StopMonitoring();
    IObservable<long> Pulse { get; }
}

internal sealed class DirectoryMonitor : IDirectoryMonitor
{
    private const int DELAY_MS = 5000;

    private readonly IFileService _fileService;
    private readonly Subject<long> _pulse = new();
    private int _interrupted;
    private Thread? _thread;

    public DirectoryMonitor(IFileService fileService)
    {
        _fileService = fileService.NotNull();
    }

    public void StartMonitoring(string path)
    {
        Interlocked.Exchange(ref _interrupted, 0);

        if (_thread is not null)
        {
            throw new InvalidOperationException("Monitoring has already been started.");
        }

        _thread = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(DELAY_MS);

                if (Interlocked.CompareExchange(ref _interrupted, 0, 0) == 0)
                {
                    var dirSize = _fileService.GetDirectorySize(path, true);
                    _pulse.OnNext(dirSize);
                }

                break;
            }
        });
        _thread.Start();
    }

    public void StopMonitoring()
    {
        Interlocked.Exchange(ref _interrupted, 1);
        _thread?.Interrupt();
        _thread = null;
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    public IObservable<long> Pulse => _pulse;
}
