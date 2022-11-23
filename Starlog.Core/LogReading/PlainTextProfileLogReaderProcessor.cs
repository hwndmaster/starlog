using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public sealed class PlainTextProfileLogReaderProcessor : ILogReaderProcessor
{
    public async Task<IEnumerable<LogRecord>> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream)
    {
        using var reader = new StreamReader(stream);
        var readerSettings = (PlainTextProfileLogReader)profile.LogReader;

        var fileArtifacts = await ReadArtifactsAsync(profile, reader);

        // TODO: Move regex to Profile.LogReader (PlainTextProfileLogReader) settings.
        Regex regex = new(@"(?<level>\w+)\s(?<datetime>[\d\-:\.]+\s[\d\-:\.]+)\s\[(?<thread>\w)+\]\s(?<logger>\w+)\s-\s(?<message>.+)");

        Dictionary<int, LoggerRecord> loggers = new();
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

            lastRecord = new LogRecord(dateTime, ParseLogLevel(level), thread, fileRecord, fileArtifacts, loggerRecord, message, null);
        }

        return records;

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

    private async Task<FileArtifacts> ReadArtifactsAsync(Profile profile, StreamReader reader)
    {
        if (profile.FileArtifactLinesCount == 0)
        {
            return new FileArtifacts(Array.Empty<string>());
        }

        List<string> artifacts = new();
        for (var i = 0; i < profile.FileArtifactLinesCount; i++)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null) break;

            artifacts.Add(line);
        }

        return new FileArtifacts(artifacts.ToArray());
    }

    private static LogLevel ParseLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "trace" or "statistics" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warn" => LogLevel.Warn,
            "error" => LogLevel.Error,
            "fatal" => LogLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException($"Cannot parse log level: {level}")
        };
    }
}
