using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.ProfileLoading;

namespace Genius.Starlog.Core.Comparison;

public interface IComparisonService
{
    Task<ComparisonContext?> LoadProfilesAsync(Profile profile1, Profile profile2);
}

internal sealed partial class ComparisonService : IComparisonService
{
    private const int LOOK_AHEAD_THRESHOLD_MS = 500;
    private static readonly TimeSpan LookAheadThreshold = TimeSpan.FromMilliseconds(LOOK_AHEAD_THRESHOLD_MS);

    private readonly IProfileLoaderFactory _profileLoaderFactory;

    public ComparisonService(IProfileLoaderFactory profileLoaderFactory)
    {
        _profileLoaderFactory = profileLoaderFactory.NotNull();
    }

    public async Task<ComparisonContext?> LoadProfilesAsync(Profile profile1, Profile profile2)
    {
        var logContainer1 = new LogContainer();
        var logContainer2 = new LogContainer();

        var profileLoader1 = _profileLoaderFactory.Create(profile1);
        var profileLoader2 = _profileLoaderFactory.Create(profile2);

        if (profileLoader1 is null || profileLoader2 is null)
        {
            throw new NotImplementedException();
            // TODO: return ComparisonContext.WithError();
        }

        var taskProfileLoading1 = profileLoader1.LoadProfileAsync(profile1, logContainer1);
        var taskProfileLoading2 = profileLoader2.LoadProfileAsync(profile2, logContainer2);

        var results = await Task.WhenAll(taskProfileLoading1, taskProfileLoading2).ConfigureAwait(false);

        if (results.Any(x => x is null))
        {
            return null;
        }

        var logContext1 = CalculateLogRecordHashes(logContainer1);
        var logContext2 = CalculateLogRecordHashes(logContainer2);

        int lastIndexJ = 0;
        List<ComparisonRecord> resultingRecords = new(logContext1.Records.Length + logContext2.Records.Length);

        for (var i = 0; i < logContext1.Records.Length; i++)
        {
            var record1 = logContext1.Records[i];
            var refTimeDiff1 = i == logContext1.Records.Length - 1 ? TimeSpan.Zero : logContext1.Records[i + 1].Record.DateTime - record1.Record.DateTime;
            var refTime2 = lastIndexJ < logContext2.Records.Length ? logContext2.Records[lastIndexJ].Record.DateTime : DateTimeOffset.MinValue;
            var added = false;

            for (var j = lastIndexJ; j < logContext2.Records.Length; j++)
            {
                var record2 = logContext2.Records[j];
                var refTimeDiff2 = record2.Record.DateTime - refTime2;

                if (refTimeDiff2 > refTimeDiff1 + LookAheadThreshold)
                {
                    // Went too far, breaking here.
                    break;
                }

                if (record1.Hash == record2.Hash)
                {
                    // Fully matching record found.
                    for (var j0 = lastIndexJ; j0 < j; j0++)
                    {
                        resultingRecords.Add(new ComparisonRecord(null, logContext2.Records[j0].Record));
                    }

                    lastIndexJ = j + 1;
                    resultingRecords.Add(new ComparisonRecord(record1.Record, record2.Record));
                    added = true;
                    break;
                }

                /*else if (i != logContext1.Records.Length - 1 && logContext1.Records[i + 1].Hash == record2.Hash)
                {
                    var comparisonRecord = new ComparisonRecord(logContext1.Records[i + 1].Record, record2.Record);
                    lastIndexJ = ..;
                    // TODO: ...
                }*/
            }

            if (!added)
            {
                resultingRecords.Add(new ComparisonRecord(record1.Record, null));
            }
        }

        for (var j = lastIndexJ; j < logContext2.Records.Length; j++)
        {
            resultingRecords.Add(new ComparisonRecord(null, logContext2.Records[j].Record));
        }

        return new ComparisonContext(profile1, logContainer1, profile2, logContainer2, resultingRecords.ToImmutableArray());
    }

    private LogComparisonContext CalculateLogRecordHashes(LogContainer logContainer)
    {
        var files = logContainer.GetSources().ToDictionary(
            x => x.DisplayName.GetHashCode(),
            x => DigitsAndNonCharRegex().Replace(x.DisplayName, (m) => ""));
        var logs = logContainer.GetLogs();
        var initialTime = logs[0].DateTime;
        var logsCalculated = logContainer.GetLogs().Select(x => new LogRecordWithHash(x, CalculateHash(x, files))).ToImmutableArray();
        return new LogComparisonContext(logContainer, initialTime, logsCalculated);
    }

    private static int CalculateHash(LogRecord logRecord, Dictionary<int, string> filesDict)
    {
        var fileName = filesDict[logRecord.Source.DisplayName.GetHashCode()];

        // TODO: Make trimming optional
        var messageTrimmed = DigitsAndNonCharRegex().Replace(logRecord.Message, (m) => "");

        return HashCode.Combine(
            fileName,
            logRecord.Level.Id,
            // TODO: logRecord.Logger.Id,
            messageTrimmed
        );
    }

    [GeneratedRegex("[\\d\\W]")]
    private static partial Regex DigitsAndNonCharRegex();

    private sealed record LogComparisonContext(LogContainer Container, DateTimeOffset InitialTime, ImmutableArray<LogRecordWithHash> Records);
    private readonly record struct LogRecordWithHash(LogRecord Record, int Hash);
}
