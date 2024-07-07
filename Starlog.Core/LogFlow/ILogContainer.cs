using System.Collections.Immutable;

namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   The container of all log sources for the selected profile.
/// </summary>
public interface ILogContainer
{
    /// <summary>
    ///  Returns a container of the fields.
    /// </summary>
    ILogFieldsContainerReadonly GetFields();

    /// <summary>
    ///   Returns all the logs, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LogRecord> GetLogs();

    /// <summary>
    ///   Returns all unique log levels, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LogLevelRecord> GetLogLevels();

    /// <summary>
    ///   Returns a source by its name. If not found, returns <c>null</c>.
    /// </summary>
    /// <param name="name">The name of the source.</param>
    LogSourceBase? GetSource(string name);

    /// <summary>
    ///   Returns all the log sources, read so far for the selected profile.
    /// </summary>
    ImmutableArray<LogSourceBase> GetSources();

    /// <summary>
    ///   An observable to handle an event when a new source is read.
    /// </summary>
    IObservable<LogSourceBase> SourceAdded { get; }

    /// <summary>
    ///   An observable to handle an event when an existing source has been renamed.
    /// </summary>
    IObservable<(LogSourceBase OldRecord, LogSourceBase NewRecord)> SourceRenamed { get; }

    /// <summary>
    ///   An observable to handle an event when a source has been removed.
    /// </summary>
    IObservable<LogSourceBase> SourceRemoved { get; }

    /// <summary>
    ///   An observable to handle events when sources count has changed.
    /// </summary>
    IObservable<int> SourcesCountChanged { get; }

    /// <summary>
    ///   An observable to handle an event when a bunch of log records are added.
    /// </summary>
    IObservable<ImmutableArray<LogRecord>> LogsAdded { get; }

    /// <summary>
    ///   An observable to handle an event when a bunch of log records are removed.
    /// </summary>
    IObservable<ImmutableArray<LogRecord>> LogsRemoved { get; }

    /// <summary>
    ///   Gets the count of the currently loaded sources.
    /// </summary>
    int SourcesCount { get; }
}
