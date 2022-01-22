using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PactSharp.Types;

public class PactCmd
{
    public string Nonce { get; set; }

    [JsonPropertyName("meta")] public ChainwebMetadata Metadata { get; set; }

    public List<PactSigner> Signers { get; set; }
    public string NetworkId { get; set; }

    [YamlIgnore] public PactPayload Payload { get; set; }

    [JsonIgnore]
    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Code => Payload.Exec.Code;

    [JsonIgnore]
    public object Data =>
        YamlDeserializer.Deserialize(new StringReader(Payload.Exec.Data.ToJsonString(PactClient.PactJsonOptions)));

    [JsonIgnore] [YamlMember(Alias = "type")] public string CommandType => Payload.Exec != null ? "exec" : "cont";
    
    public static IDeserializer YamlDeserializer = (new DeserializerBuilder())
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
}