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
    // Finish id → absolute additional unit cost in THB.
    private Dictionary<Guid, decimal> _finishPrices = new();

    // ── Two-phase DFM analysis state ─────────────────────────────────────
    private Dictionary<string, DfmAnalysisResponse> _dfmReports = new();
    // Typed per-process report cache: keyed by ProcessCode so CNC_TURN and CNC_MILL stay separate.
    private readonly Dictionary<string, object> _typedReportsByProcess = new();
    // Cancellation token for the currently-running DFM analysis request.
    private CancellationTokenSource? _dfmCts;
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
                _ = RefreshFinishPricesAsync();
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

        // Cancel any in-flight DFM request for the previous process.
        var oldCts = _dfmCts;
        _dfmCts = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();

        Part.ProcessCode = p.Code;
        Part.ProcessId = p.Id;

        // Reset stale analysis flags so prior errors don't bleed into the new process selection.
        Part.DfmAnalysisTimedOut = false;
        Part.AnalysisErrorCode = null;

        // Check if we already have results for this process.
        if (_dfmReports.ContainsKey(p.Code))
        {
            Logger?.LogInformation("Using cached DFM results for process {ProcessCode}", p.Code);
            // Restore the correct typed report slot from our per-process cache.
            ApplyTypedReportFromCache(p.Code);
            Part.ResolveDfmReport();
            Part.AvailableMaterials = [];
            Part.AvailableFinishes = [];
            Part.AvailableTolerances = [];
            Part.AvailableProcessOptions = [];
            Part.ProcessOptionValues = new();
            await OnPartChanged.InvokeAsync(Part);
            return;
        }

        // No cached result — clear DFM state and kick off a fresh analysis.
        Part.DfmReport = null;
        Part.AvailableMaterials = [];
        Part.AvailableFinishes = [];
        Part.AvailableTolerances = [];
        Part.AvailableProcessOptions = [];
        Part.ProcessOptionValues = new();
        await OnPartChanged.InvokeAsync(Part);   // materials load NOW

        // DFM runs after materials are already loading. finally block fires OnPartChanged again with DFM state.
        await AnalyzeProcessForDfm(p);
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
        await OnPartChanged.InvokeAsync(Part);
        _ = RefreshFinishPricesAsync();
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

    private async Task RefreshFinishPricesAsync()
    {
        if (Part == null
            || string.IsNullOrEmpty(Part.ProcessCode)
            || !Part.MaterialId.HasValue
            || !Part.ToleranceId.HasValue
            || !Part.EstimatedUnitPrice.HasValue
            || Part.AvailableFinishes.Count == 0)
            return;

        try
        {
            var request = new FinishPriceRequestDto
            {
                ProcessCode = Part.ProcessCode,
                MaterialId = Part.MaterialId.Value,
                ToleranceId = Part.ToleranceId.Value,
                BaseUnitPrice = Part.EstimatedUnitPrice.Value,
                FinishIds = Part.AvailableFinishes.Select(f => f.Id).ToList(),
            };
            var response = await Http.PostAsJsonAsync("api/pricing/finish-options", request);
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

        StateHasChanged();

        try
        {
            Logger?.LogInformation("Starting DFM analysis for upload {UploadId}, process {ProcessCode}",
                part.FileId, process.Code);

            var response = await Http.PostAsJsonAsync(
                $"api/geometry/{part.FileId}/dfm/{process.Code}",
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
            if (part.ProcessCode == process.Code)
            {
                part.ResolveDfmReport();
                part.AvailableMaterials = [];
                part.AvailableFinishes = [];
                part.AvailableTolerances = [];
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
                    $"api/uploads/viewer-url?storagePath={Uri.EscapeDataString(path)}");
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
