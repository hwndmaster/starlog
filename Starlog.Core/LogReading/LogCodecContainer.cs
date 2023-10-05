using Genius.Atom.Infrastructure.Entities;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public interface ILogCodecContainer
{
    ProfileLogCodecBase CreateProfileLogCodec(LogCodec logCodec);
    ILogCodecProcessor CreateLogCodecProcessor(ProfileLogCodecBase profileLogCodec);
    IEnumerable<LogCodec> GetLogCodecs();
    void RegisterLogCodec<TProfileLogCodec, TLogCodec>(LogCodec logCodec)
        where TProfileLogCodec : ProfileLogCodecBase
        where TLogCodec : class, ILogCodecProcessor;
}

internal sealed class LogCodecContainer : ILogCodecContainer, IQueryService<LogCodec>
{
    private readonly record struct LogCodecRecord(LogCodec Codec, Type ProfileLogCodecType, Type ProcessorType);

    private readonly Dictionary<Guid /* LogCodec.Id */, LogCodecRecord> _registeredLogCodecs = new();

    private readonly Lazy<IEnumerable<ILogCodecProcessor>> _processors;

    public LogCodecContainer(Lazy<IEnumerable<ILogCodecProcessor>> logCodecProcessors)
    {
        _processors = logCodecProcessors;
    }

    public ProfileLogCodecBase CreateProfileLogCodec(LogCodec logCodec)
    {
        if (!_registeredLogCodecs.TryGetValue(logCodec.Id, out var value))
        {
            throw new InvalidOperationException("The log codec '" + logCodec.Id + "' doesn't exists.");
        }

        return (ProfileLogCodecBase)Activator.CreateInstance(value.ProfileLogCodecType, logCodec).NotNull();
    }

    public ILogCodecProcessor CreateLogCodecProcessor(ProfileLogCodecBase profileLogCodec)
    {
        if (!_registeredLogCodecs.TryGetValue(profileLogCodec.LogCodec.Id, out var value))
        {
            throw new InvalidOperationException("The log codec '" + profileLogCodec.LogCodec.Id + "' doesn't exists.");
        }

        return _processors.Value.First(x => x.GetType() == value.ProcessorType);
    }

    public Task<LogCodec?> FindByIdAsync(Guid entityId)
    {
        // TODO: Cover with unit tests
        var hasRecord = _registeredLogCodecs.TryGetValue(entityId, out var record);
        return Task.FromResult(hasRecord ? record.Codec : null);
    }

    public Task<IEnumerable<LogCodec>> GetAllAsync()
    {
        // TODO: Cover with unit tests
        return Task.FromResult(GetLogCodecs());
    }

    public IEnumerable<LogCodec> GetLogCodecs()
    {
        return _registeredLogCodecs.Select(x => x.Value.Codec);
    }

    public void RegisterLogCodec<TProfileLogCodec, TLogCodecProcessor>(LogCodec logCodec)
        where TProfileLogCodec : ProfileLogCodecBase
        where TLogCodecProcessor : class, ILogCodecProcessor
    {
        if (_registeredLogCodecs.ContainsKey(logCodec.Id))
        {
            throw new InvalidOperationException("The log codec '" + logCodec.Id + "' already exists.");
        }

        _registeredLogCodecs.Add(logCodec.Id, new LogCodecRecord(logCodec, typeof(TProfileLogCodec), typeof(TLogCodecProcessor)));
    }
}
