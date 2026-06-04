using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Components.Shared;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>Sidebar panel for configuring a part's manufacturing process, routing, pricing, and scheduling.</summary>
public partial class PartConfigSidebar : ComponentBase
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


    /// <summary>The part to configure. When null, an empty state message is shown.</summary>
    [Parameter] public PartViewModel? Part { get; set; }

    /// <summary>The list of available manufacturing processes to select from.</summary>
    [Parameter] public List<ProcessDto> Processes { get; set; } = [];

    /// <summary>The temporary project identifier passed through to <see cref="DrawingUploadSection"/>.</summary>
    [Parameter] public Guid TempProjectId { get; set; }

    /// <summary>
    /// The customer identifier that owns this project. Null if no customer is selected yet.
    /// Passed through to <see cref="DrawingUploadSection"/>.
    /// </summary>
    [Parameter] public Guid? CustomerId { get; set; }

    /// <summary>Callback invoked after any mutation to the part configuration.</summary>
    [Parameter] public EventCallback<PartViewModel> OnPartChanged { get; set; }

    /// <summary>Callback invoked when the user wants to upload a revised CAD model for this part.</summary>
    [Parameter] public EventCallback OnReviseModel { get; set; }

    /// <summary>Controls the layout mode of the sidebar. Sidebar renders the full-width panel; Inline renders a compact version for use inside cards.</summary>
    [Parameter] public PartConfigSidebarDisplayMode DisplayMode { get; set; } = PartConfigSidebarDisplayMode.Sidebar;

    /// <summary>The active project editor layout mode.</summary>
    [Parameter] public LayoutMode LayoutMode { get; set; } = LayoutMode.Configurator;

    /// <summary>Callback invoked when the user requests a different project editor layout.</summary>
    [Parameter] public EventCallback<LayoutMode> OnLayoutModeChanged { get; set; }

    private bool _routingExpanded;
    private List<BulkPricingTable.BulkTier> _bulkTiers = [];
    private IReadOnlyCollection<string> _selectedFeatures = [];
    private readonly Dictionary<Guid, decimal> _basePricesByPart = new();
    private readonly Dictionary<Guid, decimal> _lastSeenPricesByPart = new();
    private readonly Dictionary<Guid, List<BulkPricingTable.BulkTier>> _bulkTiersByPart = new();
    private int _lastQuantity;
    private ElementReference _processRowElement;
    // Finish id → absolute additional unit cost in THB.
    private Dictionary<Guid, decimal> _finishPrices = new();

    // ── Two-phase DFM analysis state ─────────────────────────────────────
    private readonly Dictionary<string, DfmAnalysisResponse> _dfmReports = new(StringComparer.OrdinalIgnoreCase);
    // Typed per-process report cache: keyed by ProcessCode so CNC_TURN and CNC_MILL stay separate.
    private readonly Dictionary<string, object> _typedReportsByProcess = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _dfmCacheFileId;
    // Cancellation token for the currently-running DFM analysis request.
    private CancellationTokenSource? _dfmCts;
    /// <summary>Logger for two-phase DFM operations.</summary>
    [Inject] public ILogger<PartConfigSidebar> Logger { get; set; } = null!;

    private const string PaintColorHexKey = "paint_color_hex";
    private const string PaintColorReferenceKey = "paint_color_reference";
    private const string MaterialColorKey = "material_color";
    private const string PowderCoatColorKey = "powder_coat_color";
    private const string PowderFusionNaturalGreyColor = "Natural Grey";
    private const string MaterialImageBasePath = "/images/materials/";
    private const string PowderFusionRawImage = "natural-grey-plastic-part-material.png";
    private const string PowderFusionDyedBlackImage = "dyed-black-powder-fusion-part-material.png";

    private static readonly ProcessConfigOptionDto PowderCoatSyntheticColorOption = new(
        Guid.Empty,
        PowderCoatColorKey,
        "Powder coat color",
        "color",
        "Black",
        null,
        null,
        "Select the powder coating color.",
        false,
        0);

    private static readonly Dictionary<string, string> PowderFusionColorImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = PowderFusionDyedBlackImage,
        ["matteblack"] = PowderFusionDyedBlackImage,
        ["dyedblack"] = PowderFusionDyedBlackImage,
        ["red"] = "dyed-red-powder-fusion-part-material.png",
        ["blue"] = "dyed-blue-powder-fusion-part-material.png",
        ["green"] = "dyed-green-powder-fusion-part-material.png",
        ["yellow"] = "dyed-yellow-powder-fusion-part-material.png",
        ["orange"] = "dyed-orange-powder-fusion-part-material.png",
        ["pink"] = "dyed-pink-powder-fusion-part-material.png",
    };

    private static readonly Dictionary<string, string> MaterialImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["al6061"] = "aluminum-6061-part-material.png",
        ["aluminum6061t6"] = "aluminum-6061-part-material.png",
        ["al6061t6"] = "aluminum-6061-part-material.png",
        ["al7075"] = "aluminum-7075-part-material.png",
        ["aluminum7075t6"] = "aluminum-7075-part-material.png",
        ["al7075t6"] = "aluminum-7075-part-material.png",
        ["aluminum"] = "aluminum-6061-part-material.png",
        ["aluminium"] = "aluminum-6061-part-material.png",
        ["ss304"] = "stainless-304-part-material.png",
        ["stainlesssteel304"] = "stainless-304-part-material.png",
        ["ss316"] = "stainless-316-part-material.png",
        ["stainlesssteel316l"] = "stainless-316-part-material.png",
        ["stainless"] = "stainless-304-part-material.png",
        ["stainlesssteel"] = "stainless-304-part-material.png",
        ["steelmild"] = "raw-metal-part-material.png",
        ["mildsteel"] = "raw-metal-part-material.png",
        ["steel"] = "raw-metal-part-material.png",
        ["brass"] = "brass-c360-part-material.png",
        ["brassc360"] = "brass-c360-part-material.png",
        ["copper"] = "copper-c110-part-material.png",
        ["copperc110"] = "copper-c110-part-material.png",
        ["titanium"] = "titanium-ti64-part-material.png",
        ["titaniumti6al4v"] = "titanium-ti64-part-material.png",
        ["ti6al4v"] = "titanium-ti64-part-material.png",
        ["delrin"] = "white-plastic-part-material.png",
        ["pom"] = "white-plastic-part-material.png",
        ["pomc"] = "white-plastic-part-material.png",
        ["acetal"] = "white-plastic-part-material.png",
        ["pla"] = "blue-plastic-part-material.png",
        ["petg"] = "clear-plastic-part-material.png",
        ["petgclear"] = "clear-plastic-part-material.png",
        ["acrylicclear"] = "clear-plastic-part-material.png",
        ["clearacrylic"] = "clear-plastic-part-material.png",
        ["pmma"] = "clear-plastic-part-material.png",
        ["abs"] = "black-plastic-part-material.png",
        ["nylon"] = "black-plastic-part-material.png",
        ["tough2000"] = "white-plastic-part-material.png",
        ["toughresin"] = "white-plastic-part-material.png",
        ["clearresin"] = "clear-plastic-part-material.png",
        ["resinclear"] = "clear-plastic-part-material.png",
        ["clear"] = "clear-plastic-part-material.png",
        ["hightemp"] = "white-plastic-part-material.png",
        ["pa12"] = "natural-plastic-part-material.png",
        ["peek"] = "peek-natural-part-material.png",
        ["metal"] = "raw-metal-part-material.png",
        ["plastic"] = "natural-plastic-part-material.png",
    };

    private static readonly Dictionary<string, string> ColorImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = "black-plastic-part-material.png",
        ["matteblack"] = "black-plastic-part-material.png",
        ["ral9005"] = "black-plastic-part-material.png",
        ["white"] = "white-plastic-part-material.png",
        ["naturalwhite"] = "white-plastic-part-material.png",
        ["ral9010"] = "white-plastic-part-material.png",
        ["blue"] = "blue-plastic-part-material.png",
        ["red"] = "red-plastic-part-material.png",
        ["yellow"] = "yellow-plastic-part-material.png",
        ["green"] = "green-plastic-part-material.png",
        ["gray"] = "natural-grey-plastic-part-material.png",
        ["grey"] = "natural-grey-plastic-part-material.png",
        ["naturalgray"] = "natural-grey-plastic-part-material.png",
        ["naturalgrey"] = "natural-grey-plastic-part-material.png",
        ["mjfnaturalgray"] = "natural-grey-plastic-part-material.png",
        ["mjfnaturalgrey"] = "natural-grey-plastic-part-material.png",
        ["natural"] = "natural-plastic-part-material.png",
        ["clear"] = "clear-plastic-part-material.png",
        ["transparent"] = "clear-plastic-part-material.png",
        ["raw"] = "raw-metal-part-material.png",
        ["silver"] = "raw-metal-part-material.png",
        ["peeknatural"] = "peek-natural-part-material.png",
    };

    private static readonly Dictionary<string, string> FinishImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["machined"] = "finish-as-machined-part-surface.png",
        ["asmachined"] = "finish-as-machined-part-surface.png",
        ["beadblast"] = "finish-bead-blast-part-surface.png",
        ["beadblasted"] = "finish-bead-blast-part-surface.png",
        ["anodizedclear"] = "finish-anodized-clear-part-surface.png",
        ["anodizeclear"] = "finish-anodized-clear-part-surface.png",
        ["anodizedblack"] = "finish-anodized-black-part-surface.png",
        ["anodizeblack"] = "finish-anodized-black-part-surface.png",
        ["anodizedred"] = "finish-painted-red-part-surface.png",
        ["anodizedblue"] = "finish-painted-blue-part-surface.png",
        ["anodizedgold"] = "brass-c360-part-material.png",
        ["anodizedgreen"] = "finish-anodized-green-part-surface.png",
        ["anodizedpurple"] = "finish-painted-blue-part-surface.png",
        ["powder"] = "finish-painted-part-surface.png",
        ["powdercoat"] = "finish-painted-part-surface.png",
        ["painted"] = "finish-painted-part-surface.png",
        ["paint"] = "finish-painted-part-surface.png",
        ["asprinted"] = "natural-plastic-part-material.png",
        ["sanded"] = "finish-bead-blast-part-surface.png",
        ["uvcured"] = "finish-painted-part-surface.png",
        ["natural"] = "finish-as-machined-part-surface.png",
        ["turned"] = "finish-as-machined-part-surface.png",
        ["raw"] = "raw-metal-part-material.png",
    };

    private static readonly Dictionary<string, string> AnodizeColorImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["clear"] = "finish-anodized-clear-part-surface.png",
        ["black"] = "finish-anodized-black-part-surface.png",
        ["red"] = "finish-painted-red-part-surface.png",
        ["blue"] = "finish-painted-blue-part-surface.png",
        ["gold"] = "brass-c360-part-material.png",
        ["green"] = "finish-painted-green-part-surface.png",
        ["purple"] = "finish-painted-blue-part-surface.png",
    };

    private static readonly Dictionary<string, string> PaintColorImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = "finish-painted-black-part-surface.png",
        ["white"] = "finish-painted-white-part-surface.png",
        ["signalred"] = "finish-painted-red-part-surface.png",
        ["trafficblue"] = "finish-painted-blue-part-surface.png",
        ["resedagreen"] = "finish-painted-green-part-surface.png",
        ["lightgray"] = "finish-painted-light-gray-part-surface.png",
        ["lightgrey"] = "finish-painted-light-gray-part-surface.png",
        ["ral9005"] = "finish-painted-black-part-surface.png",
        ["ral9010"] = "finish-painted-white-part-surface.png",
        ["ral3001"] = "finish-painted-red-part-surface.png",
        ["ral5017"] = "finish-painted-blue-part-surface.png",
        ["ral6011"] = "finish-painted-green-part-surface.png",
        ["ral7035"] = "finish-painted-light-gray-part-surface.png",
    };

    private static readonly HashSet<string> HiddenCustomerOptionKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "layer_height",
        "layerHeight",
        "layer_mm",
        "layerHeightMm",
        "print_layer_height",
        "printLayerHeight",
        "infill",
        "infill_percentage",
        "infillPercentage",
        "infill_percent",
        "infillPercent",
        "print_infill",
        "printInfill",
        "support_type",
        "supportType",
        "threaded_holes",
        "threadedHoles",
        "thread_holes",
        "threadHoles",
        "tapped_holes",
        "tappedHoles",
        "tap_holes",
        "tapHoles",
        "thread_inserts",
        "threadInserts",
        "threaded_inserts",
        "threadedInserts",
        "inserts",
        "heat_set_inserts",
        "heatSetInserts",
        "uv_cure",
        "uvCure",
    };

    private static readonly IReadOnlyList<RoughnessOption> RoughnessOptions =
    [
        new("RA_3_2", "Ra 3.2 um", "Standard machined finish"),
        new("RA_1_6", "Ra 1.6 um", "Fine visible faces"),
        new("RA_0_8", "Ra 0.8 um", "Precision cosmetic or sliding surfaces"),
        new("RA_0_4", "Ra 0.4 um", "Special polishing requirement"),
    ];

    private static readonly IReadOnlyList<InspectionOption> InspectionOptions =
    [
        new(InspectionLevel.Standard, "Standard", "Visual and basic dimensional check", Icons.Material.Outlined.FactCheck),
        new(InspectionLevel.Dimensional, "Dimensional", "Measurement report for critical dimensions", Icons.Material.Outlined.Straighten),
        new(InspectionLevel.FullCmm, "Full CMM", "Complete CMM inspection report", Icons.Material.Outlined.AssignmentTurnedIn),
    ];

    private static readonly IReadOnlyList<string> DefaultPlasticColors =
    [
        "Black",
        "White",
        "Natural",
        "Gray",
        "Blue",
        "Red",
        "Yellow",
        "Green",
    ];

    private static readonly IReadOnlyList<string> PowderFusionRawColors =
    [
        PowderFusionNaturalGreyColor,
    ];

    private static readonly IReadOnlyList<string> PowderFusionDyeColors =
    [
        "Black",
        "Red",
        "Blue",
        "Green",
        "Yellow",
        "Orange",
        "Pink",
    ];

    private static readonly IReadOnlyList<string> PomMaterialColors =
    [
        "Black",
        "White",
        "Blue",
    ];

    private static readonly IReadOnlyList<string> PeekMaterialColors =
    [
        "PEEK Natural",
    ];

    private static readonly IReadOnlyList<string> MetalRawColor =
    [
        "Raw",
    ];

    private static readonly IReadOnlyList<string> DefaultAnodizeColors =
    [
        "Clear",
        "Black",
        "Red",
        "Blue",
        "Gold",
        "Green",
        "Purple",
    ];

    private static readonly IReadOnlyList<PaintColorOption> StandardPaintColors =
    [
        new("Black", "#111111", "RAL 9005"),
        new("White", "#f7f7f2", "RAL 9010"),
        new("Signal Red", "#c8333a", "RAL 3001"),
        new("Traffic Blue", "#2f6fd6", "RAL 5017"),
        new("Reseda Green", "#2f8f5b", "RAL 6011"),
        new("Light Gray", "#9ba3af", "RAL 7035"),
    ];

    // ── Computed properties ───────────────────────────────────────────────

    private ProcessDto? SelectedProcess =>
        Processes.FirstOrDefault(p => p.Code == Part?.ProcessCode);

    private CatalogMaterialDto? SelectedMaterial =>
        Part?.AvailableMaterials.FirstOrDefault(m => m.Id == Part.MaterialId);

    private CatalogSurfaceFinishDto? SelectedFinish =>
        Part?.AvailableFinishes.FirstOrDefault(f => f.Id == Part.FinishId);

    private CatalogToleranceDto? SelectedTolerance =>
        Part?.AvailableTolerances.FirstOrDefault(t => t.Id == Part.ToleranceId);

    private bool HasSelectedProcess =>
        !string.IsNullOrWhiteSpace(Part?.ProcessCode);

    private bool CanConfigurePartFeaturesAndInspection
    {
        get
        {
            if (Part == null || !HasSelectedProcess || Part.CatalogLoading || SelectedMaterial == null)
                return false;

            if (GetVisibleFinishes(Part.AvailableFinishes).Count > 0 && SelectedFinish == null)
                return false;

            if (VisibleToleranceList.Count > 0 && SelectedTolerance == null)
                return false;

            return true;
        }
    }

    private static string GetDisabledAriaValue(bool disabled) =>
        disabled ? "true" : "false";

    private IEnumerable<CatalogToleranceDto> VisibleTolerances =>
        Part?.AvailableTolerances.Where(IsVisibleTolerance) ?? [];

    private IEnumerable<ProcessConfigOptionDto> CatalogVisibleProcessOptions =>
        (Part?.AvailableProcessOptions ?? [])
            .Where(IsVisibleProcessOption)
            .OrderBy(opt => opt.SortOrder);

    private IReadOnlyList<ProcessConfigOptionDto> VisibleFinishColorOptions
    {
        get
        {
            var catalogOptions = CatalogVisibleProcessOptions
                .Where(IsFinishColorOption)
                .OrderBy(opt => opt.SortOrder)
                .ToList();

            if (IsPowderCoatFinish() && catalogOptions.Count == 0)
                catalogOptions.Add(PowderCoatSyntheticColorOption);

            return catalogOptions;
        }
    }

    private IEnumerable<ProcessConfigOptionDto> VisibleProcessOptions =>
        CatalogVisibleProcessOptions.Where(option => !IsFinishColorOption(option));

    private bool HasCoatingSpecificColorOption =>
        (Part?.AvailableProcessOptions ?? [])
            .Any(option => IsPaintSpecificColorOption(option) || IsPowderCoatSpecificColorOption(option));

    private bool RequiresDedicatedMaterialColorSelection =>
        SelectedMaterial != null
        && (IsPomMaterial(SelectedMaterial) || IsPeekMaterial(SelectedMaterial) || IsMetalMaterial(SelectedMaterial));

    private IReadOnlyList<string> MaterialColorOptions =>
        SelectedMaterial switch
        {
            null => [],
            var m when IsPomMaterial(m) => PomMaterialColors,
            var m when IsPeekMaterial(m) => PeekMaterialColors,
            var m when IsMetalMaterial(m) => MetalRawColor,
            _ => [],
        };

    private bool IsFdmProcess =>
        IsProcess("FDM") || IsProcess("FDM_3D_PRINTING");

    private bool IsCncProcess =>
        IsProcess("CNC") || IsProcess("CNC_MILL") || IsProcess("CNC_TURN");

    private IReadOnlyList<CatalogToleranceDto> VisibleToleranceList =>
        VisibleTolerances.OrderBy(t => t.SortOrder).ToList();

    private IReadOnlyList<ToleranceOptionGroup> VisibleToleranceGroups =>
        VisibleToleranceList
            .GroupBy(GetToleranceGroupKind)
            .OrderBy(group => GetToleranceGroupSortOrder(group.Key))
            .Select(group => new ToleranceOptionGroup(
                GetToleranceGroupTitle(group.Key),
                GetToleranceGroupStandard(group.Key),
                GetToleranceGroupCssClass(group.Key),
                group.OrderBy(t => t.SortOrder).ToList()))
            .ToList();

    private string RootClass =>
        DisplayMode == PartConfigSidebarDisplayMode.Inline
            ? "pcs-root pcs-root--inline"
            : "pcs-root";

    private Task SwitchToTableEditAsync() =>
        OnLayoutModeChanged.InvokeAsync(Maliev.Intranet.Client.Components.Project.LayoutMode.SummaryTable);

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Part == null)
        {
            _bulkTiers = [];
            ResetDfmAnalysisCache(null);
            return;
        }

        ResetDfmAnalysisCache(Part.FileId);

        var features = new List<string>();
        if (Part.HasThreadedHoles) features.Add("ThreadedHoles");
        if (Part.HasInserts) features.Add("Inserts");
        _selectedFeatures = features;
        RemoveHiddenProcessOptionValues();

        var fileId = Part.FileId;
        if (fileId == Guid.Empty)
        {
            _bulkTiers = [];
            return;
        }

        if (_bulkTiersByPart.TryGetValue(fileId, out var storedTiers))
            _bulkTiers = storedTiers;

        var baseUnitPrice = Part.EstimatedBaseUnitPrice ?? Part.EstimatedUnitPrice;
        if (baseUnitPrice.HasValue)
        {
            var currentPrice = baseUnitPrice.Value;
            var isNewPrice = !_lastSeenPricesByPart.TryGetValue(fileId, out var lastSeen)
                             || currentPrice != lastSeen;

            if (isNewPrice)
            {
                _basePricesByPart[fileId] = currentPrice;
                _lastSeenPricesByPart[fileId] = currentPrice;
                Part.EstimatedBaseUnitPrice = currentPrice;
                await FetchBulkTiersAsync();
                _ = RefreshFinishPricesAsync();
            }
        }
        else if (_lastSeenPricesByPart.ContainsKey(fileId))
        {
            _lastSeenPricesByPart.Remove(fileId);
            _basePricesByPart.Remove(fileId);
            _bulkTiersByPart.Remove(fileId);
            Part.EstimatedBaseUnitPrice = null;
            _bulkTiers = [];
        }
    }

    private async Task FetchBulkTiersAsync()
    {
        var fileId = Part!.FileId;
        if (!_basePricesByPart.TryGetValue(fileId, out var basePrice)) return;
        try
        {
            var request = new BulkPricingRequestDto { BaseUnitPrice = basePrice, Quantities = [1, 2, 5, 10, 25, 50, 100] };
            var response = await Http.PostAsJsonAsync("api/v1/pricing/bulk", request);
            if (response.IsSuccessStatusCode)
            {
                var tiers = await response.Content.ReadFromJsonAsync<List<BulkPriceTierDto>>();
                if (tiers != null)
                {
                    _bulkTiers = tiers.Select(t => new BulkPricingTable.BulkTier(t.Quantity, t.UnitPrice)).ToList();
                    _bulkTiersByPart[fileId] = _bulkTiers;
                }
            }
        }
        catch
        {
            // Non-fatal — bulk pricing table stays empty
        }
    }

    // ── Event handlers ───────────────────────────────────────────────────

    private async Task OnProcessChanged(ProcessDto? p)
    {
        if (Part == null || p == null) return;

        var processChanged = ProjectPartBulkEdit.ApplyProcess(Part, p);
        await ScrollProcessIntoStartAsync(p.Code);
        if (!processChanged)
        {
            if (_dfmReports.ContainsKey(p.Code))
            {
                Logger?.LogInformation("Using cached DFM results for process {ProcessCode}", p.Code);
                ApplyTypedReportFromCache(p.Code);
                Part.ResolveDfmReport();
                await OnPartChanged.InvokeAsync(Part);
            }

            return;
        }

        // Cancel any in-flight DFM request for the previous process.
        var oldCts = _dfmCts;
        _dfmCts = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();

        // Check if we already have results for this process.
        if (_dfmReports.ContainsKey(p.Code))
        {
            Logger?.LogInformation("Using cached DFM results for process {ProcessCode}", p.Code);
            // Restore the correct typed report slot from our per-process cache.
            ApplyTypedReportFromCache(p.Code);
            Part.ResolveDfmReport();
            await OnPartChanged.InvokeAsync(Part);
            return;
        }

        // No cached result: catalog reload happens first, then DFM fills in asynchronously.
        await OnPartChanged.InvokeAsync(Part);   // materials load NOW

        // DFM runs after materials are already loading. finally block fires OnPartChanged again with DFM state.
        await AnalyzeProcessForDfm(p);
    }

    private async Task ScrollProcessIntoStartAsync(string processCode)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync(
                "malievPartConfigSidebar.scrollProcessIntoStart",
                _processRowElement,
                processCode);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or OperationCanceledException)
        {
            Logger?.LogDebug(ex, "Could not scroll selected process {ProcessCode} into view", processCode);
        }
    }

    private void ApplyTypedReportFromCache(string processCode)
    {
        if (!_typedReportsByProcess.TryGetValue(processCode, out var cached)) return;
        var upper = processCode.ToUpperInvariant();
        if (upper is "SLA" or "SLA_DLP" or "DLP")
            Part!.SlaDfmReport = cached;
        else if (upper is "CNC" or "CNC_MILL" or "CNC_TURN")
            Part!.CncDfmReport = cached;
        else
            Part!.FdmDfmReport = cached;
    }

    private async Task OnMaterialChanged(CatalogMaterialDto? m)
    {
        if (Part == null) return;
        Part.MaterialCode = m?.Code;
        Part.MaterialId = m?.Id;
        if (m == null)
        {
            Part.ProcessOptionValues.Remove(MaterialColorKey);
        }
        else if (IsPomMaterial(m))
        {
            if (!Part.ProcessOptionValues.TryGetValue(MaterialColorKey, out var color)
                || string.IsNullOrWhiteSpace(color)
                || !PomMaterialColors.Contains(color, StringComparer.OrdinalIgnoreCase))
            {
                Part.ProcessOptionValues[MaterialColorKey] = PomMaterialColors[0];
            }
        }
        else if (IsPeekMaterial(m))
        {
            Part.ProcessOptionValues[MaterialColorKey] = PeekMaterialColors[0];
        }
        else if (IsMetalMaterial(m))
        {
            Part.ProcessOptionValues[MaterialColorKey] = MetalRawColor[0];
        }
        else
        {
            Part.ProcessOptionValues.Remove(MaterialColorKey);
        }

        await OnPartChanged.InvokeAsync(Part);
        _ = RefreshFinishPricesAsync();
    }

    private async Task OnFinishChanged(CatalogSurfaceFinishDto? f)
    {
        if (Part == null) return;
        Part.FinishCode = f?.Code;
        Part.FinishId = f?.Id;
        RemoveFinishSpecificOptionValues();
        await OnPartChanged.InvokeAsync(Part);
    }

    private bool IsSelectedFinishGroup(CatalogSurfaceFinishDto finish)
    {
        if (SelectedFinish == null)
            return false;

        return string.Equals(
            GetSurfaceFinishDisplayKey(SelectedFinish),
            GetSurfaceFinishDisplayKey(finish),
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task OnToleranceChanged(CatalogToleranceDto? t)
    {
        if (Part == null) return;
        Part.ToleranceCode = t?.Code;
        Part.ToleranceId = t?.Id;
        await OnPartChanged.InvokeAsync(Part);
        _ = RefreshFinishPricesAsync();
    }

    private async Task OnProcessOptionValueChanged(string key, string? value)
    {
        if (Part == null) return;
        if (string.IsNullOrEmpty(value))
            Part.ProcessOptionValues.Remove(key);
        else
            Part.ProcessOptionValues[key] = value;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnProcessOptionBoolChanged(string key, bool value)
    {
        await OnProcessOptionValueChanged(key, value ? "true" : null);
    }

    private async Task OnMaterialColorChanged(string color)
    {
        if (Part == null) return;

        Part.ProcessOptionValues[MaterialColorKey] = color;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnPaintColorHexChanged(string catalogKey, string? value)
    {
        if (Part == null) return;

        SetProcessOptionValue(PaintColorHexKey, value);
        if (!Part.ProcessOptionValues.TryGetValue(PaintColorReferenceKey, out var reference)
            || string.IsNullOrWhiteSpace(reference))
        {
            SetProcessOptionValue(catalogKey, value);
        }

        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnPaintColorReferenceChanged(string catalogKey, string? value)
    {
        if (Part == null) return;

        SetProcessOptionValue(PaintColorReferenceKey, value);
        SetProcessOptionValue(catalogKey, value);
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnStandardPaintColorChanged(string catalogKey, PaintColorOption paint)
    {
        if (Part == null) return;

        SetProcessOptionValue(PaintColorHexKey, paint.Hex);
        SetProcessOptionValue(PaintColorReferenceKey, paint.Reference);
        SetProcessOptionValue(catalogKey, paint.Reference);
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnCustomPaintColorSelected(string catalogKey)
    {
        if (Part == null) return;

        var currentHex = GetProcessOptionValue(PaintColorHexKey, "#111111");
        var currentReference = GetProcessOptionValue(PaintColorReferenceKey, string.Empty);
        var isStandardPaint = StandardPaintColors.Any(paint => IsPaintColorSelected(paint, currentHex, currentReference));
        var customHex = isStandardPaint ? "#000000" : currentHex;
        var customReference = isStandardPaint ? string.Empty : currentReference;

        SetProcessOptionValue(PaintColorHexKey, customHex);
        SetProcessOptionValue(PaintColorReferenceKey, customReference);
        SetProcessOptionValue(catalogKey, string.IsNullOrWhiteSpace(customReference) ? customHex : customReference);
        await OnPartChanged.InvokeAsync(Part);
    }

    private void SetProcessOptionValue(string key, string? value)
    {
        if (Part == null) return;

        if (string.IsNullOrWhiteSpace(value))
            Part.ProcessOptionValues.Remove(key);
        else
            Part.ProcessOptionValues[key] = value;
    }

    private async Task RefreshFinishPricesAsync()
    {
        if (Part == null
            || string.IsNullOrEmpty(Part.ProcessCode)
            || !Part.MaterialId.HasValue
            || !Part.ToleranceId.HasValue
            || !(Part.FinishPricingBaseUnitPrice ?? Part.EstimatedUnitPrice).HasValue
            || Part.AvailableFinishes.Count == 0)
            return;

        try
        {
            var request = new FinishPriceRequestDto
            {
                ProcessCode = Part.ProcessCode,
                MaterialId = Part.MaterialId.Value,
                ToleranceId = Part.ToleranceId.Value,
                BaseUnitPrice = (Part.FinishPricingBaseUnitPrice ?? Part.EstimatedUnitPrice)!.Value,
                FinishIds = Part.AvailableFinishes.Select(f => f.Id).ToList(),
            };
            var response = await Http.PostAsJsonAsync("api/v1/pricing/finish-options", request);
            if (response.IsSuccessStatusCode)
            {
                var items = await response.Content.ReadFromJsonAsync<List<FinishPriceItemDto>>();
                if (items != null)
                {
                    _finishPrices = items.ToDictionary(i => i.FinishId, i => i.AdditionalUnitCost);
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch
        {
            // Non-fatal — finish prices will show as "incl."
        }
    }

    private string FormatFinishPrice(CatalogSurfaceFinishDto fin)
    {
        if (_finishPrices.TryGetValue(fin.Id, out var cost) && cost > 0)
            return $"+{CurrencyService.Format(cost)}";
        return "incl.";
    }

    private async Task OnRoughnessChanged(string? v)
    {
        if (Part == null) return;
        Part.RoughnessCode = v;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnFeaturesChanged(IReadOnlyCollection<string> selected)
    {
        if (Part == null || !CanConfigurePartFeaturesAndInspection) return;
        _selectedFeatures = selected;
        Part.HasThreadedHoles = selected.Contains("ThreadedHoles");
        Part.HasInserts = selected.Contains("Inserts");
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnThreadedHolesChanged(bool value)
    {
        if (Part == null || !CanConfigurePartFeaturesAndInspection) return;
        Part.HasThreadedHoles = value;
        if (!value)
        {
            Part.ThreadedHoleSpec = null;
            Part.ThreadedHoleCount = 0;
        }

        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnInsertsChanged(bool value)
    {
        if (Part == null || !CanConfigurePartFeaturesAndInspection) return;
        Part.HasInserts = value;
        if (!value)
        {
            Part.InsertType = InsertType.None;
            Part.InsertCount = 0;
        }

        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnBagAndTagChanged(IReadOnlyCollection<string> selected)
    {
        if (Part == null) return;
        Part.BagAndTag = selected.Contains("BagAndTag");
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnPartMarkingChanged(PartMarkingType markingType)
    {
        if (Part == null) return;
        Part.MarkingType = markingType;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnQuantityChanged(int qty)
    {
        if (Part == null) return;
        Part.Quantity = qty;
        _lastQuantity = qty;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnNotesChanged(string? v)
    {
        if (Part == null) return;
        Part.PartNotes = v;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnInspectionChanged(InspectionLevel level)
    {
        if (Part == null || !CanConfigurePartFeaturesAndInspection) return;
        Part.InspectionLevel = level;
        await OnPartChanged.InvokeAsync(Part);
    }

    private bool IsProcess(string processCode) =>
        string.Equals(Part?.ProcessCode, processCode, StringComparison.OrdinalIgnoreCase);

    private static string GetProcessCardName(ProcessDto process)
    {
        return NormalizeOptionText(process.Code) switch
        {
            "cnc" or "cncmill" or "cncmilling" => "CNC Milling",
            "cncturn" or "cncturning" => "CNC Turning",
            "fdm" or "fdm3dprinting" => "FDM",
            "sla" or "sladlp" or "dlp" => "SLA / DLP",
            "sls" => "SLS",
            "mjf" => "MJF",
            "mj" or "materialjetting" => "Material Jet",
            "bj" or "binderjetting" => "Binder Jet",
            "dmls" => "DMLS",
            _ => CompactProcessCardText(process.Name, process.Code),
        };
    }

    private static string GetProcessDescription(ProcessDto process)
    {
        return NormalizeOptionText(process.Code) switch
        {
            "cnc" or "cncmill" or "cncmilling" => "Milled parts",
            "cncturn" or "cncturning" => "Turned parts",
            "fdm" or "fdm3dprinting" => "3D print",
            "sla" or "sladlp" or "dlp" => "Resin print",
            "sls" => "Nylon powder",
            "mjf" => "Powder fusion",
            "mj" or "materialjetting" => "Fine detail",
            "bj" or "binderjetting" => "Binder jet",
            "dmls" => "Metal print",
            _ => "Process",
        };
    }

    private static string CompactProcessCardText(string? preferred, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred.Trim();
        var parenthesisIndex = value.IndexOf(" (", StringComparison.Ordinal);
        if (parenthesisIndex > 0)
            value = value[..parenthesisIndex];

        return value.Length <= 16 ? value : $"{value[..15]}...";
    }

    private bool IsVisibleTolerance(CatalogToleranceDto tolerance)
    {
        if (!IsFdmProcess)
            return true;

        var combined = $"{tolerance.Code} {tolerance.Name} {tolerance.IsoStandard} {tolerance.Grade}";
        return !combined.Contains("ISO 2768-c", StringComparison.OrdinalIgnoreCase)
            && !combined.Contains("ISO 2768_C", StringComparison.OrdinalIgnoreCase)
            && !combined.Contains("ISO2768_C", StringComparison.OrdinalIgnoreCase)
            && !combined.Contains("ISO 2768-v", StringComparison.OrdinalIgnoreCase)
            && !combined.Contains("ISO 2768_V", StringComparison.OrdinalIgnoreCase)
            && !combined.Contains("ISO2768_V", StringComparison.OrdinalIgnoreCase);
    }

    private static ToleranceGroupKind GetToleranceGroupKind(CatalogToleranceDto tolerance)
    {
        var normalized = NormalizeOptionText($"{tolerance.Code} {tolerance.Name} {tolerance.IsoStandard} {tolerance.Grade}");

        if (normalized.Contains("iso2768", StringComparison.Ordinal)
            || normalized.Contains("2768", StringComparison.Ordinal))
            return ToleranceGroupKind.Iso2768;

        if (normalized.Contains("iso286", StringComparison.Ordinal)
            || normalized.Contains("it6", StringComparison.Ordinal)
            || normalized.Contains("it7", StringComparison.Ordinal)
            || normalized.Contains("it8", StringComparison.Ordinal))
            return ToleranceGroupKind.ItGrade;

        return ToleranceGroupKind.ProcessSpecific;
    }

    private static int GetToleranceGroupSortOrder(ToleranceGroupKind group) =>
        group switch
        {
            ToleranceGroupKind.Iso2768 => 0,
            ToleranceGroupKind.ItGrade => 1,
            _ => 2,
        };

    private static string GetToleranceGroupTitle(ToleranceGroupKind group) =>
        group switch
        {
            ToleranceGroupKind.Iso2768 => "General tolerances",
            ToleranceGroupKind.ItGrade => "Fit / precision grades",
            _ => "Process tolerances",
        };

    private static string GetToleranceGroupStandard(ToleranceGroupKind group) =>
        group switch
        {
            ToleranceGroupKind.Iso2768 => "ISO 2768",
            ToleranceGroupKind.ItGrade => "ISO 286 / IT",
            _ => "Manufacturing process",
        };

    private static string GetToleranceGroupCssClass(ToleranceGroupKind group) =>
        group switch
        {
            ToleranceGroupKind.Iso2768 => "pcs-tolerance-group--iso",
            ToleranceGroupKind.ItGrade => "pcs-tolerance-group--it",
            _ => "pcs-tolerance-group--process",
        };

    private static string FormatOptionCount(int count) =>
        count == 1 ? "1 option" : $"{count} options";

    private bool IsVisibleProcessOption(ProcessConfigOptionDto option)
    {
        if (HiddenCustomerOptionKeys.Contains(option.ConfigKey))
            return false;

        if (IsHiddenCustomerOption(option))
            return false;

        if (IsAnodizeColorOption(option))
            return IsAnodizeFinish();

        if (IsPaintColorOption(option))
            return IsCoatedColorFinish();

        if (IsDedicatedPanelOption(option))
            return false;

        if (IsMaterialColorOption(option) || IsGenericColorOption(option))
            return !RequiresDedicatedMaterialColorSelection && !IsCoatedColorFinish();

        return true;
    }

    private static bool IsHiddenCustomerOption(ProcessConfigOptionDto option)
    {
        var normalized = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        return IsHiddenCustomerOptionText(normalized);
    }

    private static bool IsHiddenCustomerOptionText(string normalized) =>
        normalized.Contains("layerheight", StringComparison.Ordinal)
        || normalized.Contains("infill", StringComparison.Ordinal)
        || normalized.Contains("supporttype", StringComparison.Ordinal)
        || normalized.Contains("threadspec", StringComparison.Ordinal)
        || normalized.Contains("threadspecification", StringComparison.Ordinal)
        || normalized.Contains("threadedhole", StringComparison.Ordinal)
        || normalized.Contains("threadhole", StringComparison.Ordinal)
        || normalized.Contains("tappedhole", StringComparison.Ordinal)
        || normalized.Contains("taphole", StringComparison.Ordinal)
        || normalized.Contains("threadinsert", StringComparison.Ordinal)
        || normalized.Contains("threadedinsert", StringComparison.Ordinal)
        || normalized.Contains("heatsetinsert", StringComparison.Ordinal)
        || normalized.Contains("uvcure", StringComparison.Ordinal)
        || normalized.Contains("postcure", StringComparison.Ordinal)
        || normalized.Contains("groove", StringComparison.Ordinal)
        || normalized.Contains("undercut", StringComparison.Ordinal);

    private bool IsBooleanOption(ProcessConfigOptionDto option)
    {
        var key = option.ConfigKey;
        var label = option.Label;
        return option.ConfigType.Equals("boolean", StringComparison.OrdinalIgnoreCase)
            || option.ConfigType.Equals("checkbox", StringComparison.OrdinalIgnoreCase)
            || key.Contains("deburr", StringComparison.OrdinalIgnoreCase)
            || key.Contains("tap", StringComparison.OrdinalIgnoreCase)
            || key.Contains("thread", StringComparison.OrdinalIgnoreCase)
            || key.Contains("insert", StringComparison.OrdinalIgnoreCase)
            || label.Contains("deburr", StringComparison.OrdinalIgnoreCase)
            || label.Contains("tap", StringComparison.OrdinalIgnoreCase)
            || label.Contains("thread", StringComparison.OrdinalIgnoreCase)
            || label.Contains("insert", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCardChoiceOption(ProcessConfigOptionDto option) =>
        option.ConfigType.Equals("dropdown", StringComparison.OrdinalIgnoreCase)
        || option.ConfigType.Equals("select", StringComparison.OrdinalIgnoreCase);

    private bool IsColorChoiceOption(ProcessConfigOptionDto option)
    {
        var key = option.ConfigKey;
        var label = option.Label;
        return IsAnodizeColorOption(option)
            || key.Contains("color", StringComparison.OrdinalIgnoreCase)
            || key.Contains("colour", StringComparison.OrdinalIgnoreCase)
            || label.Contains("color", StringComparison.OrdinalIgnoreCase)
            || label.Contains("colour", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPowderFusionColorOption(ProcessConfigOptionDto option) =>
        IsPowderFusionProcess
        && !IsPaintColorOption(option)
        && !IsAnodizeColorOption(option)
        && (IsMaterialColorOption(option) || IsGenericColorOption(option));

    private bool IsFinishColorOption(ProcessConfigOptionDto option) =>
        IsAnodizeColorOption(option)
        || (IsCoatedColorFinish() && IsPaintColorOption(option));

    private bool IsPaintColorOption(ProcessConfigOptionDto option) =>
        IsPaintSpecificColorOption(option)
        || IsPowderCoatSpecificColorOption(option)
        || (IsCoatedColorFinish() && !HasCoatingSpecificColorOption && IsGenericColorOption(option));

    private static bool IsPaintSpecificColorOption(ProcessConfigOptionDto option) =>
        option.ConfigKey.Equals("paint_color", StringComparison.OrdinalIgnoreCase)
        || option.ConfigKey.Equals("paint_colour", StringComparison.OrdinalIgnoreCase)
        || option.Label.Contains("paint", StringComparison.OrdinalIgnoreCase);

    private static bool IsPowderCoatSpecificColorOption(ProcessConfigOptionDto option)
    {
        var normalized = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        return normalized.Contains("powdercoatcolor", StringComparison.Ordinal)
            || normalized.Contains("powdercoatcolour", StringComparison.Ordinal)
            || normalized.Contains("powdercoatingcolor", StringComparison.Ordinal)
            || normalized.Contains("powdercoatingcolour", StringComparison.Ordinal)
            || normalized.Contains("powdercolor", StringComparison.Ordinal)
            || normalized.Contains("powdercolour", StringComparison.Ordinal);
    }

    private static bool IsDedicatedPanelOption(ProcessConfigOptionDto option)
    {
        var normalized = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        return normalized.Contains("surfaceroughness", StringComparison.Ordinal)
            || normalized.Contains("roughness", StringComparison.Ordinal)
            || normalized.Contains("inspectionlevel", StringComparison.Ordinal)
            || normalized.Contains("inspection", StringComparison.Ordinal)
            || normalized.Contains("qualityinspection", StringComparison.Ordinal);
    }

    private static bool IsGenericColorOption(ProcessConfigOptionDto option)
    {
        var normalized = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        return (normalized.Contains("color", StringComparison.Ordinal)
                || normalized.Contains("colour", StringComparison.Ordinal))
            && !normalized.Contains("paint", StringComparison.Ordinal)
            && !normalized.Contains("anod", StringComparison.Ordinal);
    }

    private static bool IsMaterialColorOption(ProcessConfigOptionDto option)
    {
        var normalized = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        return IsMaterialColorOptionText(normalized);
    }

    private static bool IsMaterialColorOptionText(string normalized) =>
        (normalized.Contains("materialcolor", StringComparison.Ordinal)
            || normalized.Contains("materialcolour", StringComparison.Ordinal)
            || normalized.Contains("plasticcolor", StringComparison.Ordinal)
            || normalized.Contains("plasticcolour", StringComparison.Ordinal))
        && !normalized.Contains("paint", StringComparison.Ordinal)
        && !normalized.Contains("anod", StringComparison.Ordinal);

    private static bool IsAnodizeColorOption(ProcessConfigOptionDto option) =>
        option.ConfigKey.Equals("anodize_color", StringComparison.OrdinalIgnoreCase)
        || option.ConfigKey.Equals("anodise_color", StringComparison.OrdinalIgnoreCase)
        || option.Label.Contains("anodize", StringComparison.OrdinalIgnoreCase)
        || option.Label.Contains("anodise", StringComparison.OrdinalIgnoreCase);

    private static bool IsPomMaterial(CatalogMaterialDto material)
    {
        var normalized = NormalizeOptionText($"{material.Code} {material.Name} {material.Description}");
        return normalized.Contains("pom", StringComparison.Ordinal)
            || normalized.Contains("delrin", StringComparison.Ordinal)
            || normalized.Contains("acetal", StringComparison.Ordinal);
    }

    private static bool IsPeekMaterial(CatalogMaterialDto material)
    {
        var normalized = NormalizeOptionText($"{material.Code} {material.Name} {material.Description}");
        return normalized.Contains("peek", StringComparison.Ordinal);
    }

    private static bool IsMetalMaterial(CatalogMaterialDto material)
    {
        var normalized = NormalizeOptionText($"{material.Code} {material.Name} {material.Description} {material.Category}");
        return normalized.Contains("metal", StringComparison.Ordinal)
            || normalized.Contains("aluminium", StringComparison.Ordinal)
            || normalized.Contains("aluminum", StringComparison.Ordinal)
            || normalized.Contains("steel", StringComparison.Ordinal)
            || normalized.Contains("brass", StringComparison.Ordinal)
            || normalized.Contains("copper", StringComparison.Ordinal)
            || normalized.Contains("bronze", StringComparison.Ordinal)
            || normalized.Contains("titanium", StringComparison.Ordinal)
            || normalized.Contains("stainless", StringComparison.Ordinal);
    }

    private static IReadOnlyList<CatalogSurfaceFinishDto> GetVisibleFinishes(IReadOnlyList<CatalogSurfaceFinishDto> finishes) =>
        finishes
            .OrderBy(f => f.SortOrder)
            .GroupBy(GetSurfaceFinishDisplayKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

    private static string GetSurfaceFinishDisplayName(CatalogSurfaceFinishDto finish)
    {
        var key = GetSurfaceFinishDisplayKey(finish);
        return key switch
        {
            "ANODIZE_TYPE_II" => "Anodized Type II",
            "ANODIZE_TYPE_III" => "Anodized Type III",
            _ => finish.Name,
        };
    }

    private static string GetSurfaceFinishDisplayKey(CatalogSurfaceFinishDto finish)
    {
        var normalized = NormalizeOptionText($"{finish.Code} {finish.Name}");
        if (normalized.Contains("anod", StringComparison.Ordinal))
        {
            if (normalized.Contains("typeiii", StringComparison.Ordinal)
                || normalized.Contains("hard", StringComparison.Ordinal))
                return "ANODIZE_TYPE_III";

            return "ANODIZE_TYPE_II";
        }

        return finish.Code;
    }

    private static string NormalizeOptionText(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray();
        return new string(chars);
    }

    private bool IsPowderFusionProcess =>
        IsProcess("SLS") || IsProcess("MJF");

    private bool IsPaintedFinish() =>
        FinishContains("paint") && !FinishContains("unpaint");

    private bool IsPowderCoatFinish() =>
        FinishContains("powder")
        && (FinishContains("coat") || FinishContains("coated") || FinishContains("coating"));

    private bool IsCoatedColorFinish() =>
        IsPaintedFinish() || IsPowderCoatFinish();

    private bool IsDyeFinish() =>
        FinishContains("dye") || FinishContains("dyed");

    private bool IsRawPowderFusionFinish =>
        IsPowderFusionProcess && !IsPaintedFinish() && !IsDyeFinish();

    private bool IsAnodizeFinish() =>
        FinishContains("anod");

    private bool FinishContains(string value) =>
        Part?.FinishCode?.Contains(value, StringComparison.OrdinalIgnoreCase) == true
        || SelectedFinish?.Code.Contains(value, StringComparison.OrdinalIgnoreCase) == true
        || SelectedFinish?.Name.Contains(value, StringComparison.OrdinalIgnoreCase) == true
        || SelectedFinish?.Description?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsPowderFusionRawSurfaceFinish(CatalogSurfaceFinishDto finish) =>
        (ContainsFinishText(finish, "as printed")
            || ContainsFinishText(finish, "as-printed")
            || ContainsFinishText(finish, "as_printed")
            || ContainsFinishText(finish, "natural")
            || ContainsFinishText(finish, "raw")
            || ContainsFinishText(finish, "standard")
            || ContainsFinishText(finish, "unpaint"))
        && !ContainsFinishText(finish, "dye")
        && !ContainsFinishText(finish, "dyed")
        && (!ContainsFinishText(finish, "paint") || ContainsFinishText(finish, "unpaint"));

    private static bool IsPowderFusionDyedSurfaceFinish(CatalogSurfaceFinishDto finish) =>
        ContainsFinishText(finish, "dye")
        || ContainsFinishText(finish, "dyed");

    private static bool ContainsFinishText(CatalogSurfaceFinishDto finish, string value) =>
        finish.Code.Contains(value, StringComparison.OrdinalIgnoreCase)
        || finish.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
        || finish.Description?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsPowderFusionMaterial(CatalogMaterialDto material)
    {
        var normalized = NormalizeOptionText($"{material.Code} {material.Name} {material.Description} {material.Category}");
        return normalized.Contains("pa12", StringComparison.Ordinal)
            || normalized.Contains("nylon", StringComparison.Ordinal)
            || normalized.Contains("polyamide", StringComparison.Ordinal);
    }

    private string GetToleranceRange(CatalogToleranceDto tolerance)
    {
        if (!string.IsNullOrWhiteSpace(tolerance.ToleranceRange))
            return NormalizeToleranceRange(tolerance.ToleranceRange);

        var code = tolerance.Code.ToUpperInvariant();
        if (code.Contains("FDM") || IsFdmProcess)
        {
            if (code.Contains("FINE") || code.Contains("TIGHT"))
                return "+-0.127 mm";
            if (code.Contains("STANDARD") || code.Contains("STD"))
                return "+-0.254 mm";
            if (code.Contains("COMMERCIAL") || code.Contains("LOOSE"))
                return "+-0.500 mm";
        }

        if (code.Contains("IT6")) return "IT6";
        if (code.Contains("IT7")) return "IT7";
        if (code.Contains("IT8")) return "IT8";
        if (code.Contains("ISO2768_M") || code.Contains("ISO_2768_M")) return "ISO 2768-m";
        if (code.Contains("ISO2768_F") || code.Contains("ISO_2768_F")) return "ISO 2768-f";

        return tolerance.Grade;
    }

    private static string NormalizeToleranceRange(string value) =>
        value.Replace("±", "+-", StringComparison.Ordinal)
            .Replace("+/-", "+-", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();

    private IEnumerable<string> GetOptionChoices(ProcessConfigOptionDto option)
    {
        if (IsPowderFusionColorOption(option))
        {
            if (IsRawPowderFusionFinish)
                return PowderFusionRawColors;

            if (IsDyeFinish())
                return PowderFusionDyeColors;
        }

        if (!string.IsNullOrWhiteSpace(option.OptionsJson))
        {
            var parsed = ParseOptionChoices(option.OptionsJson);
            if (parsed.Count > 0)
                return parsed;
        }

        if (IsAnodizeColorOption(option))
            return DefaultAnodizeColors;

        if (option.ConfigKey.Contains("color", StringComparison.OrdinalIgnoreCase)
            || option.Label.Contains("color", StringComparison.OrdinalIgnoreCase))
            return DefaultPlasticColors;

        return [];
    }

    private string? GetCurrentOptionChoiceValue(ProcessConfigOptionDto option, IReadOnlyList<string> options)
    {
        var currentValue = Part?.ProcessOptionValues.TryGetValue(option.ConfigKey, out var value) == true
            ? value
            : option.DefaultValue;

        if (!string.IsNullOrWhiteSpace(currentValue)
            && options.Contains(currentValue, StringComparer.OrdinalIgnoreCase))
        {
            return currentValue;
        }

        return options.FirstOrDefault();
    }

    private static List<string> ParseOptionChoices(string optionsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(optionsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var values = new List<string>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        values.Add(value);
                    continue;
                }

                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var propertyName in new[] { "label", "name", "value", "code" })
                    {
                        if (item.TryGetProperty(propertyName, out var property)
                            && property.ValueKind == JsonValueKind.String)
                        {
                            var value = property.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                                values.Add(value);
                            break;
                        }
                    }
                }
            }

            return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (JsonException)
        {
            return optionsJson.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private void ResetDfmAnalysisCache(Guid? fileId)
    {
        if (_dfmCacheFileId == fileId)
            return;

        _dfmCts?.Cancel();
        _dfmCts?.Dispose();
        _dfmCts = null;
        _dfmReports.Clear();
        _typedReportsByProcess.Clear();
        _dfmCacheFileId = fileId;
    }

    private bool GetProcessOptionBool(string key) =>
        Part?.ProcessOptionValues.TryGetValue(key, out var value) == true
        && bool.TryParse(value, out var result)
        && result;

    private string GetProcessOptionValue(string key, string defaultValue) =>
        Part?.ProcessOptionValues.TryGetValue(key, out var value) == true && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;

    private static bool IsPaintColorSelected(PaintColorOption paint, string? paintHex, string? paintReference) =>
        string.Equals(paintHex, paint.Hex, StringComparison.OrdinalIgnoreCase)
        || string.Equals(paintReference, paint.Reference, StringComparison.OrdinalIgnoreCase)
        || string.Equals(paintReference, paint.Name, StringComparison.OrdinalIgnoreCase);

    private static bool IsCustomPaintColorSelected(string? paintHex, string? paintReference) =>
        !string.IsNullOrWhiteSpace(paintHex)
        && !StandardPaintColors.Any(paint => IsPaintColorSelected(paint, paintHex, paintReference));

    private static string FormatOptionLabel(string value) =>
        value.Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Trim();

    private string GetFinishColorOptionLabel(ProcessConfigOptionDto option) =>
        IsPowderCoatFinish() && IsPaintColorOption(option)
            ? "Powder coat color"
            : option.Label;

    private string GetMaterialImageUrl(CatalogMaterialDto material)
    {
        if (IsPowderFusionProcess && IsPowderFusionMaterial(material))
            return MaterialImageBasePath + PowderFusionRawImage;

        foreach (var token in GetImageLookupTokens(material.Code, material.Name, material.Category, material.Description))
        {
            if (MaterialImages.TryGetValue(token, out var image))
                return MaterialImageBasePath + image;
        }

        return MaterialImageBasePath + "raw-metal-part-material.png";
    }

    private static string GetMaterialColorImageUrl(string color) =>
        MaterialImageBasePath + GetMappedImage(ColorImages, color, "natural-plastic-part-material.png");

    private static string GetPowderFusionColorImageUrl(string color)
    {
        foreach (var token in GetImageLookupTokens(color))
            if (PowderFusionColorImages.TryGetValue(token, out var image))
                return MaterialImageBasePath + image;

        return MaterialImageBasePath + PowderFusionRawImage;
    }

    private string GetSurfaceFinishImageUrl(CatalogSurfaceFinishDto finish)
    {
        if (IsPowderFusionProcess && IsPowderFusionDyedSurfaceFinish(finish))
            return MaterialImageBasePath + PowderFusionDyedBlackImage;

        if (IsPowderFusionProcess && IsPowderFusionRawSurfaceFinish(finish))
            return MaterialImageBasePath + PowderFusionRawImage;

        var displayKey = GetSurfaceFinishDisplayKey(finish);
        foreach (var token in GetImageLookupTokens(displayKey, finish.Code, finish.Name, finish.Description))
        {
            if (FinishImages.TryGetValue(token, out var image))
                return MaterialImageBasePath + image;

            if (token.Contains("anod", StringComparison.Ordinal)
                && token.Contains("typeiii", StringComparison.Ordinal))
                return MaterialImageBasePath + "finish-anodized-black-part-surface.png";

            if (token.Contains("anod", StringComparison.Ordinal))
                return MaterialImageBasePath + "finish-anodized-clear-part-surface.png";
        }

        return MaterialImageBasePath + "finish-as-machined-part-surface.png";
    }

    private static string GetBooleanOptionImageUrl(ProcessConfigOptionDto option)
    {
        var normalized = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        if (normalized.Contains("deburr", StringComparison.Ordinal))
            return MaterialImageBasePath + "deburr-edges-part-detail.png";

        return MaterialImageBasePath + "finish-as-machined-part-surface.png";
    }

    private static string GetPaintColorImageUrl(PaintColorOption paint)
    {
        foreach (var token in GetImageLookupTokens(paint.Name, paint.Reference))
        {
            if (PaintColorImages.TryGetValue(token, out var image))
                return MaterialImageBasePath + image;
        }

        return MaterialImageBasePath + "finish-painted-part-surface.png";
    }

    private static string GetCustomPaintImageUrl() =>
        MaterialImageBasePath + "finish-painted-part-surface.png";

    private string GetColorChoiceImageUrl(ProcessConfigOptionDto option, string value)
    {
        if (IsPowderFusionColorOption(option))
            return GetPowderFusionColorImageUrl(value);

        if (IsAnodizeColorOption(option))
            return MaterialImageBasePath + GetMappedImage(AnodizeColorImages, value, "finish-anodized-clear-part-surface.png");

        if (PaintColorImages.TryGetValue(NormalizeOptionText(value), out var paintImage))
            return MaterialImageBasePath + paintImage;

        return GetMaterialColorImageUrl(value);
    }

    private static string GetCardChoiceOptionImageUrl(ProcessConfigOptionDto option, string value)
    {
        var optionText = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        var valueText = NormalizeOptionText(value);
        if (optionText.Contains("deburr", StringComparison.Ordinal))
        {
            return MaterialImageBasePath + (valueText.Contains("no", StringComparison.Ordinal)
                || valueText.Contains("none", StringComparison.Ordinal)
                    ? "no-deburr-part-detail.png"
                    : "deburr-edges-part-detail.png");
        }

        if (optionText.Contains("finish", StringComparison.Ordinal)
            || optionText.Contains("surface", StringComparison.Ordinal))
        {
            foreach (var token in GetImageLookupTokens(value, option.Label, option.ConfigKey))
            {
                if (FinishImages.TryGetValue(token, out var image))
                    return MaterialImageBasePath + image;
            }
        }

        return MaterialImageBasePath + "finish-as-machined-part-surface.png";
    }

    private static string GetRoughnessImageUrl(string code) =>
        MaterialImageBasePath + (NormalizeOptionText(code) switch
        {
            var normalized when normalized.Contains("ra04", StringComparison.Ordinal) => "roughness-ra-0-4-part-surface.png",
            var normalized when normalized.Contains("ra08", StringComparison.Ordinal) => "roughness-ra-0-8-part-surface.png",
            var normalized when normalized.Contains("ra16", StringComparison.Ordinal) => "roughness-ra-1-6-part-surface.png",
            _ => "roughness-ra-3-2-part-surface.png",
        });

    private static string GetFeatureImageUrl(string featureKey) =>
        MaterialImageBasePath + (NormalizeOptionText(featureKey) switch
        {
            var normalized when normalized.Contains("insert", StringComparison.Ordinal) => "feature-thread-inserts-part.png",
            _ => "feature-tapped-holes-part.png",
        });

    private static string GetInspectionImageUrl(InspectionLevel level) =>
        MaterialImageBasePath + (level switch
        {
            InspectionLevel.FullCmm => "inspection-cmm-part-check.png",
            InspectionLevel.Dimensional => "inspection-dimensional-part-check.png",
            _ => "inspection-standard-part-check.png",
        });

    private static string GetMappedImage(
        IReadOnlyDictionary<string, string> images,
        string value,
        string fallback)
    {
        foreach (var token in GetImageLookupTokens(value))
        {
            if (images.TryGetValue(token, out var image))
                return image;
        }

        return fallback;
    }

    private static IEnumerable<string> GetImageLookupTokens(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var normalized = NormalizeOptionText(value);
            if (!string.IsNullOrEmpty(normalized))
                yield return normalized;
        }
    }

    private static string GetOptionIcon(string key)
    {
        if (key.Contains("anod", StringComparison.OrdinalIgnoreCase))
            return Icons.Material.Outlined.AutoFixHigh;
        if (key.Contains("rough", StringComparison.OrdinalIgnoreCase))
            return Icons.Material.Outlined.Grain;
        if (key.Contains("cert", StringComparison.OrdinalIgnoreCase))
            return Icons.Material.Outlined.Verified;

        return Icons.Material.Outlined.Tune;
    }

    private static string GetBooleanOptionIcon(ProcessConfigOptionDto option)
    {
        var normalized = NormalizeOptionText($"{option.ConfigKey} {option.Label}");
        if (normalized.Contains("deburr", StringComparison.Ordinal))
            return Icons.Material.Outlined.CleaningServices;
        if (normalized.Contains("cert", StringComparison.Ordinal))
            return Icons.Material.Outlined.Verified;

        return Icons.Material.Outlined.CheckCircle;
    }

    private static string GetOptionColor(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "black" or "matte black" or "ral 9005" => "#111111",
            "white" or "natural white" or "ral 9010" => "#f7f7f2",
            "natural" => "#eee7d2",
            "clear" or "transparent" => "linear-gradient(135deg, #f6fbff, #bcd7ef)",
            "gray" or "grey" or "silver" => "#9ba3af",
            "red" => "#c8333a",
            "blue" => "#2f6fd6",
            "green" => "#2f8f5b",
            "yellow" => "#f1c232",
            "orange" => "#e47c2f",
            "gold" => "#d4a72c",
            "purple" => "#7c4dff",
            "peek natural" => "#B3AA9E",
            "raw" => "#c4c6ca",
            _ when normalized.Contains("black", StringComparison.Ordinal) => "#111111",
            _ when normalized.Contains("white", StringComparison.Ordinal) => "#f7f7f2",
            _ when normalized.Contains("blue", StringComparison.Ordinal) => "#2f6fd6",
            _ when normalized.Contains("red", StringComparison.Ordinal) => "#c8333a",
            _ when normalized.Contains("green", StringComparison.Ordinal) => "#2f8f5b",
            _ when normalized.Contains("gold", StringComparison.Ordinal) => "#d4a72c",
            _ when normalized.Contains("peek", StringComparison.Ordinal) => "#B3AA9E",
            _ => "linear-gradient(135deg, #d8dce4, #a8aeb8)",
        };
    }

    private void RemoveHiddenProcessOptionValues()
    {
        if (Part == null || Part.ProcessOptionValues.Count == 0)
            return;

        foreach (var key in HiddenCustomerOptionKeys)
            Part.ProcessOptionValues.Remove(key);

        foreach (var key in Part.ProcessOptionValues.Keys.Where(IsHiddenCustomerOptionKey).ToList())
            Part.ProcessOptionValues.Remove(key);

        foreach (var key in Part.ProcessOptionValues.Keys.Where(IsDedicatedPanelOptionKey).ToList())
            Part.ProcessOptionValues.Remove(key);

        RemoveFinishSpecificOptionValues();
    }

    private void RemoveFinishSpecificOptionValues()
    {
        if (Part == null)
            return;

        if (!IsCoatedColorFinish())
        {
            Part.ProcessOptionValues.Remove("paint_color");
            Part.ProcessOptionValues.Remove("paint_colour");
            Part.ProcessOptionValues.Remove(PaintColorHexKey);
            Part.ProcessOptionValues.Remove(PaintColorReferenceKey);
            Part.ProcessOptionValues.Remove(PowderCoatColorKey);
        }
        else
        {
            foreach (var key in Part.ProcessOptionValues.Keys.Where(IsMaterialColorOptionKey).ToList())
                Part.ProcessOptionValues.Remove(key);
        }

        if (!IsPowderCoatFinish())
            Part.ProcessOptionValues.Remove(PowderCoatColorKey);

        if (IsRawPowderFusionFinish)
        {
            foreach (var option in Part.AvailableProcessOptions.Where(IsPowderFusionColorOption))
                Part.ProcessOptionValues[option.ConfigKey] = PowderFusionNaturalGreyColor;
        }

        if (!IsAnodizeFinish())
        {
            Part.ProcessOptionValues.Remove("anodize_color");
            Part.ProcessOptionValues.Remove("anodise_color");
        }
    }

    private static bool IsHiddenCustomerOptionKey(string key) =>
        IsHiddenCustomerOptionText(NormalizeOptionText(key));

    private static bool IsMaterialColorOptionKey(string key) =>
        IsMaterialColorOptionText(NormalizeOptionText(key));

    private static bool IsDedicatedPanelOptionKey(string key)
    {
        var normalized = NormalizeOptionText(key);
        return normalized.Contains("surfaceroughness", StringComparison.Ordinal)
            || normalized.Contains("roughness", StringComparison.Ordinal)
            || normalized.Contains("inspectionlevel", StringComparison.Ordinal)
            || normalized.Contains("inspection", StringComparison.Ordinal)
            || normalized.Contains("qualityinspection", StringComparison.Ordinal);
    }

    private sealed record RoughnessOption(string Code, string Name, string Description);

    private sealed record InspectionOption(InspectionLevel Level, string Name, string Description, string Icon);

    private sealed record PaintColorOption(string Name, string Hex, string Reference);

    private enum ToleranceGroupKind
    {
        Iso2768,
        ItGrade,
        ProcessSpecific,
    }

    private sealed record ToleranceOptionGroup(
        string Title,
        string Standard,
        string CssClass,
        IReadOnlyList<CatalogToleranceDto> Options);

    private async Task OnDrawingsChanged(List<DraftProjectAttachmentDto> files)
    {
        if (Part == null) return;
        Part.DrawingFiles = files;
        await OnPartChanged.InvokeAsync(Part);
    }

    private void OpenScheduleDialog()
    {
        if (Part?.ProductionRouting == null) return;
        var parameters = new DialogParameters<ScheduleDialogContent>
        {
            { x => x.Routing,   Part.ProductionRouting },
            { x => x.MachineId, Part.ProductionRouting.MachineCode },
        };
        DialogService.ShowAsync<ScheduleDialogContent>("Planning Schedule", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true });
    }

    // ── Two-phase DFM analysis ─────────────────────────────────────────────

    private async Task AnalyzeProcessForDfm(ProcessDto process)
    {
        if (Part == null || Part.FileId == Guid.Empty)
        {
            Logger?.LogWarning("No upload ID available for two-phase DFM analysis");
            if (Part != null)
            {
                Part.ResolveDfmReport();
                Part.AvailableMaterials = [];
                Part.AvailableFinishes = [];
                Part.AvailableTolerances = [];
                await OnPartChanged.InvokeAsync(Part);
            }
            return;
        }

        var part = Part!;
        // Capture the token that was created for this invocation in OnProcessChanged.
        var token = _dfmCts?.Token ?? CancellationToken.None;
        BrowserDfmReportSync.ClearTerminalLocalAttempt(part, process.Code);

        StateHasChanged();

        try
        {
            Logger?.LogInformation("Starting DFM analysis for upload {UploadId}, process {ProcessCode}",
                part.FileId, process.Code);

            if (await BrowserDfmReportSync.WaitForCurrentReportAsync(part, process.Code, token))
            {
                Logger?.LogInformation("Using browser-first local DFM results for process {ProcessCode}", process.Code);
                return;
            }

            var response = await Http.PostAsJsonAsync(
                $"api/v1/geometry/{part.FileId}/dfm/{process.Code}",
                new GeometryAnalysisRequest { StoragePath = part.StoragePath },
                token
            );

            // If cancelled mid-request (user already switched process), bail without touching part state.
            token.ThrowIfCancellationRequested();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DfmAnalysisResponse>(token);

                if (result != null && result.Status == "analysis_complete")
                {
                    _dfmReports[process.Code] = result;

                    Logger?.LogInformation("DFM analysis complete for {ProcessCode}: {IssueCount} issues found in {Time:F2}s",
                        process.Code, result.DfmReport.Issues.Count, result.DfmReport.AnalysisTimeSeconds);

                    // Store typed report in the per-process cache (keeps CNC_TURN and CNC_MILL separate).
                    _typedReportsByProcess[process.Code] = result.DfmReport;
                    ApplyTypedReportFromCache(process.Code);

                    // Stamp body count from DFM analysis if provided (fills in when
                    // FileAnalyzedEvent was missed or arrived before this analysis).
                    if (result.BodyCount.HasValue && !part.BodyCount.HasValue)
                        part.BodyCount = result.BodyCount.Value;

                    if (result.OverlayPaths.Count > 0)
                    {
                        part.OverlayPaths ??= new Dictionary<string, string>();
                        foreach (var (key, path) in result.OverlayPaths)
                            part.OverlayPaths[key] = path;

                        part.OverlayUrls = await ResolveOverlayUrlsAsync(part.OverlayPaths);
                    }
                }
                else if (result != null && result.Status == "timeout")
                {
                    Logger?.LogWarning("DFM analysis timed out for upload {UploadId}, process {ProcessCode}",
                        part.FileId, process.Code);
                    part.DfmAnalysisTimedOut = true;
                    part.AnalysisErrorCode = "GEOMETRY_PHASE2_TIMEOUT";
                }
                else
                {
                    Logger?.LogError("DFM analysis failed with status {Status}", result?.Status ?? "unknown");
                    part.DfmAnalysisTimedOut = true;
                    part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                if (BrowserDfmReportSync.HasCurrentReport(part, process.Code))
                {
                    Logger?.LogInformation(
                        "Ignoring missing-file DFM failure because browser-first local results are current for process {ProcessCode}",
                        process.Code);
                    return;
                }

                Logger?.LogWarning("DFM analysis failed — file no longer in storage for upload {UploadId}", part.FileId);
                part.DfmAnalysisTimedOut = false;
                part.AnalysisErrorCode = "FILE_MISSING";
                Snackbar.Add("File expired or missing — please re-upload to run analysis.", MudBlazor.Severity.Error);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(token);
                Logger?.LogError("DFM analysis HTTP request failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                part.DfmAnalysisTimedOut = true;
                part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";
            }
        }
        catch (OperationCanceledException)
        {
            // User switched process before this one completed — do not update part state.
            Logger?.LogInformation("DFM analysis cancelled for process {ProcessCode} (user switched process)", process.Code);
            return;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "DFM analysis error for upload {UploadId}, process {ProcessCode}",
                part.FileId, process.Code);
            part.DfmAnalysisTimedOut = true;
            part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";
        }
        finally
        {
            // Only update part state if this analysis is still for the current process.
            // If the user already switched, ProcessCode has changed and we must not trample it.
            if (ProcessCodeNormalizer.Equals(part.ProcessCode, process.Code))
            {
                part.ResolveDfmReport();
                StateHasChanged();
                await OnPartChanged.InvokeAsync(part);
            }
        }
    }

    /// <summary>
    /// Signs raw GCS overlay paths into fresh signed URLs using the BFF viewer-url endpoint.
    /// Returns a dictionary of signed URLs keyed by the same overlay keys.
    /// </summary>
    private async Task<Dictionary<string, string>> ResolveOverlayUrlsAsync(Dictionary<string, string> overlayPaths)
    {
        var signed = new Dictionary<string, string>(overlayPaths.Count);
        foreach (var (key, path) in overlayPaths)
        {
            try
            {
                var resp = await Http.GetAsync(
                    $"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(path)}");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadFromJsonAsync<JsonDocument>();
                    var url = json?.RootElement.GetProperty("url").GetString();
                    if (!string.IsNullOrEmpty(url))
                        signed[key] = url;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex, "Failed to sign overlay URL for key {Key}", key);
            }
        }
        return signed;
    }

}
