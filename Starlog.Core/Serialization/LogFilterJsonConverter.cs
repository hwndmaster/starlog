using System.Text.Json;
using System.Text.Json.Serialization;
using Genius.Atom.Data.Persistence;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Serialization;

internal sealed class LogFilterJsonConverter : JsonConverter<LogFilter>, IJsonConverter
{
    private readonly ILogFilterContainer _logFilterContainer;

    public LogFilterJsonConverter(ILogFilterContainer logFilterContainer)
    {
        _logFilterContainer = logFilterContainer.NotNull();
    }

    public override LogFilter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var id = reader.GetGuid();
        return _logFilterContainer.GetLogFilters(includingObsolete: true).First(x => x.Id == id);
    }

    public override void Write(Utf8JsonWriter writer, LogFilter value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}
