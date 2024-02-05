using Genius.Atom.Infrastructure.Entities;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogReading;

public interface ILogCodecContainer
{
    ProfileSettingsBase CreateProfileSettings(LogCodec logCodec);
    ILogCodecProcessor FindLogCodecProcessor(ProfileSettingsBase profileSettings);
    IEnumerable<LogCodec> GetLogCodecs();
    void RegisterLogCodec<TProfileSettings, TLogCodec>(LogCodec logCodec)
        where TProfileSettings : ProfileSettingsBase
        where TLogCodec : class, ILogCodecProcessor;
}

internal sealed class LogCodecContainer : ILogCodecContainer, IQueryService<LogCodec>
{
    private readonly record struct LogCodecRecord(LogCodec Codec, Type ProfileSettingsType, Type ProcessorType);

    private readonly Dictionary<Guid /* LogCodec.Id */, LogCodecRecord> _registeredLogCodecs = new();

    private readonly Lazy<IEnumerable<ILogCodecProcessor>> _processors;

    public LogCodecContainer(Lazy<IEnumerable<ILogCodecProcessor>> logCodecProcessors)
    {
        _processors = logCodecProcessors;
    }

    public ProfileSettingsBase CreateProfileSettings(LogCodec logCodec)
    {
        if (!_registeredLogCodecs.TryGetValue(logCodec.Id, out var value))
        {
            throw new InvalidOperationException("The log codec '" + logCodec.Id + "' doesn't exists.");
        }

        return (ProfileSettingsBase)Activator.CreateInstance(value.ProfileSettingsType, logCodec).NotNull();
    }

    public ILogCodecProcessor FindLogCodecProcessor(ProfileSettingsBase profileSettings)
    {
        if (!_registeredLogCodecs.TryGetValue(profileSettings.LogCodec.Id, out var value))
        {
            throw new InvalidOperationException("The log codec '" + profileSettings.LogCodec.Id + "' doesn't exists.");
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

    public void RegisterLogCodec<TProfileSettings, TLogCodecProcessor>(LogCodec logCodec)
        where TProfileSettings : ProfileSettingsBase
        where TLogCodecProcessor : class, ILogCodecProcessor
    {
        if (_registeredLogCodecs.ContainsKey(logCodec.Id))
        {
            throw new InvalidOperationException("The log codec '" + logCodec.Id + "' already exists.");
        }

        _registeredLogCodecs.Add(logCodec.Id, new LogCodecRecord(logCodec, typeof(TProfileSettings), typeof(TLogCodecProcessor)));
    }
}
