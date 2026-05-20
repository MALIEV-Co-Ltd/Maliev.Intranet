namespace Maliev.Intranet.Tests.Client.Layout;

public class ThemeToggleSourceTests
{
    [Fact]
    public void TopBarThemeToggle_UsesOnlyEffectiveLightOrDarkPresentation()
    {
        var topBar = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        var navMenu = ReadRepoFile("Maliev.Intranet.Client", "Layout", "NavMenu.razor");
        var navMenuStyles = ReadRepoFile("Maliev.Intranet.Client", "Layout", "NavMenu.razor.css");
        var login = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Login.razor.cs");
        var profile = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Hr", "Profile.razor");

        Assert.Contains("LayoutService.IsDarkMode", topBar, StringComparison.Ordinal);
        Assert.Contains("Icons.Material.Outlined.DarkMode", topBar, StringComparison.Ordinal);
        Assert.Contains("Icons.Material.Outlined.LightMode", topBar, StringComparison.Ordinal);
        Assert.Contains("? \"Dark Theme\"", topBar, StringComparison.Ordinal);
        Assert.Contains(": \"Light Theme\"", topBar, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoMode", topBar, StringComparison.Ordinal);
        Assert.DoesNotContain("Auto Theme", topBar, StringComparison.Ordinal);

        Assert.DoesNotContain("Auto Theme", navMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("auto-mode-gradient", navMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("auto-mode-icon", navMenuStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoMode", login, StringComparison.Ordinal);
        Assert.DoesNotContain("Toggle Theme", login, StringComparison.Ordinal);

        Assert.DoesNotContain("new(\"system\", \"System\")", profile, StringComparison.Ordinal);
        Assert.Contains("var defaultThemeMode = LayoutService.IsDarkMode ? \"dark\" : \"light\";", profile, StringComparison.Ordinal);
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
