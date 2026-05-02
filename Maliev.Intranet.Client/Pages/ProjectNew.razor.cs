using System.Collections.Concurrent;
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
    /// Holds live DotNetObjectReference instances so JS Interop can invoke
    /// OnUploadProgress and update the matching part's progress bar.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DotNetObjectReference<UploadProgressCallback>> _uploadCallbacks = [];

    // ── Project-level state ───────────────────────────────────────────
    private Guid _tempProjectId = Guid.NewGuid();  // non-readonly; reassigned on Duplicate
    private string _title = $"Project {DateTime.Today:yyyy-MM-dd}";
    private CustomerSummaryDto? _selectedCustomer;
    private List<LeadTimeOptionDto> _leadTimeCatalogOptions = [];
    private List<LeadTimeOptionDto> _leadTimeOptions = [];
    private LeadTimeOptionDto? _selectedLeadTime;
    private List<ProcessDto> _processes = [];
    private bool _saving;
    private bool _autoSaving;
    private DateTimeOffset? _lastSavedAt;

    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
    private const string ProjectUploadContainerId = "project-new-file-upload";

    // ── Validation ────────────────────────────────────────────────────
    private bool _titleHasError;

    // ── Parts ─────────────────────────────────────────────────────────
    private readonly List<PartViewModel> _parts = [];
    private int _selectedPartIndex;
    private bool _partsDrawerOpen;

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
    private const string ReferenceDataStateKey = "ProjectNew.ReferenceData";

    // ── Session ────────────────────────────────────────────────────────
    private Guid _sessionId;
    private PersistingComponentStateSubscription? _referenceDataSubscription;

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

    [Inject] private CurrencyService CurrencyService { get; set; } = null!;
    [Inject] private FileTypesSettings FileTypes { get; set; } = null!;
    [Inject] private UploadSettings UploadSettings { get; set; } = null!;
    [Inject] private CookieProvider CookieProvider { get; set; } = null!;
    [Inject] private ILogger<ProjectNew> Logger { get; set; } = null!;
    [Inject] private PersistentComponentState ComponentState { get; set; } = null!;

    private bool CanSubmit =>
        !_saving &&
        _selectedCustomer != null &&
        !string.IsNullOrWhiteSpace(_title) &&
        !_titleHasError &&
        _selectedLeadTime != null &&
        _parts.Count > 0 &&
        !_parts.Any(p => p.QueuedUpload || p.Uploading || p.PricingLoading) &&
        _parts.All(p => p.IsFullyConfigured && !p.PricingFailed) &&
        _parts.All(p => p.IsManifold != false || p.DfmAcknowledged);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _pricingTokens.Values) { await cts.CancelAsync(); cts.Dispose(); }
        foreach (var cts in _statusPollCts.Values) { cts.Cancel(); cts.Dispose(); }
        _statusPollCts.Clear();
        foreach (var callbackRef in _uploadCallbacks.Values) { callbackRef.Dispose(); }
        _uploadCallbacks.Clear();
        _referenceDataSubscription?.Dispose();
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

        _referenceDataSubscription ??= ComponentState.RegisterOnPersisting(PersistReferenceDataAsync);

        // ── Load reference data ────────────────────────────────────────
        var currenciesTask = CurrencyService.InitializeAsync();
        if (ComponentState.TryTakeFromJson<ProjectNewReferenceDataState>(ReferenceDataStateKey, out var cachedReferenceData) &&
            cachedReferenceData is { Processes.Count: > 0, LeadTimes.Count: > 0 })
        {
            await currenciesTask;
            _processes = cachedReferenceData.Processes.Where(p => p.Code is not "CNC").ToList();
            _leadTimeCatalogOptions = cachedReferenceData.LeadTimes
                .Where(lt => !lt.Code.Equals("RUSH", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            var processesTask = Http.GetFromJsonAsync<List<ProcessDto>>("api/v1/catalog/processes");
            var leadTimesTask = Http.GetFromJsonAsync<List<LeadTimeOptionDto>>("api/v1/pricing/lead-times");

            await Task.WhenAll(
                currenciesTask,
                processesTask.ContinueWith(_ => { }),
                leadTimesTask.ContinueWith(_ => { }));

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
                    _leadTimeCatalogOptions = result
                        .Where(lt => !lt.Code.Equals("RUSH", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }
            catch (Exception ex) { Snackbar.Add($"Failed to load lead times: {ex.Message}", Severity.Warning); }
        }

        await RestoreDraftAsync();
        RefreshLeadTimeOptionsFromPricing();

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
            var parts = _parts.Where(p => p.StoragePath == payload.StoragePath).ToList();
            if (parts.Count == 0) return;

            if (payload.Failed)
            {
                foreach (var p in parts)
                {
                    p.AwaitingPreview = false;
                    p.AnalysisErrorCode = payload.ErrorCode;
                    p.DfmAnalysisTimedOut = true;
                    p.StatusText = DfmStatusMessages.GetStatusText(payload.ErrorCode);
                }
                StopStatusWatchdog(payload.StoragePath);
                TriggerAutoSave();
                await InvokeAsync(StateHasChanged);
                return;
            }

            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(payload.ThumbnailUrl) && string.IsNullOrEmpty(part.ThumbnailSmallUrl))
                    part.ThumbnailSmallUrl = payload.ThumbnailUrl;

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
                }

                if (payload.NonManifoldReason != null || payload.Dimensions != null || payload.BodyCount.HasValue)
                {
                    part.IsManifold = payload.NonManifoldReason == null;
                    part.NonManifoldReason = payload.NonManifoldReason;
                    part.NonManifoldFaceCount = payload.NonManifoldFaceCount;
                }

                if (payload.BodyCount.HasValue)
                {
                    part.BodyCount = payload.BodyCount.Value;
                    if (payload.Bodies != null)
                        part.Bodies = payload.Bodies.Select(b => new PartViewModel.BodyInfo(
                            b.Index,
                            b.Name,
                            b.VolumeCm3,
                            new[] { b.BboxMin.X, b.BboxMin.Y, b.BboxMin.Z },
                            new[] { b.BboxMax.X, b.BboxMax.Y, b.BboxMax.Z },
                            null
                        )).ToList();
                }

                if (payload.PreviewUrls != null)
                {
                    if (!string.IsNullOrEmpty(payload.PreviewUrls.ThumbnailSmall))
                        part.ThumbnailSmallUrl = payload.PreviewUrls.ThumbnailSmall;
                    if (!string.IsNullOrEmpty(payload.PreviewUrls.ThumbnailLarge))
                        part.ThumbnailLargeUrl = payload.PreviewUrls.ThumbnailLarge;
                    else if (!string.IsNullOrEmpty(payload.HiResThumbnailUrl))
                        part.ThumbnailLargeUrl = payload.HiResThumbnailUrl;
                    part.ThumbnailSmallGcsPath = payload.PreviewUrls.ThumbnailSmallGcsPath;
                    part.ThumbnailLargeGcsPath = payload.PreviewUrls.ThumbnailLargeGcsPath;
                    part.AwaitingPreview = false;
                    part.StatusText = "Ready";
                }
            }
            TriggerAutoSave();
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<SignalRGlbReadyPayload>("GlbReady", async payload =>
        {
            var parts = _parts.Where(p => p.StoragePath == payload.StoragePath).ToList();
            if (parts.Count == 0) return;

            if (!payload.Failed)
            {
                foreach (var part in parts)
                {
                    part.GlbSignedUrl = payload.GlbUrl;
                    part.GlbStoragePath ??= payload.StoragePath;
                    part.ViewerUrl ??= payload.GlbUrl;

                    if (payload.BodyCount.HasValue)
                    {
                        part.BodyCount = payload.BodyCount.Value;
                        if (payload.Bodies != null)
                            part.Bodies = payload.Bodies.Select(b => new PartViewModel.BodyInfo(
                                b.Index,
                                b.Name,
                                b.VolumeCm3,
                                new[] { b.BboxMin.X, b.BboxMin.Y, b.BboxMin.Z },
                                new[] { b.BboxMax.X, b.BboxMax.Y, b.BboxMax.Z },
                                null
                            )).ToList();
                    }
                }
                TriggerAutoSave();
            }
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<SignalRDfmAnalysisPayload>("DfmAnalysisReady", async payload =>
        {
            var parts = _parts.Where(p => p.StoragePath == payload.StoragePath).ToList();
            if (parts.Count == 0) return;

            foreach (var part in parts)
            {
                // Only overwrite when the incoming event actually carries data.
                // Per-process events (SLS, MJF, SLA_DLP, …) set only one of the three
                // report fields and leave the other two null. Without this guard those
                // null fields would wipe reports set by earlier process events.
                if (payload.FdmReport != null) part.FdmDfmReport = payload.FdmReport;
                if (payload.SlaReport != null) part.SlaDfmReport = payload.SlaReport;
                if (payload.CncReport != null) part.CncDfmReport = payload.CncReport;
                if (payload.OverlayUrls != null) part.OverlayUrls = payload.OverlayUrls;
                if (payload.OverlayPaths != null) part.OverlayPaths = payload.OverlayPaths;
                // Stamp body count unconditionally so single-body files also resolve Pending state.
                if (payload.BodyCount.HasValue)
                    part.BodyCount = payload.BodyCount.Value;
                // Stamp mesh-integrity info from DFM event if not already set (cache-miss recovery path).
                if (payload.NonManifoldReason != null && part.NonManifoldReason == null)
                {
                    part.IsManifold = false;
                    part.NonManifoldReason = payload.NonManifoldReason;
                    part.NonManifoldFaceCount = payload.NonManifoldFaceCount;
                }
                else if (payload.NonManifoldReason == null && payload.BodyCount.HasValue && part.IsManifold == null)
                {
                    // DFM event arrived with body count but no manifold issue — mark as manifold.
                    part.IsManifold = true;
                }
                part.ResolveDfmReport();
            }
            StopStatusWatchdog(payload.StoragePath);

            await InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();

        foreach (var part in _parts.Where(p => !string.IsNullOrEmpty(p.StoragePath)))
            await _hubConnection.InvokeAsync("JoinFileGroup", part.StoragePath);
    }

    private Task PersistReferenceDataAsync()
    {
        if (_processes.Count > 0 && _leadTimeCatalogOptions.Count > 0)
        {
            ComponentState.PersistAsJson(
                ReferenceDataStateKey,
                new ProjectNewReferenceDataState(_processes, _leadTimeCatalogOptions));
        }

        return Task.CompletedTask;
    }

    private sealed record ProjectNewReferenceDataState(
        List<ProcessDto> Processes,
        List<LeadTimeOptionDto> LeadTimes);

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
                $"api/v1/customers?query={Uri.EscapeDataString(value)}&page=1&pageSize=10",
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

    // ── Task 10: File upload / polling ─────────────────────────────────

    /// <inheritdoc />
    private async Task HandleFileSelected(IReadOnlyList<IBrowserFile> files)
    {
        var validFiles = new List<ProjectUploadItem>();
        var fileIndex = 0;

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.Name);
            if (!FileTypes.ThreeDExtensions.Contains(ext))
            {
                Snackbar.Add($"File type '{ext}' is not allowed.", Severity.Warning);
                fileIndex++;
                continue;
            }

            if (file.Size > UploadSettings.ThreeDModelLimit)
            {
                var sizeMB = file.Size / (1024.0 * 1024.0);
                Snackbar.Add(
                    $"File '{file.Name}' ({sizeMB:F1} MB) exceeds the {UploadSettings.ThreeDModelLimit / (1024.0 * 1024.0):F0} MB limit.",
                    Severity.Error
                );
                fileIndex++;
                continue;
            }

            var clientUploadId = Guid.NewGuid().ToString("N");
            var part = new PartViewModel
            {
                Name = file.Name,
                QueuedUpload = true,
                Uploading = false,
                AwaitingPreview = false,
                StatusText = "Queued...",
                FileSizeBytes = file.Size,
                UploadedAt = DateTimeOffset.UtcNow,
                ClientUploadId = clientUploadId,
            };

            _parts.Add(part);
            _selectedPartIndex = _parts.Count - 1;
            validFiles.Add(new ProjectUploadItem(file, part, fileIndex, clientUploadId));
            fileIndex++;
        }

        if (validFiles.Count > 0)
        {
            try
            {
                await CaptureBrowserFilesAsync(validFiles);
            }
            catch (JSException ex)
            {
                Logger.LogError(ex, "Failed to capture selected browser files for direct upload.");
                foreach (var item in validFiles)
                    await MarkUploadFailedAsync(item.Part, "Browser file capture failed.");
                return;
            }

            _ = UploadFilesConcurrentlyAsync(validFiles);
        }
    }

    private async Task CaptureBrowserFilesAsync(IReadOnlyList<ProjectUploadItem> items)
    {
        var mappings = items.Select(item => new
        {
            clientUploadId = item.ClientUploadId,
            index = item.InputIndex
        });

        await JS.InvokeVoidAsync("window.projectNewUploads.captureFiles", ProjectUploadContainerId, mappings);
    }

    private async Task UploadFilesConcurrentlyAsync(IReadOnlyList<ProjectUploadItem> items)
    {
        var maxConcurrency = Math.Max(1, UploadSettings.MaxConcurrentUploads);
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        await Task.WhenAll(items.Select(item => UploadSingleProjectFileAsync(item, semaphore)));
    }

    private async Task UploadSingleProjectFileAsync(ProjectUploadItem item, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();

        var part = item.Part;
        var callbackId = Guid.NewGuid();
        DotNetObjectReference<UploadProgressCallback>? callbackRef = null;

        try
        {
            part.QueuedUpload = false;
            part.Uploading = true;
            part.AwaitingPreview = false;
            part.ProgressPercent = 0;
            part.StatusText = "Uploading...";
            part.Error = null;
            await InvokeAsync(StateHasChanged);

            var browserContentType = string.IsNullOrWhiteSpace(item.File.ContentType)
                ? null
                : item.File.ContentType;

            var initiateRequest = new BffInitiateResumableUploadRequest
            {
                FileName = item.File.Name,
                ContentType = browserContentType,
                FileSize = item.File.Size,
                ProjectId = _tempProjectId,
                CustomerId = _selectedCustomerId
            };

            var initiateResponse = await Http.PostAsJsonAsync("api/v1/uploads/resumable", initiateRequest);
            if (!initiateResponse.IsSuccessStatusCode)
            {
                await MarkUploadFailedAsync(part, $"Upload initiation failed ({(int)initiateResponse.StatusCode}).");
                return;
            }

            var session = await initiateResponse.Content.ReadFromJsonAsync<BffResumableUploadSessionResponse>();
            if (session == null || string.IsNullOrWhiteSpace(session.UploadId) || string.IsNullOrWhiteSpace(session.SessionUri))
            {
                await MarkUploadFailedAsync(part, "Upload initiation response was invalid.");
                return;
            }

            var callback = new UploadProgressCallback(part, () => InvokeAsync(StateHasChanged));
            callbackRef = DotNetObjectReference.Create(callback);
            _uploadCallbacks[callbackId] = callbackRef;

            var uploadResult = await JS.InvokeAsync<UploadResult>(
                "window.projectNewUploads.uploadFile",
                item.ClientUploadId,
                session.SessionUri,
                $"api/v1/uploads/resumable/{Uri.EscapeDataString(session.UploadId)}",
                browserContentType ?? "application/octet-stream",
                item.File.Size,
                callbackRef);

            if (uploadResult == null || uploadResult.Status < 200 || uploadResult.Status >= 300)
            {
                var status = uploadResult?.Status ?? 0;
                await MarkUploadFailedAsync(part, $"Upload failed ({status}).");
                return;
            }

            var completeResponse = await Http.PostAsJsonAsync(
                $"api/v1/uploads/resumable/{Uri.EscapeDataString(session.UploadId)}/complete",
                new { });

            if (!completeResponse.IsSuccessStatusCode)
            {
                await MarkUploadFailedAsync(part, $"Upload completion failed ({(int)completeResponse.StatusCode}).");
                return;
            }

            var completedUpload = await completeResponse.Content.ReadFromJsonAsync<BffUploadResponse>();
            if (completedUpload == null || string.IsNullOrWhiteSpace(completedUpload.StoragePath))
            {
                await MarkUploadFailedAsync(part, "Upload completion response was invalid.");
                return;
            }

            part.StoragePath = completedUpload.StoragePath;
            part.FileId = Guid.TryParse(completedUpload.UploadId, out var fileId) ? fileId : Guid.NewGuid();
            part.QueuedUpload = false;
            part.Uploading = false;
            part.ProgressPercent = 0;
            part.AwaitingPreview = true;
            part.StatusText = "Processing geometry...";

            if (_hubConnection?.State == HubConnectionState.Connected)
                await _hubConnection.InvokeAsync("JoinFileGroup", completedUpload.StoragePath);

            var pollCts = new CancellationTokenSource();
            _statusPollCts[completedUpload.StoragePath] = pollCts;
            _ = Task.Run(() => StatusWatchdogLoopAsync(part, completedUpload.StoragePath, pollCts.Token));
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Polling cancelled — expected when part is removed
        }
        catch (Exception ex)
        {
            await MarkUploadFailedAsync(part, $"Upload error: {ex.Message}");
        }
        finally
        {
            if (callbackRef != null)
            {
                _uploadCallbacks.TryRemove(callbackId, out _);
                callbackRef.Dispose();
            }

            try
            {
                await JS.InvokeVoidAsync("window.projectNewUploads.clearFile", item.ClientUploadId);
            }
            catch (JSDisconnectedException)
            {
                // Component disposal can race with background upload cleanup.
            }
            catch (JSException ex)
            {
                Logger.LogDebug(ex, "Failed to clear browser upload file reference.");
            }

            semaphore.Release();
        }
    }

    private sealed record UploadResult(int Status, string Body);

    private sealed record ProjectUploadItem(
        IBrowserFile File,
        PartViewModel Part,
        int InputIndex,
        string ClientUploadId);

    private async Task MarkUploadFailedAsync(PartViewModel part, string message)
    {
        part.QueuedUpload = false;
        part.Uploading = false;
        part.AwaitingPreview = false;
        part.ProgressPercent = 0;
        part.StatusText = "Upload failed";
        part.Error = message;
        Snackbar.Add($"{part.Name}: {message}", Severity.Error);
        await InvokeAsync(StateHasChanged);
    }

    // Maximum wall-clock time to wait for DFM analysis before declaring a client-side
    // timeout. Chosen to exceed the server's Phase 1 (300 s) + Phase 2 (360 s) hard
    // limits with headroom for network latency and queue time.
    private const int DfmHardTimeoutMs = 8 * 60 * 1000; // 8 minutes

    /// <summary>
    /// Polls <c>analysis-status</c> on a 30 s interval until a terminal state is reached or the
    /// token is cancelled. Handles the case where a SignalR event is missed due to reconnect timing.
    /// The first fetch is delayed by <see cref="CatchUpDelayMs"/> (5 s) to let SignalR deliver first.
    /// After <see cref="DfmHardTimeoutMs"/> the watchdog declares a client-side timeout so the
    /// DFM overlay never spins indefinitely regardless of server state.
    /// </summary>
    private async Task StatusWatchdogLoopAsync(PartViewModel part, string storagePath, CancellationToken ct)
    {
        try
        {
            var started = DateTime.UtcNow;
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

                // Client-side hard timeout: declare failure if the server never sends
                // a terminal event, so the DFM overlay is never stuck forever.
                if ((DateTime.UtcNow - started).TotalMilliseconds >= DfmHardTimeoutMs)
                {
                    part.DfmAnalysisTimedOut = true;
                    part.StatusText = DfmStatusMessages.GetStatusText("CLIENT_TIMEOUT");
                    await InvokeAsync(StateHasChanged);
                    break;
                }

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
                $"api/v1/uploads/analysis-status?storagePath={Uri.EscapeDataString(storagePath)}");

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
            part.NonManifoldReason = status.NonManifoldReason;
            part.NonManifoldFaceCount = status.NonManifoldFaceCount;
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
                // Only update if not empty (preserves existing thumbnail from earlier SignalR event)
                if (!string.IsNullOrEmpty(status.PreviewUrls.ThumbnailSmall))
                    part.ThumbnailSmallUrl = status.PreviewUrls.ThumbnailSmall;
                if (!string.IsNullOrEmpty(status.PreviewUrls.ThumbnailLargeUrl))
                    part.ThumbnailLargeUrl = status.PreviewUrls.ThumbnailLargeUrl;
                else if (!string.IsNullOrEmpty(status.HiResThumbnailUrl))
                    part.ThumbnailLargeUrl = status.HiResThumbnailUrl;
            }
            else if (!string.IsNullOrEmpty(status.ThumbnailUrl))
            {
                // Legacy single thumbnail - treat as small (256px)
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
                $"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(storagePath)}");
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
                    $"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(path)}");
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

    /// <summary>
    /// Refreshes the 3D viewer signed URL when the current one has expired.
    /// Called by PartDetailCard when BabylonJS viewer fails to load.
    /// </summary>
    private async Task RequestFreshViewerUrlAsync(string storagePath)
    {
        var part = _parts.FirstOrDefault(p => p.GlbStoragePath == storagePath || p.StoragePath == storagePath);
        if (part == null)
        {
            Snackbar.Add("Part not found for URL refresh.", Severity.Warning);
            return;
        }

        try
        {
            var viewerResp = await Http.GetAsync(
                $"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(storagePath)}");
            if (viewerResp.IsSuccessStatusCode)
            {
                var viewerJson = await viewerResp.Content.ReadFromJsonAsync<JsonDocument>();
                var resolvedUrl = viewerJson?.RootElement.GetProperty("url").GetString();
                if (!string.IsNullOrEmpty(resolvedUrl))
                {
                    part.ViewerUrl = resolvedUrl;
                    part.GlbSignedUrl = resolvedUrl;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }

            if (viewerResp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Snackbar.Add("Session expired. Please refresh the page and log in again.", Severity.Error);
            }
            else
            {
                Snackbar.Add("Failed to refresh 3D viewer URL. Please refresh the page.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to refresh viewer: {ex.Message}", Severity.Warning);
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
            try { await Http.DeleteAsync($"api/v1/uploads/attachments/{att.FileId}"); } catch { /* non-fatal */ }
        }

        // Delete from server if the project has been cloud-saved
        if (_serverProjectId.HasValue && !string.IsNullOrEmpty(part.StoragePath))
        {
            try { await Http.DeleteAsync($"api/v1/projects/{_serverProjectId}/parts/{part.FileId}"); } catch { /* non-fatal */ }
        }

        _parts.Remove(part);

        if (_selectedPartIndex >= _parts.Count)
            _selectedPartIndex = Math.Max(0, _parts.Count - 1);

        RefreshLeadTimeOptionsFromPricing();
        TriggerAutoSave();
    }

    // ── Task 19: Activate part from list view ─────────────────────────

    /// <summary>Selects the given part for editing.</summary>
    private void ActivatePartFromList(PartViewModel part)
    {
        _selectedPartIndex = _parts.IndexOf(part);
        if (_selectedPartIndex < 0) _selectedPartIndex = 0;
        StateHasChanged();
    }

    private void SelectPartFromPanel(int index)
    {
        _selectedPartIndex = index;
        StateHasChanged();
    }

    private void SelectPartFromDrawer(int index)
    {
        _selectedPartIndex = index;
        _partsDrawerOpen = false;
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
                    $"api/v1/catalog/processes/{Uri.EscapeDataString(processCode)}/materials");
                var finishesTask = Http.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>(
                    $"api/v1/catalog/processes/{Uri.EscapeDataString(processCode)}/finishes");
                var tolerancesTask = Http.GetFromJsonAsync<List<CatalogToleranceDto>>(
                    $"api/v1/catalog/processes/{Uri.EscapeDataString(processCode)}/tolerances");
                var configOptionsTask = Http.GetFromJsonAsync<List<ProcessConfigOptionDto>>(
                    $"api/v1/catalog/processes/{Uri.EscapeDataString(processCode)}/config-options");

                await Task.WhenAll(materialsTask, finishesTask, tolerancesTask, configOptionsTask);

                part.AvailableMaterials = materialsTask.Result ?? [];
                part.AvailableFinishes = finishesTask.Result ?? [];
                part.AvailableTolerances = FilterProcessTolerances(processCode, tolerancesTask.Result ?? []).ToList();
                part.AvailableProcessOptions = configOptionsTask.Result ?? [];

                ApplyCatalogDefaults(part);
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
            part.AvailableProcessOptions = [];
            part.EstimatedLeadTimeDays = 0;
            RefreshLeadTimeOptionsFromPricing();
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

    private void RefreshLeadTimeOptionsFromPricing()
    {
        var projectLeadTimeDays = _parts
            .Where(p => p.EstimatedLeadTimeDays > 0 && !p.PricingLoading && !p.PricingFailed)
            .Select(p => p.EstimatedLeadTimeDays)
            .DefaultIfEmpty(0)
            .Max();

        if (projectLeadTimeDays <= 0 || _leadTimeCatalogOptions.Count == 0)
        {
            _leadTimeOptions = [];
            _selectedLeadTime = null;
            return;
        }

        _leadTimeOptions = _leadTimeCatalogOptions
            .Select(option => option with
            {
                MinDays = GetBufferedLeadTimeRange(option, projectLeadTimeDays).MinDays,
                MaxDays = GetBufferedLeadTimeRange(option, projectLeadTimeDays).MaxDays
            })
            .ToList();

        if (_selectedLeadTime is not null)
        {
            _selectedLeadTime = _leadTimeOptions.FirstOrDefault(lt => lt.Code == _selectedLeadTime.Code);
        }
        else
        {
            _selectedLeadTime = _leadTimeOptions.FirstOrDefault(lt => lt.IsDefault)
                ?? _leadTimeOptions.FirstOrDefault(lt => lt.Code.Equals("STANDARD", StringComparison.OrdinalIgnoreCase))
                ?? _leadTimeOptions.FirstOrDefault();
        }
    }

    private static (int MinDays, int MaxDays) GetBufferedLeadTimeRange(
        LeadTimeOptionDto option,
        int standardLeadTimeDays)
    {
        var code = option.Code.ToUpperInvariant();
        var minDays = standardLeadTimeDays;
        var maxDays = standardLeadTimeDays + 3;

        if (code.Contains("ECONOMY", StringComparison.Ordinal))
        {
            minDays = standardLeadTimeDays + 2;
            maxDays = standardLeadTimeDays + 5;
        }
        else if (code.Contains("EXPRESS", StringComparison.Ordinal))
        {
            minDays = Math.Max(1, standardLeadTimeDays - 3);
            maxDays = Math.Max(minDays + 2, standardLeadTimeDays - 1);
        }

        return (minDays, maxDays);
    }

    private static void ApplyCatalogDefaults(PartViewModel part)
    {
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

        if (part.ToleranceId.HasValue
            && part.AvailableTolerances.All(t => t.Id != part.ToleranceId.Value))
        {
            part.ToleranceId = null;
            part.ToleranceCode = null;
        }

        if (!part.ToleranceId.HasValue)
        {
            var defaultTolerance = SelectDefaultTolerance(part.ProcessCode, part.AvailableTolerances);
            if (defaultTolerance != null)
            {
                part.ToleranceId = defaultTolerance.Id;
                part.ToleranceCode = defaultTolerance.Code;
            }
        }

        if (IsCncProcessCode(part.ProcessCode) && string.IsNullOrWhiteSpace(part.RoughnessCode))
            part.RoughnessCode = "RA_3_2";
    }

    private static CatalogToleranceDto? SelectDefaultTolerance(
        string? processCode,
        IEnumerable<CatalogToleranceDto> tolerances)
    {
        var ordered = tolerances.OrderBy(t => t.SortOrder).ToList();
        if (IsCncProcessCode(processCode))
        {
            var medium = ordered.FirstOrDefault(IsIso2768MediumTolerance);
            if (medium != null)
                return medium;
        }

        return ordered.FirstOrDefault();
    }

    private static bool IsCncProcessCode(string? processCode) =>
        string.Equals(processCode, "CNC", StringComparison.OrdinalIgnoreCase)
        || string.Equals(processCode, "CNC_MILL", StringComparison.OrdinalIgnoreCase)
        || string.Equals(processCode, "CNC_TURN", StringComparison.OrdinalIgnoreCase);

    private static bool IsIso2768MediumTolerance(CatalogToleranceDto tolerance)
    {
        var combined = $"{tolerance.Code} {tolerance.Name} {tolerance.IsoStandard} {tolerance.Grade}";
        return combined.Contains("ISO2768_M", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("ISO_2768_M", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("ISO 2768-m", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("ISO 2768 m", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tolerance.Grade, "m", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<CatalogToleranceDto> FilterProcessTolerances(
        string? processCode,
        IEnumerable<CatalogToleranceDto> tolerances)
    {
        if (!string.Equals(processCode, "FDM", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(processCode, "FDM_3D_PRINTING", StringComparison.OrdinalIgnoreCase))
        {
            return tolerances;
        }

        return tolerances.Where(t =>
        {
            var combined = $"{t.Code} {t.Name} {t.IsoStandard} {t.Grade}";
            return !combined.Contains("ISO 2768-c", StringComparison.OrdinalIgnoreCase)
                && !combined.Contains("ISO 2768_C", StringComparison.OrdinalIgnoreCase)
                && !combined.Contains("ISO2768_C", StringComparison.OrdinalIgnoreCase)
                && !combined.Contains("ISO 2768-v", StringComparison.OrdinalIgnoreCase)
                && !combined.Contains("ISO 2768_V", StringComparison.OrdinalIgnoreCase)
                && !combined.Contains("ISO2768_V", StringComparison.OrdinalIgnoreCase);
        });
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
            part.EstimatedLeadTimeDays = 0;
            RefreshLeadTimeOptionsFromPricing();
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
            var processCode = part.ProcessCode ?? process?.Code ?? string.Empty;

            var request = new PricingRequestDto
            {
                FileId = part.FileId,
                CustomerId = _selectedCustomerId ?? Guid.Empty,
                MaterialId = part.MaterialId ?? Guid.Empty,
                MaterialCode = part.MaterialCode ?? string.Empty,
                ManufacturingProcessId = part.ProcessId ?? Guid.Empty,
                ManufacturingProcessName = processCode,
                ManufacturingProcessCode = processCode,
                Quantity = part.Quantity,
                Geometry = geometry!,
                StoragePath = part.StoragePath,
                LeadTimeCode = _selectedLeadTime?.Code,
                FinishId = part.FinishId,
                ProcessOptionValues = part.ProcessOptionValues.Count > 0 ? part.ProcessOptionValues : null,
                ToleranceCode = part.ToleranceCode,
                ToleranceAdditionalCostPercent = part.AvailableTolerances
                    .FirstOrDefault(t => t.Code == part.ToleranceCode)?.AdditionalCostPercent,
            };

            var response = await Http.PostAsJsonAsync("api/v1/pricing/calculate", request, ct);
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
                part.EstimatedLeadTimeDays = 0;
            }
        }
        catch (Exception)
        {
            part.PricingFailed = true;
            part.EstimatedLeadTimeDays = 0;
        }
        finally
        {
            part.PricingLoading = false;
            RefreshLeadTimeOptionsFromPricing();
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
                $"api/v1/projects/{_tempProjectId}/parts/{partId}/routing?processType={processCode}");
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
                CustomerId = _selectedCustomer?.Id,
                CustomerName = _selectedCustomer?.Name,
                CustomerCompanyName = _selectedCustomer?.CompanyName,
                CustomerEmail = _selectedCustomer?.Email,
                CustomerMobile = _selectedCustomer?.Mobile,
                CustomerLandline = _selectedCustomer?.Landline,
                CustomerCompanyPhone = _selectedCustomer?.CompanyPhone,
                SelectedLeadTimeCode = _selectedLeadTime?.Code ?? "STANDARD",
                SelectedCurrencyCode = CurrencyService.Code,
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
                    Currency = CurrencyService.Code,
                };

                var response = await Http.PostAsJsonAsync("api/v1/projects", createRequest);
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
                                var partResponse = await Http.PostAsJsonAsync($"api/v1/projects/{_serverProjectId}/parts", addPartRequest);
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
                            CustomerId = _selectedCustomer?.Id,
                            CustomerName = _selectedCustomer?.Name,
                            CustomerCompanyName = _selectedCustomer?.CompanyName,
                            CustomerEmail = _selectedCustomer?.Email,
                            CustomerMobile = _selectedCustomer?.Mobile,
                            CustomerLandline = _selectedCustomer?.Landline,
                            CustomerCompanyPhone = _selectedCustomer?.CompanyPhone,
                            SelectedLeadTimeCode = _selectedLeadTime?.Code ?? "STANDARD",
                            SelectedCurrencyCode = CurrencyService.Code,
                            LastModified = DateTime.UtcNow,
                            Parts = _parts.Select(p => p.ToDraftPartState()).ToList(),
                        };
                        var updatedJson = JsonSerializer.Serialize(updatedDraft);
                        await JS.InvokeVoidAsync("sessionStorage.setItem", DraftStorageKey, updatedJson);
                        _lastSavedAt = DateTimeOffset.UtcNow;
                    }
                }
            }
            else
            {
                await Http.PutAsJsonAsync($"api/v1/projects/{_serverProjectId}", new { Title = _title });

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
                            var partResponse = await Http.PostAsJsonAsync($"api/v1/projects/{_serverProjectId}/parts", addPartRequest);
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
                            $"api/v1/projects/{_serverProjectId}/parts/{part.ServerPartId}",
                            partRequest);
                    }
                    catch
                    {
                        // Non-fatal: part update will retry on next auto-save
                    }
                }

                try
                {
                }
                catch
                {
                    // Non-fatal
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
            _selectedLeadTime = string.IsNullOrEmpty(draft.SelectedLeadTimeCode)
                ? null
                : _leadTimeOptions.FirstOrDefault(lt => lt.Code == draft.SelectedLeadTimeCode);

            if (!string.IsNullOrEmpty(draft.SelectedCurrencyCode))
            {
                var c = CurrencyService.Currencies.FirstOrDefault(x => x.Code == draft.SelectedCurrencyCode);
                if (c != null) await CurrencyService.SetCurrencyAsync(c);
            }

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
            var response = await Http.GetAsync($"api/v1/projects/{projectId}");
            if (!response.IsSuccessStatusCode) return;

            var project = await response.Content.ReadFromJsonAsync<ProjectDetailDto>();
            if (project == null) return;
            if (project.Status != "Draft" && project.Status != "Configuring") return;

            _serverProjectId = project.Id;
            _tempProjectId = project.Id;
            _title = project.Title;

            if (!string.IsNullOrEmpty(project.Currency))
            {
                var c = CurrencyService.Currencies.FirstOrDefault(x => x.Code == project.Currency);
                if (c != null) await CurrencyService.SetCurrencyAsync(c);
            }

            _selectedCustomer = new CustomerSummaryDto
            {
                Id = project.CustomerId,
                Name = project.CustomerName,
            };

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
                $"api/v1/catalog/processes/{Uri.EscapeDataString(part.ProcessCode)}/materials");
            var finishesTask = Http.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>(
                $"api/v1/catalog/processes/{Uri.EscapeDataString(part.ProcessCode)}/finishes");
            var tolerancesTask = Http.GetFromJsonAsync<List<CatalogToleranceDto>>(
                $"api/v1/catalog/processes/{Uri.EscapeDataString(part.ProcessCode)}/tolerances");
            var configOptionsTask = Http.GetFromJsonAsync<List<ProcessConfigOptionDto>>(
                $"api/v1/catalog/processes/{Uri.EscapeDataString(part.ProcessCode)}/config-options");

            await Task.WhenAll(materialsTask, finishesTask, tolerancesTask, configOptionsTask);

            part.AvailableMaterials = materialsTask.Result ?? [];
            part.AvailableFinishes = finishesTask.Result ?? [];
            part.AvailableTolerances = FilterProcessTolerances(part.ProcessCode, tolerancesTask.Result ?? []).ToList();
            part.AvailableProcessOptions = configOptionsTask.Result ?? [];

            if (part.MaterialId.HasValue)
                await ComputePriceAsync(part, CancellationToken.None);
        }
        catch
        {
            // Catalog reload failures are non-fatal
        }
    }

    // ── PDF generation ─────────────────────────────────────────────────

    private async Task GenerateDraftPdfAsync()
    {
        try
        {
            var customerDetail = await GetDraftPdfCustomerDetailAsync();
            var nowUtc = DateTime.UtcNow;
            var pdfData = ProjectQuotationPdfMapper.BuildDraftPdfData(
                _tempProjectId,
                _selectedCustomer,
                customerDetail,
                CurrencyService.Code,
                nowUtc,
                BuildDraftDeliveryExpectation(),
                _parts,
                _processes);

            var response = await Http.PostAsJsonAsync("api/v1/quotations/draft-pdf", pdfData);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (result.TryGetProperty("storageUrl", out var urlProp))
                {
                    var url = urlProp.GetString();
                    if (url != null) await JS.InvokeVoidAsync("window.open", url, "_blank");
                }
            }
            else
            {
                Snackbar.Add("Failed to generate PDF.", MudBlazor.Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"PDF generation error: {ex.Message}", MudBlazor.Severity.Error);
        }
    }

    private string BuildDraftDeliveryExpectation()
    {
        var maxLeadTimeDays = _parts
            .Where(part => part.EstimatedLeadTimeDays > 0)
            .Select(part => part.EstimatedLeadTimeDays)
            .DefaultIfEmpty(0)
            .Max();

        return maxLeadTimeDays > 0
            ? $"{maxLeadTimeDays} business days after order confirmation"
            : "To be confirmed after project review";
    }

    private async Task<CustomerDetailDto?> GetDraftPdfCustomerDetailAsync()
    {
        if (_selectedCustomer == null)
            return null;

        try
        {
            return await Http.GetFromJsonAsync<CustomerDetailDto>($"api/v1/customers/{_selectedCustomer.Id}");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not load customer details for draft PDF {CustomerId}", _selectedCustomer.Id);
            return null;
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

                var updateResponse = await Http.PutAsJsonAsync($"api/v1/projects/{projectId}", new { Title = _title });
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
                        var partResponse = await Http.PostAsJsonAsync($"api/v1/projects/{projectId}/parts", addPartRequest);
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
                    Currency = CurrencyService.Code,
                };

                using var projectResponse = await Http.PostAsJsonAsync("api/v1/projects", createRequest);
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

                    using var partResponse = await Http.PostAsJsonAsync($"api/v1/projects/{projectId}/parts", addPartRequest);
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

            using var quoteResponse = await Http.PostAsync($"api/v1/projects/{projectId}/generate-quotation", null);
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
        if (_parts.Count == 0)
            return;

        _tempProjectId = Guid.NewGuid();
        _serverProjectId = null;
        _title = string.IsNullOrEmpty(_title) ? string.Empty : $"{_title} (Copy)";
        _selectedCustomer = null;
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

    /// <summary>Duplicates a single part within the project, reusing the source's analysis artifacts.</summary>
    private async Task DuplicateSinglePart(PartViewModel sourcePart)
    {
        var cloned = new PartViewModel
        {
            // Identity — reuse same physical upload
            Name = sourcePart.Name,
            FileId = sourcePart.FileId,
            StoragePath = sourcePart.StoragePath,
            FileSizeBytes = sourcePart.FileSizeBytes,
            UploadedAt = sourcePart.UploadedAt,

            // Completed analysis artifacts — copy, don't re-run
            ThumbnailSmallUrl = sourcePart.ThumbnailSmallUrl,
            ThumbnailLargeUrl = sourcePart.ThumbnailLargeUrl,
            ThumbnailSmallGcsPath = sourcePart.ThumbnailSmallGcsPath,
            ThumbnailLargeGcsPath = sourcePart.ThumbnailLargeGcsPath,
            GlbStoragePath = sourcePart.GlbStoragePath,
            GlbSignedUrl = sourcePart.GlbSignedUrl,
            ViewerUrl = sourcePart.ViewerUrl,
            Dimensions = sourcePart.Dimensions,
            VolumeMm3 = sourcePart.VolumeMm3,
            IsManifold = sourcePart.IsManifold,
            NonManifoldReason = sourcePart.NonManifoldReason,
            NonManifoldFaceCount = sourcePart.NonManifoldFaceCount,
            BodyCount = sourcePart.BodyCount,
            Bodies = sourcePart.Bodies,
            FdmDfmReport = sourcePart.FdmDfmReport,
            SlaDfmReport = sourcePart.SlaDfmReport,
            CncDfmReport = sourcePart.CncDfmReport,
            OverlayUrls = sourcePart.OverlayUrls,
            OverlayPaths = sourcePart.OverlayPaths,

            // Per-part config
            Quantity = sourcePart.Quantity,
            ProcessId = sourcePart.ProcessId,
            ProcessCode = sourcePart.ProcessCode,
            MaterialId = sourcePart.MaterialId,
            MaterialCode = sourcePart.MaterialCode,
            FinishId = sourcePart.FinishId,
            FinishCode = sourcePart.FinishCode,
            ToleranceId = sourcePart.ToleranceId,
            ToleranceCode = sourcePart.ToleranceCode,
            PartNotes = sourcePart.PartNotes,
            DfmAcknowledged = sourcePart.DfmAcknowledged,
            BagAndTag = true,
            AvailableMaterials = sourcePart.AvailableMaterials,
            AvailableFinishes = sourcePart.AvailableFinishes,
            AvailableTolerances = sourcePart.AvailableTolerances,
        };
        cloned.ResolveDfmReport();

        var index = _parts.IndexOf(sourcePart);
        _parts.Insert(index + 1, cloned);
        _selectedPartIndex = index + 1;

        // Join the file group so late-arriving events reach this clone too.
        if (!string.IsNullOrEmpty(cloned.StoragePath) && _hubConnection is not null)
        {
            try { await _hubConnection.InvokeAsync("JoinFileGroup", cloned.StoragePath); }
            catch (Exception ex) { Logger.LogWarning(ex, "Duplicate: JoinFileGroup failed"); }
        }

        await TriggerRoutingFetchAsync(cloned);

        TriggerAutoSave();
        await InvokeAsync(StateHasChanged);
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

    private async Task OnCustomerSelected(CustomerSummaryDto? customer)
    {
        var previousCustomerId = _selectedCustomerId;
        _selectedCustomer = customer;

        if (previousCustomerId.HasValue || !_selectedCustomerId.HasValue)
            return;

        var partsInTemp = _parts.Where(p => !string.IsNullOrEmpty(p.StoragePath) && p.StoragePath.StartsWith("projects/")).ToList();
        if (partsInTemp.Count == 0)
            return;

        var migrationResult = await Http.PostAsJsonAsync(
            $"api/v1/uploads/migrate-project?projectId={_tempProjectId}&customerId={_selectedCustomerId}",
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

        var resp = await Http.GetAsync($"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(part.GlbStoragePath)}");
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

