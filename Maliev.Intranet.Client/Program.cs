using Maliev.Intranet.Client;
using Maliev.Intranet.Client.Authorization;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly", LogLevel.Warning);
builder.Logging.AddFilter("Maliev.Intranet.Client.Components.Project.PartConfigSidebar", LogLevel.Warning);

builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Resolve authenticated employee state from the BFF cookie after the WASM shell starts.
builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddScoped<LayoutService>();
builder.Services.AddScoped<CurrencyService>();
builder.Services.AddScoped<IProjectDraftService, ProjectDraftService>();
builder.Services.AddScoped<CookieProvider>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<BreadcrumbService>();
builder.Services.AddScoped<IReferenceDataService, ClientReferenceDataService>();
builder.Services.AddSingleton<FileTypesSettings>(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("FileTypes").Get<FileTypesSettings>()!);
builder.Services.AddSingleton<UploadSettings>(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("Upload").Get<UploadSettings>()!);
builder.Services.AddSingleton<IMarkdownService, MarkdownService>();
builder.Services.AddMudServices();


// HttpClient for BFF communication - cookies are handled automatically by the browser
// Use HttpClient factory with better lifetime management and connection pooling
builder.Services.AddHttpClient("MalievAPI", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add convenience accessor for components
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("MalievAPI");
});

// SignalR services for real-time updates
builder.Services.AddScoped<ISignalRCustomerService, SignalRCustomerService>();
builder.Services.AddScoped<ProductionHubService>();

// OpenTelemetry disabled for WebAssembly due to platform compatibility issues
// Telemetry is handled by the BFF server-side instead

await builder.Build().RunAsync();
