using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing user authentication and identity.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController(IHttpClientFactory httpClientFactory, IWebHostEnvironment env, ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Initiates the login process using Google Workspace.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string returnUrl = "/")
    {
        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, GoogleDefaults.AuthenticationScheme);
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

        var authClient = httpClientFactory.CreateClient("AuthService");

        // Proxy request to the real AuthService (snake_case property names)
        var response = await authClient.PostAsJsonAsync("/auth/v1/login", new
        {
            username,
            password,
            user_type = "employee"
        });

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Login failed for {Username}. Status: {StatusCode}", username, response.StatusCode);
            return LoginAttemptResult.InvalidCredentials;
        }

        // Read and deserialize response (AuthService returns snake_case)
        var rawResponse = await response.Content.ReadAsStringAsync();
        var authResult = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(rawResponse, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        });

        if (authResult?.User == null || string.IsNullOrEmpty(authResult.AccessToken))
        {
            logger.LogWarning("Login failed for {Username}: Invalid response from AuthService", username);
            return LoginAttemptResult.InvalidCredentials;
        }

        // Parse JWT to extract claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(authResult.AccessToken);
        var identityEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? authResult.User.Email;

        if (!WorkspaceEmailDomainPolicy.IsAllowedEmployeeEmail(identityEmail))
        {
            logger.LogWarning("Rejected Intranet login for {Username}: AuthService returned non-workspace email", username);
            return LoginAttemptResult.InvalidWorkspaceEmail;
        }

        var workspaceEmail = identityEmail!.Trim();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? authResult.User.UserId),
            new Claim(ClaimTypes.Name, jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? authResult.User.Name ?? username),
            new Claim("email", workspaceEmail),
            new Claim("user_type", jwtToken.Claims.FirstOrDefault(c => c.Type == "user_type")?.Value ?? authResult.User.UserType),
            new Claim("permissions", MalievPermissions.Auth.SessionsRead),
            new Claim("access_token", authResult.AccessToken)
        };

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

        // Store access token in authentication properties for middleware access
        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = authResult.AccessToken }
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        logger.LogInformation("User {Username} logged in successfully", username);

        // Auto-bootstrap: promote first employee to platform owner in Development
        // Calls promote directly — the IAM endpoint has its own guard (humanUsers.Count <= 1)
        if (env.IsDevelopment())
        {
            try
            {
                using var bootstrapClient = httpClientFactory.CreateClient("IAMServiceBootstrap");
                bootstrapClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                var promoteResp = await bootstrapClient.PostAsync("/iam/v1/principals/bootstrap/promote", null);
                if (promoteResp.IsSuccessStatusCode)
                {
                    logger.LogInformation("Auto-bootstrapped first user {Username} as platform owner", username);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bootstrap auto-promotion check failed for {Username} (non-fatal)", username);
            }
        }

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
    }

    private enum LoginAttemptResult
    {
        SignedIn,
        InvalidCredentials,
        InvalidWorkspaceEmail
    }
}
