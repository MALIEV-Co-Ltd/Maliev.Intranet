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
    Program.Log.StartingHost(bootstrapLogger, "Intranet BFF");

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
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

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

    Program.Log.ServiceStarted(logger, "Intranet BFF");
    app.Run();
}
catch (Exception ex)
{
    Program.Log.HostTerminated(bootstrapLogger, ex, "Intranet BFF");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the Intranet BFF.
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);
    }
}
