namespace Maliev.Intranet.Tests.Bff.Components;

public class AppWasmRenderModeTests
{
    [Fact]
    public void AppRoot_RendersRoutesAndHeadOutlet_AsInteractiveWebAssembly()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");

        Assert.Contains("HeadOutlet @rendermode=\"WasmRenderMode\"", source);
        Assert.Contains("Routes @rendermode=\"WasmRenderMode\"", source);
        Assert.Contains("new InteractiveWebAssemblyRenderMode(prerender: false)", source);
        Assert.DoesNotContain("InteractiveServer", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppRoot_IncludesStaticWasmLoadingScreen_WithThemeCookieBootstrap()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");

        Assert.Contains("id=\"wasm-loading\"", source);
        Assert.Contains("Loading workspace", source);
        Assert.Contains("data-maliev-theme", source);
        Assert.Contains("maliev_theme", source);
        Assert.Contains("maliev_accent_hue", source);
    }

    [Fact]
    public void Program_RegistersOnlyInteractiveWebAssemblyComponents_ForRazorShell()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Program.cs");

        Assert.Contains("AddInteractiveWebAssemblyComponents()", source);
        Assert.Contains("AddInteractiveWebAssemblyRenderMode()", source);
        Assert.DoesNotContain("AddInteractiveServerComponents", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddInteractiveServerRenderMode", source, StringComparison.Ordinal);
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
