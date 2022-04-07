#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class StringCoercingJsonConverter : JsonConverter<string>
{
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetDouble().ToString();
        return reader.GetString();
    }
}
