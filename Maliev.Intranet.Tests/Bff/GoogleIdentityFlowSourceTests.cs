namespace Maliev.Intranet.Tests.Bff;

public class GoogleIdentityFlowSourceTests
{
    [Fact]
    public void IntranetGoogleSignIn_UsesOfficialGisNonceBoundExchangeAndRefreshTokens()
    {
        var repoRoot = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Bff", "Program.cs"));
        var authController = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Bff", "Controllers", "AuthController.cs"));
        var loginPage = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Bff", "Controllers", "LoginPageController.cs"));
        var clientLogin = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Client", "Pages", "Login.razor"));
        var appShell = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Bff", "Components", "App.razor"));
        var clientIndex = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Client", "wwwroot", "index.html"));
        var googleScriptPath = Path.Combine(repoRoot, "Maliev.Intranet.Bff", "wwwroot", "js", "google-identity-signin.js");
        Assert.True(File.Exists(googleScriptPath), "Expected the BFF-owned official Google Identity Services integration script.");
        var googleScript = File.ReadAllText(googleScriptPath);
        var userContextHandler = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Bff", "UserContextHandler.cs"));
        var claimsMiddleware = File.ReadAllText(Path.Combine(repoRoot, "Maliev.Intranet.Bff", "Middleware", "JwtClaimsEnrichmentMiddleware.cs"));

        Assert.Contains("https://accounts.google.com/gsi/client", loginPage, StringComparison.Ordinal);
        Assert.Contains("data-google-signin-host", loginPage, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"btn-google\"", loginPage, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"google-logo\"", loginPage, StringComparison.Ordinal);
        Assert.Contains("data-google-signin-host", clientLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"btn-google\"", clientLogin, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"google-logo\"", clientLogin, StringComparison.Ordinal);
        Assert.Contains("https://accounts.google.com/gsi/client", appShell, StringComparison.Ordinal);
        Assert.Contains("google-identity-signin.js", appShell, StringComparison.Ordinal);
        Assert.Contains("https://accounts.google.com/gsi/client", clientIndex, StringComparison.Ordinal);
        Assert.Contains("google-identity-signin.js", clientIndex, StringComparison.Ordinal);

        Assert.Contains("google.accounts.id.initialize", googleScript, StringComparison.Ordinal);
        Assert.Contains("google.accounts.id.renderButton", googleScript, StringComparison.Ordinal);
        Assert.Contains("window.malievGoogleIdentity = { initializeHost }", googleScript, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/google/nonce", googleScript, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/google", googleScript, StringComparison.Ordinal);
        Assert.Contains("nonce", googleScript, StringComparison.Ordinal);
        Assert.Contains("credential", googleScript, StringComparison.Ordinal);

        Assert.Contains("/auth/v1/exchange/google/nonce", authController, StringComparison.Ordinal);
        Assert.Contains("GoogleIdentityApplication = \"intranet\"", authController, StringComparison.Ordinal);
        Assert.Contains("application = GoogleIdentityApplication", authController, StringComparison.Ordinal);
        Assert.DoesNotContain("google_user_id", authController, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full_name", authController, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(".AddGoogle(", program, StringComparison.Ordinal);
        Assert.Contains("new Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountTokenProvider(config, \"IntranetBff\")", program, StringComparison.Ordinal);

        Assert.Contains("/auth/v1/refresh", userContextHandler, StringComparison.Ordinal);
        Assert.Contains("refresh_token", userContextHandler, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"access_token\")]", userContextHandler, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"refresh_token\")]", userContextHandler, StringComparison.Ordinal);
        Assert.DoesNotContain("/auth/v1/exchange/google", userContextHandler, StringComparison.Ordinal);
        Assert.DoesNotContain("google_user_id", userContextHandler, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("/auth/v1/refresh", claimsMiddleware, StringComparison.Ordinal);
        Assert.Contains("refresh_token", claimsMiddleware, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"access_token\")]", claimsMiddleware, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"refresh_token\")]", claimsMiddleware, StringComparison.Ordinal);
        Assert.DoesNotContain("/auth/v1/exchange/google", claimsMiddleware, StringComparison.Ordinal);
        Assert.DoesNotContain("google_user_id", claimsMiddleware, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Maliev.Intranet.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate Maliev.Intranet repository root.");
    }
}
