using System.Text.Json.Serialization;
using PactSharp.Services;

namespace PactSharp.Types;

public class ChainwebBlockHeader : ICacheable
{
    public string CacheKey => GetCacheKey(Hash);

    public static string GetCacheKey(string blockHash) => $"block-header@{blockHash}";
    
    [JsonConverter(typeof(StringCoercingJsonConverter))]
    public string ChainId { get; set; }
    public string Nonce { get; set; }
    public long CreationTime { get; set; }
    public string Parent { get; set; }
    public string Target { get; set; }
    public string PayloadHash { get; set; }
    public string Weight { get; set; }
    public int Height { get; set; }
    public string ChainwebVersion { get; set; }
    public long EpochStart { get; set; }
    public object FeatureFlags { get; set; }
    public string Hash { get; set; }
}