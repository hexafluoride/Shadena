using Blazored.LocalStorage;
using PactSharp;
using PactSharp.Types;

namespace Shadena;

public class SessionStorageKeypairManager : IKeypairManager
{
    protected ILocalStorageService LocalStorage { get; set; }
    private List<PactKeypair> _keypairs = null;

    public SessionStorageKeypairManager(ILocalStorageService storage)
    {
        LocalStorage = storage;
    }

    private const string STORAGE_KEY = "keypairs";

    public async Task<PactKeypair[]> GetKeypairsAsync()
    {
        if (_keypairs != null)
            return _keypairs.ToArray();
        
        if (!await LocalStorage.ContainKeyAsync(STORAGE_KEY))
            return Array.Empty<PactKeypair>();

        _keypairs = (await LocalStorage.GetItemAsync<string[]>(STORAGE_KEY)).Select(str => new PactKeypair(str))
            .ToList();
        
        return _keypairs.ToArray();
    }

    private async Task WriteKeypairs(IEnumerable<PactKeypair> keypairs)
    {
        var enumerated = keypairs.ToList();

        if (_keypairs == null)
            _keypairs = new List<PactKeypair>();
        
        _keypairs.Clear();
        _keypairs.AddRange(enumerated);
        await LocalStorage.SetItemAsync(STORAGE_KEY, _keypairs.Select(k => k.PrivateKey.ToHexString()));
    }

    public async Task<bool> AddKeypairAsync(string privateKey)
    {
        var currentKeypairs = await GetKeypairsAsync();
        var newKeypairsList = currentKeypairs.ToList();
        newKeypairsList.Add(new PactKeypair(privateKey));

        await WriteKeypairs(newKeypairsList);
        return true;
    }

    public async Task<bool> RemoveKeypairAsync(string publicKey)
    {
        var currentKeypairs = await GetKeypairsAsync();
        var newKeypairsList = currentKeypairs.ToList();
        newKeypairsList.RemoveAll(p => p?.PublicKey?.SequenceEqual(publicKey.ToByteArray()) == true);

        await WriteKeypairs(newKeypairsList);
        return true;
    }

    public async Task<bool> RemoveKeypairAsync(PactKeypair keypair)
    {
        return await RemoveKeypairAsync(keypair.PublicKey.ToHexString());
    }

    public async Task<PactKeypair> GetKeypairByPublicKeyAsync(string publicKey)
    {
        var currentKeypairs = await GetKeypairsAsync();
        var publicKeyArray = publicKey.ToByteArray();

        return currentKeypairs.FirstOrDefault(k => k.PublicKey.SequenceEqual(publicKeyArray));
    }
}

public interface IKeypairManager
{
    Task<PactKeypair[]> GetKeypairsAsync();
    Task<bool> AddKeypairAsync(string privateKey);
    Task<bool> RemoveKeypairAsync(string publicKey);
    Task<bool> RemoveKeypairAsync(PactKeypair keypair);
    Task<PactKeypair> GetKeypairByPublicKeyAsync(string publicKey);
}