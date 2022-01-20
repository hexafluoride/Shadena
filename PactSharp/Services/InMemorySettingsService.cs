using PactSharp.Types;

namespace PactSharp.Services;

public class InMemorySettingsService : ISettingsService
{
    public SettingsModel Settings { get; set; } = new();
    
    public InMemorySettingsService(SettingsModel settings)
    {
        Settings = settings;
    }
    
    public async Task<SettingsModel> GetSettingsAsync()
    {
        return Settings;
    }

    public async Task SaveSettingsAsync(SettingsModel settings)
    {
        Settings = settings;
    }
}