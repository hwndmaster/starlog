using System.Text.Json;
using System.Text.Json.Serialization;
using Genius.Atom.Data.Persistence;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Repositories;

internal sealed class LogCodecJsonConverter : JsonConverter<LogCodec>, IJsonConverter
{
    private readonly ILogCodecContainer _logCodecContainer;

    public LogCodecJsonConverter(ILogCodecContainer logCodecContainer)
    {
        _logCodecContainer = logCodecContainer.NotNull();
    }

    public override LogCodec? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var id = reader.GetGuid();
        return _logCodecContainer.GetLogCodecs().First(x => x.Id == id);
    }

    public override void Write(Utf8JsonWriter writer, LogCodec value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}
