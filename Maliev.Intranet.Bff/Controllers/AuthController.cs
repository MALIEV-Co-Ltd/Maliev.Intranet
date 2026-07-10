using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing user authentication and identity.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController(IHttpClientFactory httpClientFactory, IWebHostEnvironment env, ILogger<AuthController> logger) : ControllerBase
{
    private const string GoogleIdentityApplication = "intranet";
    private const string GoogleIdentityFlowCookie = "Maliev.Intranet.GoogleIdentity";
    private const string GoogleIdentityFlowProtectionPurpose = "Maliev.Intranet.GoogleIdentity.Flow.v1";
    private static readonly JsonSerializerOptions SnakeCaseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Issues a one-time nonce for the browser's official Google Identity Services flow.</summary>
    [AllowAnonymous]
    [HttpPost("google/nonce")]
    public async Task<IActionResult> IssueGoogleIdentityNonce(
        [FromBody] GoogleIdentityBrowserNonceRequest request,
        CancellationToken cancellationToken)
    {
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var clientId = configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            logger.LogError("Google Identity Services client ID is not configured for Intranet.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google sign-in is temporarily unavailable.");
        }

        using var authClient = httpClientFactory.CreateClient("AuthService");
        using var nonceResponse = await authClient.PostAsJsonAsync(
            "/auth/v1/exchange/google/nonce",
            new { application = GoogleIdentityApplication },
            cancellationToken);
        if (!nonceResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("AuthService rejected the Intranet Google nonce request with {StatusCode}.", nonceResponse.StatusCode);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google sign-in is temporarily unavailable.");
        }

        var issued = await nonceResponse.Content.ReadFromJsonAsync<GoogleIdentityNonceResponse>(
            SnakeCaseJsonOptions,
            cancellationToken);
        if (issued is null || string.IsNullOrWhiteSpace(issued.Nonce) || issued.ExpiresAtUtc <= DateTime.UtcNow)
        {
            logger.LogWarning("AuthService returned an invalid Intranet Google nonce response.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google sign-in is temporarily unavailable.");
        }

        var state = new GoogleIdentityFlowState(
            issued.Nonce,
            ToSafeLocalUrl(request.ReturnUrl),
            issued.ExpiresAtUtc);
        var protectedState = ProtectGoogleIdentityFlow(state);
        Response.Cookies.Append(GoogleIdentityFlowCookie, protectedState, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Path = "/api/v1/auth/google",
            Expires = new DateTimeOffset(issued.ExpiresAtUtc, TimeSpan.Zero)
        });

        return Ok(new
        {
            clientId,
            nonce = issued.Nonce,
            expiresAtUtc = issued.ExpiresAtUtc
        });
    }

    /// <summary>Exchanges a nonce-bound GIS credential and establishes the MALIEV employee session.</summary>
    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> CompleteGoogleIdentitySignIn(
        [FromBody] GoogleIdentityBrowserExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(GoogleIdentityFlowCookie, out var protectedState) ||
            !TryUnprotectGoogleIdentityFlow(protectedState, out var state) ||
            state.ExpiresAtUtc <= DateTime.UtcNow ||
            !FixedTimeEquals(state.Nonce, request.Nonce))
        {
            DeleteGoogleIdentityFlowCookie();
            return Unauthorized("Google sign-in session is invalid or expired.");
        }

        DeleteGoogleIdentityFlowCookie();
        using var authClient = httpClientFactory.CreateClient("AuthService");
        using var exchangeResponse = await authClient.PostAsJsonAsync(
            "/auth/v1/exchange/google",
            new
            {
                credential = request.Credential,
                application = GoogleIdentityApplication,
                nonce = request.Nonce
            },
            cancellationToken);
        if (!exchangeResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("AuthService rejected the Intranet GIS credential with {StatusCode}.", exchangeResponse.StatusCode);
            return Unauthorized("Google sign-in could not be completed.");
        }

        var authResult = await exchangeResponse.Content.ReadFromJsonAsync<LoginResponse>(
            SnakeCaseJsonOptions,
            cancellationToken);
        if (authResult is null)
        {
            return Unauthorized("Google sign-in could not be completed.");
        }

        var signInResult = await SignInWithPlatformIdentityAsync(
            authResult,
            authResult.User?.Email ?? "Google employee",
            rememberMe: false,
            cancellationToken);
        if (signInResult != LoginAttemptResult.SignedIn)
        {
            return Unauthorized(signInResult == LoginAttemptResult.InvalidWorkspaceEmail
                ? WorkspaceEmailDomainPolicy.UnauthorizedDomainMessage
                : "Google sign-in could not be completed.");
        }

        return Ok(new { returnUrl = state.ReturnUrl });
    }

    /// <summary>
    /// Authenticates a user using standard corporate credentials by proxying to AuthService.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> LoginStandard([FromBody] InternalLoginRequest request)
    {
        var result = await TrySignInWithCorporateCredentialsAsync(request.Username, request.Password, request.RememberMe);
        return result switch
        {
            LoginAttemptResult.SignedIn => Ok(),
            LoginAttemptResult.InvalidWorkspaceEmail => Unauthorized(WorkspaceEmailDomainPolicy.UnauthorizedDomainMessage),
            _ => Unauthorized("Invalid corporate credentials.")
        };
    }

    /// <summary>
    /// Authenticates a user from the server-owned login form.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login-form")]
    public async Task<IActionResult> LoginForm([FromForm] InternalLoginFormRequest request)
    {
        var returnUrl = Url.IsLocalUrl(request.ReturnUrl) ? request.ReturnUrl : "/";
        var result = await TrySignInWithCorporateCredentialsAsync(request.Username, request.Password, request.RememberMe);

        if (result == LoginAttemptResult.SignedIn)
        {
            return LocalRedirect(returnUrl);
        }

        var encodedReturnUrl = System.Net.WebUtility.UrlEncode(returnUrl);
        var message = result == LoginAttemptResult.InvalidWorkspaceEmail
            ? WorkspaceEmailDomainPolicy.UnauthorizedDomainMessage
            : "Invalid credentials. Please try again.";
        var encodedError = System.Net.WebUtility.UrlEncode(message);
        return Redirect($"/login?returnUrl={encodedReturnUrl}&error={encodedError}");
    }

    private async Task<LoginAttemptResult> TrySignInWithCorporateCredentialsAsync(string username, string password, bool rememberMe)
    {
        if (!WorkspaceEmailDomainPolicy.IsAllowedEmployeeEmail(username))
        {
            logger.LogWarning("Rejected Intranet login attempt for non-workspace username {Username}", username);
            return LoginAttemptResult.InvalidWorkspaceEmail;
        }

        using var authClient = httpClientFactory.CreateClient("AuthService");

        // Proxy request to the real AuthService (snake_case property names)
        using var response = await authClient.PostAsJsonAsync("/auth/v1/login", new
        {
            username,
            password,
            user_type = "employee"
        }, HttpContext.RequestAborted);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Login failed for {Username}. Status: {StatusCode}", username, response.StatusCode);
            return LoginAttemptResult.InvalidCredentials;
        }

        var authResult = await response.Content.ReadFromJsonAsync<LoginResponse>(
            SnakeCaseJsonOptions,
            HttpContext.RequestAborted);

        if (authResult?.User == null || string.IsNullOrEmpty(authResult.AccessToken))
        {
            logger.LogWarning("Login failed for {Username}: Invalid response from AuthService", username);
            return LoginAttemptResult.InvalidCredentials;
        }

        return await SignInWithPlatformIdentityAsync(
            authResult,
            username,
            rememberMe,
            HttpContext.RequestAborted);
    }

    private async Task<LoginAttemptResult> SignInWithPlatformIdentityAsync(
        LoginResponse authResult,
        string loginIdentifier,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        var accessToken = authResult.AccessToken;
        var refreshToken = authResult.RefreshToken;

        if (env.IsDevelopment())
        {
            try
            {
                using var bootstrapClient = httpClientFactory.CreateClient("IAMServiceBootstrap");
                bootstrapClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                using var promoteResponse = await bootstrapClient.PostAsync(
                    "/iam/v1/principals/bootstrap/promote",
                    null,
                    cancellationToken);
                if ((promoteResponse.IsSuccessStatusCode || promoteResponse.StatusCode == System.Net.HttpStatusCode.BadRequest) &&
                    !string.IsNullOrWhiteSpace(refreshToken))
                {
                    using var authClient = httpClientFactory.CreateClient("AuthService");
                    using var refreshResponse = await authClient.PostAsJsonAsync(
                        "/auth/v1/refresh",
                        new { refresh_token = refreshToken },
                        cancellationToken);
                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(
                            SnakeCaseJsonOptions,
                            cancellationToken);
                        if (refreshed is not null && !string.IsNullOrWhiteSpace(refreshed.AccessToken))
                        {
                            accessToken = refreshed.AccessToken;
                            refreshToken = refreshed.RefreshToken;
                        }
                    }

                    logger.LogInformation("Auto-bootstrap check completed for {Username}.", loginIdentifier);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bootstrap auto-promotion check failed for {Username} (non-fatal)", loginIdentifier);
            }
        }

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
        {
            logger.LogWarning("Login failed for {Username}: AuthService returned an unreadable access token", loginIdentifier);
            return LoginAttemptResult.InvalidCredentials;
        }

        var jwtToken = handler.ReadJwtToken(accessToken);
        var identityEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? authResult.User.Email;

        if (!WorkspaceEmailDomainPolicy.IsAllowedEmployeeEmail(identityEmail))
        {
            logger.LogWarning("Rejected Intranet login for {Username}: AuthService returned non-workspace email", loginIdentifier);
            return LoginAttemptResult.InvalidWorkspaceEmail;
        }

        var workspaceEmail = identityEmail!.Trim();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? authResult.User.UserId),
            new Claim(ClaimTypes.Name, jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? authResult.User.Name ?? loginIdentifier),
            new Claim("email", workspaceEmail),
            new Claim("user_type", jwtToken.Claims.FirstOrDefault(c => c.Type == "user_type")?.Value ?? authResult.User.UserType),
            new Claim("permissions", MalievPermissions.Auth.SessionsRead),
            new Claim("access_token", accessToken)
        };

        if (!string.IsNullOrWhiteSpace(authResult.User.ProfileImageUrl))
        {
            claims.Add(new Claim("picture", authResult.User.ProfileImageUrl));
        }

        // Add roles and permissions from JWT
        foreach (var role in jwtToken.Claims.Where(c => c.Type == "roles"))
        {
            claims.Add(new Claim("roles", role.Value));
            claims.Add(new Claim(ClaimTypes.Role, role.Value));
        }

        foreach (var permission in jwtToken.Claims.Where(c => c.Type == "permissions"))
        {
            claims.Add(new Claim("permissions", permission.Value));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // Cookie expires in 8 hours
        };

        var sessionTokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token", Value = accessToken }
        };
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            sessionTokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
        }

        authProperties.StoreTokens(sessionTokens);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        logger.LogInformation("User {Username} logged in successfully", loginIdentifier);

        return LoginAttemptResult.SignedIn;
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // Redirect to login page after signing out
        return Redirect("/login");
    }

    /// <summary>
    /// Retrieves the current user context.
    /// </summary>
    [RequirePermission(MalievPermissions.Auth.SessionsRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("user")]
    public IActionResult GetUser()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            // Try to find the user ID from various claims
            // user_id is our platform GUID, NameIdentifier might be the source ID (string)
            var userId = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            return Ok(new UserContextDto
            {
                UserId = userId,
                DisplayName = User.Identity.Name ?? User.FindFirst("name")?.Value ?? "Unknown",
                Email = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "No email",
                ProfileImageUrl = User.GetProfileImageUrl(),
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("roles").Select(c => c.Value)).Distinct().ToList(),
                Permissions = User.FindAll("permission").Select(c => c.Value)
                        .Concat(User.FindAll("permissions").Select(c => c.Value))
                        .Distinct()
                        .ToList()
            });
        }

        return Unauthorized();
    }


    /// <summary>
    /// Diagnostic endpoint to debug user claims and tokens.
    /// </summary>
    [RequirePermission(MalievPermissions.IAM.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("debug")]
    public IActionResult DebugClaims()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            AuthenticationType = User.Identity?.AuthenticationType,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
            // DO NOT include access_token in production logs, but for debugging we can check if it exists
            HasAccessToken = User.HasClaim(c => c.Type == "access_token")
        });
    }

    private string ProtectGoogleIdentityFlow(GoogleIdentityFlowState state)
    {
        var protector = HttpContext.RequestServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(GoogleIdentityFlowProtectionPurpose);
        return protector.Protect(JsonSerializer.Serialize(state));
    }

    private bool TryUnprotectGoogleIdentityFlow(string protectedState, out GoogleIdentityFlowState state)
    {
        try
        {
            var protector = HttpContext.RequestServices
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(GoogleIdentityFlowProtectionPurpose);
            var json = protector.Unprotect(protectedState);
            var parsed = JsonSerializer.Deserialize<GoogleIdentityFlowState>(json);
            if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Nonce))
            {
                state = parsed;
                return true;
            }
        }
        catch (CryptographicException)
        {
            // Tampered or unreadable flow cookies are rejected without exposing details.
        }
        catch (JsonException)
        {
            // Invalid protected payloads are treated as an expired flow.
        }

        state = new GoogleIdentityFlowState(string.Empty, "/", DateTime.MinValue);
        return false;
    }

    private void DeleteGoogleIdentityFlowCookie()
    {
        Response.Cookies.Delete(GoogleIdentityFlowCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Path = "/api/v1/auth/google"
        });
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static string ToSafeLocalUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return returnUrl.StartsWith("/", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";
    }

    /// <summary>
    /// Internal request model for standard login.
    /// </summary>
    public class InternalLoginRequest
    {
        /// <summary>
        /// Gets or sets the corporate email address.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user password.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to persist the session.
        /// </summary>
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// Internal request model for the server-owned login form.
    /// </summary>
    public class InternalLoginFormRequest : InternalLoginRequest
    {
        /// <summary>
        /// Gets or sets the local URL to redirect to after successful authentication.
        /// </summary>
        public string ReturnUrl { get; set; } = "/";
    }

    /// <summary>Browser request for a server-bound Google Identity Services nonce.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class GoogleIdentityBrowserNonceRequest
    {
        /// <summary>Gets or sets the local route to continue to after sign-in.</summary>
        [StringLength(2048)]
        public string? ReturnUrl { get; set; }
    }

    /// <summary>Browser credential callback from the official Google Identity Services library.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class GoogleIdentityBrowserExchangeRequest
    {
        /// <summary>Gets or sets the raw GIS ID-token credential.</summary>
        [Required]
        [StringLength(8192, MinimumLength = 1)]
        public string Credential { get; set; } = string.Empty;

        /// <summary>Gets or sets the nonce supplied to GIS for this browser flow.</summary>
        [Required]
        [StringLength(256, MinimumLength = 32)]
        public string Nonce { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response from AuthService login endpoint.
    /// </summary>
    private class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public UserIdentityResponse User { get; set; } = null!;
    }

    /// <summary>
    /// User identity information from AuthService.
    /// </summary>
    private class UserIdentityResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }

        public string? ProfileImageUrl { get; set; }
    }

    private sealed class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class GoogleIdentityNonceResponse
    {
        public string Nonce { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }
    }

    private sealed record GoogleIdentityFlowState(string Nonce, string ReturnUrl, DateTime ExpiresAtUtc);

    private enum LoginAttemptResult
    {
        SignedIn,
        InvalidCredentials,
        InvalidWorkspaceEmail
    }

    /// <summary>
    /// Proxies a profile image URL to avoid CORS/auth issues with external providers (e.g., Google).
    /// </summary>
    [HttpGet("avatar")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return NotFound();
        }

        // Only allow known safe domains
        var allowedHosts = new[] { "lh3.googleusercontent.com", "lh4.googleusercontent.com", "lh5.googleusercontent.com", "lh6.googleusercontent.com", "avatars.githubusercontent.com", "platform-lookaside.fbsbx.com" };
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported image URL");
        }

        if (!allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported image host");
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Maliev-Intranet/1.0");

            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrWhiteSpace(contentType)
                || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Unsupported image content type");
            }

            var stream = await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted);

            // Cache for 1 hour
            Response.Headers.CacheControl = "public, max-age=3600";
            return File(stream, contentType);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch
        {
            return StatusCode(502);
        }
    }
}
