using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class PactKeyset
{
    public List<string> Keys { get; set; } = new();

    [JsonPropertyName("pred")] public string Predicate { get; set; } = "keys-all";
}