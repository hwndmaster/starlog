using System.Collections.Immutable;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.LogReading;

internal sealed class PlainTextLogCodecProcessor : ILogCodecProcessor, ILogCodecSettingsReader
{
    private readonly ISettingsQueryService _settingsQuery;
    private readonly ILogger<PlainTextLogCodecLineMaskPatternParser> _plainTextLogCodecLineMaskPatternParserLogger;

    public PlainTextLogCodecProcessor(ISettingsQueryService settingsQuery, ILogger<PlainTextLogCodecLineMaskPatternParser> plainTextLogCodecLineMaskPatternParserLogger)
    {
        _settingsQuery = settingsQuery.NotNull();
        _plainTextLogCodecLineMaskPatternParserLogger = plainTextLogCodecLineMaskPatternParserLogger.NotNull();
    }

    public async Task<LogReadingResult> ReadAsync(Profile profile, LogSourceBase source, Stream stream, LogReadingSettings settings)
    {
        var profileSettings = (PlainTextProfileSettings)profile.Settings;

        using var reader = new StreamReader(stream, leaveOpen: true);
        var fileArtifacts = settings.ReadSourceArtifacts ? await ReadFileArtifactsAsync(profileSettings, reader) : null;

        if (reader.EndOfStream)
        {
            return LogReadingResult.Empty;
        }

        var patternValue = _settingsQuery.Get().PlainTextLogCodecLinePatterns.FirstOrDefault(x => x.Id == profileSettings.LinePatternId);
        if (patternValue is null)
        {
            throw new InvalidOperationException("Profile Pattern couldn't not be found: " + profileSettings.LinePatternId);
        }

        // TODO: Cover switch with unit tests
        IPlainTextLogCodecLineParser lineParser = patternValue.Type switch
        {
            PatternType.RegularExpression => new PlainTextLogCodecLineRegexParser(patternValue.Pattern),
            PatternType.MaskPattern => new PlainTextLogCodecLineMaskPatternParser(profileSettings.DateTimeFormat, patternValue.Pattern, _plainTextLogCodecLineMaskPatternParserLogger),
            _ => throw new NotSupportedException($"Pattern type '{patternValue.Type}' is not supported.")
        };

        Dictionary<int, LoggerRecord> loggers = new();
        Dictionary<int, LogLevelRecord> logLevels = new();
        List<LogRecord> records = new();

        LogRecord? lastRecord = null;
        List<string> lastRecordArtifacts = new();
        List<string> errors = new();

        while (true)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                FlushRecentRecord();
                break;
            }

            var match = lineParser.Parse(line);
            if (match is null)
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

            var level = match.Value.Level;
            var dateTimeParsed = DateTimeOffset.TryParseExact(match.Value.DateTime,
                profileSettings.DateTimeFormat,
                Thread.CurrentThread.CurrentCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var dateTime);
            if (!dateTimeParsed)
            {
                errors.Add("Could not parse datetime: " + match.Value.DateTime);
                continue;
            }
            var thread = match.Value.Thread;
            var logger = match.Value.Logger;
            var message = match.Value.Message;

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

            lastRecord = new LogRecord(dateTime, logLevelRecord, thread, source, loggerRecord, message, null);
        }

        return new LogReadingResult(fileArtifacts, records.ToImmutableArray(), loggers.Values, logLevels.Values, errors);

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

    public bool ReadFromCommandLineArguments(ProfileSettingsBase profileSettings, string[] codecSettings)
    {
        Guard.NotNull(profileSettings);

        if (codecSettings is null || codecSettings.Length == 0)
        {
            return false;
        }

        var plainTextSettings = (PlainTextProfileSettings)profileSettings;

        var settings = _settingsQuery.Get();
        var pattern = settings.PlainTextLogCodecLinePatterns.FirstOrDefault(x => x.Name.Equals(codecSettings[0], StringComparison.OrdinalIgnoreCase));
        if (pattern is null)
            return false;

        plainTextSettings.LinePatternId = pattern.Id;
        return true;
    }

    public bool MayContainSourceArtifacts(ProfileSettingsBase profileSettings)
    {
        if (profileSettings is not PlainTextProfileSettings plainTextProfileSettings)
            return false;

        return plainTextProfileSettings.FileArtifactLinesCount > 0;
    }

    private static async Task<SourceArtifacts> ReadFileArtifactsAsync(PlainTextProfileSettings settings, StreamReader reader)
    {
        if (settings.FileArtifactLinesCount == 0)
        {
            return new SourceArtifacts(Array.Empty<string>());
        }

        List<string> artifacts = new();
        for (var i = 0; i < settings.FileArtifactLinesCount; i++)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                // File has insufficient number of lines, skipping.
                return new SourceArtifacts(Array.Empty<string>());
            }

            artifacts.Add(line);
        }

        return new SourceArtifacts(artifacts.ToArray());
    }
}
