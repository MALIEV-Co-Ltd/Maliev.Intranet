using Maliev.Aspire.ServiceDefaults;
using Maliev.Intranet.Bff;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Extensions;
using Maliev.Intranet.Bff.Middleware;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
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

    // Add Service Defaults (OpenTelemetry, health checks, etc.)
    builder.AddServiceDefaults();
    builder.AddServiceMeters("intranet-meter");

    // Add services to the container.
    builder.Services.AddSingleton<BffMetrics>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.LayoutService>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.BreadcrumbService>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.ChatService>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.ISignalRCustomerService, Maliev.Intranet.Client.Services.SignalRCustomerService>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.ProductionHubService>();
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.IProjectDraftService, Maliev.Intranet.Client.Services.ProjectDraftService>();
    builder.Services.AddScoped<Maliev.Intranet.Shared.Services.IReferenceDataService, Maliev.Intranet.Bff.Services.ReferenceDataService>();
    builder.Services.AddScoped<Maliev.Intranet.Bff.Services.IChatContextResolver, Maliev.Intranet.Bff.Services.ChatContextResolver>();
    builder.Services.AddSingleton<Maliev.Intranet.Bff.Services.ChatHubService>();
    builder.Services.AddSingleton<Maliev.Intranet.Shared.Services.IMarkdownService, Maliev.Intranet.Shared.Services.MarkdownService>();
    builder.Services.AddSingleton<Maliev.Intranet.Client.Services.FileTypesSettings>(sp =>
        sp.GetRequiredService<IConfiguration>().GetSection("FileTypes").Get<Maliev.Intranet.Client.Services.FileTypesSettings>()!);
    builder.Services.AddSingleton<Maliev.Intranet.Client.Services.UploadSettings>(sp =>
        sp.GetRequiredService<IConfiguration>().GetSection("Upload").Get<Maliev.Intranet.Client.Services.UploadSettings>()!);
    builder.Services.AddSignalR();
    builder.Services.AddMudServices();
    builder.AddStandardCache("IntranetBff");
    builder.Services.AddSingleton<IFileAnalysisStatusService, FileAnalysisStatusService>();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddTransient<UserContextHandler>();
    builder.Services.AddTransient<Maliev.Intranet.Bff.Handlers.CookieForwardingHandler>();

    // Use ephemeral data protection in development to ensure cookies are invalidated on restart

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDataProtection()
            .UseEphemeralDataProtectionProvider();
    }

    // Configure Authentication
    builder.AddIAMServiceClient("IntranetBff");

    // Register IAM management client for admin operations
    builder.Services.AddHttpClient<Maliev.Intranet.Bff.Clients.IAMServiceClient>(client =>
    {
        client.BaseAddress = new Uri("http://IAMService");
    })
    .AddServiceDiscovery();

    // Named client for IAM bootstrap (no UserContextHandler - token attached manually)
    builder.Services.AddHttpClient("IAMServiceBootstrap", client =>
    {
        client.BaseAddress = new Uri("http://IAMService");
    })
    .AddServiceDiscovery();

    // AddJwtAuthentication registers JwtBearerDefaults.AuthenticationScheme ("Bearer")
    builder.AddJwtAuthentication();
    builder.Services.AddPermissionAuthorization();

    builder.Services.AddAuthentication(options =>
    {
        // Use a policy scheme to choose between Cookies and JWT Bearer for authentication
        options.DefaultAuthenticateScheme = "SmartScheme";
        // Always challenge and forbid using Cookies to ensure browser redirects
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme("SmartScheme", "SmartScheme", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                return JwtBearerDefaults.AuthenticationScheme;
            }

            // Default to Cookies for all browser-based requests
            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })

    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "Maliev.Intranet.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax; // Lax allows the cookie on top-level nav (GET) and same-site AJAX calls; SameSite=Strict broke AJAX-based auth after OAuth callback
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Add cookie size limits to prevent 431 errors
        options.Cookie.MaxAge = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured");

        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true; // Required so UserContextHandler can retrieve access_token for downstream service calls

        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };

        options.Events.OnTicketReceived = async context =>
        {
            var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var fullName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var googleUserId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;

            if (!string.IsNullOrEmpty(email) && identity != null && !identity.HasClaim(c => c.Type == "email"))
                identity.AddClaim(new System.Security.Claims.Claim("email", email));

            if (string.IsNullOrEmpty(email) || !email.EndsWith("@maliev.com"))
            {
                context.Fail("Unauthorized domain.");
                return;
            }

            var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var authClient = httpClientFactory.CreateClient("AuthService");

            try
            {
                var response = await authClient.PostAsJsonAsync("/auth/v1/exchange/google", new { email, full_name = fullName, google_user_id = googleUserId });
                if (response.IsSuccessStatusCode)
                {
                    var authResult = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    var accessToken = authResult.GetProperty("access_token").GetString();
                    if (!string.IsNullOrEmpty(accessToken) && context.Properties != null)
                    {
                        // Store access_token in AuthenticationProperties instead of claims to reduce cookie size
                        context.Properties.StoreTokens(new[] {
                            new AuthenticationToken { Name = "access_token", Value = accessToken }
                        });

                        // Parse JWT to extract essential claims for cookie
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(accessToken);

                        // Only store minimal claims needed for UI personalization (not roles/permissions)
                        var essentialClaims = jwtToken.Claims.Where(c =>
                            c.Type is "sub" or "user_id" or "user_type" or
                            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email or
                            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name);

                        // Remove the old Google NameIdentifier (numeric sub) before adding platform claims
                        // to prevent SignInAsync from merging it with the new platform sub.
                        var oldNameIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                        if (oldNameIdClaim != null)
                        {
                            identity?.RemoveClaim(oldNameIdClaim);
                        }

                        foreach (var claim in essentialClaims)
                        {
                            identity?.AddClaim(new System.Security.Claims.Claim(claim.Type, claim.Value));
                        }

                        // Add access_token as a claim fallback — if cookie truncation prevents GetTokenAsync
                        // from finding the stored token, UserContextHandler can fall back to user.FindFirst("access_token")
                        identity?.AddClaim(new System.Security.Claims.Claim("access_token", accessToken));

                        // Do NOT add roles/permissions to cookie - they will be read from JWT during authorization

                        // Auto-bootstrap: promote first employee to platform owner in Development
                        // Calls promote directly — the IAM endpoint has its own guard (humanUsers.Count <= 1)
                        if (context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                        {
                            try
                            {
                                var iamFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                                using var iamHttp = iamFactory.CreateClient("IAMServiceBootstrap");
                                iamHttp.DefaultRequestHeaders.Authorization =
                                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                                await iamHttp.PostAsync("/iam/v1/principals/bootstrap/promote", null);
                            }
                            catch { /* Bootstrap is best-effort */ }
                        }
                    }
                }
            }
            catch { context.Fail("Auth exchange failed"); }
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
        // Default policy for [Authorize] attributes without a policy name.
        // Explicitly specify both Bearer and Cookies schemes for BFF scenarios (Blazor SSR + API)
        options.DefaultPolicy = new AuthorizationPolicyBuilder(
            JwtBearerDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();

        // DO NOT use FallbackPolicy here - it breaks static file serving
        // Instead, we require authorization explicitly on Razor Components below
    });

    // Fix auth cookie size limit: store tickets server-side instead of in the cookie
    // The cookie will only hold the user's sub claim (~36 bytes) instead of the full ~7KB encrypted ticket
    builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.SessionStore = new DistributedCacheTicketStore();
    });

    // Named client for Google OAuth callback (no UserContextHandler - pre-auth)
    builder.Services.AddHttpClient("AuthService", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var url = config["Services:AuthService:BaseUrl"];
        client.BaseAddress = new Uri(!string.IsNullOrEmpty(url) ? url : "http://AuthService");
    })
    .AddServiceDiscovery()
    .AddStandardResilienceHandler();

    // BFF service clients with user context forwarding
    builder.AddBffServiceClient<CustomerServiceClient>("CustomerService");
    builder.AddBffServiceClient<CountryServiceClient>("CountryService");
    builder.AddBffServiceClient<RegistryServiceClient>("RegistryService");
    builder.AddBffServiceClient<OrderServiceClient>("OrderService");
    builder.AddBffServiceClient<QuotationServiceClient>("QuotationService");
    builder.AddBffServiceClient<MaterialServiceClient>("MaterialService");
    builder.AddBffServiceClient<EmployeeServiceClient>("EmployeeService");
    builder.AddBffServiceClient<InvoiceServiceClient>("InvoiceService");
    builder.AddBffServiceClient<PaymentServiceClient>("PaymentService");
    builder.AddBffServiceClient<SupplierServiceClient>("SupplierService");
    builder.AddBffServiceClient<UploadServiceClient>("UploadService");
    builder.AddBffServiceClient<ChatbotServiceClient>("ChatbotService");
    builder.AddBffServiceClient<CareerServiceClient>("CareerService");
    builder.AddBffServiceClient<TimeOffServiceClient>("EmployeeService"); // TimeOff uses EmployeeService
    builder.AddBffServiceClient<ComplianceServiceClient>("ComplianceService");
    builder.AddBffServiceClient<PerformanceServiceClient>("PerformanceService");
    builder.AddBffServiceClient<CompensationServiceClient>("CompensationService");
    builder.AddBffServiceClient<DeliveryServiceClient>("DeliveryService");
    builder.AddBffServiceClient<IAccountingServiceClient, AccountingServiceClient>("AccountingService");
    builder.AddBffServiceClient<IReceiptServiceClient, ReceiptServiceClient>("ReceiptService");
    builder.AddBffServiceClient<ILifecycleServiceClient, LifecycleServiceClient>("LifecycleService");
    builder.AddBffServiceClient<IPurchaseOrderServiceClient, PurchaseOrderServiceClient>("PurchaseOrderService");
    builder.AddBffServiceClient<ILeaveServiceClient, LeaveServiceClient>("LeaveService");
    builder.AddBffServiceClient<IPricingServiceClient, PricingServiceClient>("PricingService");
    builder.AddBffServiceClient<INotificationServiceClient, NotificationServiceClient>("NotificationService");
    builder.AddBffServiceClient<IFacilityServiceClient, FacilityServiceClient>("FacilityService");
    builder.AddBffServiceClient<ProjectServiceClient>("ProjectService");
    builder.AddBffServiceClient<JobServiceClient>("JobService");
    builder.AddBffServiceClient<CurrencyServiceClient>("CurrencyService");

    // Named HTTP client with service account authentication for reference data
    builder.Services.AddHttpClient("CountryServiceAccount", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var explicitUrl = config["Services:CountryService:BaseUrl"];
        client.BaseAddress = new Uri(!string.IsNullOrEmpty(explicitUrl) ? explicitUrl : "http://CountryService");
        client.Timeout = TimeSpan.FromSeconds(90);
    })
    .AddServiceDiscovery()
    .AddHttpMessageHandler<Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler>()
    .AddStandardResilienceHandler();

    // Named HTTP clients for SeedController — uses standard service account token
    builder.Services.AddHttpClient("SeedCustomerClient", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var explicitUrl = config["Services:CustomerService:BaseUrl"];
        client.BaseAddress = new Uri(!string.IsNullOrEmpty(explicitUrl) ? explicitUrl : "http://CustomerService");
        client.Timeout = TimeSpan.FromSeconds(90);
    })
    .AddServiceDiscovery()
    .AddHttpMessageHandler(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var tokenProvider = new Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountTokenProvider(config, "IntranetBff");
        return new Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler(tokenProvider, loggerFactory.CreateLogger<Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler>());
    })
    .AddStandardResilienceHandler();

    // Named HttpClient for UploadServiceClient used inside MassTransit consumers.
    // Consumers run outside the HTTP pipeline (no HttpContext), so this uses
    // ServiceAccountAuthenticationHandler instead of UserContextHandler.
    builder.Services.AddHttpClient("UploadServiceClient.Consumer", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var explicitUrl = config["Services:UploadService:BaseUrl"];
        client.BaseAddress = new Uri(!string.IsNullOrEmpty(explicitUrl) ? explicitUrl : "http://UploadService");
        client.Timeout = TimeSpan.FromSeconds(120);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { MaxConnectionsPerServer = 20 })
    .AddServiceDiscovery()
    .AddHttpMessageHandler(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var tokenProvider = new Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountTokenProvider(config, "IntranetBff");
        return new Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler(tokenProvider, loggerFactory.CreateLogger<Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler>());
    })
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient("SeedCountryClient", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var explicitUrl = config["Services:CountryService:BaseUrl"];
        client.BaseAddress = new Uri(!string.IsNullOrEmpty(explicitUrl) ? explicitUrl : "http://CountryService");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddServiceDiscovery()
    .AddHttpMessageHandler(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var tokenProvider = new Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountTokenProvider(config, "IntranetBff");
        return new Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler(tokenProvider, loggerFactory.CreateLogger<Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler>());
    })
    .AddStandardResilienceHandler();

    builder.Services.AddHttpClient("ServiceHealthCheck")
    .AddServiceDiscovery();

    builder.Services.AddHttpClient("BffInternal")
    .AddHttpMessageHandler<Maliev.Intranet.Bff.Handlers.CookieForwardingHandler>()
    .AddServiceDiscovery()
    .AddStandardResilienceHandler();

    builder.Services.AddScoped(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var client = httpClientFactory.CreateClient("BffInternal");
        client.BaseAddress = new Uri(navigationManager.BaseUri);
        return client;
    });

    // Register authentication state provider that persists state for interactive components

    builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
    builder.Services.AddCascadingAuthenticationState();

    // MassTransit + RabbitMQ — consume GeometryService events and push to SignalR
    builder.AddMassTransitWithRabbitMq(
        configure: mt =>
        {
            mt.AddConsumer<FileAnalyzedConsumer>();
            mt.AddConsumer<FileMetricsReadyConsumer>();
            mt.AddConsumer<SmallThumbnailReadyConsumer>();
            mt.AddConsumer<PreviewImagesGeneratedConsumer>();
            mt.AddConsumer<DfmAnalysisReadyConsumer>();
            mt.AddConsumer<PriceCalculatedConsumer>();
        },
        configureRabbitMq: (ctx, cfg) =>
        {
            cfg.ReceiveEndpoint("intranet-bff-geometry-analysis", ep =>
            {
                ep.ConfigureConsumer<FileAnalyzedConsumer>(ctx);
                ep.Bind("maliev.events", b =>
                {
                    b.ExchangeType = "topic";
                    b.RoutingKey = "maliev.geometryservice.v1.analysis.completed";
                });
            });

            cfg.ReceiveEndpoint("intranet-bff-geometry-metrics", ep =>
            {
                ep.ConfigureConsumer<FileMetricsReadyConsumer>(ctx);
                ep.Bind("maliev.events", b =>
                {
                    b.ExchangeType = "topic";
                    b.RoutingKey = "maliev.geometryservice.v1.metrics.ready";
                });
            });

            cfg.ReceiveEndpoint("intranet-bff-geometry-thumbnail-small", ep =>
            {
                ep.ConfigureConsumer<SmallThumbnailReadyConsumer>(ctx);
                ep.Bind("maliev.events", b =>
                {
                    b.ExchangeType = "topic";
                    b.RoutingKey = "maliev.geometryservice.v1.thumbnail.small.ready";
                });
            });

            cfg.ReceiveEndpoint("intranet-bff-geometry-preview", ep =>
            {
                ep.ConfigureConsumer<PreviewImagesGeneratedConsumer>(ctx);
                ep.Bind("maliev.events", b =>
                {
                    b.ExchangeType = "topic";
                    b.RoutingKey = "maliev.geometryservice.v1.preview-images.generated";
                });
            });

            cfg.ReceiveEndpoint("intranet-bff-geometry-dfm", ep =>
            {
                ep.ConfigureConsumer<DfmAnalysisReadyConsumer>(ctx);
                ep.Bind("maliev.events", b =>
                {
                    b.ExchangeType = "topic";
                    b.RoutingKey = "maliev.geometryservice.v1.dfm.ready";
                });
            });

            cfg.ReceiveEndpoint("intranet-bff-pricing-calculated", ep =>
            {
                ep.ConfigureConsumer<PriceCalculatedConsumer>(ctx);
                ep.Bind("maliev.events", b =>
                {
                    b.ExchangeType = "topic";
                    b.RoutingKey = "maliev.pricingservice.v1.price.calculated";
                });
            });
        });

    builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.UseWebAssemblyDebugging();
    if (app.Environment.IsEnvironment("Testing"))
        app.UseExceptionHandler(exHandler => exHandler.Run(async ctx => ctx.Response.StatusCode = 500));
    else if (app.Environment.IsDevelopment())
        app.UseExceptionHandler();
    else
        { app.UseExceptionHandler("/Error", createScopeForErrors: true); app.UseHsts(); }

    if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.MapStaticAssets();
    app.UseAntiforgery();

    app.MapDefaultEndpoints("intranet");

    // Stale cookie handling is NOT needed here:
    // - DistributedCacheTicketStore stores tickets server-side in memory.
    // - On restart, the in-memory store is empty, so RetrieveAsync returns null.
    // - The auth middleware then treats the user as unauthenticated and redirects to /login.
    // - Ephemeral data protection key loss also causes natural auth failure without explicit deletion.
    // DO NOT delete the auth cookie unconditionally — it destroys valid sessions after login.

    app.UseAuthentication();

    // Apply JWT claims enrichment only to non-static requests
    app.UseWhen(
        context => !IsStaticResource(context.Request.Path),
        appBuilder => appBuilder.UseMiddleware<JwtClaimsEnrichmentMiddleware>());

    // Redirect unauthenticated root requests to login to avoid 401/NavigationException loops
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/" && context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.Redirect("/login");
            return;
        }
        await next();
    });

    app.UseAuthorization();

    // Do NOT add .RequireAuthorization() here - it conflicts with AuthorizeRouteView
    // Authorization is handled by AuthorizeRouteView in Routes.razor + [AllowAnonymous] on Login page
    app.MapRazorComponents<Maliev.Intranet.Bff.Components.App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(Maliev.Intranet.Client._Imports).Assembly);

    app.MapHub<Maliev.Intranet.Bff.Hubs.NotificationHub>("/hubs/notifications")
        .RequireAuthorization(new AuthorizationPolicyBuilder("SmartScheme").RequireAuthenticatedUser().Build());

    app.MapHub<Maliev.Intranet.Bff.Hubs.ChatHub>("/hubs/chat")
        .RequireAuthorization(new AuthorizationPolicyBuilder("SmartScheme").RequireAuthenticatedUser().Build());

    app.MapHub<Maliev.Intranet.Bff.Hubs.ProductionHub>("/hubs/production")
        .RequireAuthorization(new AuthorizationPolicyBuilder("SmartScheme").RequireAuthenticatedUser().Build());

    app.MapControllers()
        .RequireAuthorization(); // Require authentication for all API controllers by default


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
/// The main entry point for the Intranet BFF application.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Determines if the request path is for a static resource that should bypass JWT claims enrichment.
    /// </summary>
    /// <param name="path">The request path to check.</param>
    /// <returns>True if the path is for a static resource; otherwise, false.</returns>
    private static bool IsStaticResource(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;
        return pathValue.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
               pathValue.Contains("/hubs/", StringComparison.OrdinalIgnoreCase) ||
               pathValue.EndsWith(".styles.css", StringComparison.OrdinalIgnoreCase);
    }

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
