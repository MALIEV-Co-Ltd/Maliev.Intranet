using Maliev.Intranet.Bff;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Program.Log.StartingHost(bootstrapLogger, "Intranet BFF");

    var builder = WebApplication.CreateBuilder(args);

    // Add shared secrets from Aspire AppHost directory during local development
    if (builder.Environment.IsDevelopment())
    {
        var sharedSecretsPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "Maliev.Aspire", "Maliev.Aspire.AppHost", "sharedsecrets.json");
        if (File.Exists(sharedSecretsPath))
        {
            builder.Configuration.AddJsonFile(sharedSecretsPath, optional: true, reloadOnChange: true);
        }
    }

    // Add Service Defaults (OpenTelemetry, Health Checks, etc.)
    builder.AddServiceDefaults();
    builder.AddServiceMeters("intranet-meter");

    // Add services to the container.
    builder.Services.AddSingleton<BffMetrics>();
    builder.Services.AddHostedService<AlertBackgroundService>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.LayoutService>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.ChatService>();
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
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured");
        options.Events.OnTicketReceived = async context =>
        {
            var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var fullName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var googleUserId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var picture = context.Principal?.FindFirst("picture")?.Value;

            var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;

            // Ensure standard email claim is present for internal logic
            if (!string.IsNullOrEmpty(email) && identity != null && !identity.HasClaim(c => c.Type == "email"))
            {
                identity.AddClaim(new System.Security.Claims.Claim("email", email));
            }

            if (!string.IsNullOrEmpty(picture) && identity != null && !identity.HasClaim(c => c.Type == "picture"))
            {
                identity.AddClaim(new System.Security.Claims.Claim("picture", picture));
            }

            if (string.IsNullOrEmpty(email) || !email.EndsWith("@maliev.com"))
            {
                context.Fail("Unauthorized domain. Only @maliev.com accounts are allowed.");
                return;
            }

            // Exchange Google token for Maliev Platform JWT
            var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var authClient = httpClientFactory.CreateClient("AuthService");

            try
            {
                var exchangeRequest = new
                {
                    email = email,
                    full_name = fullName,
                    google_user_id = googleUserId
                };

                var response = await authClient.PostAsJsonAsync("/auth/v1/exchange/google", exchangeRequest);

                if (response.IsSuccessStatusCode)
                {
                    var authResult = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    var accessToken = authResult.GetProperty("access_token").GetString();
                    var refreshToken = authResult.GetProperty("refresh_token").GetString();

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Add the platform JWT to the user's claims so it can be propagated
                        identity?.AddClaim(new System.Security.Claims.Claim("access_token", accessToken));

                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            identity?.AddClaim(new System.Security.Claims.Claim("refresh_token", refreshToken));
                        }

                        // Extract user info, roles, and permissions from the platform JWT
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(accessToken);

                        // Add user_id (sub claim from JWT)
                        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            identity?.AddClaim(new System.Security.Claims.Claim("user_id", userId));
                        }

                        // Add user_type claim
                        var userType = jwtToken.Claims.FirstOrDefault(c => c.Type == "user_type")?.Value;
                        if (!string.IsNullOrEmpty(userType))
                        {
                            identity?.AddClaim(new System.Security.Claims.Claim("user_type", userType));
                        }

                        // Extract and add roles/permissions from the platform JWT
                        foreach (var claim in jwtToken.Claims.Where(c => c.Type is "roles" or "role" or "permissions"))
                        {
                            identity?.AddClaim(new System.Security.Claims.Claim(claim.Type, claim.Value));
                            if (claim.Type == "roles" || claim.Type == "role")
                            {
                                identity?.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, claim.Value));
                            }
                        }

                        logger.LogInformation("Successfully exchanged Google token for platform JWT for user {Email}", email);
                    }
                    else
                    {
                        logger.LogError("Exchange endpoint returned success but no access_token for {Email}", email);
                        context.Fail("Authentication service returned invalid response.");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogWarning("Google SSO exchange rejected for {Email}: {Error}", email, errorContent);

                    // Parse error to provide better user feedback
                    if (errorContent.Contains("inactive", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Fail("Your employee account is inactive. Please contact HR.");
                    }
                    else if (errorContent.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Fail("Employee account not found. Please contact IT support.");
                    }
                    else
                    {
                        context.Fail("Access denied. Please contact support.");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    logger.LogError("Employee lookup service unavailable during Google SSO for {Email}", email);
                    context.Fail("Authentication service is temporarily unavailable. Please try again later.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("Failed to exchange Google token for {Email}. Status: {StatusCode}, Error: {Error}",
                        email, response.StatusCode, errorContent);
                    context.Fail("Failed to complete authentication. Please try again.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception during Google SSO token exchange for {Email}", email);
                context.Fail("An error occurred during authentication. Please try again.");
            }
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/login?error=" + System.Net.WebUtility.UrlEncode(context.Failure?.Message ?? "Authentication failed"));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        // Map GCP-style permissions to Authorization Policies using type-safe constants
        var permissions = new[]
        {
            MalievPermissions.Customer.Read,
            MalievPermissions.Order.Read,
            MalievPermissions.Order.Approve,
            MalievPermissions.Iam.Manage,
            MalievPermissions.Accounting.View
        };

        foreach (var permission in permissions)
        {
            options.AddPolicy(permission, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => (c.Type == "permissions" || c.Type == "permission") && c.Value == permission)));
        }
    });

    builder.Services.AddHttpClient("AuthService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:AuthService:BaseUrl"] ?? "http://maliev-authservice-api");
    })
    .AddStandardResilienceHandler();

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

    builder.Services.AddHttpClient<IAMServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:IAMService:BaseUrl"] ?? "http://maliev-iamservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<QuotationServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:QuotationService:BaseUrl"] ?? "http://maliev-quotationservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<MaterialServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:MaterialService:BaseUrl"] ?? "http://maliev-materialservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<EmployeeServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:EmployeeService:BaseUrl"] ?? "http://maliev-employeeservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<InvoiceServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:InvoiceService:BaseUrl"] ?? "http://maliev-invoiceservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<PaymentServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:PaymentService:BaseUrl"] ?? "http://maliev-paymentservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<SupplierServiceClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:SupplierService:BaseUrl"] ?? "http://maliev-supplierservice-api");
    })
    .AddHttpMessageHandler<UserContextHandler>()
    .AddStandardResilienceHandler();

    // Register HttpClient for server-side execution of client components
    builder.Services.AddScoped(sp =>
    {
        // In server-side Blazor, we need an HttpClient that points to the local server
        var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        return new HttpClient
        {
            BaseAddress = new Uri(navigationManager.BaseUri)
        };
    });

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
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
        .AddInteractiveServerRenderMode()
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
