using PactSharp.Types;

namespace Shadena;

public class SettingsModel
{
    public Network Network { get; set; } = Network.Testnet;
    public string CustomNetworkEndpoint { get; set; }
    public string CustomNetworkId { get; set; }
    public List<string> Tokens { get; set; }

    public PactClientSettings GenerateClientSettings()
    {
        return new PactClientSettings(
            network: Network, 
            customNetworkEndpoint: CustomNetworkEndpoint,
            customNetworkId: CustomNetworkId);
    }
}