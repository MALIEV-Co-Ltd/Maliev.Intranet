namespace Maliev.Intranet.Tests.Client.Layout;

public class ThemeToggleSourceTests
{
    [Fact]
    public void TopBarThemeToggle_UsesOnlyEffectiveLightOrDarkPresentation()
    {
        var topBar = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var navMenu = ReadRepoFile("Maliev.Intranet.Client", "Layout", "NavMenu.razor");
        var navMenuStyles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "NavMenu.razor.css");
        var topBarStyles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        var login = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Login.razor.cs");
        var loginStyles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Login.razor.css");
        var themeIcons = ReadRepoFile("Maliev.Intranet.Client", "ThemeIcons.cs");
        var profile = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Hr", "Profile.razor");

        Assert.Contains("LayoutService.IsDarkMode", topBar, StringComparison.Ordinal);
        Assert.Contains("ThemeIcons.Dark", topBar, StringComparison.Ordinal);
        Assert.Contains("Icons.Material.Outlined.LightMode", topBar, StringComparison.Ordinal);
        Assert.Contains("ThemeIcons.Dark", login, StringComparison.Ordinal);
        Assert.Contains("M21 14.5A8.5 8.5 0 0 1 9.5 3", themeIcons, StringComparison.Ordinal);
        Assert.DoesNotContain("Icons.Material.Outlined.DarkMode", topBar, StringComparison.Ordinal);
        Assert.Contains("? \"Dark Theme\"", topBar, StringComparison.Ordinal);
        Assert.Contains(": \"Light Theme\"", topBar, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoMode", topBar, StringComparison.Ordinal);
        Assert.DoesNotContain("Auto Theme", topBar, StringComparison.Ordinal);

        Assert.DoesNotContain("Auto Theme", navMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("auto-mode-gradient", navMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("auto-mode-icon", navMenuStyles, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .topbar-theme-toggle", topBarStyles, StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-text-primary);", topBarStyles, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .topbar-theme-toggle:hover", topBarStyles, StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-primary);", topBarStyles, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .topbar-theme-toggle .mud-icon-root", topBarStyles, StringComparison.Ordinal);
        Assert.Contains(".topbar-right ::deep .topbar-theme-toggle .topbar-theme-icon path[d^=\"M21 14.5\"]", topBarStyles, StringComparison.Ordinal);
        Assert.Contains("fill: none;", topBarStyles, StringComparison.Ordinal);
        Assert.Contains("stroke: currentColor;", topBarStyles, StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-text-primary);", loginStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoMode", login, StringComparison.Ordinal);
        Assert.DoesNotContain("Toggle Theme", login, StringComparison.Ordinal);

        Assert.DoesNotContain("new(\"system\", \"System\")", profile, StringComparison.Ordinal);
        Assert.Contains("var defaultThemeMode = LayoutService.IsDarkMode ? \"dark\" : \"light\";", profile, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_ProfileImage_UsesProxiedUrl()
    {
        var topBar = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");

        // The proxy wraps Google CDN URLs through the BFF avatar endpoint to avoid
        // any network/browser policy issues with direct cross-origin image requests.
        Assert.Contains("GetProxiedProfileImageUrl(GetProfileImageUrl(auth.User))", topBar, StringComparison.Ordinal);

        // Make sure the raw (unproxied) form is gone.
        Assert.DoesNotContain("GetProfileImageUrl(auth.User);", topBar, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
