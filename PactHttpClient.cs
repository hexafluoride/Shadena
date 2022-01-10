using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Shadena;

public class PactHttpClient
{
    private readonly HttpClient http;
    private readonly ISettingsManager settingsManager;

    public string ApiHost { get; set; }
    public string NetworkId { get; set; }

    public int DefaultGasLimit { get; set; } = 1500;
    public decimal DefaultGasPrice { get; set; } = 0.00000001m;
    
    public static JsonSerializerOptions PactJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
//        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public List<string> RecognizedChains { get; set; } = new();
    
    public PactHttpClient(HttpClient http, ISettingsManager settingsManager)
    {
        this.http = http;
        this.settingsManager = settingsManager;
        
        RecognizedChains = Enumerable.Range(0, 20).Select(i => i.ToString()).ToList();
    }

    public async Task Initialize()
    {
        var settings = await settingsManager.GetSettingsAsync();
        SetNetwork(settings.Network);
    }

    public void SetNetwork(Network network)
    {
        switch (network)
        {
            case Network.Mainnet:
                ApiHost = "api.chainweb.com";
                NetworkId = "mainnet01";
                break;
            case Network.Testnet:
                ApiHost = "api.testnet.chainweb.com";
                NetworkId = "testnet04";
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    private string GetApiUrl(string endpoint, string chain) =>
        $"https://{ApiHost}/chainweb/0.0/{NetworkId}/chain/{chain}/pact{endpoint}";

    public async Task<string> SendTransactionAsync(PactCommand command)
    {
        var chain = command.Command.Metadata.ChainId;

        var req = new {cmds = new[] {command}};
        Console.WriteLine($"Encoded: {JsonSerializer.Serialize(req, PactJsonOptions)}");
        var resp = await http.PostAsJsonAsync(GetApiUrl($"/api/v1/send", chain), req, PactJsonOptions);

        var respString = await resp.Content.ReadAsStringAsync();
        
        try
        {
            var parsedResponse = JsonDocument.Parse(respString);
            return parsedResponse.RootElement.GetProperty("requestKeys")[0].GetString();
        }
        catch (Exception e)
        {
            return e.Message + "\n" + respString;
        }
        
    }

    public async Task<PactCommandResponse> ExecuteLocalAsync(PactCommand command)
    {
        var chain = command.Command.Metadata.ChainId;

        Console.WriteLine($"Encoded: {JsonSerializer.Serialize(command, PactJsonOptions)}");
        var resp = await http.PostAsJsonAsync(GetApiUrl($"/api/v1/local", chain), command, PactJsonOptions);

        var respString = await resp.Content.ReadAsStringAsync();
        
        try
        {
            var parsedResponse = await JsonSerializer.DeserializeAsync<PactCommandResponse>(await resp.Content.ReadAsStreamAsync(), PactJsonOptions);
            parsedResponse.SourceCommand = command;
            return parsedResponse;
        }
        catch (Exception e)
        {
            return new PactCommandResponse()
            {
                Result = new PactCommandResult() {Status = "failure", Error = new PactError() {Message = respString, Info = e.Message, CallStack = e.StackTrace.Split('\n')}},
                SourceCommand = command
            };
        }
    }

    public PactCommand GenerateAccountDetailQuery(string chain, string module, string account)
    {
        var code = $"({module}.details (read-msg 'account))";
        var cmd = GenerateExecCommand(chain, code, new {account});

        return BuildCommand(cmd);
    }

    public PactCmd GenerateExecCommand(string chain, string code, object data = null)
    {
        var cmd = new PactCmd()
        {
            Metadata = GenerateMetadata(chain),
            NetworkId = NetworkId,
            Nonce = DateTime.UtcNow.ToLongDateString().HashEncoded(),
            Signers = new List<PactSigner>(),
            Payload = new PactPayload()
            {
                Exec = new PactExecPayload()
                {
                    Code = code,
                    Data = JsonObject.Create(JsonSerializer.SerializeToElement(data, PactJsonOptions)) ?? new JsonObject()
                }
            }
        };

        return cmd;
    }

    public PactMetadata GenerateMetadata(string chain, int gasLimit = -1, decimal gasPrice = -1m,
        string sender = "", int ttl = 3600,
        DateTime creationTime = default)
    {
        return new PactMetadata()
        {
            ChainId = chain,
            CreationTime = (long) ((creationTime != default ? creationTime : DateTime.UtcNow) - DateTime.UnixEpoch).TotalSeconds,
            GasLimit = gasLimit < 0 ? DefaultGasLimit : gasLimit,
            GasPrice = gasPrice < 0 ? DefaultGasPrice : gasPrice,
            Sender = sender,
            Ttl = ttl
        };
    }

    public PactCommand BuildCommand(PactCmd cmd, PactSignature[] signatures = null)
    {
        var ret = new PactCommand();
        
        var cmdEscaped = JsonSerializer.Serialize(cmd, PactJsonOptions);

        ret.Command = cmd;
        ret.Signatures = signatures ?? Array.Empty<PactSignature>();
        ret.CommandEncoded = cmdEscaped;
        ret.Hash = cmdEscaped.HashEncoded();

        return ret;
    }
}

public class PactExecutionException : Exception
{
    public override string Message { get; }

    public PactExecutionException(PactCommandResponse resp)
    {
        if (resp == null)
        {
            Message = "Null response received";
        }
        else if (resp.Result.Status != "success")
        {
            Message =
                $"Execution status is \"{resp.Result.Status}\" with remote message \"{resp.Result.Error.Message}\" and \"{resp.Result.Error.Info}\", type {resp.Result.Error.Type}";
        }
        else
        {
            Message = "Unknown Pact execution error";
        }
    }
}

public class PactModuleMetadata : ICacheable
{
    public string CacheKey => GetCacheKey(Network, Chain, Name);
    public static string GetCacheKey(string network, string chain, string module) => $"module-metadata@{network}${chain}${module}";
    public string Chain { get; set; }
    public string Network { get; set; }
    public string Name { get; set; }
    public string Hash { get; set; }
    public string[] Interfaces { get; set; }
    public string[] Blessed { get; set; }
    public string Code { get; set; }
    public string Governance { get; set; }
    public bool Exists => !string.IsNullOrWhiteSpace(Hash);
}

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

public class PactCommandResult
{
    public string Status { get; set; }
    public JsonElement Data { get; set; }
    public PactError Error { get; set; }
}

public class PactError
{
    public string[] CallStack { get; set; }
    public string Info { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
}

public class PactContinuation
{
    public bool Executed { get; set; }
    public string PactId { get; set; }
    public int Step { get; set; }
    public int StepCount { get; set; }
    public PactContinuationMetadata Continuation { get; set; }
    public PactYield Yield { get; set; }
}

public class PactContinuationMetadata
{
    [JsonPropertyName("args")]
    public object[] Arguments { get; set; }
    public string Def { get; set; }
}

public class PactYield
{
    public Dictionary<string, object> Data { get; set; }
    public PactProvenance Provenance { get; set; }
}

public class PactProvenance
{
    public string TargetChainId { get; set; }
    public string ModuleHash { get; set; }
}

public class PactEvent
{
    public string Name { get; set; }
    
    [JsonPropertyName("params")]
    public object[] Parameters { get; set; }
    
    public PactModuleReference Module { get; set; }
    public string ModuleHash { get; set; }
}

public class PactModuleReference
{
    public string Namespace { get; set; }
    public string Name { get; set; }
}

public class PactKeyset
{
    public List<string> Keys { get; set; } = new();

    [JsonPropertyName("pred")] public string Predicate { get; set; } = "keys-all";
}

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
        set
        {
            _command = value;
            CommandEncoded = JsonSerializer.Serialize(_command, PactHttpClient.PactJsonOptions);
        }
    }
    
    [JsonIgnore]
    public string JsonEncoded { get; set; }
    
    [JsonIgnore]
    public string YamlEncoded { get; set; }

    private PactCmd _command;

    public void UpdateHash()
    {
        CommandEncoded = JsonSerializer.Serialize(_command, PactHttpClient.PactJsonOptions);
        Hash = CommandEncoded.HashEncoded();
        JsonEncoded = JsonSerializer.Serialize(this, PactHttpClient.PactJsonOptions);
        var yamlSerializer =
            (new YamlDotNet.Serialization.SerializerBuilder()).WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        YamlEncoded = yamlSerializer.Serialize(_command);
    }
}

public class PactCmd
{
    public string Nonce { get; set; }

