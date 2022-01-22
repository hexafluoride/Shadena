using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class EstatsTransaction
{
    public DateTime CreationTime { get; set; }
    [JsonPropertyName("ttl")]
    public int TimeToLive { get; set; }
    public object Proof { get; set; }
    public DateTime BlockTime { get; set; }
    [JsonPropertyName("height")]
    public int BlockHeight { get; set; }
    [JsonPropertyName("gas")]
    public int GasConsumed { get; set; }
    public int GasLimit { get; set; }
    public object Data { get; set; }
    public decimal GasPrice { get; set; }
    public string Sender { get; set; }
    public bool Success { get; set; }
    public string BlockHash { get; set; }
    public string PactId { get; set; }
    public object Rollback { get; set; }
    public string RequestKey { get; set; }
    public object Result { get; set; }
    public string Logs { get; set; }
    public PactEvent[] Events { get; set; }
    public object Step { get; set; }
    public ChainwebMetadata Metadata { get; set; }
    public string Code { get; set; }
    public string Chain { get; set; }
    [JsonPropertyName("txid")]
    public int TransactionId { get; set; }
    public string Nonce { get; set; }
    public PactContinuation Continuation { get; set; }
}