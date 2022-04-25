using Blazored.LocalStorage;
using PactSharp.Services;
using PactSharp.Types;

namespace Shadena;

public class SessionStorageSettingsManager
{
    private ILocalStorageService localStorage = null;
    
    public SessionStorageSettingsManager(ILocalStorageService storage)
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