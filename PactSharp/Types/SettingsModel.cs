namespace PactSharp.Types;

public class SettingsModel
{
    public Network Network { get; set; } = Network.Testnet;
    public string CustomNetworkEndpoint { get; set; }
    public string CustomNetworkId { get; set; }
    public List<string> Tokens { get; set; } = new() { "coin" };
}

public enum Network
{
    Mainnet,
    Testnet,
    Local,
    Custom
}