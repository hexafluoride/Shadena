using Blazored.LocalStorage;

namespace Shadena;

public class SentTransaction
{
    public string RequestKey { get; set; }
    public string NetworkId { get; set; }
    public string Chain { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TransactionHistoryService
{
    private const string HISTORY_KEY = "sentTx";
    private ILocalStorageService _localStorage;
    private List<SentTransaction> _cache;
    
    public TransactionHistoryService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<IEnumerable<SentTransaction>> GetSentTransactions()
    {
        if (_cache == null)
        {
            _cache = await _localStorage.GetItemAsync<List<SentTransaction>>(HISTORY_KEY);
            if (_cache == null)
                _cache = new();
        }
        
        return _cache;
    }

    public async Task AddSentTransaction(SentTransaction tx)
    {
        if (_cache == null)
            await GetSentTransactions();
        
        _cache.Add(tx);
        await _localStorage.SetItemAsync(HISTORY_KEY, _cache);
    }
}