using System.Text.Json;
using System.Text.Json.Serialization;
using Genius.Atom.Data.Persistence;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Repositories;

internal sealed class LogReaderJsonConverter : JsonConverter<LogReader>, IJsonConverter
{
    private readonly ILogReaderContainer _logReaderContainer;

    public LogReaderJsonConverter(ILogReaderContainer logReaderContainer)
    {
        _logReaderContainer = logReaderContainer.NotNull();
    }

    public override LogReader? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var id = reader.GetGuid();
        return _logReaderContainer.GetLogReaders().First(x => x.Id == id);
    }

    public override void Write(Utf8JsonWriter writer, LogReader value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}
