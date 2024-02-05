using System.Reactive.Subjects;
using System.Threading.Tasks.Dataflow;
using Genius.Atom.Infrastructure.Io;

namespace Genius.Starlog.Core;

public interface IDirectoryMonitor : IDisposable
{
    IDisposable StartMonitoring(string path, string searchPattern);
    void StopMonitoring();
    IObservable<long> Pulse { get; }
}

internal sealed class DirectoryMonitor : IDirectoryMonitor
{
    private const int DELAY_MS = 5000;

    private readonly IFileService _fileService;
    private readonly Subject<long> _pulse = new();
    private int _interrupted;
    private ITargetBlock<object>? _backgroundTask;
    private CancellationTokenSource? _cancellation;

    public DirectoryMonitor(IFileService fileService)
    {
        _fileService = fileService.NotNull();
    }

    public IDisposable StartMonitoring(string path, string searchPattern)
    {
        Interlocked.Exchange(ref _interrupted, 0);

        if (_backgroundTask is not null)
        {
            throw new InvalidOperationException("Monitoring has already been started.");
        }

        _cancellation = new CancellationTokenSource();

        _backgroundTask = CreateNeverEndingTask(now => DoWork(path, searchPattern), _cancellation.Token);
        _backgroundTask.Post(default);

        return new DisposableAction(() => StopMonitoring());
    }

    public void StopMonitoring()
    {
        Interlocked.Exchange(ref _interrupted, 1);
        _cancellation?.Cancel();
        _cancellation = null;
        _backgroundTask = null;
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    // Consider detaching from DirectoryMonitor
    private ITargetBlock<object?> CreateNeverEndingTask(
        Action<object?> action, CancellationToken cancellationToken)
    {
        Guard.NotNull(action);

        ActionBlock<object?>? block = null;

        block = new ActionBlock<object?>(async now => {
            action(now);
            await Task.Delay(TimeSpan.FromMilliseconds(DELAY_MS), cancellationToken).ConfigureAwait(false);
            block!.Post(default); // Continue the flow
        }, new ExecutionDataflowBlockOptions {
            CancellationToken = cancellationToken
        });

        // Return the block.
        return block;
    }

    private void DoWork(string path, string searchPattern)
    {
        if (Interlocked.CompareExchange(ref _interrupted, 0, 0) == 0)
        {
            var dirSize = _fileService.GetDirectorySize(path, searchPattern, true);
            _pulse.OnNext(dirSize);
        }
    }

    public IObservable<long> Pulse => _pulse;
}
