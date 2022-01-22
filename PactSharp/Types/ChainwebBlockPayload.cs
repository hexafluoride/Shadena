using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using PactSharp.Services;

namespace PactSharp.Types;

public class ChainwebBlockPayload : ICacheable
{
    public string CacheKey => GetCacheKey(PayloadHash);

    public static string GetCacheKey(string payloadHash) => $"block-payload@{payloadHash}";
    
    public string PayloadHash { get; set; }
    public string TransactionsHash { get; set; }
    public string OutputsHash { get; set; }
    public string MinerData { get; set; }
    public string Coinbase { get; set; }
    public string[][] Transactions { get; set; } = Array.Empty<string[]>();

    private PactCommand[] _deserialized { get; set; } = Array.Empty<PactCommand>();
    private Dictionary<string, int> _txHashes { get; set; } = new();

    public async Task<PactCommand> GetTransaction(string requestKey)
    {
        if (_txHashes.ContainsKey(requestKey))
        {
            Console.WriteLine($"Found tx {requestKey} in block payload {PayloadHash}");
            return _deserialized[_txHashes[requestKey]];
        }

        if (_txHashes.Count == Transactions.Length)
        {
            Console.WriteLine($"Could not find tx {requestKey} in block payload {PayloadHash}");
            return null;
        }

        _deserialized = new PactCommand[Transactions.Length];
        await Task.Run(delegate
        {
            for (int i = 0; i < Transactions.Length; i++)
            {
                var encoded = Transactions[i][0];
                var decoded = Base64UrlTextEncoder.Decode(encoded);
                var command = JsonSerializer.Deserialize<PactCommand>(decoded, PactClient.PactJsonOptions);
                command.SetCommand(command.CommandEncoded);
                _txHashes[command.Hash] = i;
                _deserialized[i] = command;
            }
        });

        return await GetTransaction(requestKey);
    }
}