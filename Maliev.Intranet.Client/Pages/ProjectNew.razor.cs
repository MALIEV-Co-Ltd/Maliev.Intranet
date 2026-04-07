using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Helpers;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace Maliev.Intranet.Client.Pages;

/// <summary>
/// New project creation page — shell with stub state. Logic added in Tasks 4–17.</summary>
public partial class ProjectNew : IAsyncDisposable
{
    /// <summary>
    /// Holds a live DotNetObjectReference to a callback instance so JS Interop can
    /// invoke OnUploadProgress and update the part's progress bar.
    /// </summary>
    private readonly Dictionary<Guid, DotNetObjectReference<UploadProgressCallback>> _uploadCallbacks = [];

    // ── Project-level state ───────────────────────────────────────────
    private Guid _tempProjectId = Guid.NewGuid();  // non-readonly; reassigned on Duplicate
    private string _title = $"Project {DateTime.Today:yyyy-MM-dd}";
    private string? _description;
    private CustomerSummaryDto? _selectedCustomer;
    private bool _showCustomerSearch = true;
    private CurrencyDto? _selectedCurrency;
    private decimal _exchangeRate = 1m;
    private List<CurrencyDto> _currencies = [];
    private List<LeadTimeOptionDto> _leadTimeOptions = [];
    private LeadTimeOptionDto? _selectedLeadTime;
    private List<ProcessDto> _processes = [];
    private bool _saving;
    private bool _autoSaving;
    private DateTimeOffset? _lastSavedAt;
    private LayoutMode _layoutMode = LayoutMode.Configurator;

    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
    private ProjectLeftPanel? _leftPanel;

    // ── Validation ────────────────────────────────────────────────────
    private bool _titleHasError;
    private bool _descriptionHasError;

    // ── Parts ─────────────────────────────────────────────────────────
    private readonly List<PartViewModel> _parts = [];
    private int _selectedPartIndex;

    // ── Customer search ────────────────────────────────────────────────
    private CancellationTokenSource? _searchCts;

    // ── Upload catch-up / watchdog ─────────────────────────────────────
    private const int CatchUpDelayMs = 5000;   // first fetch after SignalR group join
    private const int StatusPollIntervalMs = 30_000; // subsequent interval
    private readonly Dictionary<string, CancellationTokenSource> _statusPollCts = new();

    // ── Pricing debounce ───────────────────────────────────────────────
    private readonly Dictionary<Guid, CancellationTokenSource> _pricingTokens = new();
    // Tracks which process code was in effect when routing was last fetched per part, to avoid
    // redundant re-fetches on non-process-change events (e.g. quantity, material, finish updates).
    private readonly Dictionary<Guid, string> _routingProcessByPart = new();
    private const int PricingDebounceMs = 300;

    // ── Session ────────────────────────────────────────────────────────
    private Guid _sessionId;

    /// <summary>
    /// When non-null, the project has been persisted to the ProjectService as a Draft.
    /// Subsequent auto-saves PUT updates to this project instead of POSTing a new one.
    /// </summary>
    private Guid? _serverProjectId;

    // ── Auto-save debounce ─────────────────────────────────────────────
    private const int AutoSaveDebounceMs = 1000;
    private string DraftStorageKey => $"project-draft-{_sessionId}";
    private Timer? _autoSaveDebounceTimer;

    // ── SignalR ────────────────────────────────────────────────────────
    private HubConnection? _hubConnection;

    [Inject] private FileTypesSettings FileTypes { get; set; } = null!;
    [Inject] private UploadSettings UploadSettings { get; set; } = null!;
    [Inject] private CookieProvider CookieProvider { get; set; } = null!;
    [Inject] private ILogger<ProjectNew> Logger { get; set; } = null!;

