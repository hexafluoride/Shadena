using System.Text.Json;

namespace PactSharp.Types;

public class PactCommandResult
{
    public string Status { get; set; }
    public JsonElement Data { get; set; }
    public PactError Error { get; set; }
}