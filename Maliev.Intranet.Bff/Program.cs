#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
using Maliev.Intranet.Bff;
using Maliev.Intranet.Bff.Clients;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    bootstrapLogger.LogInformation("Starting Intranet BFF host");

    var builder = WebApplication.CreateBuilder(args);

    // Add Service Defaults (OpenTelemetry, Health Checks, etc.)
    builder.AddServiceDefaults();
    builder.AddServiceMeters("intranet-meter");

    // Add services to the container.
    builder.Services.AddSingleton<BffMetrics>();
    builder.Services.AddHostedService<AlertBackgroundService>();
    builder.Services.AddSignalR();
    builder.Services.AddMudServices();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddTransient<UserContextHandler>();

    // Configure Authentication
    builder.AddJwtAuthentication(); // Standard platform JWT support

    builder.Services.AddAuthentication(options =>
    {
        // Default to Cookies for the web UI
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder";
        options.Events.OnTicketReceived = context =>
        {
            var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email) || !email.EndsWith("@maliev.com"))
            {
                context.Fail("Unauthorized domain. Only @maliev.com accounts are allowed.");
            }
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/login?error=" + System.Net.WebUtility.UrlEncode(context.Failure?.Message ?? "Authentication failed"));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddHttpClient<CustomerServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:CustomerService:BaseUrl"] ?? "http://maliev-customerservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<OrderServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:OrderService:BaseUrl"] ?? "http://maliev-orderservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorComponents()
        .AddInteractiveWebAssemblyComponents();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();
    app.MapStaticAssets();
    app.UseAntiforgery();

    app.MapDefaultEndpoints("intranet");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorComponents<Maliev.Intranet.Bff.Components.App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(Maliev.Intranet.Client._Imports).Assembly);

    app.MapHub<Maliev.Intranet.Bff.Hubs.NotificationHub>("/hubs/notifications");

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    bootstrapLogger.LogCritical(ex, "Intranet BFF host terminated unexpectedly during startup");
    throw;
}
finally
{
    loggerFactory.Dispose();
}
