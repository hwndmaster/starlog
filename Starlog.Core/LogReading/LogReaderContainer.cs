using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public interface ILogReaderContainer
{
    ProfileLogReadBase CreateProfileLogReader(LogReader logReader);
    ILogReaderProcessor CreateLogReaderProcessor(ProfileLogReadBase profileLogReader);
    IEnumerable<LogReader> GetLogReaders();
    void RegisterLogReader<TProfileLogReader, TLogReader>(LogReader logReader)
        where TProfileLogReader : ProfileLogReadBase
        where TLogReader : class, ILogReaderProcessor;
}

internal sealed class LogReaderContainer : ILogReaderContainer
{
    private readonly record struct LogReaderRecord(LogReader LogReader, Type ProfileLogReaderType, Type LogReaderProcessorType);


    private readonly Dictionary<Guid /* LogReader.Id */, LogReaderRecord> _registeredLogReaders = new();

    private readonly ILogReaderProcessor[] _logReaderProcessors;

    public LogReaderContainer(IEnumerable<ILogReaderProcessor> LogReaderProcessors)
    {
        _logReaderProcessors = LogReaderProcessors.ToArray();
    }

    public ProfileLogReadBase CreateProfileLogReader(LogReader logReader)
    {
        if (!_registeredLogReaders.TryGetValue(logReader.Id, out var value))
        {
            throw new InvalidOperationException("The log reader '" + logReader.Id + "' doesn't exists.");
        }

        return (ProfileLogReadBase)Activator.CreateInstance(value.ProfileLogReaderType, logReader).NotNull();
    }

    public ILogReaderProcessor CreateLogReaderProcessor(ProfileLogReadBase profileLogReader)
    {
        if (!_registeredLogReaders.TryGetValue(profileLogReader.LogReader.Id, out var value))
        {
            throw new InvalidOperationException("The log reader '" + profileLogReader.LogReader.Id + "' doesn't exists.");
        }

        return _logReaderProcessors.First(x => x.GetType() == value.LogReaderProcessorType);
    }

    public IEnumerable<LogReader> GetLogReaders()
    {
        return _registeredLogReaders.Select(x => x.Value.LogReader);
    }

    public void RegisterLogReader<TProfileLogReader, TLogReaderProcessor>(LogReader logReader)
        where TProfileLogReader : ProfileLogReadBase
        where TLogReaderProcessor : class, ILogReaderProcessor
    {
        if (_registeredLogReaders.ContainsKey(logReader.Id))
        {
            throw new InvalidOperationException("The log reader '" + logReader.Id + "' already exists.");
        }

        _registeredLogReaders.Add(logReader.Id, new LogReaderRecord(logReader, typeof(TProfileLogReader), typeof(TLogReaderProcessor)));
    }
}
