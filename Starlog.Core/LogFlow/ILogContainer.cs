using System.Collections.Immutable;

namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   The container of all log files for the selected profile.
/// </summary>
public interface ILogContainer
{
    /// <summary>
    ///   Returns all the files, read so far for the selected profile.
    /// </summary>
    ImmutableArray<FileRecord> GetFiles();

    /// <summary>
    ///   Returns all the logs, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LogRecord> GetLogs();

    /// <summary>
    ///   Returns all unique loggers, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LoggerRecord> GetLoggers();

    /// <summary>
    ///   Returns all unique log levels, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LogLevelRecord> GetLogLevels();

    /// <summary>
    ///   Returns all unique threads, read so far for the selected profile.
    /// </summary>
    ImmutableArray<string> GetThreads();

    /// <summary>
    ///   An observable to handle an event when a new file is read.
    /// </summary>
    IObservable<FileRecord> FileAdded { get; }

    /// <summary>
    ///   An observable to handle an event when an existing file has been renamed.
    /// </summary>
    IObservable<(FileRecord OldRecord, FileRecord NewRecord)> FileRenamed { get; }

    /// <summary>
    ///   An observable to handle an event when a file has been removed.
    /// </summary>
    IObservable<FileRecord> FileRemoved { get; }

    /// <summary>
    ///   An observable to handle an event when a bunch of log records are added.
    /// </summary>
    IObservable<ImmutableArray<LogRecord>> LogsAdded { get; }

    /// <summary>
    ///   An observable to handle an event when a bunch of log records are removed.
    /// </summary>
    IObservable<ImmutableArray<LogRecord>> LogsRemoved { get; }
}
