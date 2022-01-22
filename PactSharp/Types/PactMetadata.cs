using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class PactMetadata
{
    public long BlockTime { get; set; }
    public int BlockHeight { get; set; }
    
    public string BlockHash { get; set; }
    [JsonPropertyName("prevBlockHash")]
    public string PreviousBlockHash { get; set; }
    
    [JsonPropertyName("publicMeta")]
    public ChainwebMetadata PublicMetadata { get; set; }
}