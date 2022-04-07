using PactSharp.Types;

namespace PactSharp.Services;

public class InMemorySettingsService : ISettingsService
{
    public SettingsModel Settings { get; set; } = new();
    
    public InMemorySettingsService(SettingsModel settings)
    {
        Settings = settings;
    }
    
    public Task<SettingsModel> GetSettingsAsync()
    {
        return Task.FromResult(Settings);
    }

    public Task SaveSettingsAsync(SettingsModel settings)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}
