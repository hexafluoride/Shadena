using System.ComponentModel.Design.Serialization;
using System.Text.Json;
using Blazored.LocalStorage;

namespace Shadena;

public class ChainwebQueryService : IChainwebQueryService
{
    private PactHttpClient PactClient { get; set; }
    private ILocalStorageService _localStorage;
    private ICacheService _cache;

    private string _network => PactClient.NetworkId;

    public ChainwebQueryService(PactHttpClient client, ILocalStorageService localStorage, ICacheService cache)
    {
        PactClient = client;
        _localStorage = localStorage;
        _cache = cache;
    }

    public async Task Initialize()
    {
    }

    private async Task SaveCache()
    {
        await _cache.Flush();
    }

    public async Task<IEnumerable<FungibleV2Account>> GetAccountDetailsAsync(string chain, string[] modules,
        AccountIdentifier[] accounts, bool ignoreCache = false)
    {
        var moduleMetadata = await GetModuleMetadataAsync(chain, modules);
        var modulesValidForChain = modules.Select(module =>
                new {Module = module, Metadata = moduleMetadata.First(m => m.Name == module)})
            .Where(composite => composite.Metadata.Exists && composite.Metadata.Interfaces.Contains("fungible-v2"))
            .Select(composite => composite.Module);

        var cacheKeys = modulesValidForChain.SelectMany(module =>
            accounts.Select(account => FungibleV2Account.GetCacheKey(_network, chain, module, account.Name)));

        if (!ignoreCache && cacheKeys.All(_cache.HasItem))
            return await Task.WhenAll(cacheKeys.Select(key => _cache.GetItem<FungibleV2Account>(key)));
        
        var code = @"
(namespace 'free)
(module bighelp T
  (defcap T () true)
  (defun try-get-details (account:string token:module{fungible-v2}) { 'module: (format ""{}"" [token]), 'account: account, 'result: (try {} (token::details account))})
  (defun fetch-accounts (tokens:[module{fungible-v2}] account:string)
    (map (try-get-details account) tokens)
  )
  (defun fetch-accounts-many (tokens:[module{fungible-v2}] accounts:[string])
    (map (fetch-accounts tokens) accounts)
  )
)
(fetch-accounts-many [" + string.Join(' ', modulesValidForChain) + @"] (read-msg 'accounts))
";

        var detailsCmd = PactClient.GenerateExecCommand(chain, code, new {accounts = accounts.Select(a => a.Name)});
        var detailsCommand = PactClient.BuildCommand(detailsCmd);

        var resultAccounts = new List<FungibleV2Account>();
        
        var result = await PactClient.ExecuteLocalAsync(detailsCommand);

        if (result.Result.Status != "success")
        {
            return resultAccounts;
        }

        foreach (var subArray in result.Result.Data.EnumerateArray())
        {
            foreach (var accountDetails in subArray.EnumerateArray())
            {
                var module = accountDetails.GetProperty("module").GetString();
                var account = accountDetails.GetProperty("account").GetString();
                var accountObject = accountDetails.GetProperty("result");
                
                var ret = new FungibleV2Account()
                {
                    Network = result.SourceCommand.Command.NetworkId,
                    Chain = chain,
                    Module = module,
                    Account = account
                };

                if (accountObject.TryGetProperty("balance", out JsonElement _))
                {
                    ret.Guard = accountObject.GetProperty("guard");
                    ret.Balance = accountObject.GetProperty("balance").GetDecimal();
                    ret.Account = accountObject.GetProperty("account").GetString();
                }
                
                resultAccounts.Add(ret);
            }
        }

        await Task.WhenAll(resultAccounts.Select(account => _cache.SetItem(account, 30)));
        await _cache.Flush();

        return resultAccounts;
    }

    public async Task<FungibleV2Account> GetAccountDetailsAsync(string chain, string module, AccountIdentifier account, bool ignoreCache = false)
    {
        var cacheKey = FungibleV2Account.GetCacheKey(_network, chain, module, account.Name);

        if (_cache.HasItem(cacheKey))
            return await _cache.GetItem<FungibleV2Account>(cacheKey);

        var detailsCommand = PactClient.GenerateAccountDetailQuery(chain, module, account.Name);
        var result = await PactClient.ExecuteLocalAsync(detailsCommand);
        var resultAccount = FungibleV2Account.FromResponse(result, module);

        if (result.Result.Status == "success")
        {
            await _cache.SetItem(resultAccount);
        }

        return resultAccount;
    }

    public async Task<IEnumerable<FungibleV2Account>> GetAccountDetailsAsync(string module, AccountIdentifier account, bool ignoreCache = false)
    {
        return await ForAllChains(chain => GetAccountDetailsAsync(chain, module, account, ignoreCache));
    }

    public async Task<IEnumerable<FungibleV2Account>> GetAccountDetailsAsync(string[] modules, AccountIdentifier[] accounts, bool ignoreCache = false)
    {
        await ForAllChains(chain => GetModuleMetadataAsync(chain, modules));
        return (await ForAllChains(chain => GetAccountDetailsAsync(chain, modules, accounts, ignoreCache)))
            .SelectMany(t => t);
    }

    public async Task<bool> ModuleExistsAsync(string chain, string module)
    {
        var moduleMetadata = await GetModuleMetadataAsync(chain, module);
        return moduleMetadata.Exists;
    }

    public async Task<PactModuleMetadata> GetModuleMetadataAsync(string chain, string module)
    {
        return (await GetModuleMetadataAsync(chain, new[] {module})).Single();
    }

    public async Task<IEnumerable<PactModuleMetadata>> GetModuleMetadataAsync(string chain, string[] modules)
    {
        var cacheKeys = modules.Select(module => new
        {
            Module = module, Key = PactModuleMetadata.GetCacheKey(_network, chain, module)
        }).Select(composite => new { composite.Module, composite.Key, Cached = _cache.HasItem(composite.Key) });
        
        var uncached = cacheKeys.Where(k => !k.Cached);
        var cached = cacheKeys.Where(k => k.Cached);
        var results = (await Task.WhenAll(cached.Select(k => _cache.GetItem<PactModuleMetadata>(k.Key)))).ToList(); 

        if (!uncached.Any())
        {
            Console.WriteLine($"Total cache hit on chain {chain} modules {string.Join(',', modules)}");
            return results;
        }

        var queryModules = uncached.Select(k => k.Module);
        var queryStatements =
            queryModules.Select(module => $"{{ 'module: \"{module}\", 'metadata: (try {{}} (describe-module \"{module}\")) }}");
        var query = $"[{string.Join(' ', queryStatements)}]";

        var queryCmd = PactClient.GenerateExecCommand(chain, query);
        var queryCommand = PactClient.BuildCommand(queryCmd);

        var result = await PactClient.ExecuteLocalAsync(queryCommand);

        if (result?.Result?.Status != "success")
        {
            Console.WriteLine($"Error on chain {chain} modules {string.Join(',', modules)}");
            throw new PactExecutionException(result);
        }

        foreach (var queryResult in result.Result.Data.EnumerateArray())
        {
            var moduleName = queryResult.GetProperty("module").GetString();
            var moduleMetadata = queryResult.GetProperty("metadata");

            var metadataObject = new PactModuleMetadata()
            {
                Network = PactClient.NetworkId,
                Chain = chain,
                Name = moduleName
            };

            if (moduleMetadata.TryGetProperty("hash", out JsonElement _))
            {
                metadataObject.Hash = moduleMetadata.GetProperty("hash").GetString();
                metadataObject.Blessed = moduleMetadata.GetProperty("blessed").EnumerateArray().Select(t => t.GetString()).ToArray();
                metadataObject.Code = moduleMetadata.GetProperty("code").GetString();
                metadataObject.Governance = moduleMetadata.GetProperty("keyset").GetString();
                metadataObject.Interfaces = moduleMetadata.GetProperty("interfaces").EnumerateArray().Select(t => t.GetString()).ToArray();
            }

            await _cache.SetItem(metadataObject);
            results.Add(metadataObject);
        }

        Console.WriteLine($"Cache miss on chain {chain} modules {string.Join(',', modules)}");
        await SaveCache();
        return results;
    }

    private async Task<IEnumerable<TResult>> ForAllChains<TResult>(Func<string, Task<TResult>> app) =>
        await Task.WhenAll(PactClient.RecognizedChains.Select(app));

    // public void InvalidateCache(string chain = null, string module = null, AccountIdentifier account = null)
    // {
    //     if (account == null)
    //         throw new ArgumentNullException();
    //
    //     var keys = _accountCache.Keys.ToList();
    //     
    //     foreach (var key in keys)
    //     {
    //         var parts = key.Split('$');
    //
    //         var keyChain = parts[0];
    //         var keyModule = parts[1];
    //         var keyAccount = string.Join('$', parts.Skip(2));
    //
    //         if (keyAccount != account.Name)
    //             continue;
    //
    //         if (module != null && keyModule != module)
    //             continue;
    //
    //         if (chain != null && keyChain != chain)
    //             continue;
    //
    //         _accountCache.Remove(key);
    //     }
    // }
}

public interface IChainwebQueryService
{
    Task<FungibleV2Account> GetAccountDetailsAsync(string chain, string module, AccountIdentifier account, bool ignoreCache = false);
    Task<IEnumerable<FungibleV2Account>> GetAccountDetailsAsync(string module, AccountIdentifier account, bool ignoreCache = false);
    Task<IEnumerable<FungibleV2Account>> GetAccountDetailsAsync(string chain, string[] modules, AccountIdentifier[] accounts, bool ignoreCache = false);
    Task<IEnumerable<FungibleV2Account>> GetAccountDetailsAsync(string[] modules, AccountIdentifier[] accounts, bool ignoreCache = false);
    Task<IEnumerable<PactModuleMetadata>> GetModuleMetadataAsync(string chain, string[] modules);
    Task<PactModuleMetadata> GetModuleMetadataAsync(string chain, string module);
    Task<bool> ModuleExistsAsync(string chain, string module);
    //void InvalidateCache(string chain = null, string module = null, AccountIdentifier account = null);
    Task Initialize();
}

public class FungibleV2Account : ICacheable
{
    public string CacheKey => GetCacheKey(Network, Chain, Module, Account);
    public static string GetCacheKey(string network, string chain, string module, string account) =>
        $"fungible-v2@{network}${chain}${module}${account}";
    
    public string Network { get; set; }
    public string Chain { get; set; }
    public string Module { get; set; }
    public string Account { get; set; }
    public decimal Balance { get; set; }
    public object Guard { get; set; }

    public static FungibleV2Account FromResponse(PactCommandResponse response, string module)
    {
        var ret = new FungibleV2Account()
        {
            Network = response.SourceCommand.Command.NetworkId,
            Chain = response.SourceCommand.Command.Metadata.ChainId,
            Module = module
        };
        
        if (response.Result?.Status != "success")
            return ret;

        ret.Guard = response.Result.Data.GetProperty("guard");
        ret.Balance = response.Result.Data.GetProperty("balance").GetDecimal();
        ret.Account = response.Result.Data.GetProperty("account").GetString();

        return ret;
    }
}