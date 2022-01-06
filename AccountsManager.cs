using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace Shadena;

public class AccountsManager : IAccountsManager
{
    protected ILocalStorageService LocalStorage { get; set; }

    public AccountsManager(ILocalStorageService storage)
    {
        LocalStorage = storage;
    }

    private const string STORAGE_KEY = "accounts";

    public async Task<IEnumerable<AccountIdentifier>> GetAccountsRegisteredAsync()
    {
        if (!await LocalStorage.ContainKeyAsync(STORAGE_KEY))
            return Array.Empty<AccountIdentifier>();
        
        return await LocalStorage.GetItemAsync<AccountIdentifier[]>(STORAGE_KEY);
    }

    public async ValueTask<bool> AddAccountAsync(AccountIdentifier account)
    {
        var currentAccounts = await GetAccountsRegisteredAsync();
        var newAccountsList = currentAccounts.ToList();
        newAccountsList.Add(account);

        await LocalStorage.SetItemAsync(STORAGE_KEY, newAccountsList);
        return true;
    }

    public async ValueTask<bool> AddAccountAsync(string name) => await AddAccountAsync(new AccountIdentifier(name));

    public async ValueTask<bool> RemoveAccountAsync(string name)
    {
        var currentAccounts = await GetAccountsRegisteredAsync();
        var newAccountsList = currentAccounts.ToList();
        if (newAccountsList.RemoveAll(account => account.Name == name) == 0)
            return false;

        await LocalStorage.SetItemAsync(STORAGE_KEY, newAccountsList);
        return true;
    }

    public async ValueTask<bool> RemoveAccountAsync(AccountIdentifier account) =>
        await RemoveAccountAsync(account.Name);
}

public interface IAccountsManager
{
    Task<IEnumerable<AccountIdentifier>> GetAccountsRegisteredAsync();
    ValueTask<bool> AddAccountAsync(string name);
    ValueTask<bool> RemoveAccountAsync(string name);
    ValueTask<bool> AddAccountAsync(AccountIdentifier account);
    ValueTask<bool> RemoveAccountAsync(AccountIdentifier account);
}

public class AccountIdentifier
{
    public string Name { get; set; }

    public AccountIdentifier(string name)
    {
        Name = name;
    }
}