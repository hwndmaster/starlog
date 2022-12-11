using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public sealed class PlainTextProfileLogReaderProcessor : ILogReaderProcessor
{
    public async Task<LogReaderResult> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream)
    {
        using var reader = new StreamReader(stream);
        var readerSettings = (PlainTextProfileLogReader)profile.LogReader;
        Regex regex = new(readerSettings.LineRegex);

        var fileArtifacts = await ReadArtifactsAsync(profile, reader);
        if (reader.EndOfStream)
        {
            return LogReaderResult.Empty;
        }

        Dictionary<int, LoggerRecord> loggers = new();
        Dictionary<int, LogLevelRecord> logLevels = new();
        List<LogRecord> records = new();

        LogRecord? lastRecord = null;
        List<string> lastRecordArtifacts = new();

        while (true)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                FlushRecentRecord();
                break;
            }

            var match = regex.Match(line);
            if (!match.Success)
            {
                if (lastRecord is null)
                {
                    // Seems a wrong file format, better to stop processing it now
                    return LogReaderResult.Empty;
                }
                lastRecordArtifacts.Add(line);
                continue;
            }
            else
            {
                FlushRecentRecord();
            }

            var level = match.Groups["level"].Value;
            var dateTime = DateTimeOffset.ParseExact(match.Groups["datetime"].Value, "yyyy-MM-dd HH:mm:ss.fff", Thread.CurrentThread.CurrentCulture);
            var thread = match.Groups["thread"].Value;
            var logger = match.Groups["logger"].Value;
            var message = match.Groups["message"].Value;

            var loggerHash = logger.GetHashCode();
            if (!loggers.TryGetValue(loggerHash, out var loggerRecord))
            {
                loggerRecord = new LoggerRecord(loggerHash, logger);
                loggers.Add(loggerHash, loggerRecord);
            }

            var logLevelHash = level.GetHashCode();
            if (!logLevels.TryGetValue(logLevelHash, out var logLevelRecord))
            {
                logLevelRecord = new LogLevelRecord(logLevelHash, level, DetermineLogSeverity(level));
                logLevels.Add(logLevelHash, logLevelRecord);
            }

            lastRecord = new LogRecord(dateTime, logLevelRecord, thread, fileRecord, fileArtifacts, loggerRecord, message, null);
        }

        return new LogReaderResult(records.ToImmutableArray(), loggers.Values, logLevels.Values);

        void FlushRecentRecord()
        {
            if (lastRecord is not null)
            {
                if (lastRecordArtifacts.Count > 0)
                {
                    lastRecord = lastRecord.Value with { LogArtifacts = string.Join(Environment.NewLine, lastRecordArtifacts) };
                }
                records.Add(lastRecord.Value);
                lastRecord = null;
            }

            lastRecordArtifacts.Clear();
        }
    }

    private static LogSeverity DetermineLogSeverity(string logLevel)
    {
        return logLevel.ToLowerInvariant() switch
        {
            "debug" or "trace" or "statistics" => LogSeverity.Minor,
            "warn" or "warning" => LogSeverity.Warning,
            "err" or "error" or "exception" => LogSeverity.Major,
            "fatal" => LogSeverity.Critical,
            _ => LogSeverity.Normal
        };
    }

    private static async Task<FileArtifacts> ReadArtifactsAsync(Profile profile, StreamReader reader)
    {
        if (profile.FileArtifactLinesCount == 0)
        {
            return new FileArtifacts(Array.Empty<string>());
        }

        List<string> artifacts = new();
        for (var i = 0; i < profile.FileArtifactLinesCount; i++)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                // File has insufficient number of lines, skipping.
                return new FileArtifacts(Array.Empty<string>());
            }

            artifacts.Add(line);
        }

        return new FileArtifacts(artifacts.ToArray());
    }
}
