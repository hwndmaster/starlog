using Genius.Atom.Infrastructure.Io;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.LogFlow;

internal interface IProfileLoader
{
    Task<bool> LoadProfileAsync(Profile profile, ILogContainerWriter logContainer);
    Task<bool> LoadFileAsync(Profile profile, string file, ILogContainerWriter logContainer);
    Task<bool> ReadLogsAsync(Profile profile, Stream stream, FileRecord fileRecord, ILogContainerWriter logContainer);
}

// TODO: Cover with unit tests
internal sealed class ProfileLoader : IProfileLoader
{
    private readonly IFileService _fileService;
    private readonly ILogCodecContainer _logCodecContainer;
    private readonly ILogger<ProfileLoader> _logger;

    public ProfileLoader(
        IFileService fileService,
        ILogCodecContainer logCodecContainer,
        ILogger<ProfileLoader> logger)
    {
        _logCodecContainer = logCodecContainer.NotNull();
        _logger = logger.NotNull();
        _fileService = fileService.NotNull();
    }

    public async Task<bool> LoadProfileAsync(Profile profile, ILogContainerWriter logContainer)
    {
        Guard.NotNull(profile);
        Guard.NotNull(logContainer);

        _logger.LogDebug("Loading profile: {profileId}", profile.Id);

        if (!_fileService.PathExists(profile.Path))
            return false;

        var isFile = !_fileService.IsDirectory(profile.Path);

        IEnumerable<string> files;
        if (isFile)
        {
            files = profile.Path.Split('|').ToArray();
        }
        else
        {
            files = _fileService.EnumerateFiles(profile.Path, "*.*", new EnumerationOptions());
        }
        var tasks = files.Select(async file => await LoadFileAsync(profile, file, logContainer));
        await Task.WhenAll(tasks);

        return true;
    }

    public async Task<bool> LoadFileAsync(Profile profile, string file, ILogContainerWriter logContainer)
    {
        if (!_fileService.FileExists(file))
        {
            return false;
        }

        Guard.NotNull(profile);
        Guard.NotNull(logContainer);

        using var fileStream = _fileService.OpenReadNoLock(file);
        var fileRecord = new FileRecord(file, 0);

        await ReadLogsAsync(profile, fileStream, fileRecord, logContainer);

        logContainer.AddFile(fileRecord);

        return true;
    }

    public async Task<bool> ReadLogsAsync(Profile profile, Stream stream, FileRecord fileRecord, ILogContainerWriter logContainer)
    {
        Guard.NotNull(profile);
        Guard.NotNull(stream);
        Guard.NotNull(fileRecord);
        Guard.NotNull(logContainer);

        var tp = TracePerf.Start<ProfileLoader>(nameof(ReadLogsAsync));

        var logCodecProcessor = _logCodecContainer.CreateLogCodecProcessor(profile.Settings.LogCodec);

        var settings = new LogReadingSettings(
            ReadFileArtifacts: fileRecord.LastReadOffset == 0 && profile.Settings.FileArtifactLinesCount > 0
        );
        var logRecordResult = await logCodecProcessor.ReadAsync(profile, fileRecord, stream, settings);

        _logger.LogDebug("File {FileName} read {RecordsCount} logs", fileRecord.FileName, logRecordResult.Records.Length);

        fileRecord.LastReadOffset = stream.Length;

        if (settings.ReadFileArtifacts)
        {
            fileRecord.Artifacts = logRecordResult.FileArtifacts;
        }

        foreach (var logger in logRecordResult.Loggers)
        {
            logContainer.AddLogger(logger);
        }

        foreach (var logLevel in logRecordResult.LogLevels)
        {
            logContainer.AddLogLevel(logLevel);
        }

        foreach (var thread in logRecordResult.Records.Select(x => x.Thread))
        {
            logContainer.AddThread(thread);
        }

        logContainer.AddLogs(logRecordResult.Records);

        tp.StopAndReport();

        return true;
    }
}
