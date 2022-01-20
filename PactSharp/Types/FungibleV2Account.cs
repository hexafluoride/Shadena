using PactSharp.Services;

namespace PactSharp.Types;

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