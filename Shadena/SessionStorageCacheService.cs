using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using PactSharp.Services;

namespace Shadena;

public class SessionStorageCacheService : ICacheService
{
    private ILocalStorageService _localStorage;

    private Dictionary<string, CachedItem> _buffer = new();

    public SessionStorageCacheService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public bool HasItem(string key)
    {
        return Get(key) != null && Get(key).Expires >= DateTime.UtcNow;
    }

    public async Task<T> GetItem<T>(string key) where T : ICacheable
    {
        var cached = Get(key);
        if (cached == null)
            return default(T);

        if (cached.Expires < DateTime.UtcNow)
        {
            await Evict(cached);
            return default(T);
        }

        return cached.Unwrap<T>();
    }

    public async Task<bool> SetItem<T>(T item, int expirySeconds = 0) where T : ICacheable
    {
        var cached = Get(item);
        var length = expirySeconds == 0 ? TimeSpan.FromSeconds(3600) : TimeSpan.FromSeconds(expirySeconds);

        if (cached != null)
        {
            cached.Contents = JsonSerializer.SerializeToElement(item);
            cached.Cached = DateTime.UtcNow;
            cached.Expires = cached.Cached + length;
            cached.Invalidate();
        }
        else
        {
            _buffer[item.CacheKey] = CachedItem.FromCacheable(item, length);
            _buffer[item.CacheKey].Dirty = true;
        }

        return true;
    }

    public async Task<int> InvalidatePrefix(string prefix)
    {
        var toEvict = new List<CachedItem>();
        
        foreach (var pair in _buffer)
        {
            if (pair.Key.StartsWith(prefix))
                toEvict.Add(pair.Value);
        }

        foreach (var item in toEvict)
            await Evict(item);

        return 0;
    }

    public async Task<bool> InvalidateExact(string exact)
    {
        var cacheItem = Get(exact);
        if (cacheItem != null)
            return await Evict(cacheItem);

        return false;
    }

    public async Task<bool> InvalidateExact<T>(T item) where T : ICacheable
    {
        var cacheItem = Get(item);
        if (cacheItem != null)
            return await Evict(cacheItem);

        return false;
    }

    private CachedItem Get(ICacheable item) => Get(item.CacheKey);
    private CachedItem Get(string key) => _buffer.ContainsKey(key) ? _buffer[key] : null;
    
    private async Task<bool> Evict(CachedItem item)
    {
        item.Dirty = true;
        _buffer.Remove(item.Key);
        await _localStorage.RemoveItemAsync(item.PrefixedKey);
        return true;
    }

    public async Task<bool> Flush()
    {
        foreach (var pair in _buffer)
        {
            var item = pair.Value;
            if (!item.Dirty)
                continue;

            await _localStorage.SetItemAsync(item.PrefixedKey, item);
        }

        return true;
    }

    public async Task<bool> Initialize()
    {
        var localItemCount = await _localStorage.LengthAsync();

        for (int i = 0; i < localItemCount; i++)
        {
            var key = await _localStorage.KeyAsync(i);
            if (!key.StartsWith("$cache$"))
                continue;

            var value = await _localStorage.GetItemAsync<CachedItem>(key);

            if (value.PrefixedKey != key)
            {
                Console.WriteLine($"key mismatch between {value.PrefixedKey} and {key}");
                continue;
            }

            _buffer[value.Key] = value;
        }

        return true;
    }

    internal static string WithPrefix(string key) => "$cache$" + key;

    class CachedItem
    {
        [JsonIgnore] public string PrefixedKey => WithPrefix(Key);
        [JsonIgnore] public bool Dirty { get; set; }
        public string Key { get; set; }
        public DateTime Cached { get; set; }
        public DateTime Expires { get; set; }
        public JsonElement Contents { get; set; }
        private object _unwrapped;

        internal static CachedItem FromCacheable<T>(T item, TimeSpan expiry) where T : ICacheable
        {
            var ret = new CachedItem()
            {
                Cached = DateTime.UtcNow,
                Expires = DateTime.UtcNow + expiry,
                Key = item.CacheKey,
                Contents = JsonSerializer.SerializeToElement(item)
            };

            return ret;
        }

        internal void Invalidate()
        {
            _unwrapped = null;
            Dirty = true;
        }

        internal T Unwrap<T>() where T : ICacheable
        {
            if (_unwrapped == null)
                _unwrapped = Contents.Deserialize<T>();
            
            return (T)_unwrapped;
        }
    }
}