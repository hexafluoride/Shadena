using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PactSharp.Types;

public class PactCmd
{
    public string Nonce { get; set; }

    [JsonPropertyName("meta")]
    [YamlMember(Alias = "publicMeta")] public ChainwebMetadata Metadata { get; set; }

    public List<PactSigner> Signers { get; set; }
    public string NetworkId { get; set; }

    [YamlIgnore] public PactPayload Payload { get; set; } = new PactPayload() {Exec = new PactExecPayload()};

    [JsonIgnore]
    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Code
    {
        get => Payload.Exec.Code;
        set { Payload.Exec.Code = value; }
    }

    [JsonIgnore]
    public object Data
    {
        get => YamlDeserializer.Deserialize(new StringReader(Payload.Exec.Data.ToJsonString(PactClient.PactJsonOptions)));
        set { Payload.Exec.Data = JsonSerializer.SerializeToNode(value, PactClient.PactJsonOptions); }
    }

    [JsonIgnore] [YamlMember(Alias = "type")] public string CommandType => Payload.Exec != null ? "exec" : "cont";
    
    public static IDeserializer YamlDeserializer = (new DeserializerBuilder())
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
}