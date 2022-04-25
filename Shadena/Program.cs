using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PactSharp;
using PactSharp.Services;
using Shadena;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IAccountsManager, AccountsManager>();
builder.Services.AddScoped<IKeypairManager, SessionStorageKeypairManager>();
builder.Services.AddScoped<SessionStorageSettingsManager>();
builder.Services.AddScoped<ICacheService, SessionStorageCacheService>();
builder.Services.AddScoped<PactClient>(sp => new PactClient(new HttpClient()));
builder.Services.AddScoped<IChainwebQueryService, ChainwebQueryService>();

var host = builder.Build();

var appSettings = await host.Services.GetService<SessionStorageSettingsManager>().GetSettingsAsync();
await host.Services.GetService<PactClient>().UpdateSettings(appSettings.GenerateClientSettings());
await host.Services.GetService<ICacheService>().Initialize();

await host.RunAsync();
