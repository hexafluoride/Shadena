using System.Text.Json;
using PactSharp;
using PactSharp.Services;
using PactSharp.Types;

namespace Shadena;

public class PollService
{
    private PactClient _pactClient;
    private ICacheService _cache;
    
    public PollService(PactClient pactClient, ICacheService cacheService)
    {
        _pactClient = pactClient;
        _cache = cacheService;
    }

    public async Task<PactCommandResponse> PollRequestAsync(string chain, string requestKey)
    {
        return await FetchAndCache(PactCommandResponse.GetCacheKey(requestKey),
            async () => await _pactClient.PollRequestAsync(chain, requestKey));
    }
    
    private async Task<T> FetchAndCache<T>(string cacheKey, Func<Task<T>> fetchAction) where T : ICacheable
    {
        T result;

        if (_cache.HasItem(cacheKey))
            result = await _cache.GetItem<T>(cacheKey);
        else
            result = await fetchAction();

        if (string.Equals(result?.CacheKey, cacheKey))
        {
            await _cache.SetItem(result);
        }

        return result;
    }
}