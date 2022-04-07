using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PactSharp.Types;

public class PactCommand
{
    [JsonPropertyName("sigs")]
    public PactSignature[] Signatures { get; set; }
    
    [JsonPropertyName("cmd")]
    public string CommandEncoded { get; set; }
    public string Hash { get; set; }

    [JsonIgnore]
    public PactCmd Command
    {
        get => _command;
        set { _command = value; }
    }
    
    [JsonIgnore]
    public string JsonEncodedForLocal { get; set; }
    [JsonIgnore]
    public string JsonEncodedForSend { get; set; }
    
    [JsonIgnore]
    public string YamlEncoded { get; set; }

    private PactCmd _command;

    public void SetCommand(string encodedJson)
    {
        Command = JsonSerializer.Deserialize<PactCmd>(encodedJson, PactClient.PactJsonOptions);
    }

    public void UpdateHash()
    {
        CommandEncoded = JsonSerializer.Serialize(_command, PactClient.PactJsonOptions);
        Hash = CommandEncoded.HashEncoded();
        JsonEncodedForLocal = JsonSerializer.Serialize(this, PactClient.PactJsonOptions);
        JsonEncodedForSend = JsonSerializer.Serialize(new {cmds = new[] {this}}, PactClient.PactJsonOptions);
        
        var yamlSerializer =
            (new YamlDotNet.Serialization.SerializerBuilder()).WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        YamlEncoded = yamlSerializer.Serialize(_command);
    }
}
