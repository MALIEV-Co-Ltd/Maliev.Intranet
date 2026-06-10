using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class PartDetailCardLocalDfmTests : BunitContext
{
    public PartDetailCardLocalDfmTests()
    {
        Services.AddMudServices(conf => conf.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLogging();

        var http = new HttpClient { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(http);
        Services.AddSingleton<CurrencyService>();
        Services.AddSingleton<LayoutService>();

        Services.AddSingleton(new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl", ".step" },
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" },
            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png" },
            OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf" },
            SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        });

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static PartViewModel BasePartWithProcess(string processCode) => new()
    {
        FileId = Guid.NewGuid(),
        Name = "test-part.stl",
        ProcessCode = processCode,
    };

    [Fact]
    public void LocalDfmChip_NotShown_WhenNoLocalDfmActivity()
    {
        var part = BasePartWithProcess("FDM");

        var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

        Assert.DoesNotContain("pdc-local-dfm-chip", cut.Markup);
    }

    [Fact]
    public void LocalDfmChip_NotShown_WhenNoProcessCode()
    {
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "test-part.stl",
        };

        var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

        Assert.DoesNotContain("pdc-local-dfm-chip", cut.Markup);
    }

    [Fact]
    public void LocalDfmChip_ShowsRunning_WhenActiveLocalAttempt()
    {
        var part = BasePartWithProcess("FDM");
        part.LocalDfmRuntimeRunningProcessCode = "FDM";
        part.LocalDfmRuntimeStartedAtUtc = DateTimeOffset.UtcNow;

        var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

        Assert.Contains("pdc-local-dfm-chip--running", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--completed", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--unavailable", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--error", cut.Markup);
    }

    [Fact]
    public void LocalDfmChip_ShowsCompleted_WhenLocalDfmCompletedSuccessfully()
    {
        var part = BasePartWithProcess("FDM");
        part.LocalDfmRuntimeCompletedForProcessCode = "FDM";

        var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

        Assert.Contains("pdc-local-dfm-chip--completed", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--running", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--unavailable", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--error", cut.Markup);
    }

    [Fact]
    public void LocalDfmChip_ShowsUnavailable_WhenTerminalWithNonErrorReason()
    {
        var part = BasePartWithProcess("FDM");
        part.LocalDfmRuntimeTerminalProcessCode = "FDM";
        part.LocalDfmRuntimeTerminalReason = "runtime_unsupported";

        var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

        Assert.Contains("pdc-local-dfm-chip--unavailable", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--error", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--running", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--completed", cut.Markup);
    }

    [Theory]
    [InlineData("worker_failed")]
    [InlineData("invalid_result")]
    public void LocalDfmChip_ShowsError_WhenTerminalWithWorkerError(string terminalReason)
    {
        var part = BasePartWithProcess("FDM");
        part.LocalDfmRuntimeTerminalProcessCode = "FDM";
        part.LocalDfmRuntimeTerminalReason = terminalReason;

        var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

        Assert.Contains("pdc-local-dfm-chip--error", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--unavailable", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--running", cut.Markup);
        Assert.DoesNotContain("pdc-local-dfm-chip--completed", cut.Markup);
    }

    [Fact]
    public void LocalDfmChip_ShowsUnavailable_ForAllNonErrorTerminalReasons()
    {
        string[] unavailableReasons =
        [
            "process_code_missing",
            "no_input",
            "manifest_unavailable",
            "manifest_incompatible",
            "input_too_large",
            "asset_unavailable",
            "interactive_server_dfm_fallback_disabled",
        ];

        foreach (var reason in unavailableReasons)
        {
            var part = BasePartWithProcess("FDM");
            part.LocalDfmRuntimeTerminalProcessCode = "FDM";
            part.LocalDfmRuntimeTerminalReason = reason;

            var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

            Assert.True(
                cut.Markup.Contains("pdc-local-dfm-chip--unavailable"),
                $"Expected 'unavailable' chip for terminal reason '{reason}'");
        }
    }

    [Fact]
    public void LocalDfmChip_NotShown_WhenTerminalProcessCodeDoesNotMatchCurrentProcess()
    {
        var part = BasePartWithProcess("CNC_MILL");
        part.LocalDfmRuntimeTerminalProcessCode = "FDM";
        part.LocalDfmRuntimeTerminalReason = "worker_failed";

        var cut = Render<PartDetailCard>(p => p.Add(x => x.Part, part));

        Assert.DoesNotContain("pdc-local-dfm-chip", cut.Markup);
    }
}
