using PactSharp.Services;

namespace PactSharp.Types;

public class PactModuleMetadata : ICacheable
{
    public string CacheKey => GetCacheKey(Network, Chain, Name);
    public static string GetCacheKey(string network, string chain, string module) => $"module-metadata@{network}${chain}${module}";
    public string Chain { get; set; }
    public string Network { get; set; }
    public string Name { get; set; }
    public string Hash { get; set; }
    public string[] Interfaces { get; set; }
    public string[] Blessed { get; set; }
    public string Code { get; set; }
    public string Governance { get; set; }
    public bool Exists => !string.IsNullOrWhiteSpace(Hash);
}