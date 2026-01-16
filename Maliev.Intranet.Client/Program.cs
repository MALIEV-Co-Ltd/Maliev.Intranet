using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Maliev.Intranet.Client;
using Maliev.Intranet.Client.Services;
using MudBlazor.Services;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, BffAuthenticationStateProvider>();
builder.Services.AddScoped<LayoutService>();
builder.Services.AddScoped<MockDataService>();
builder.Services.AddMudServices();


// HttpClient for BFF communication - cookies are handled automatically by the browser
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// OpenTelemetry disabled for WebAssembly due to platform compatibility issues
// Telemetry is handled by the BFF server-side instead

await builder.Build().RunAsync();