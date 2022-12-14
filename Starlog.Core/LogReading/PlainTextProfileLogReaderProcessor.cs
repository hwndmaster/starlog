using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public sealed class PlainTextProfileLogReaderProcessor : ILogReaderProcessor
{
    public async Task<LogReaderResult> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream, bool readFileArtifacts)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var fileArtifacts = readFileArtifacts ? await ReadFileArtifactsAsync(profile, reader) : null;

        if (reader.EndOfStream)
        {
            return LogReaderResult.Empty;
        }

        var readerSettings = (PlainTextProfileLogRead)profile.LogReader;
        if (string.IsNullOrEmpty(readerSettings.LineRegex))
        {
            throw new InvalidOperationException("Cannot open profile with empty LineRegex setting.");
        }
        Regex regex = new(readerSettings.LineRegex);

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
            var dateTime = DateTimeOffset.ParseExact(match.Groups["datetime"].Value,
                "yyyy-MM-dd HH:mm:ss.fff",
                Thread.CurrentThread.CurrentCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal);
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
                logLevelRecord = new LogLevelRecord(logLevelHash, level);
                logLevels.Add(logLevelHash, logLevelRecord);
            }

            lastRecord = new LogRecord(dateTime, logLevelRecord, thread, fileRecord, loggerRecord, message, null);
        }

        return new LogReaderResult(fileArtifacts, records.ToImmutableArray(), loggers.Values, logLevels.Values);

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

    private static async Task<FileArtifacts> ReadFileArtifactsAsync(Profile profile, StreamReader reader)
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
