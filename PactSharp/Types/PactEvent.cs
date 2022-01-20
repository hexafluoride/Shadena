using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class PactEvent
{
    public string Name { get; set; }
    
    [JsonPropertyName("params")]
    public object[] Parameters { get; set; }
    
    public PactModuleReference Module { get; set; }
    public string ModuleHash { get; set; }
}