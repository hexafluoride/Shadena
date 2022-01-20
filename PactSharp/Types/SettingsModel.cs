namespace PactSharp.Types;

public class SettingsModel
{
    public Network Network { get; set; } = Network.Testnet;
    public List<string> Tokens { get; set; } = new() { "coin" };
}

public enum Network
{
    Mainnet,
    Testnet,
    Local
}