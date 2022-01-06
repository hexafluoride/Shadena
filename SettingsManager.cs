using Blazored.LocalStorage;
using Shadena.Pages;

namespace Shadena;

public class SettingsManager : ISettingsManager
{
    private ILocalStorageService localStorage = null;
    
    public SettingsManager(ILocalStorageService storage)
    {
        localStorage = storage;
    }
    
    public async Task<SettingsModel> GetSettingsAsync()
    {
        return await localStorage.GetItemAsync<SettingsModel>("settings") ?? new SettingsModel();
    }

    public async Task SaveSettingsAsync(SettingsModel settings)
    {
        await localStorage.SetItemAsync("settings", settings);
    }
}

public interface ISettingsManager
{
    Task<SettingsModel> GetSettingsAsync();
    Task SaveSettingsAsync(SettingsModel settings);
}


public class SettingsModel
{
    public Network Network { get; set; } = Network.Testnet;
    public List<string> Tokens { get; set; } = new() { "coin" };
}

public enum Network
{
    Mainnet,
    Testnet
}