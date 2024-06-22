using Genius.Atom.Infrastructure.Entities;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

internal interface ILogCodecContainerInternal : ILogCodecContainer
{
    ILogCodecProcessor FindLogCodecProcessor(ProfileSettingsBase profileSettings);
    void RegisterLogCodec<TProfileSettings, TLogCodec>(LogCodec logCodec)
        where TProfileSettings : ProfileSettingsBase
        where TLogCodec : class, ILogCodecProcessor;
}

public interface ILogCodecContainer
{
    ILogCodecSettingsReader FindLogCodecSettingsReader(ProfileSettingsBase profileSettings);
    ProfileSettingsBase CreateProfileSettings(LogCodec logCodec);
    IEnumerable<LogCodec> GetLogCodecs();
}

internal sealed class LogCodecContainer : ILogCodecContainerInternal, IQueryService<LogCodec>
{
    private readonly record struct LogCodecRecord(LogCodec Codec, Type ProfileSettingsType, Type ProcessorType);

    private readonly Dictionary<LogCodecId, LogCodecRecord> _registeredLogCodecs = [];

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

    public ILogCodecSettingsReader FindLogCodecSettingsReader(ProfileSettingsBase profileSettings)
    {
        return FindLogCodecProcessor(profileSettings);
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
        var hasRecord = _registeredLogCodecs.TryGetValue(entityId, out var record);
        return Task.FromResult(hasRecord ? record.Codec : null);
    }

    public Task<IEnumerable<LogCodec>> GetAllAsync()
    {
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
