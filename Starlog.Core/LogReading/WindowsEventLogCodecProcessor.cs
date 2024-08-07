using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1416 // Validate platform compatibility: We're running only on Windows

namespace Genius.Starlog.Core.LogReading;

internal sealed class WindowsEventLogCodecProcessor : ILogCodecProcessor
{
    private readonly ILogger<WindowsEventLogCodecProcessor> _logger;

    public WindowsEventLogCodecProcessor(ILogger<WindowsEventLogCodecProcessor> logger)
    {
        _logger = logger.NotNull();
    }

    public bool MayContainSourceArtifacts(ProfileSettingsBase profileSettings)
    {
        return false;
    }

    public Task<LogReadingResult> ReadAsync(Profile profile, LogSourceBase source, Stream stream, LogReadingSettings settings, ILogFieldsContainer fields)
    {
        var profileSettings = (WindowsEventProfileSettings)profile.Settings;

        List<LogRecord> records = [];
        Dictionary<LogLevelHash, LogLevelRecord> logLevels = [];
        List<string> errors = [];

        var eventsQuery = new EventLogQuery(source.Name, PathType.LogName)
        {
            ReverseDirection = true
        };

        try
        {
            using var logReader = new EventLogReader(eventsQuery);

            EventRecord entry;
            while ((entry = logReader.ReadEvent()) is not null)
            {
                // Level
                var logLevelHash = (int)(entry.Level ?? 0);
                if (!logLevels.TryGetValue(logLevelHash, out var logLevelRecord))
                {
                    var logLevelName = ExtractLogLevelName(entry.Level);
                    logLevelRecord = new LogLevelRecord(logLevelHash, logLevelName);
                    logLevels.Add(logLevelHash, logLevelRecord);
                }

                // Message and artifacts
                var message = entry.FormatDescription() ?? string.Empty;
                string? logArtifacts = null;
                if (message.Length > 80)
                {
                    var newLineIndex = message.IndexOfAny(['\r', '\n']);
                    if (newLineIndex > 0)
                    {
                        logArtifacts = message;
                        message = message[0..newLineIndex];
                    }
                }

                // Other fields
                var fieldValueIndices = new int[4];

                var fieldId = fields.GetOrAddFieldId("Event ID");
                fieldValueIndices[fieldId] = fields.AddFieldValue(fieldId, entry.Id.ToString(CultureInfo.CurrentCulture));

                fieldId = fields.GetOrAddFieldId("Provider");
                fieldValueIndices[fieldId] = fields.AddFieldValue(fieldId, entry.ProviderName);

                if (entry.UserId is not null)
                {
                    fieldId = fields.GetOrAddFieldId("User ID");
                    fieldValueIndices[fieldId] = fields.AddFieldValue(fieldId, entry.UserId.Value);
                }

                if (entry.ProcessId is not null)
                {
                    fieldId = fields.GetOrAddFieldId("Process ID");
                    fieldValueIndices[fieldId] = fields.AddFieldValue(fieldId, entry.ProcessId.Value.ToString(CultureInfo.CurrentCulture));
                }

                // Create record
                var record = new LogRecord(
                    new DateTimeOffset(entry.TimeCreated ?? DateTime.MinValue),
                    logLevelRecord,
                    source,
                    fieldValueIndices.ToImmutableArray(),
                    message,
                    logArtifacts);
                records.Add(record);

                if (records.Count == profileSettings.SelectCount)
                {
                    break;
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            var errorMessage = $"Error reading source '{source.Name}' in profile '{profile.Name}'";
            errors.Add(errorMessage);
            _logger.LogError(ex, errorMessage);
        }
        return Task.FromResult(new LogReadingResult(null, records.ToImmutableArray(), fields, logLevels.Values, errors));
    }

    public bool ReadFromCommandLineArguments(ProfileSettingsBase profileSettings, string[] codecSettings)
    {
        return false;
    }

    private static string ExtractLogLevelName(byte? level)
    {
        // Mapping reference: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.eventing.reader.standardeventlevel
        return level switch
        {
            1 => "Critical",
            2 => "Error",
            3 => "Warning",
            4 => "Info",
            5 => "Verbose",
            _ => "Unknown"
        };
    }
}
