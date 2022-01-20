namespace PactSharp.Services;

public interface ICacheService
{
    bool HasItem(string key);
    Task<T> GetItem<T>(string key) where T : ICacheable;
    Task<bool> SetItem<T>(T item, int expirySeconds = 0) where T : ICacheable;
    Task<int> InvalidatePrefix(string prefix);
    Task<bool> InvalidateExact(string exact);
    Task<bool> InvalidateExact<T>(T item) where T : ICacheable;
    Task<bool> Flush();
    Task<bool> Initialize();
}

public interface ICacheable
{
    string CacheKey { get; }
}