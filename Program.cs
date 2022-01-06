using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Shadena;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
//builder.Services.AddScoped<>()
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IAccountsManager, AccountsManager>();
builder.Services.AddScoped<ISettingsManager, SettingsManager>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<PactHttpClient>(sp => new PactHttpClient(new HttpClient(), sp.GetService<ISettingsManager>()));
builder.Services.AddScoped<IChainwebQueryService, ChainwebQueryService>();

var host = builder.Build();

await host.Services.GetService<PactHttpClient>().Initialize();
await host.Services.GetService<ICacheService>().Initialize();

await host.RunAsync();
