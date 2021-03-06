using PactSharp.Types;

namespace Shadena;

public class SettingsModel
{
    public Network Network { get; set; } = Network.Testnet;
    public string CustomNetworkEndpoint { get; set; }
    public string CustomNetworkId { get; set; }
    public List<string> Tokens { get; set; } = new() { "coin" };

    public PactClientSettings GenerateClientSettings()
    {
        return new PactClientSettings(
            network: Network, 
            customNetworkRpcEndpoint: CustomNetworkEndpoint,
            customNetworkId: CustomNetworkId);
    }
}