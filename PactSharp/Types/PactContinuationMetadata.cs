using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class PactContinuationMetadata
{
    [JsonPropertyName("args")]
    public object[] Arguments { get; set; }
    public string Def { get; set; }
}