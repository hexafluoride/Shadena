using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace PactSharp.Types;

public class PactSigner
{
    [YamlIgnore]
    public string Scheme { get; set; } = "ED25519";
    [YamlMember(Alias = "public")]
    public string PubKey { get; set; }

    [YamlMember(Alias = "caps")]
    [JsonPropertyName("clist")] public List<PactCapability> Capabilities { get; set; } = new();
    public string Addr { get; set; }

    public PactSigner()
    {
    }

    public PactSigner(string pubKey)
    {
        PubKey = pubKey;
    }
}