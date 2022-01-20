using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class PactCommandResponse
{
    [JsonIgnore]
    public PactCommand SourceCommand { get; set; }
    
    [JsonPropertyName("reqKey")]
    public string RequestKey { get; set; }
    public PactCommandResult Result { get; set; }
    
    [JsonPropertyName("txId")]
    public string TransactionId { get; set; }
    public long Gas { get; set; }
    public string Logs { get; set; }
    public PactMetadata Metadata { get; set; }
    public PactContinuation Continuation { get; set; }
    public PactEvent[] Events { get; set; }
}