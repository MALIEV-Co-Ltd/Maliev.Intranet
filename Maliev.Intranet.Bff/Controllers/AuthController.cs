using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Shared;
using System.Security.Claims;
using System.Net.Http.Json;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing user authentication and identity.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Initiates the login process using Google Workspace.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Authenticates a user using standard corporate credentials by proxying to AuthService.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> LoginStandard([FromBody] InternalLoginRequest request)
    {
        var authClient = httpClientFactory.CreateClient();
        authClient.BaseAddress = new Uri(configuration["Services:AuthService:BaseUrl"] ?? "http://maliev-authservice-api");

        // Proxy request to the real AuthService (snake_case property names)
        var response = await authClient.PostAsJsonAsync("/auth/v1/login", new
        {
            username = request.Username,
            password = request.Password,
            user_type = "employee"
        });

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Login failed for {Username}. Status: {StatusCode}", request.Username, response.StatusCode);
            return Unauthorized("Invalid corporate credentials.");
        }

        // Read and deserialize response (AuthService returns snake_case)
        var rawResponse = await response.Content.ReadAsStringAsync();
        var authResult = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(rawResponse, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        });

        if (authResult == null || string.IsNullOrEmpty(authResult.AccessToken))
        {
            logger.LogWarning("Login failed for {Username}: Invalid response from AuthService", request.Username);
            return Unauthorized();
        }

        // Parse JWT to extract claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(authResult.AccessToken);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? authResult.User.UserId),
            new Claim(ClaimTypes.Name, jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? authResult.User.Name ?? request.Username),
            new Claim("email", jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? authResult.User.Email ?? request.Username),
            new Claim("user_type", jwtToken.Claims.FirstOrDefault(c => c.Type == "user_type")?.Value ?? authResult.User.UserType),
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
        var authProperties = new AuthenticationProperties { IsPersistent = request.RememberMe };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        logger.LogInformation("User {Username} logged in successfully", request.Username);
        return Ok();
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    /// <summary>
    /// Retrieves the current user context.
    /// </summary>
    [HttpGet("user")]
    public IActionResult GetUser()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(new UserContextDto
            {
                UserId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty,
                DisplayName = User.Identity.Name ?? "Unknown",
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            });
        }

        return Unauthorized();
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
}
