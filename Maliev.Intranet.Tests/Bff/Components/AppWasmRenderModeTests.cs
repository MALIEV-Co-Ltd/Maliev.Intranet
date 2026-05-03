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
        Assert.Contains("<WasmLogoLoadingAnimation", source);
        Assert.Contains("data-maliev-theme", source);
        Assert.Contains("maliev_theme", source);
        Assert.Contains("--maliev-logo-loader-fill: #000000", source);
        Assert.Contains("--maliev-logo-loader-fill: #ffffff", source);
        Assert.DoesNotContain("Loading workspace", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Starting the employee intranet locally.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("wasm-loading-card", source, StringComparison.Ordinal);
        Assert.DoesNotContain("wasm-loading-title", source, StringComparison.Ordinal);
        Assert.DoesNotContain("wasm-loading-subtitle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("maliev_accent_hue", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppRoot_UsesActualWasmProgressAndManualBlazorStart()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");

        Assert.Contains("--blazor-load-percentage", source);
        Assert.Contains("--wasm-logo-progress: var(--blazor-load-percentage, 0%)", source);
        Assert.Contains("linear-gradient(90deg", source);
        Assert.DoesNotContain("wasm-loading-progress", source, StringComparison.Ordinal);
        Assert.Contains("autostart=\"false\"", source);
        Assert.Contains("Blazor.start", source);
        Assert.Contains("loadBootResource", source);
        Assert.Contains("malievWasmLoader.markRuntimeReady", source);
        Assert.DoesNotContain("conic-gradient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("animation: maliev-logo-load", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@@keyframes maliev-logo-load", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientRoutes_MarksStaticWasmLoaderReadyAfterFirstRender()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Routes.razor");

        Assert.Contains("@inject IJSRuntime", source);
        Assert.Contains("OnAfterRenderAsync", source);
        Assert.Contains("malievWasmLoader.markReady", source);
        Assert.Contains("maliev-wasm-ready", source);
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
