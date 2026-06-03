using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Maliev.Intranet.Client.Pages;

/// <summary>Login page for authenticating users with username and password.</summary>
/// <summary>Login page for authenticating users with username and password.</summary>
public partial class Login : ComponentBase
{
    /// <summary>HTTP client for API calls.</summary>
    [Inject] public HttpClient Http { get; set; } = null!;
    /// <summary>Navigation manager for routing.</summary>
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    /// <summary>Snackbar service for transient notifications.</summary>
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    /// <summary>Dialog service for modal dialogs.</summary>
    [Inject] public IDialogService DialogService { get; set; } = null!;
    /// <summary>JavaScript runtime for browser interop.</summary>
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    /// <summary>Layout service for theme management.</summary>
    [Inject] public LayoutService LayoutService { get; set; } = null!;
    /// <summary>Authentication state provider for checking login status.</summary>
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    /// <summary>Logger for this component.</summary>
    [Inject] public ILogger<Login> Logger { get; set; } = null!;


    /// <summary>The URL to redirect to after successful login.</summary>
    [Parameter]
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    /// <summary>Error message passed via query string.</summary>
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private InternalLoginModel _loginModel = new();
    private bool _isProcessing;
    private string? _errorMessage;
    private string _usernameInput = string.Empty;
    private bool _isCheckingAuth = true;
    private bool _isDarkMode;
    private LoginStep _loginStep = LoginStep.Email;

    private static readonly EmailAddressAttribute EmailValidator = new();

    // Theme-aware logo URLs
    private string LogoUrl => _isDarkMode
        ? "images/logo-white.svg"
        : "images/logo.svg";

    /// <summary>Initializes the login page, checks if user is already authenticated.</summary>
    protected override async Task OnInitializedAsync()
    {
        ReturnUrl ??= "/";
        _isDarkMode = LayoutService.IsDarkMode;

        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                Navigation.NavigateTo(ReturnUrl ?? "/", replace: true);
                return;
            }
        }
        catch { }
        _isCheckingAuth = false;
    }

    /// <summary>Displays error message from query string on first render.</summary>
    /// <summary>Displays error message from query string on first render.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!string.IsNullOrEmpty(Error))
            {
                _errorMessage = Error;
                StateHasChanged();
            }
        }
    }

    private async Task ToggleThemeAsync()
    {
        await LayoutService.ToggleModeAsync();
        _isDarkMode = LayoutService.IsDarkMode;
        StateHasChanged();
    }

    private string GetThemeIcon() => LayoutService.IsDarkMode
        ? ThemeIcons.Dark
        : Icons.Material.Outlined.LightMode;

    private string GetThemeTooltip() => LayoutService.IsDarkMode
        ? "Switch to Light Mode"
        : "Switch to Dark Mode";

    private bool IsEmailStep => _loginStep == LoginStep.Email;

    private bool EmailLooksValid
    {
        get
        {
            var email = _usernameInput.Trim();
            return EmailValidator.IsValid(email) &&
                email.EndsWith("@maliev.com", StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool CredentialsReady => EmailLooksValid && !string.IsNullOrWhiteSpace(_loginModel.Password);

    private string EmailRequirementClass => EmailLooksValid
        ? "auth-requirement-item is-met"
        : "auth-requirement-item is-pending";

    private void ContinueWithEmail()
    {
        if (!EmailLooksValid)
        {
            _errorMessage = "Use your @maliev.com email.";
            return;
        }

        _errorMessage = null;
        _loginModel.Username = _usernameInput.Trim();
        _loginStep = LoginStep.Credentials;
    }

    private void BackToEmailStep()
    {
        _loginStep = LoginStep.Email;
        _loginModel.Password = string.Empty;
    }

    private async Task HandleLogin()
    {
        _isProcessing = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            if (string.IsNullOrWhiteSpace(_usernameInput))
            {
                _errorMessage = "Email is required.";
                _isProcessing = false;
                StateHasChanged();
                return;
            }

            if (string.IsNullOrWhiteSpace(_loginModel.Password))
            {
                _errorMessage = "Password is required.";
                _isProcessing = false;
                StateHasChanged();
                return;
            }

            var fullUsername = _usernameInput.Trim();

            var response = await Http.PostAsJsonAsync("api/v1/auth/login", new
            {
                Username = fullUsername,
                Password = _loginModel.Password,
                RememberMe = _loginModel.RememberMe
            });

            if (response.IsSuccessStatusCode)
            {
                Navigation.NavigateTo(ReturnUrl ?? "/", forceLoad: true);
            }
            else
            {
                _errorMessage = "Invalid credentials. Please try again.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = "Something went wrong. Please try again.";
            Logger.LogError(ex, "Login failed");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    /// <summary>Internal model for login form data.</summary>
    /// <summary>Internal model for login form data.</summary>
    public class InternalLoginModel
    {
        /// <summary>The username for login.</summary>
        [Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>The password for login.</summary>
        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>Whether to remember the user session.</summary>
        public bool RememberMe { get; set; }
    }

    /// <summary>Disposes the component.</summary>
    public async ValueTask DisposeAsync()
    {
        // No async resources to dispose
        await Task.CompletedTask;
    }

    private enum LoginStep
    {
        Email,
        Credentials
    }

}
