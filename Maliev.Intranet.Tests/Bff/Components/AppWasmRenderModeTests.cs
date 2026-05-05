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
        Assert.Contains("class=\"wasm-app-host\"", source);
        Assert.Contains("<WasmLogoLoadingAnimation", source);
        Assert.Contains("data-maliev-theme", source);
        Assert.Contains("maliev_theme", source);
        Assert.Contains("--wasm-logo-empty: #ffffff", source);
        Assert.Contains("--wasm-logo-fill: #000000", source);
        Assert.Contains("--maliev-logo-loader-shadow", source);
        Assert.Contains("drop-shadow(0 20px 32px rgba(10, 20, 40, 0.34))", source);
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
        Assert.Contains("--blazor-load-percentage: 0%", source);
        Assert.Contains("root.style.setProperty('--blazor-load-percentage', '0%')", source);
        Assert.Contains("--wasm-logo-progress: 0%", source);
        Assert.Contains("root.style.setProperty('--wasm-logo-progress', '0%')", source);
        Assert.Contains("root.style.setProperty('--wasm-logo-progress', progressText)", source);
        Assert.Contains("document.querySelectorAll('.maliev-logo-loader')", source);
        Assert.Contains("loader.style.setProperty('--wasm-logo-progress', progressText)", source);
        Assert.Contains("var(--wasm-logo-fill, #000000) 0 var(--wasm-logo-progress)", source);
        Assert.Contains("var(--wasm-logo-empty, #ffffff) var(--wasm-logo-progress) 100%", source);
        Assert.Contains("maliev-wasm-loader-visible", source);
        Assert.Contains("linear-gradient(", source);
        Assert.Contains("90deg", source);
        Assert.DoesNotContain("wasm-loading-progress", source, StringComparison.Ordinal);
        Assert.Contains("autostart=\"false\"", source);
        Assert.Contains("Blazor.start", source);
        Assert.Contains("loadBootResource", source);
        Assert.Contains("window.malievWasmLoader.markRuntimeReady", source);
        Assert.Contains("window.malievMarkWasmReady = window.malievWasmLoader.markReady", source);
        Assert.DoesNotContain("conic-gradient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("html.maliev-wasm-runtime-ready .wasm-loading .maliev-logo-loader", source, StringComparison.Ordinal);
        Assert.DoesNotContain("animation: maliev-logo-load", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@@keyframes maliev-logo-load", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppRoot_FadesRenderedWasmAppInAfterLoaderCompletes()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");

        Assert.Contains(".wasm-app-host", source);
        Assert.Contains("opacity: 0", source);
        Assert.Contains("transform: translateY(6px)", source);
        Assert.Contains("html.maliev-wasm-ready .wasm-app-host", source);
        Assert.Contains("opacity: 1", source);
        Assert.Contains("window.malievWasmLoader.markReady", source);
        Assert.Contains("prefers-reduced-motion: reduce", source);
    }

    [Fact]
    public void ClientRoutes_MarksStaticWasmLoaderReadyAfterFirstRender()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Routes.razor");

        Assert.Contains("@inject IJSRuntime", source);
        Assert.Contains("OnAfterRenderAsync", source);
        Assert.Contains("malievMarkWasmReady", source);
        Assert.DoesNotContain("eval", source, StringComparison.Ordinal);
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