    [JsonPropertyName("meta")] public PactMetadata Metadata { get; set; }

    public List<PactSigner> Signers { get; set; }
    public string NetworkId { get; set; }

    [YamlIgnore] public PactPayload Payload { get; set; }

    [JsonIgnore] public string Code => Payload.Exec.Code;

    [JsonIgnore]
    public object Data =>
        YamlDeserializer.Deserialize(new StringReader(Payload.Exec.Data.ToJsonString(PactHttpClient.PactJsonOptions)));

    [JsonIgnore] [YamlMember(Alias = "type")] public string CommandType => Payload.Exec != null ? "exec" : "cont";
    
    public static IDeserializer YamlDeserializer = (new DeserializerBuilder())
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
}

public class PactMetadata
{
    public string ChainId { get; set; }
    public string Sender { get; set; }
    public int GasLimit { get; set; }
    public decimal GasPrice { get; set; }
    public double Ttl { get; set; }
    public long CreationTime { get; set; }
}

public class PactPayload
{
    public PactExecPayload Exec { get; set; }
    public PactContPayload Cont { get; set; }
}

public class PactExecPayload
{
    public string Code { get; set; }
    public JsonObject Data { get; set; } = new JsonObject();
}

public class PactContPayload
{
    public string PactId { get; set; }
    public bool Rollback { get; set; }
    public int Step { get; set; }
    public string Proof { get; set; }
    public object Data { get; set; }
}

public class PactSignature
{
    [JsonPropertyName("sig")]
    public string Signature { get; set; }
}

public class PactCapability
{
    public string Name { get; set; }

