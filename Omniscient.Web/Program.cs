using Omniscient.ServiceDefaults;
using Omniscient.Web.Clients;
using Omniscient.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("Shard1IndexerClient", client =>
{
    var baseAddresses = EnvironmentHelper.GetValue("INDEXER_BASE_ADDRESSES", builder.Configuration);
    var baseAddress = baseAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries).First();
    client.BaseAddress = new Uri(baseAddress);
});

builder.Services.AddHttpClient("Shard2IndexerClient", client =>
{
    var baseAddresses = EnvironmentHelper.GetValue("INDEXER_BASE_ADDRESSES", builder.Configuration);
    var baseAddress = baseAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries).Last();
    client.BaseAddress = new Uri(baseAddress);
});

builder.Services.AddTransient<IndexerClient>();

builder.AddServiceDefaults();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();