    private bool CanSubmit =>
        !_saving &&
        _selectedCustomer != null &&
        !string.IsNullOrWhiteSpace(_title) &&
        !_titleHasError &&
        !_descriptionHasError &&
        _selectedLeadTime != null &&
        _parts.Count > 0 &&
        !_parts.Any(p => p.Uploading || p.PricingLoading) &&
        _parts.All(p => p.IsFullyConfigured && !p.PricingFailed) &&
        _parts.All(p => p.IsManifold != false || p.DfmAcknowledged);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _pricingTokens.Values) { await cts.CancelAsync(); cts.Dispose(); }
        foreach (var cts in _statusPollCts.Values) { cts.Cancel(); cts.Dispose(); }
        _statusPollCts.Clear();
        _autoSaveDebounceTimer?.Dispose();
        if (_searchCts != null) { await _searchCts.CancelAsync(); _searchCts.Dispose(); }
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
    }

    // ── Task 4: Initialize ─────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        // ── Session management: parse or assign ?session= GUID ─────────
        var uri = new Uri(Navigation.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var sessionParam = query["session"];
        var resumeParam = query["resume"];

        if (string.IsNullOrEmpty(sessionParam) || !Guid.TryParse(sessionParam, out _sessionId))
        {
            _sessionId = Guid.NewGuid();
            var resumeFragment = Guid.TryParse(resumeParam, out var resumeId) ? $"&resume={resumeId}" : "";
            // Redirect to URL with session param. In SSR this throws NavigationException (stops execution).
            // In WASM, NavigateTo updates the URL in-place without recreating the component, so we must
            // NOT return — data loading must continue immediately with the newly assigned _sessionId.
            Navigation.NavigateTo($"/sales/projects/new?session={_sessionId}{resumeFragment}", replace: true);
        }

        // ── Load reference data ────────────────────────────────────────
        // Start all three tasks concurrently but handle failures independently —
        // a single service failure (e.g. pricing 401) must not prevent currencies
        // and processes from loading.
        var currenciesTask = Http.GetFromJsonAsync<List<CurrencyDto>>("api/referenceData/currencies");
        var processesTask  = Http.GetFromJsonAsync<List<ProcessDto>>("api/catalog/processes");
        var leadTimesTask  = Http.GetFromJsonAsync<List<LeadTimeOptionDto>>("api/pricing/lead-times");

        await Task.WhenAll(
            currenciesTask.ContinueWith(_ => { }),
            processesTask.ContinueWith(_ => { }),
            leadTimesTask.ContinueWith(_ => { }));

        try
        {
            var result = await currenciesTask;
            if (result is { Count: > 0 })
            {
                _currencies = result;
                _selectedCurrency ??= _currencies.FirstOrDefault(c => c.IsPrimary) ?? _currencies.First();
            }
        }
        catch (Exception ex) { Snackbar.Add($"Failed to load currencies: {ex.Message}", Severity.Warning); }

        try
        {
            var result = await processesTask;
            if (result is { Count: > 0 })
                _processes = result.Where(p => p.Code is not "CNC").ToList();
        }
        catch (Exception ex) { Snackbar.Add($"Failed to load processes: {ex.Message}", Severity.Warning); }

        try
        {
            var result = await leadTimesTask;
            if (result is { Count: > 0 })
            {
                _leadTimeOptions = result
                    .Where(lt => !lt.Code.Equals("RUSH", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                _selectedLeadTime ??= _leadTimeOptions.FirstOrDefault(lt => lt.IsDefault) ?? _leadTimeOptions.FirstOrDefault();
            }
        }
        catch (Exception ex) { Snackbar.Add($"Failed to load lead times: {ex.Message}", Severity.Warning); }

        await RestoreDraftAsync();

        // ── Server resume: if ?resume={id} or draft has ServerProjectId, hydrate from server ──
        var serverResumeId = Guid.TryParse(resumeParam, out var parsedResumeId) ? parsedResumeId : _serverProjectId;
        if (serverResumeId.HasValue && _selectedCustomer == null)
        {
            await ResumeFromServerAsync(serverResumeId.Value);
        }

        // ── SignalR hub connection ─────────────────────────────────────
        _hubConnection = new HubConnectionBuilder()
            .WithUrlAndCookies(Navigation.ToAbsoluteUri("/hubs/notifications").ToString(), CookieProvider.CookieHeader)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Reconnected += async _ =>
        {
            foreach (var part in _parts.Where(p => !string.IsNullOrEmpty(p.StoragePath)))
                await _hubConnection.InvokeAsync("JoinFileGroup", part.StoragePath);
        };

        _hubConnection.On<SignalRFileAnalysisPayload>("FileAnalysisCompleted", async payload =>
        {
            var part = _parts.FirstOrDefault(p => p.StoragePath == payload.StoragePath);
            if (part == null) return;

            if (payload.Failed)
            {
                part.AwaitingPreview = false;
                part.AnalysisErrorCode = payload.ErrorCode;
                part.DfmAnalysisTimedOut = true; // unblocks the DFM overlay immediately
                part.StatusText = DfmStatusMessages.GetStatusText(payload.ErrorCode);
                StopStatusWatchdog(payload.StoragePath);
                TriggerAutoSave();
                await InvokeAsync(StateHasChanged);
                return;
            }

            if (!string.IsNullOrEmpty(payload.ThumbnailUrl) && string.IsNullOrEmpty(part.ThumbnailSmallUrl))
            {
                part.ThumbnailSmallUrl = payload.ThumbnailUrl;
                await InvokeAsync(StateHasChanged);
            }

            if (payload.Dimensions != null)
            {
                part.Dimensions = new FileAnalysisDimensionsDto
                {
                    X = payload.Dimensions.X,
                    Y = payload.Dimensions.Y,
                    Z = payload.Dimensions.Z,
                    VolumeMm3 = payload.Dimensions.VolumeMm3
                };
                part.VolumeMm3 = payload.Dimensions.VolumeMm3;
                TriggerAutoSave();
            }

            if (payload.PreviewUrls != null)
            {
                part.ThumbnailSmallUrl = payload.PreviewUrls.ThumbnailSmall ?? part.ThumbnailSmallUrl;
                part.ThumbnailLargeUrl = payload.PreviewUrls.ThumbnailLarge ?? payload.HiResThumbnailUrl;
                part.ThumbnailSmallGcsPath = payload.PreviewUrls.ThumbnailSmallGcsPath;
                part.ThumbnailLargeGcsPath = payload.PreviewUrls.ThumbnailLargeGcsPath;
                part.AwaitingPreview = false;
                part.StatusText = "Ready";
                TriggerAutoSave();
                await InvokeAsync(StateHasChanged);
            }
        });

        _hubConnection.On<SignalRGlbReadyPayload>("GlbReady", async payload =>
        {
            var part = _parts.FirstOrDefault(p => p.StoragePath == payload.StoragePath);
            if (part == null) return;

            if (!payload.Failed)
            {
                part.GlbSignedUrl = payload.GlbUrl;
                part.GlbStoragePath ??= payload.StoragePath;
                part.ViewerUrl = payload.GlbUrl;
            }
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<SignalRDfmAnalysisPayload>("DfmAnalysisReady", async payload =>
        {
            var part = _parts.FirstOrDefault(p => p.StoragePath == payload.StoragePath);
            if (part == null) return;

            part.FdmDfmReport = payload.FdmReport;
            part.SlaDfmReport = payload.SlaReport;
            part.CncDfmReport = payload.CncReport;
            part.OverlayUrls = payload.OverlayUrls;
            part.OverlayPaths = payload.OverlayPaths;
            part.ResolveDfmReport();
            StopStatusWatchdog(payload.StoragePath);

            await InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();

        foreach (var part in _parts.Where(p => !string.IsNullOrEmpty(p.StoragePath)))
            await _hubConnection.InvokeAsync("JoinFileGroup", part.StoragePath);
    }

    // ── Task 4: Customer search ────────────────────────────────────────

    /// <inheritdoc />
    private async Task<IEnumerable<CustomerSummaryDto>> SearchCustomersAsync(string value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            return [];

        // Cancel any in-flight search request so debounced keystrokes don't
        // leave stale HTTP calls running in the background.
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            var result = await Http.GetFromJsonAsync<PagedResponse<CustomerSummaryDto>>(
                $"api/customers?query={Uri.EscapeDataString(value)}&page=1&pageSize=10",
                _searchCts.Token);
            return result?.Data ?? [];
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Debounce cancelled this request — not a real error.
            return [];
        }
        catch (Exception)
        {
            Snackbar.Add("Failed to search customers.", Severity.Warning);
            return [];
        }
    }

    // ── Recent Projects for left panel ────────────────────────────────

    private async Task<List<ProjectSummaryDto>> LoadRecentProjectsAsync(Guid customerId)
    {
        try
        {
            var result = await Http.GetFromJsonAsync<PagedResponse<ProjectSummaryDto>>(
                $"api/projects?customerId={customerId}&pageSize=10");
            return result?.Data
                ?.Where(p => p.Status is "Draft" or "Configuring")
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task OnRecentProjectClicked(Guid projectId)
    {
        _serverProjectId = null;
        _parts.Clear();
        _title = $"Project {DateTime.Today:yyyy-MM-dd}";
        _description = null;
        _selectedCustomer = null;
        _showCustomerSearch = true;
        _selectedLeadTime = null;
        _selectedCurrency = _currencies.FirstOrDefault(c => c.IsPrimary) ?? _currencies.FirstOrDefault();
        _lastSavedAt = null;

        await ResumeFromServerAsync(projectId);

        Navigation.NavigateTo($"/sales/projects/new?session={_sessionId}&resume={projectId}", replace: true);
    }

    // ── Task 10: File upload / polling ─────────────────────────────────

    /// <inheritdoc />
    private async Task HandleFileSelected(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.Name);
            if (!FileTypes.AllUploadExtensions.Contains(ext))
            {
                Snackbar.Add($"File type '{ext}' is not allowed.", Severity.Warning);
                continue;
            }

            var part = new PartViewModel
            {
                Name = file.Name,
                Uploading = true,
                AwaitingPreview = true,
                StatusText = "Uploading...",
            };

            _parts.Add(part);
            _selectedPartIndex = _parts.Count - 1;

            _ = UploadAndPollAsync(part, file);
        }
    }

    private async Task UploadAndPollAsync(PartViewModel part, IBrowserFile file)
    {
        string? storagePath = null;
        var uploadId = Guid.NewGuid();
        DotNetObjectReference<UploadProgressCallback>? callbackRef = null;

        try
        {
            var callback = new UploadProgressCallback(part, () => InvokeAsync(StateHasChanged));
            callbackRef = DotNetObjectReference.Create(callback);
            _uploadCallbacks[uploadId] = callbackRef;

            using var memoryStream = new MemoryStream();
            await using var readStream = file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
            await readStream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var url = $"api/uploads?projectId={_tempProjectId}&customerId={_selectedCustomerId}";
            var result = await JS.InvokeAsync<UploadResult>($"window.uploadWithProgress", url, fileBytes, file.Name, callbackRef);

            if (result.Status != 200)
            {
                part.Uploading = false;
                part.AwaitingPreview = false;
                part.Error = $"Upload failed ({result.Status})";
                Snackbar.Add($"Failed to upload {file.Name}.", Severity.Error);
                return;
            }

            var uploadResult = JsonSerializer.Deserialize<BffUploadResponse>(result.Body);
            if (uploadResult == null || string.IsNullOrEmpty(uploadResult.StoragePath))
            {
                part.Uploading = false;
                part.AwaitingPreview = false;
                part.Error = "Upload response was empty.";
                Snackbar.Add($"Upload response invalid for {file.Name}.", Severity.Error);
                return;
            }

            storagePath = uploadResult.StoragePath;
            part.StoragePath = storagePath;
            part.FileId = Guid.TryParse(uploadResult.UploadId, out var fid) ? fid : Guid.NewGuid();
            part.Uploading = false;
            part.ProgressPercent = 0;
            part.StatusText = "Processing geometry...";

            if (_hubConnection?.State == HubConnectionState.Connected)
                await _hubConnection.InvokeAsync("JoinFileGroup", storagePath);

            var pollCts = new CancellationTokenSource();
            _statusPollCts[storagePath] = pollCts;
            _ = Task.Run(() => StatusWatchdogLoopAsync(part, storagePath, pollCts.Token));
        }
        catch (OperationCanceledException)
        {
            // Polling cancelled — expected when part is removed
        }
        catch (Exception ex)
        {
            part.Uploading = false;
            part.AwaitingPreview = false;
            part.Error = $"Upload error: {ex.Message}";
            Snackbar.Add($"Error uploading {file.Name}: {ex.Message}", Severity.Error);
        }
        finally
        {
            if (callbackRef != null)
            {
                _uploadCallbacks.Remove(uploadId);
                callbackRef.Dispose();
            }
        }
    }

    private sealed record UploadResult(int Status, string Body);

    /// <summary>
    /// Polls <c>analysis-status</c> on a 30 s interval until a terminal state is reached or the
    /// token is cancelled. Handles the case where a SignalR event is missed due to reconnect timing.
    /// The first fetch is delayed by <see cref="CatchUpDelayMs"/> (5 s) to let SignalR deliver first.
    /// </summary>
    private async Task StatusWatchdogLoopAsync(PartViewModel part, string storagePath, CancellationToken ct)
    {
        try
        {
            await Task.Delay(CatchUpDelayMs, ct);
            while (!ct.IsCancellationRequested)
            {
                await FetchCurrentStatusAsync(part, storagePath);

                // Stop looping once a terminal state is known
                if (part.DfmAnalysisTimedOut
                    || part.DfmReport != null
                    || !string.IsNullOrEmpty(part.Error)
                    || (!part.AwaitingPreview && part.StatusText == "Ready")
                    || !_parts.Contains(part))
                    break;

                await Task.Delay(StatusPollIntervalMs, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (_statusPollCts.TryGetValue(storagePath, out var cts) && cts.Token == ct)
            {
                _statusPollCts.Remove(storagePath);
                cts.Dispose();
            }
        }
    }

    private void StopStatusWatchdog(string? storagePath)
    {
        if (storagePath == null) return;
        if (_statusPollCts.TryGetValue(storagePath, out var cts))
        {
            _statusPollCts.Remove(storagePath);
            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <summary>
    /// Catch-up fetch for analysis status — called by the watchdog loop.
    /// Live updates arrive via SignalR; this is a safety net for missed events.
    /// </summary>
    private async Task FetchCurrentStatusAsync(PartViewModel part, string storagePath)
    {
        try
        {
            var statusResponse = await Http.GetAsync(
                $"api/uploads/analysis-status?storagePath={Uri.EscapeDataString(storagePath)}");

            if (!statusResponse.IsSuccessStatusCode)
            {
                await ResolveViewerUrlAsync(part);
                await InvokeAsync(StateHasChanged);
                return;
            }

            var status = await statusResponse.Content.ReadFromJsonAsync<FileAnalysisStatusDto>();
            if (status == null)
            {
                await ResolveViewerUrlAsync(part);
                await InvokeAsync(StateHasChanged);
                return;
            }

            part.Dimensions = status.Dimensions;
            part.VolumeMm3 = status.Dimensions?.VolumeMm3;
            part.IsManifold = status.IsManifold;
            part.GlbStoragePath = status.GlbStoragePath;
            part.GlbSignedUrl = status.GlbSignedUrl;  // Option B: use cached signed URL directly

            if (status.DfmReport is JsonElement je && je.ValueKind == JsonValueKind.Object
                && je.TryGetProperty("FdmReport", out _))
            {
                var dfmPayload = JsonSerializer.Deserialize<SignalRDfmAnalysisPayload>(je.GetRawText());
                part.FdmDfmReport = dfmPayload?.FdmReport;
                part.SlaDfmReport = dfmPayload?.SlaReport;
                part.CncDfmReport = dfmPayload?.CncReport;
                part.ResolveDfmReport();
            }
            else
            {
                part.DfmReport = status.DfmReport;
            }

            if (status.PreviewUrls != null)
            {
                part.ThumbnailSmallUrl = status.PreviewUrls.ThumbnailSmall
                    ?? status.ThumbnailUrl
                    ?? status.PreviewUrls.ThumbnailLargeUrl
                    ?? status.HiResThumbnailUrl;
                part.ThumbnailLargeUrl = status.PreviewUrls.ThumbnailLargeUrl
                    ?? status.HiResThumbnailUrl;
            }
            else if (!string.IsNullOrEmpty(status.ThumbnailUrl))
            {
                part.ThumbnailSmallUrl = status.ThumbnailUrl;
                part.ThumbnailLargeUrl = status.HiResThumbnailUrl ?? status.ThumbnailUrl;
            }

            if (status.Status == FileAnalysisStatus.Completed &&
                (status.PreviewProcessingStatus == PreviewProcessingStatus.Completed ||
                 status.PreviewProcessingStatus == PreviewProcessingStatus.Failed))
            {
                part.AwaitingPreview = false;
                part.StatusText = "Ready";
                TriggerAutoSave();
            }
            else if (status.Status == FileAnalysisStatus.Failed)
            {
                part.AwaitingPreview = false;
                part.Error = $"Geometry analysis failed: {status.ErrorCode}";
                part.StatusText = "Analysis failed";
            }

            // Bug 1 fix: resolve a signed GLB viewer URL so the 3D viewer auto-loads
            // after a draft restore. This mirrors OpenBabylonViewer but is non-fatal.
            await ResolveViewerUrlAsync(part);

            // Re-sign overlay GLB paths so click-to-highlight works after draft restore
            await ResolveOverlayUrlsAsync(part);

            // Bug 5/6 fix: trigger pricing after the catch-up fetch has populated
            // Dimensions/VolumeMm3 so ComputePriceAsync has accurate geometry data.
            // Skip if catalog is not yet loaded (ReloadPartCatalogAsync will trigger pricing itself).
            if (part.ProcessId.HasValue && part.MaterialId.HasValue
                && part.AvailableMaterials.Count > 0)
                TriggerPricingAsync(part);

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            // Catch-up fetch is a safety net — failures are non-fatal; SignalR will deliver the final state
            await InvokeAsync(() => Snackbar.Add($"Status fetch failed for {part.Name}: {ex.Message}", Severity.Warning));
        }
    }

    private async Task ResolveViewerUrlAsync(PartViewModel part)
    {
        if (!string.IsNullOrEmpty(part.ViewerUrl)) return;

        // Option B: Use pre-signed URL from cache if available (no API call needed)
        if (!string.IsNullOrEmpty(part.GlbSignedUrl))
        {
            part.ViewerUrl = part.GlbSignedUrl;
            return;
        }

        // Fallback: Call viewer-url API for backward compatibility (drafts created before this fix)
        // Prefer GlbStoragePath (already has _viewer.glb suffix) over StoragePath to avoid double-suffix bug
        var storagePath = part.GlbStoragePath ?? part.StoragePath;
        if (string.IsNullOrEmpty(storagePath)) return;

        try
        {
            var viewerResp = await Http.GetAsync(
                $"api/uploads/viewer-url?storagePath={Uri.EscapeDataString(storagePath)}");
            if (viewerResp.IsSuccessStatusCode)
            {
                var viewerJson = await viewerResp.Content.ReadFromJsonAsync<JsonDocument>();
                var resolvedUrl = viewerJson?.RootElement.GetProperty("url").GetString();
                if (!string.IsNullOrEmpty(resolvedUrl))
                    part.ViewerUrl = resolvedUrl;
            }
        }
        catch
        {
            // Non-fatal — viewer URL resolution is best-effort
        }
    }

    /// <summary>
    /// Re-signs raw GCS overlay paths into fresh signed URLs so overlays work after
    /// draft restore when the original signed URLs may have expired.
    /// </summary>
    private async Task ResolveOverlayUrlsAsync(PartViewModel part)
    {
        if (part.OverlayPaths is not { Count: > 0 }) return;
        try
        {
            var signed = new Dictionary<string, string>(part.OverlayPaths.Count);
            foreach (var (key, path) in part.OverlayPaths)
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
            if (signed.Count > 0)
                part.OverlayUrls = signed;
        }
        catch
        {
            // Non-fatal — overlay URL resolution is best-effort
        }
    }

    // ── Task 10: Remove part ───────────────────────────────────────────

    /// <inheritdoc />
    private async Task RemovePart(PartViewModel part)
    {
        if (!string.IsNullOrEmpty(part.StoragePath) && _hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.InvokeAsync("LeaveFileGroup", part.StoragePath);

        StopStatusWatchdog(part.StoragePath);

        // Cascade delete all attachment files before removing the part
        foreach (var att in part.DrawingFiles.Concat(part.SupplementaryFiles))
        {
            try { await Http.DeleteAsync($"api/uploads/attachments/{att.FileId}"); } catch { /* non-fatal */ }
        }

        // Delete from server if the project has been cloud-saved
        if (_serverProjectId.HasValue && !string.IsNullOrEmpty(part.StoragePath))
        {
            try { await Http.DeleteAsync($"api/projects/{_serverProjectId}/parts/{part.FileId}"); } catch { /* non-fatal */ }
        }

        _parts.Remove(part);

        if (_selectedPartIndex >= _parts.Count)
            _selectedPartIndex = Math.Max(0, _parts.Count - 1);

        TriggerAutoSave();
    }

    // ── Task 19: Activate part from list view ─────────────────────────

    /// <summary>Switches to Configurator layout and selects the given part.</summary>
    private void ActivatePartFromList(PartViewModel part)
    {
        _selectedPartIndex = _parts.IndexOf(part);
        if (_selectedPartIndex < 0) _selectedPartIndex = 0;
        _layoutMode = LayoutMode.Configurator;
        StateHasChanged();
    }

    // ── Task 12: Cascading dropdowns ──────────────────────────────────

    /// <inheritdoc />
    private async Task OnPartChanged(PartViewModel part)
    {
        if (part.ProcessId.HasValue && !string.IsNullOrEmpty(part.ProcessCode)
            && part.AvailableMaterials.Count == 0) // only reload catalog on process change (OnProcessChanged clears AvailableMaterials before invoking this)
        {
            // Process changed — stale routing is for the old process type; clear it so it re-fetches.
            part.ProductionRouting = null;
            part.CatalogLoading = true;
            try
            {
                var processCode = part.ProcessCode;
                var materialsTask = Http.GetFromJsonAsync<List<CatalogMaterialDto>>(
                    $"api/catalog/processes/{Uri.EscapeDataString(processCode)}/materials");
                var finishesTask = Http.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>(
                    $"api/catalog/processes/{Uri.EscapeDataString(processCode)}/finishes");
                var tolerancesTask = Http.GetFromJsonAsync<List<CatalogToleranceDto>>(
                    $"api/catalog/processes/{Uri.EscapeDataString(processCode)}/tolerances");

                await Task.WhenAll(materialsTask, finishesTask, tolerancesTask);

                part.AvailableMaterials = materialsTask.Result ?? [];
                part.AvailableFinishes = finishesTask.Result ?? [];
                part.AvailableTolerances = tolerancesTask.Result ?? [];

                if (!part.MaterialId.HasValue)
                {
                    var defaultMaterial = part.AvailableMaterials.OrderBy(m => m.SortOrder).FirstOrDefault();
                    if (defaultMaterial != null)
                    {
                        part.MaterialId = defaultMaterial.Id;
                        part.MaterialCode = defaultMaterial.Code;
                    }
                }

                if (!part.FinishId.HasValue)
                {
                    var defaultFinish = part.AvailableFinishes.OrderBy(f => f.SortOrder).FirstOrDefault();
                    if (defaultFinish != null)
                    {
                        part.FinishId = defaultFinish.Id;
                        part.FinishCode = defaultFinish.Code;
                    }
                }

                if (!part.ToleranceId.HasValue)
                {
                    var defaultTolerance = part.AvailableTolerances.OrderBy(t => t.SortOrder).FirstOrDefault();
                    if (defaultTolerance != null)
                    {
                        part.ToleranceId = defaultTolerance.Id;
                        part.ToleranceCode = defaultTolerance.Code;
                    }
                }
            }
            catch (Exception)
            {
                Snackbar.Add("Failed to load catalog options.", Severity.Warning);
            }
            finally
            {
                part.CatalogLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
        else if (!part.ProcessId.HasValue || string.IsNullOrEmpty(part.ProcessCode))
        {
            part.AvailableMaterials = [];
            part.AvailableFinishes = [];
            part.AvailableTolerances = [];
        }

        // Bug 4 fix: only re-resolve the DFM report when there are per-process reports to
        // resolve from. Do NOT call ResolveDfmReport unconditionally — it clears DfmReport
        // to null when ProcessCode is null/empty, which causes the overlay to flash
        // "All Clear" on every keystroke in the description field or any config change.
        if ((part.FdmDfmReport != null || part.SlaDfmReport != null || part.CncDfmReport != null)
            && !string.IsNullOrEmpty(part.ProcessCode))
            part.ResolveDfmReport();

        TriggerAutoSave();
        TriggerPricingAsync(part);
    }

    // ── Task 13: Pricing ───────────────────────────────────────────────

    /// <summary>
    /// Triggers a debounced pricing calculation for the given part.
    /// </summary>
    /// <param name="part">The part to price.</param>
    private void TriggerPricingAsync(PartViewModel part)
    {
        if (_projectLocked) return;

        if (_pricingTokens.TryGetValue(part.FileId, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
            _pricingTokens.Remove(part.FileId);
        }

        var cts = new CancellationTokenSource();
        _pricingTokens[part.FileId] = cts;

        part.PricingLoading = true;
        _ = InvokeAsync(StateHasChanged);
        _ = DebouncedPriceCallAsync(part, cts.Token);
    }

    private async Task DebouncedPriceCallAsync(PartViewModel part, CancellationToken ct)
    {
        try
        {
            await Task.Delay(PricingDebounceMs, ct);
            if (ct.IsCancellationRequested) return;

            await ComputePriceAsync(part, ct);
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled — another trigger will reschedule
        }
    }

    private async Task ComputePriceAsync(PartViewModel part, CancellationToken ct)
    {
        if (_projectLocked) return;

        if (!part.ProcessId.HasValue || !part.MaterialId.HasValue)
        {
            part.PricingLoading = false;
            part.EstimatedUnitPrice = null;
            part.EstimatedTotalAmount = null;
            return;
        }

        part.PricingLoading = true;
        part.PricingFailed = false;

        try
        {
            var geometry = part.VolumeMm3.HasValue
                ? new GeometryMetricsDto
                {
                    VolumeCm3 = (decimal)part.VolumeMm3.Value / 1_000m,
                    SupportVolumeCm3 = 0m,
                    SurfaceAreaCm2 = 0m,
                    BoundingBoxX = (decimal)(part.Dimensions?.X ?? 0),
                    BoundingBoxY = (decimal)(part.Dimensions?.Y ?? 0),
                    BoundingBoxZ = (decimal)(part.Dimensions?.Z ?? 0),
                    IsManifold = part.IsManifold ?? false,
                    TriangleCount = 0,
                }
                : null;

            var process = _processes.FirstOrDefault(p => p.Id == part.ProcessId);

            var request = new PricingRequestDto
            {
                FileId = part.FileId,
                CustomerId = _selectedCustomerId ?? Guid.Empty,
                MaterialId = part.MaterialId ?? Guid.Empty,
                MaterialCode = part.MaterialCode ?? string.Empty,
                ManufacturingProcessId = part.ProcessId ?? Guid.Empty,
                ManufacturingProcessName = process?.Name ?? string.Empty,
                Quantity = part.Quantity,
                Geometry = geometry!,
                StoragePath = part.StoragePath,
            };

            var response = await Http.PostAsJsonAsync("api/pricing/calculate", request, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PricingResultDto>(cancellationToken: ct);
                if (result != null)
                {
                    part.EstimatedUnitPrice = result.TotalUnitPrice;
                    part.EstimatedTotalAmount = result.TotalPrice;
                    part.EstimatedLeadTimeDays = result.EstimatedLeadTimeDays ?? 0;
                    part.PricingFailed = false;
                }
            }
            else
            {
                part.PricingFailed = true;
                part.EstimatedUnitPrice = null;
                part.EstimatedTotalAmount = null;
            }
        }
        catch (Exception)
        {
            part.PricingFailed = true;
        }
        finally
        {
            part.PricingLoading = false;
            TriggerAutoSave();
            await InvokeAsync(StateHasChanged);
            await TriggerRoutingFetchAsync(part);
        }
    }

    // ── Production Routing ──────────────────────────────────────────

    /// <summary>
    /// Fetches production routing data for a part when it is fully configured
    /// (pricing complete with EstimatedLeadTimeDays &gt; 0 and lead time selected).
    /// </summary>
    private async Task TriggerRoutingFetchAsync(PartViewModel part)
    {
        if (_selectedLeadTime == null)
            return;

        if (!part.ProcessId.HasValue || !part.MaterialId.HasValue || string.IsNullOrEmpty(part.ProcessCode))
            return;

        // Allow re-fetch if process type changed (routing was cleared above by OnPartChanged).
        // Skip only if a fetch is already in flight or routing is fresh for the current process.
        if (part.ProductionRoutingLoading)
            return;
        if (part.ProductionRouting != null &&
            string.Equals(part.ProductionRouting.MachineCode, "TBD", StringComparison.Ordinal) == false &&
            _routingProcessByPart.TryGetValue(part.FileId, out var cachedProcess) &&
            cachedProcess == part.ProcessCode)
            return;

        if (_tempProjectId == Guid.Empty)
            return;

        // Use ServerPartId when available (part synced to server), fall back to FileId
        var partId = part.ServerPartId ?? part.FileId;
        if (partId == Guid.Empty)
            return;

        part.ProductionRoutingLoading = true;
        var fetchedProcess = part.ProcessCode;
        try
        {
            var processCode = Uri.EscapeDataString(part.ProcessCode ?? "");
            part.ProductionRouting = await Http.GetFromJsonAsync<ProductionRoutingDto>(
                $"api/projects/{_tempProjectId}/parts/{partId}/routing?processType={processCode}");
            // Record which process this routing is for so we can detect staleness later
            if (part.ProcessCode == fetchedProcess)
                _routingProcessByPart[part.FileId] = fetchedProcess!;
        }
        catch
        {
            part.ProductionRouting = null;
        }
        finally
        {
            part.ProductionRoutingLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ── Task 14: Auto-save ─────────────────────────────────────────────

    /// <inheritdoc />
    private async Task OnCurrencyChangedAsync(CurrencyDto? currency)
    {
        _selectedCurrency = currency;

        if (currency == null || string.Equals(currency.Code, "THB", StringComparison.OrdinalIgnoreCase))
        {
            _exchangeRate = 1m;
        }
        else
        {
            try
            {
                var resp = await Http.GetFromJsonAsync<ExchangeRateResponse>(
                    $"api/reference-data/currencies/rate?from=THB&to={Uri.EscapeDataString(currency.Code)}");
                _exchangeRate = resp?.Rate ?? 1m;
            }
            catch
            {
                _exchangeRate = 1m;
            }
        }

        TriggerAutoSave();
    }

    private void TriggerAutoSave()
    {
        _autoSaveDebounceTimer?.Dispose();
        _autoSaveDebounceTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () => await SaveDraftAsync());
        }, null, AutoSaveDebounceMs, Timeout.Infinite);
    }

    private Guid? _selectedCustomerId => _selectedCustomer?.Id;

    private bool _serverSaveInProgress;

    /// <summary>
    /// True when the project has transitioned past Draft/Configuring (e.g. Quoted or Accepted).
    /// Pricing must not re-trigger for locked projects.
    /// </summary>
    private bool _projectLocked;

    private async Task SaveDraftAsync()
    {
        _autoSaving = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var draft = new DraftProjectState
            {
                TempProjectId = _tempProjectId,
                ServerProjectId = _serverProjectId,
                Title = _title,
                Description = _description,
                CustomerId = _selectedCustomer?.Id,
                CustomerName = _selectedCustomer?.Name,
                CustomerCompanyName = _selectedCustomer?.CompanyName,
                CustomerEmail = _selectedCustomer?.Email,
                CustomerMobile = _selectedCustomer?.Mobile,
                CustomerLandline = _selectedCustomer?.Landline,
                CustomerCompanyPhone = _selectedCustomer?.CompanyPhone,
                SelectedLeadTimeCode = _selectedLeadTime?.Code ?? "STANDARD",
                SelectedCurrencyCode = _selectedCurrency?.Code,
                LastModified = DateTime.UtcNow,
                Parts = _parts.Select(p => p.ToDraftPartState()).ToList(),
            };

            var json = JsonSerializer.Serialize(draft);
            await JS.InvokeVoidAsync("sessionStorage.setItem", DraftStorageKey, json);
            _lastSavedAt = DateTimeOffset.UtcNow;

            if (_selectedCustomerId.HasValue && !_serverSaveInProgress)
            {
                _ = SaveDraftToServerAsync();
            }
        }
        catch (Exception)
        {
            Snackbar.Add("Auto-save failed.", Severity.Warning);
        }
        finally
        {
            if (!_serverSaveInProgress)
            {
                _autoSaving = false;
                StateHasChanged();
            }
        }
    }

    private async Task SaveDraftToServerAsync()
    {
        _serverSaveInProgress = true;
        try
        {
            if (_serverProjectId == null)
            {
                var createRequest = new CreateProjectRequest
                {
                    CustomerId = _selectedCustomerId!.Value,
                    CustomerName = _selectedCustomer?.Name ?? string.Empty,
                    Title = _title,
                    Description = _description,
                    Currency = _selectedCurrency?.Code ?? "THB",
                };

                var response = await Http.PostAsJsonAsync("api/projects", createRequest);
                if (response.IsSuccessStatusCode)
                {
                    var project = await response.Content.ReadFromJsonAsync<ProjectDetailDto>();
                    if (project != null)
                    {
                        _serverProjectId = project.Id;
                        _tempProjectId = project.Id;

                        foreach (var part in _parts.Where(p => p.IsFullyConfigured && !string.IsNullOrEmpty(p.StoragePath)))
                        {
                            try
                            {
                                var addPartRequest = new AddProjectPartRequest
                                {
                                    FileId = part.FileId,
                                    FileName = part.Name,
                                    ProcessType = part.ProcessCode,
                                    MaterialId = part.MaterialId,
                                    Quantity = part.Quantity,
                                    Finish = part.FinishCode,
                                    Tolerance = part.ToleranceCode,
                                };
                                var partResponse = await Http.PostAsJsonAsync($"api/projects/{_serverProjectId}/parts", addPartRequest);
                                if (partResponse.IsSuccessStatusCode)
                                {
                                    var createdPart = await partResponse.Content.ReadFromJsonAsync<ProjectPartDto>();
                                    if (createdPart != null)
                                    {
                                        part.ServerPartId = createdPart.Id;
                                    }
                                }
                            }
                            catch
                            {
                                // Non-fatal: part sync will retry on next auto-save
                            }
                        }

                        var updatedDraft = new DraftProjectState
                        {
                            TempProjectId = _tempProjectId,
                            ServerProjectId = _serverProjectId,
                            Title = _title,
                            Description = _description,
                            CustomerId = _selectedCustomer?.Id,
                            CustomerName = _selectedCustomer?.Name,
                            CustomerCompanyName = _selectedCustomer?.CompanyName,
                            CustomerEmail = _selectedCustomer?.Email,
                            CustomerMobile = _selectedCustomer?.Mobile,
                            CustomerLandline = _selectedCustomer?.Landline,
                            CustomerCompanyPhone = _selectedCustomer?.CompanyPhone,
                            SelectedLeadTimeCode = _selectedLeadTime?.Code ?? "STANDARD",
                            SelectedCurrencyCode = _selectedCurrency?.Code,
                            LastModified = DateTime.UtcNow,
                            Parts = _parts.Select(p => p.ToDraftPartState()).ToList(),
                        };
                        var updatedJson = JsonSerializer.Serialize(updatedDraft);
                        await JS.InvokeVoidAsync("sessionStorage.setItem", DraftStorageKey, updatedJson);
                        _lastSavedAt = DateTimeOffset.UtcNow;

                        if (_leftPanel is not null)
                            await _leftPanel.RefreshRecentProjectsAsync();
                    }
                }
            }
            else
            {
                var updatePayload = new { Title = _title, Description = _description };
                await Http.PutAsJsonAsync($"api/projects/{_serverProjectId}", updatePayload);

                foreach (var part in _parts.Where(p => p.IsFullyConfigured))
                {
                    try
                    {
                        // Parts not yet synced to the server need to be created first
                        if (!part.ServerPartId.HasValue)
                        {
                            var addPartRequest = new AddProjectPartRequest
                            {
                                FileId = part.FileId,
                                FileName = part.Name,
                                ProcessType = part.ProcessCode,
                                MaterialId = part.MaterialId,
                                Quantity = part.Quantity,
                                Finish = part.FinishCode,
                                Tolerance = part.ToleranceCode,
                            };
                            var partResponse = await Http.PostAsJsonAsync($"api/projects/{_serverProjectId}/parts", addPartRequest);
                            if (partResponse.IsSuccessStatusCode)
                            {
                                var createdPart = await partResponse.Content.ReadFromJsonAsync<ProjectPartDto>();
                                if (createdPart != null)
                                {
                                    part.ServerPartId = createdPart.Id;
                                }
                            }

                            continue;
                        }

                        var partRequest = new UpdateProjectPartRequest
                        {
                            ProcessType = part.ProcessCode,
                            MaterialId = part.MaterialId,
                            Quantity = part.Quantity,
                            Finish = part.FinishCode,
                            Tolerance = part.ToleranceCode,
                        };
                        await Http.PutAsJsonAsync(
                            $"api/projects/{_serverProjectId}/parts/{part.ServerPartId}",
                            partRequest);
                    }
                    catch
                    {
                        // Non-fatal: part update will retry on next auto-save
                    }
                }

                try
                {
                    if (_leftPanel is not null)
                        await _leftPanel.RefreshRecentProjectsAsync();
                }
                catch
                {
                    // Non-fatal: left panel refresh is best-effort
                }
            }
        }
        catch
        {
            // Server save failure is non-fatal; sessionStorage draft is the fallback
        }
        finally
        {
            _serverSaveInProgress = false;
            _autoSaving = false;
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async Task RestoreDraftAsync()
    {
        try
        {
            var json = await JS.InvokeAsync<string?>("sessionStorage.getItem", DraftStorageKey);
            if (string.IsNullOrEmpty(json))
                return;

            var draft = JsonSerializer.Deserialize<DraftProjectState>(json);
            if (draft == null)
                return;

            _tempProjectId = draft.TempProjectId;
            _serverProjectId = draft.ServerProjectId;
            _title = draft.Title;
            _description = draft.Description;
            _selectedLeadTime = _leadTimeOptions.FirstOrDefault(lt => lt.Code == draft.SelectedLeadTimeCode)
                                ?? _leadTimeOptions.FirstOrDefault();

            if (!string.IsNullOrEmpty(draft.SelectedCurrencyCode))
                _selectedCurrency = _currencies.FirstOrDefault(c => c.Code == draft.SelectedCurrencyCode) ?? _selectedCurrency;

            if (draft.CustomerId.HasValue)
            {
                _selectedCustomer = new CustomerSummaryDto
                {
                    Id = draft.CustomerId.Value,
                    Name = draft.CustomerName ?? string.Empty,
                    CompanyName = draft.CustomerCompanyName,
                    Email = draft.CustomerEmail ?? string.Empty,
                    Mobile = draft.CustomerMobile,
                    Landline = draft.CustomerLandline,
                    CompanyPhone = draft.CustomerCompanyPhone,
                };
                // Bug 2 fix: hide the search box so the customer card is shown immediately.
                _showCustomerSearch = false;
            }

            _parts.Clear();
            foreach (var partState in draft.Parts)
            {
                var partVm = PartViewModel.FromDraftPartState(partState);
                if (partVm.ProcessId.HasValue && !string.IsNullOrEmpty(partVm.ProcessCode))
                    _ = ReloadPartCatalogAsync(partVm);

                // Catch-up fetch for any part with a storage path — refreshes thumbnail signed URLs,
                // restores DFM results from BFF cache, and repopulates GlbStoragePath.
                if (!string.IsNullOrEmpty(partVm.StoragePath))
                {
                    var p = partVm; var path = partVm.StoragePath!;
                    _ = Task.Run(async () => { await Task.Delay(CatchUpDelayMs); await FetchCurrentStatusAsync(p, path); });
                }

                _parts.Add(partVm);
            }

            _selectedPartIndex = 0;
            _lastSavedAt = draft.LastModified;
            Logger.LogDebug("Draft restored from session storage for session {SessionId}", _sessionId);
        }
        catch (Exception)
        {
            // Draft restore failures are non-fatal
        }
    }

    private async Task ResumeFromServerAsync(Guid projectId)
    {
        try
        {
            var response = await Http.GetAsync($"api/projects/{projectId}");
            if (!response.IsSuccessStatusCode) return;

            var project = await response.Content.ReadFromJsonAsync<ProjectDetailDto>();
            if (project == null) return;
            if (project.Status != "Draft" && project.Status != "Configuring") return;

            _serverProjectId = project.Id;
            _tempProjectId = project.Id;
            _title = project.Title;
            _description = project.Description;

            if (!string.IsNullOrEmpty(project.Currency))
            {
                _selectedCurrency = _currencies.FirstOrDefault(c => c.Code == project.Currency) ?? _selectedCurrency;
            }

            _selectedCustomer = new CustomerSummaryDto
            {
                Id = project.CustomerId,
                Name = project.CustomerName,
            };
            _showCustomerSearch = false;

            foreach (var part in project.Parts)
            {
                var existing = _parts.FirstOrDefault(p => p.ServerPartId == part.Id || p.FileId == part.FileId);
                if (existing == null && !string.IsNullOrEmpty(part.FileName))
                {
                    var partVm = new PartViewModel
                    {
                        FileId = part.FileId,
                        ServerPartId = part.Id,
                        Name = part.FileName,
                        ProcessCode = part.ProcessType,
                        MaterialId = part.MaterialId,
                        MaterialCode = part.MaterialName,
                        Quantity = part.Quantity,
                        FinishCode = part.Finish,
                        ToleranceCode = part.Tolerance,
                        EstimatedUnitPrice = part.ConfirmedPrice ?? part.EstimatedPrice,
                    };

                    if (!string.IsNullOrEmpty(part.ProcessType))
                    {
                        var process = _processes.FirstOrDefault(p =>
                            p.Code.Equals(part.ProcessType, StringComparison.OrdinalIgnoreCase));
                        if (process != null)
                        {
                            partVm.ProcessId = process.Id;
                            partVm.ProcessCode = process.Code;
                            _ = ReloadPartCatalogAsync(partVm);
                        }
                    }

                    if (!string.IsNullOrEmpty(part.ModelPreviewUrl))
                    {
                        partVm.ThumbnailSmallUrl = part.ModelPreviewUrl;
                    }

                    _parts.Add(partVm);
                }
            }

            _selectedPartIndex = 0;
            await SaveDraftAsync();
            Snackbar.Add("Draft restored from server.", Severity.Info);
        }
        catch (Exception)
        {
            // Server resume failures are non-fatal; sessionStorage draft is the fallback
        }
    }

    private async Task ReloadPartCatalogAsync(PartViewModel part)
    {
        if (!part.ProcessId.HasValue || string.IsNullOrEmpty(part.ProcessCode))
            return;

        try
        {
            var materialsTask = Http.GetFromJsonAsync<List<CatalogMaterialDto>>(
                $"api/catalog/processes/{Uri.EscapeDataString(part.ProcessCode)}/materials");
            var finishesTask = Http.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>(
                $"api/catalog/processes/{Uri.EscapeDataString(part.ProcessCode)}/finishes");
            var tolerancesTask = Http.GetFromJsonAsync<List<CatalogToleranceDto>>(
                $"api/catalog/processes/{Uri.EscapeDataString(part.ProcessCode)}/tolerances");

            await Task.WhenAll(materialsTask, finishesTask, tolerancesTask);

            part.AvailableMaterials = materialsTask.Result ?? [];
            part.AvailableFinishes = finishesTask.Result ?? [];
            part.AvailableTolerances = tolerancesTask.Result ?? [];

            if (part.MaterialId.HasValue)
                await ComputePriceAsync(part, CancellationToken.None);
        }
        catch
        {
            // Catalog reload failures are non-fatal
        }
    }

    // ── Task 15: Create project + quotation ───────────────────────────

    /// <inheritdoc />
    private async Task CreateProjectAndQuoteAsync()
    {
        if (!CanSubmit)
            return;

        _saving = true;
        try
        {
            Guid projectId;

            if (_serverProjectId.HasValue)
            {
                projectId = _serverProjectId.Value;

                var updatePayload = new { Title = _title, Description = _description };
                var updateResponse = await Http.PutAsJsonAsync($"api/projects/{projectId}", updatePayload);
                if (!updateResponse.IsSuccessStatusCode)
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    Snackbar.Add($"Failed to update project: {errorContent}", Severity.Error);
                    return;
                }

                foreach (var part in _parts.Where(p => p.IsFullyConfigured && !string.IsNullOrEmpty(p.StoragePath) && !p.ServerPartId.HasValue))
                {
                    var addPartRequest = new AddProjectPartRequest
                    {
                        FileId = part.FileId,
                        FileName = part.Name,
                        ProcessType = part.ProcessCode,
                        MaterialId = part.MaterialId,
                        Quantity = part.Quantity,
                        Finish = part.FinishCode,
                        Tolerance = part.ToleranceCode,
                    };

                    try
                    {
                        var partResponse = await Http.PostAsJsonAsync($"api/projects/{projectId}/parts", addPartRequest);
                        if (partResponse.IsSuccessStatusCode)
                        {
                            var createdPart = await partResponse.Content.ReadFromJsonAsync<ProjectPartDto>();
                            if (createdPart != null)
                                part.ServerPartId = createdPart.Id;
                        }
                    }
                    catch { /* parts may already exist from auto-save; non-fatal */ }
                }
            }
            else
            {
                var createRequest = new CreateProjectRequest
                {
                    CustomerId = _selectedCustomer!.Id,
                    CustomerName = _selectedCustomer.Name,
                    Title = _title,
                    Description = _description,
                    Currency = _selectedCurrency?.Code ?? "THB",
                };

                using var projectResponse = await Http.PostAsJsonAsync("api/projects", createRequest);
                if (!projectResponse.IsSuccessStatusCode)
                {
                    var errorContent = await projectResponse.Content.ReadAsStringAsync();
                    Snackbar.Add($"Failed to create project: {errorContent}", Severity.Error);
                    return;
                }

                var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDetailDto>();
                if (project == null)
                {
                    Snackbar.Add("Project created but response was invalid.", Severity.Error);
                    return;
                }

                projectId = project.Id;

                foreach (var part in _parts.Where(p => p.IsFullyConfigured))
                {
                    var addPartRequest = new AddProjectPartRequest
                    {
                        FileId = part.FileId,
                        FileName = part.Name,
                        ProcessType = part.ProcessCode,
                        MaterialId = part.MaterialId,
                        Quantity = part.Quantity,
                        Finish = part.FinishCode,
                        Tolerance = part.ToleranceCode,
                    };

                    using var partResponse = await Http.PostAsJsonAsync($"api/projects/{projectId}/parts", addPartRequest);
                    if (partResponse.IsSuccessStatusCode)
                    {
                        var createdPart = await partResponse.Content.ReadFromJsonAsync<ProjectPartDto>();
                        if (createdPart != null)
                            part.ServerPartId = createdPart.Id;
                    }
                    else
                    {
                        Snackbar.Add($"Failed to add part '{part.Name}'.", Severity.Warning);
                    }
                }
            }

            using var quoteResponse = await Http.PostAsync($"api/projects/{projectId}/generate-quotation", null);
            if (!quoteResponse.IsSuccessStatusCode)
            {
                Snackbar.Add("Project created but quotation generation failed.", Severity.Warning);
                Navigation.NavigateTo($"/sales/projects/{projectId}");
                return;
            }

            // Lock pricing before clearing the draft so no in-flight debounced calls reprice
            _projectLocked = true;

            await JS.InvokeVoidAsync("sessionStorage.removeItem", DraftStorageKey);

            Snackbar.Add("Project and quotation created successfully!", Severity.Success);
            Navigation.NavigateTo($"/sales/projects/{projectId}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    // ── Task 15: Duplicate project ─────────────────────────────────────

    /// <inheritdoc />
    private void DuplicateProject()
    {
        _tempProjectId = Guid.NewGuid();
        _serverProjectId = null;
        _title = string.IsNullOrEmpty(_title) ? string.Empty : $"{_title} (Copy)";
        _selectedCustomer = null;
        _showCustomerSearch = true;
        _lastSavedAt = null;

        var clonedParts = _parts.Select(p => new PartViewModel
        {
            Name = p.Name,
            FileId = Guid.NewGuid(),
            StoragePath = null,
            Quantity = p.Quantity,
            ProcessId = p.ProcessId,
            ProcessCode = p.ProcessCode,
            MaterialId = p.MaterialId,
            MaterialCode = p.MaterialCode,
            FinishId = p.FinishId,
            FinishCode = p.FinishCode,
            ToleranceId = p.ToleranceId,
            ToleranceCode = p.ToleranceCode,
            PartNotes = p.PartNotes,
            DfmAcknowledged = p.DfmAcknowledged,
            BagAndTag = true,
            AvailableMaterials = p.AvailableMaterials,
            AvailableFinishes = p.AvailableFinishes,
            AvailableTolerances = p.AvailableTolerances,
        }).ToList();

        _parts.Clear();
        _parts.AddRange(clonedParts);
        _selectedPartIndex = 0;

        Snackbar.Add("Project duplicated. Please re-upload files for each part.", Severity.Info);
        TriggerAutoSave();
    }

    // ── Helpers / existing stubs ──────────────────────────────────────

    /// <inheritdoc />
    private Task OpenFilePicker() => _fileUpload?.OpenFilePickerAsync() ?? Task.CompletedTask;

    private bool Is3DFile(string name) =>
        FileTypes.Is3DFile(Path.GetExtension(name));

    private string GetFileIcon(string name) =>
        FileTypes.GetIcon(Path.GetExtension(name));

    private string? ResolvePreviewUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        return url.StartsWith("/") ? $"{baseUri}{url}" : $"{baseUri}/{url}";
    }

    private void ValidateTitle()
    {
        _titleHasError = string.IsNullOrWhiteSpace(_title) || _title.Length > 500;
    }

    private void ValidateDescription()
    {
        if (_description?.Length > 2000) { _descriptionHasError = true; }
        else { _descriptionHasError = false; }
    }

    private async Task OnCustomerSelected(CustomerSummaryDto? customer)
    {
        var previousCustomerId = _selectedCustomerId;
        _selectedCustomer = customer;
        if (customer != null) _showCustomerSearch = false;

        if (previousCustomerId.HasValue || !_selectedCustomerId.HasValue)
            return;

        var partsInTemp = _parts.Where(p => !string.IsNullOrEmpty(p.StoragePath) && p.StoragePath.StartsWith("projects/")).ToList();
        if (partsInTemp.Count == 0)
            return;

        var migrationResult = await Http.PostAsJsonAsync(
            $"api/uploads/migrate-project?projectId={_tempProjectId}&customerId={_selectedCustomerId}",
            (object?)null);

        if (!migrationResult.IsSuccessStatusCode)
        {
            Snackbar.Add("Failed to migrate files to customer storage. Please try again.", Severity.Error);
            return;
        }

        var migrated = await migrationResult.Content.ReadFromJsonAsync<JsonDocument>();
        if (migrated == null)
        {
            Snackbar.Add("Migration failed.", Severity.Error);
            return;
        }

        var root = migrated.RootElement;

        if (!root.TryGetProperty("errors", out var errorsElement) && 
            !root.TryGetProperty("Errors", out errorsElement))
        {
            Snackbar.Add("Migration response is invalid: missing errors property.", Severity.Error);
            return;
        }

        var totalMigrated = 0;
        if (root.TryGetProperty("total_migrated", out var totalMigratedElement) || 
            root.TryGetProperty("TotalMigrated", out totalMigratedElement))
        {
            totalMigrated = totalMigratedElement.GetInt32();
        }

        if (!root.TryGetProperty("migrated_files", out var migratedFilesElement) &&
            !root.TryGetProperty("MigratedFiles", out migratedFilesElement) &&
            !root.TryGetProperty("migratedFiles", out migratedFilesElement))
        {
            Snackbar.Add("Migration response is invalid: missing migrated_files property.", Severity.Error);
            return;
        }

        var successfullyMigratedParts = new List<PartViewModel>();

        foreach (var entry in migratedFilesElement.EnumerateArray())
        {
            if (!entry.TryGetProperty("file_id", out var fileIdElement) &&
                !entry.TryGetProperty("FileId", out fileIdElement))
                continue;
            if (!entry.TryGetProperty("new_path", out var newPathElement) &&
                !entry.TryGetProperty("NewPath", out newPathElement))
                continue;
            if (!entry.TryGetProperty("old_path", out var oldPathElement) &&
                !entry.TryGetProperty("OldPath", out oldPathElement))
                continue;

            var fileId = fileIdElement.GetString();
            var newBasePath = newPathElement.GetString();
            var oldBasePath = oldPathElement.GetString();

            if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(newBasePath) || string.IsNullOrEmpty(oldBasePath))
                continue;

            var part = _parts.FirstOrDefault(p => p.FileId.ToString() == fileId);
            if (part == null) continue;

            part.StoragePath = newBasePath;

            if (!string.IsNullOrEmpty(part.GlbStoragePath))
            {
                var oldGlbViewerPath = oldBasePath + "_viewer.glb";
                if (part.GlbStoragePath.StartsWith(oldGlbViewerPath))
                    part.GlbStoragePath = newBasePath + "_viewer.glb";
                else if (part.GlbStoragePath.StartsWith(oldBasePath))
                    part.GlbStoragePath = newBasePath;
            }

            if (!string.IsNullOrEmpty(part.ThumbnailSmallGcsPath) && part.ThumbnailSmallGcsPath.StartsWith(oldBasePath))
                part.ThumbnailSmallGcsPath = part.ThumbnailSmallGcsPath.Replace(oldBasePath, newBasePath);

            if (!string.IsNullOrEmpty(part.ThumbnailLargeGcsPath) && part.ThumbnailLargeGcsPath.StartsWith(oldBasePath))
                part.ThumbnailLargeGcsPath = part.ThumbnailLargeGcsPath.Replace(oldBasePath, newBasePath);

            part.GlbSignedUrl = null;
            successfullyMigratedParts.Add(part);
        }

        await InvokeAsync(StateHasChanged);

        foreach (var part in successfullyMigratedParts.Where(p => p.ProcessId.HasValue && p.MaterialId.HasValue))
            TriggerPricingAsync(part);

        if (errorsElement.GetArrayLength() > 0)
        {
            var errorMessages = errorsElement.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            var summary = $"Migration completed with {errorsElement.GetArrayLength()} error(s). {successfullyMigratedParts.Count} file(s) migrated successfully.";
            if (errorMessages.Count > 0 && errorMessages.Count <= 3)
            {
                summary += " Errors: " + string.Join("; ", errorMessages);
            }

            Snackbar.Add(summary, Severity.Warning);
        }
    }

    private async Task OpenBabylonViewer(PartViewModel part)
    {
        if (string.IsNullOrEmpty(part.GlbStoragePath)) return;

        var resp = await Http.GetAsync($"api/uploads/viewer-url?storagePath={Uri.EscapeDataString(part.GlbStoragePath)}");
        if (!resp.IsSuccessStatusCode)
        {
            Snackbar.Add("Failed to load 3D viewer URL.", Severity.Error);
            return;
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        var url = json?.RootElement.GetProperty("url").GetString();

        if (string.IsNullOrEmpty(url))
        {
            Snackbar.Add("3D preview not available yet for this file.", Severity.Warning);
            return;
        }

        part.ViewerUrl = url;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// JS Interop callback for upload progress. Created per-file and kept alive via DotNetObjectReference
    /// so that window.uploadWithProgress can invoke OnUploadProgress on it.
    /// </summary>
    private sealed class UploadProgressCallback
    {
        private readonly PartViewModel _part;
        private readonly Action _stateHasChanged;

        public UploadProgressCallback(PartViewModel part, Action stateHasChanged)
        {
            _part = part;
            _stateHasChanged = stateHasChanged;
        }

        /// <summary>Called by JS Interop as the upload progresses.</summary>
        [JSInvokable]
        public void OnUploadProgress(int percent)
        {
            _part.ProgressPercent = percent;
            _stateHasChanged();
        }
    }
}