    [JsonPropertyName("args")] public List<object> Arguments { get; set; } = new();

    public override string ToString() => 
        string.IsNullOrWhiteSpace(Name) ? "" : 
            Arguments.Count == 0 ? $"({Name})" :
            $"({Name} {string.Join(' ', Arguments)})";

    static List<string> SplitSpaceSeparatedValues(string str)
    {
        var ret = new List<string>();

        var quoting = false;
        string current_run = "";
        char quote_char = ' ';

        for(int i = 0; i < str.Length; i++)
        {
            var current_char = str[i];

            if(quoting && current_char == quote_char)
            {
                current_run += current_char;
                quoting = false;

                if (!string.IsNullOrWhiteSpace(current_run))
                    ret.Add(current_run);

                current_run = "";
            }
            else if (current_char == '"')
            {
                quoting = true;
                quote_char = current_char;

                if (!string.IsNullOrWhiteSpace(current_run))
                    ret.Add(current_run.TrimEnd(' '));

                current_run = "";
                current_run += current_char;
            }
            else if (!quoting && current_char == ' ')
            {
                if (!string.IsNullOrWhiteSpace(current_run))
                    ret.Add(current_run);
                current_run = "";
            }
            else
            {
                current_run += current_char;
            }
        }

        if (!string.IsNullOrWhiteSpace(current_run))
            ret.Add(current_run);

        return ret;
    }
    
    public static PactCapability FromString(string str)
    {
        if (str.First() != '(' || str.Last() != ')')
            return null;

        var parts = SplitSpaceSeparatedValues(str.Substring(1, str.Length - 2));
        Console.WriteLine($"Split \"{str}\" to {parts.Count} parts: [{string.Join(", ", parts)}]");

        var cap = new PactCapability();
        cap.Name = parts[0];
        cap.Arguments = new List<object>();

        foreach (var arg in parts.Skip(1))
        {
            if (arg.Length > 1 && arg[0] == '"' && arg[arg.Length - 1] == '"')
                cap.Arguments.Add(arg.Substring(1, arg.Length - 2));
            else if (arg.Length > 1 && arg[0] == '\'')
                cap.Arguments.Add(arg.Substring(1));
            else if (bool.TryParse(arg, out bool boolArgument))
                cap.Arguments.Add(boolArgument);
            else if (int.TryParse(arg, out int intArgument))
                cap.Arguments.Add(intArgument);
            else if (decimal.TryParse(arg, out decimal decimalArgument))
                cap.Arguments.Add(decimalArgument);
            else
                return null;
        }

        return cap;
    }
}

public class PactSigner
{
    [YamlIgnore]
    public string Scheme { get; set; } = "ED25519";
    [YamlMember(Alias = "public")]
    public string PubKey { get; set; }

    [YamlMember(Alias = "caps")]
    [JsonPropertyName("clist")] public List<PactCapability> Capabilities { get; set; } = new();
    public string Addr { get; set; }

    public PactSigner(string key)
    {
        PubKey = key;
    }
}