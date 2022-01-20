using PactSharp.Types;

namespace PactSharp.Services;

public interface ISettingsService
{
    Task<SettingsModel> GetSettingsAsync();
    Task SaveSettingsAsync(SettingsModel settings);
}