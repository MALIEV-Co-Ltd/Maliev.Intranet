using System.Net.Http.Json;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Components.Shared;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Microsoft.Extensions.Logging;

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

    /// <summary>The currency symbol for displaying prices (e.g. "฿").</summary>
    [Parameter] public string CurrencySymbol { get; set; } = "฿";

    /// <summary>Callback invoked after any mutation to the part configuration.</summary>
    [Parameter] public EventCallback<PartViewModel> OnPartChanged { get; set; }

    /// <summary>Callback invoked when the user wants to upload a revised CAD model for this part.</summary>
    [Parameter] public EventCallback OnReviseModel { get; set; }

    /// <summary>Controls the layout mode of the sidebar. Sidebar renders the full-width panel; Inline renders a compact version for use inside cards.</summary>
    [Parameter] public PartConfigSidebarDisplayMode DisplayMode { get; set; } = PartConfigSidebarDisplayMode.Sidebar;

    private bool _routingExpanded = true;
    private List<BulkPricingTable.BulkTier> _bulkTiers = [];
    private IReadOnlyCollection<string> _selectedFeatures = [];
    private readonly Dictionary<Guid, decimal> _basePricesByPart = new();
    private readonly Dictionary<Guid, decimal> _lastSeenPricesByPart = new();
    private readonly Dictionary<Guid, List<BulkPricingTable.BulkTier>> _bulkTiersByPart = new();
    private int _lastQuantity;

    // ── Two-phase DFM analysis state ─────────────────────────────────────
    private bool _isAnalyzingDfm;
    private string _analyzingProcessName = string.Empty;
    private Dictionary<string, DfmAnalysisResponse> _dfmReports = new();
    /// <summary>Logger for two-phase DFM operations.</summary>
    [Inject] public ILogger<PartConfigSidebar> Logger { get; set; } = null!;

    // ── Computed properties ───────────────────────────────────────────────

    private ProcessDto? SelectedProcess =>
        Processes.FirstOrDefault(p => p.Code == Part?.ProcessCode);

    private CatalogMaterialDto? SelectedMaterial =>
        Part?.AvailableMaterials.FirstOrDefault(m => m.Id == Part.MaterialId);

    private CatalogSurfaceFinishDto? SelectedFinish =>
        Part?.AvailableFinishes.FirstOrDefault(f => f.Id == Part.FinishId);

    private CatalogToleranceDto? SelectedTolerance =>
        Part?.AvailableTolerances.FirstOrDefault(t => t.Id == Part.ToleranceId);

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Part == null)
        {
            _bulkTiers = [];
            return;
        }

        var features = new List<string>();
        if (Part.HasThreadedHoles) features.Add("ThreadedHoles");
        if (Part.HasInserts) features.Add("Inserts");
        _selectedFeatures = features;

        var fileId = Part.FileId;
        if (fileId == Guid.Empty)
        {
            _bulkTiers = [];
            return;
        }

        if (_bulkTiersByPart.TryGetValue(fileId, out var storedTiers))
            _bulkTiers = storedTiers;

        if (Part.EstimatedUnitPrice.HasValue)
        {
            var currentPrice = Part.EstimatedUnitPrice.Value;
            var isNewPrice = !_lastSeenPricesByPart.TryGetValue(fileId, out var lastSeen)
                             || currentPrice != lastSeen;

            if (isNewPrice)
            {
                _basePricesByPart[fileId] = currentPrice;
                _lastSeenPricesByPart[fileId] = currentPrice;
                await FetchBulkTiersAsync();
            }
            else if (_bulkTiers.Count > 0)
            {
                ApplyBulkPricingToTotal();
            }
        }
        else if (_lastSeenPricesByPart.ContainsKey(fileId))
        {
            _lastSeenPricesByPart.Remove(fileId);
            _basePricesByPart.Remove(fileId);
            _bulkTiersByPart.Remove(fileId);
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
            var response = await Http.PostAsJsonAsync("api/pricing/bulk", request);
            if (response.IsSuccessStatusCode)
            {
                var tiers = await response.Content.ReadFromJsonAsync<List<BulkPriceTierDto>>();
                if (tiers != null)
                {
                    _bulkTiers = tiers.Select(t => new BulkPricingTable.BulkTier(t.Quantity, t.UnitPrice)).ToList();
                    _bulkTiersByPart[fileId] = _bulkTiers;
                    ApplyBulkPricingToTotal();
                }
            }
        }
        catch
        {
            // Non-fatal — bulk pricing table stays empty
        }
    }

    /// <summary>
    /// Finds the active bulk tier for the current quantity and sets
    /// <see cref="PartViewModel.EstimatedUnitPrice"/> and <see cref="PartViewModel.EstimatedTotalAmount"/>
    /// to reflect the bulk-discounted pricing.
    /// </summary>
    private void ApplyBulkPricingToTotal()
    {
        if (Part == null || _bulkTiers.Count == 0) return;

        var activeTier = _bulkTiers
            .Where(t => t.Quantity <= Part.Quantity)
            .OrderByDescending(t => t.Quantity)
            .FirstOrDefault();

        if (activeTier != null)
        {
            Part.EstimatedUnitPrice = activeTier.UnitPrice;
            Part.EstimatedTotalAmount = activeTier.UnitPrice * Part.Quantity;

            if (Part.FileId != Guid.Empty)
                _lastSeenPricesByPart[Part.FileId] = activeTier.UnitPrice;
        }
    }

    // ── Event handlers ───────────────────────────────────────────────────

    private async Task OnProcessChanged(ProcessDto? p)
    {
        if (Part == null || p == null) return;

        // Store previous process to detect changes
        var previousProcess = Part.ProcessCode;
        Part.ProcessCode = p.Code;
        Part.ProcessId = p.Id;

        // Check if we already have results for this process
        if (_dfmReports.ContainsKey(p.Code))
        {
            Logger?.LogInformation("Using cached DFM results for process {ProcessCode}", p.Code);
            Part.ResolveDfmReport();
            Part.AvailableMaterials = [];
            Part.AvailableFinishes = [];
            Part.AvailableTolerances = [];
            await OnPartChanged.InvokeAsync(Part);
            return;
        }

        // Trigger two-phase DFM analysis for selected process
        await AnalyzeProcessForDfm(p);
    }

    private async Task OnMaterialChanged(CatalogMaterialDto? m)
    {
        if (Part == null) return;
        Part.MaterialCode = m?.Code;
        Part.MaterialId = m?.Id;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnFinishChanged(CatalogSurfaceFinishDto? f)
    {
        if (Part == null) return;
        Part.FinishCode = f?.Code;
        Part.FinishId = f?.Id;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnToleranceChanged(CatalogToleranceDto? t)
    {
        if (Part == null) return;
        Part.ToleranceCode = t?.Code;
        Part.ToleranceId = t?.Id;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnRoughnessChanged(string? v)
    {
        if (Part == null) return;
        Part.RoughnessCode = v;
        await OnPartChanged.InvokeAsync(Part);
    }

    private async Task OnFeaturesChanged(IReadOnlyCollection<string> selected)
    {
        if (Part == null) return;
        _selectedFeatures = selected;
        Part.HasThreadedHoles = selected.Contains("ThreadedHoles");
        Part.HasInserts = selected.Contains("Inserts");
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
        ApplyBulkPricingToTotal();
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
        if (Part == null) return;
        Part.InspectionLevel = level;
        await OnPartChanged.InvokeAsync(Part);
    }

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

    /// <summary>
    /// Analyze the part for a specific manufacturing process using two-phase DFM API
    /// </summary>
    private async Task AnalyzeProcessForDfm(ProcessDto process)
    {
        if (Part == null || Part.FileId == Guid.Empty)
        {
            // No upload ID available, fall back to old behavior
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

        // From here on, Part is guaranteed non-null
        var part = Part!;

        // Show loading state
        _isAnalyzingDfm = true;
        _analyzingProcessName = process.Name;
        StateHasChanged();

        try
        {
            Logger?.LogInformation("Starting DFM analysis for upload {UploadId}, process {ProcessCode}",
                part.FileId, process.Code);

            // Call BFF endpoint to trigger process-specific DFM analysis
            var response = await Http.PostAsJsonAsync(
                $"api/geometry/{part.FileId}/dfm/{process.Code}",
                new GeometryAnalysisRequest { StoragePath = part.StoragePath }
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DfmAnalysisResponse>();

                if (result != null && result.Status == "analysis_complete")
                {
                    // Cache results locally so switching back to this process skips re-analysis.
                    // The DFM overlay panel (populated via SignalR) shows the actual issues to the user.
                    _dfmReports[process.Code] = result;

                    Logger?.LogInformation("DFM analysis complete for {ProcessCode}: {IssueCount} issues found in {Time:F2}s",
                        process.Code, result.DfmReport.Issues.Count, result.DfmReport.AnalysisTimeSeconds);
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
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger?.LogError("DFM analysis HTTP request failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                part.DfmAnalysisTimedOut = true;
                part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";
            }
        }
        catch (OperationCanceledException)
        {
            // The resilience pipeline (BffInternal AttemptTimeout) cancelled the request after retries,
            // or the user navigated away. The analysis may still complete and arrive via SignalR.
            Logger?.LogDebug("DFM analysis HTTP request cancelled for upload {UploadId}, process {ProcessCode} — result expected via SignalR",
                part.FileId, process.Code);
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
            _isAnalyzingDfm = false;
            _analyzingProcessName = string.Empty;

            // Update part state
            part.ResolveDfmReport();
            part.AvailableMaterials = [];
            part.AvailableFinishes = [];
            part.AvailableTolerances = [];

            StateHasChanged();
            await OnPartChanged.InvokeAsync(part);
        }
    }

}
