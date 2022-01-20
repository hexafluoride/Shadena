using System.Text.Json.Nodes;

namespace PactSharp.Types;

public class PactExecPayload
{
    public string Code { get; set; }
    public JsonObject Data { get; set; } = new JsonObject();
}