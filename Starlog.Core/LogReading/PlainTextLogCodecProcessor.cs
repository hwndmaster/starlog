using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.LogReading;

public sealed class PlainTextLogCodecProcessor : ILogCodecProcessor
{
    private readonly ISettingsQueryService _settingsQuery;

    public PlainTextLogCodecProcessor(ISettingsQueryService settingsQuery)
    {
        _settingsQuery = settingsQuery.NotNull();
    }

    public async Task<LogReadingResult> ReadAsync(Profile profile, FileRecord fileRecord, Stream stream, LogReadingSettings settings)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var fileArtifacts = settings.ReadFileArtifacts ? await ReadFileArtifactsAsync(profile.Settings, reader) : null;

        if (reader.EndOfStream)
        {
            return LogReadingResult.Empty;
        }

        var logCodecSettings = (PlainTextProfileLogCodec)profile.Settings.LogCodec;
        if (string.IsNullOrEmpty(logCodecSettings.LineRegex))
        {
            throw new InvalidOperationException("Cannot open profile with empty LineRegex setting.");
        }
        Regex regex = new(logCodecSettings.LineRegex);

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
                    return LogReadingResult.Empty;
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
                profile.Settings.DateTimeFormat,
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

        return new LogReadingResult(fileArtifacts, records.ToImmutableArray(), loggers.Values, logLevels.Values);

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

    private static async Task<FileArtifacts> ReadFileArtifactsAsync(ProfileSettings settings, StreamReader reader)
    {
        if (settings.FileArtifactLinesCount == 0)
        {
            return new FileArtifacts(Array.Empty<string>());
        }

        List<string> artifacts = new();
        for (var i = 0; i < settings.FileArtifactLinesCount; i++)
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

    public bool ReadFromCommandLineArguments(ProfileLogCodecBase profileLogCodec, string[] codecSettings)
    {
        Guard.NotNull(profileLogCodec);

        if (codecSettings is null || codecSettings.Length == 0)
        {
            return false;
        }

        var logCodecSettings = (PlainTextProfileLogCodec)profileLogCodec;

        var settings = _settingsQuery.Get();
        var template = settings.PlainTextLogCodecLineRegexes.FirstOrDefault(x => x.Name.Equals(codecSettings[0], StringComparison.OrdinalIgnoreCase));
        if (template is null)
            return false;

        logCodecSettings.LineRegex = template.Value;
        return true;
    }
}
