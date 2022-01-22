using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class PactExecPayload
{
    public string Code { get; set; }
    public JsonNode? Data { get; set; } = new JsonObject();

    [JsonIgnore] public JsonObject DataAsObject => Data.AsObject();
}