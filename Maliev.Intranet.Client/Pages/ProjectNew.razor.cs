using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Client.Components;
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
    private decimal _shippingCost;
    private decimal _manualDiscountAmount;
    private string? _quotationTerms;
    private List<ProcessDto> _processes = [];
    private bool _saving;
    private DateTimeOffset? _lastSavedAt;

    // Computed properties for QuoteSummaryBar
    private string ShippingDestinationCountry => "US"; // default, will be overridden when user enters details
    private string ShippingDestinationPostalCode => "90210"; // default
    private decimal TotalWeightKg => _parts.Sum(p => (decimal)(p.VolumeMm3 ?? 0) * 0.000001m); // rough estimate from mm³ to kg

    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
    private const string ProjectUploadContainerId = "project-new-file-upload";

    // ── Validation ────────────────────────────────────────────────────
    private bool _titleHasError;

    // ── Parts ─────────────────────────────────────────────────────────
    private readonly List<PartViewModel> _parts = [];
    private readonly HashSet<PartViewModel> _bulkSelectedParts = new(ReferenceEqualityComparer.Instance);
    private LayoutMode _layoutMode = LayoutMode.Configurator;
    private int _selectedPartIndex;
    private bool _partsDrawerOpen;

    private string CenterClass =>
        _layoutMode == LayoutMode.SummaryTable
            ? "pn-center pn-center--table"
            : "pn-center";

    private string LayoutToggleIcon =>
        _layoutMode == LayoutMode.SummaryTable
            ? Icons.Material.Outlined.ViewInAr
            : Icons.Material.Outlined.TableRows;

    private string LayoutToggleTitle =>
        _layoutMode == LayoutMode.SummaryTable
            ? "Configurator"
            : "Table edit";

    // ── Customer search ────────────────────────────────────────────────
    private CancellationTokenSource? _searchCts;

    // ── Upload catch-up / watchdog ─────────────────────────────────────
    private const int CatchUpDelayMs = 5000;   // first fetch after SignalR group join
    private const int StatusPollIntervalMs = 30_000; // subsequent interval
    private const int MissingAnalysisStatusMaxPolls = 3;
    private const string MaterialColorKey = "material_color";
    private readonly Dictionary<string, CancellationTokenSource> _statusPollCts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _missingAnalysisStatusPolls = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _storageMigrationSemaphore = new(1, 1);

    // ── Snackbar dedupe for DFM terminal failures ─────────────────────────
    private readonly HashSet<string> _recentDfmFailureKeys = new(StringComparer.OrdinalIgnoreCase);
    private const int DfmFailureKeyTtlMs = 60_000; // 1 minute dedupe window
    private static readonly JsonSerializerOptions SignalRJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly HashSet<string> DefaultBrowserViewerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".3mf",
        ".glb",
        ".gltf",
        ".obj",
        ".stl"
    };
    private HashSet<string>? _runtimeBrowserViewerExtensions;
    private Task<HashSet<string>?>? _runtimeBrowserViewerExtensionsTask;
    private bool? _browserPrimaryServerDfmFallbackEnabled;
    private Task<bool?>? _browserPrimaryServerDfmFallbackEnabledTask;

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
    private const int StorageMigrationDebounceMs = 1000;
    private string DraftStorageKey => $"project-draft-{_sessionId}";
    private Timer? _autoSaveDebounceTimer;
    private Timer? _storageMigrationDebounceTimer;
    private bool _serverSavePending;
    private bool _storageMigrationInProgress;

    // ── SignalR ────────────────────────────────────────────────────────
    private HubConnection? _hubConnection;

    [Inject] private CurrencyService CurrencyService { get; set; } = null!;
    [Inject] private FileTypesSettings FileTypes { get; set; } = null!;
    [Inject] private UploadSettings UploadSettings { get; set; } = null!;
    [Inject] private CookieProvider CookieProvider { get; set; } = null!;
    [Inject] private ILogger<ProjectNew> Logger { get; set; } = null!;
    [Inject] private ThumbnailGenerationService ThumbnailService { get; set; } = null!;

    /// <summary>
    /// Optional customer identifier used to preselect the customer when starting
    /// a project from a customer record.
    /// </summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "customerId")]
    public Guid? CustomerId { get; set; }

    private bool CanSubmit =>
        !_saving &&
        _selectedCustomer != null &&
        !string.IsNullOrWhiteSpace(_title) &&
        !_titleHasError &&
        _selectedLeadTime != null &&
        _parts.Count > 0 &&
        !_parts.Any(p => p.QueuedUpload || p.Uploading || p.PricingLoading) &&
        _parts.All(p => p.IsFullyConfigured && !p.PricingFailed) &&
        _parts.All(p => ResolvePartUnitPriceForConfirmation(p).GetValueOrDefault() > 0m) &&
        _parts.All(p => !p.RequiresDfmAcknowledgement);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _pricingTokens.Values) { await cts.CancelAsync(); cts.Dispose(); }
        foreach (var cts in _statusPollCts.Values) { cts.Cancel(); cts.Dispose(); }
        _statusPollCts.Clear();
        foreach (var callbackRef in _uploadCallbacks.Values) { callbackRef.Dispose(); }
        _uploadCallbacks.Clear();
        foreach (var part in _parts)
            await ClearBrowserUploadFileAsync(part, force: true);
        _autoSaveDebounceTimer?.Dispose();
        _storageMigrationDebounceTimer?.Dispose();
        if (_searchCts != null) { await _searchCts.CancelAsync(); _searchCts.Dispose(); }
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
        _storageMigrationSemaphore.Dispose();
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
        var customerParam = query["customerId"];
        var requestedCustomerId = CustomerId ?? (Guid.TryParse(customerParam, out var parsedCustomerId)
            ? parsedCustomerId
            : (Guid?)null);

        if (string.IsNullOrEmpty(sessionParam) || !Guid.TryParse(sessionParam, out _sessionId))
        {
            _sessionId = Guid.NewGuid();
            var resumeFragment = Guid.TryParse(resumeParam, out var resumeId) ? $"&resume={resumeId}" : "";
            var customerFragment = requestedCustomerId.HasValue ? $"&customerId={requestedCustomerId.Value}" : "";
            // Redirect to URL with session param. In SSR this throws NavigationException (stops execution).
            // In WASM, NavigateTo updates the URL in-place without recreating the component, so we must
            // NOT return — data loading must continue immediately with the newly assigned _sessionId.
            Navigation.NavigateTo($"/sales/projects/new?session={_sessionId}{resumeFragment}{customerFragment}", replace: true);
        }

        // ── Load reference data ────────────────────────────────────────
        var currenciesTask = CurrencyService.InitializeAsync();
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

        await RestoreDraftAsync();
        RefreshLeadTimeOptionsFromPricing();

        // ── Server resume: if ?resume={id} or draft has ServerProjectId, hydrate from server ──
        var hasExplicitResume = Guid.TryParse(resumeParam, out var parsedResumeId);
        if (requestedCustomerId.HasValue && !hasExplicitResume && _selectedCustomer?.Id != requestedCustomerId.Value)
        {
            await LoadRequestedCustomerAsync(requestedCustomerId.Value);
        }

        var serverResumeId = hasExplicitResume ? parsedResumeId : _serverProjectId;
        if (serverResumeId.HasValue && (hasExplicitResume || _selectedCustomer == null))
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
                await JoinPartFileGroupsAsync(part);
        };

        _hubConnection.On<SignalRFileAnalysisPayload>("FileAnalysisCompleted", async payload =>
        {
            await ApplyFileAnalysisCompletedPayloadAsync(payload);
        });

        _hubConnection.On<SignalRGlbReadyPayload>("GlbReady", async payload =>
        {
            var parts = FindPartsByStoragePath(payload.StoragePath);
            if (parts.Count == 0) return;

            if (!payload.Failed)
            {
                foreach (var part in parts)
                {
                    part.GlbSignedUrl = payload.GlbUrl;
                    var viewerStoragePath = NormalizeMigratedArtifactPath(part, payload.ViewerStoragePath)
                        ?? BuildViewerGlbStoragePath(NormalizeMigratedArtifactPath(part, payload.StoragePath));
                    part.ViewerStoragePath = viewerStoragePath;
                    part.ViewerFileExtension = NormalizeViewerFileExtension(payload.ViewerFileExtension, viewerStoragePath);
                    if (string.Equals(part.ViewerFileExtension, ".glb", StringComparison.OrdinalIgnoreCase))
                        part.GlbStoragePath = viewerStoragePath;
                    part.ViewerUrl = payload.GlbUrl;

                    // Formats the browser cannot parse directly (e.g. STEP) get their
                    // thumbnails generated locally from the server-exported viewer GLB.
                    if (!CanGenerateThumbnailsLocally(part.StoragePath)
                        && CanGenerateThumbnailsLocally(viewerStoragePath ?? part.ViewerFileExtension)
                        && !string.IsNullOrEmpty(payload.GlbUrl))
                    {
                        part.SignedDownloadUrl = payload.GlbUrl;
                        TriggerLocalThumbnailsFromViewer(part, payload.GlbUrl);
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
                }
                TriggerAutoSave();
            }
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<SignalRDfmAnalysisPayload>("DfmAnalysisReady", async payload =>
        {
            var parts = FindPartsByStoragePath(payload.StoragePath);
            if (parts.Count == 0) return;

            foreach (var part in parts)
            {
                ApplyDfmPayload(part, payload);
            }
            StopStatusWatchdogs(parts);

            await InvokeAsync(StateHasChanged);
        });

        try
        {
            await _hubConnection.StartAsync();

            foreach (var part in _parts.Where(p => !string.IsNullOrEmpty(p.StoragePath)))
                await JoinPartFileGroupsAsync(part);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "ProjectNew notifications hub is unavailable during initialization.");
        }
    }

    // ── Task 4: Customer search ────────────────────────────────────────

    /// <inheritdoc />
    private async Task<IEnumerable<CustomerSummaryDto>> SearchCustomersAsync(string value, CancellationToken ct)
    {
        var query = value?.Trim() ?? string.Empty;
        if (query.Length == 1)
            return [];

        // Cancel any in-flight search request so debounced keystrokes don't
        // leave stale HTTP calls running in the background.
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            var url = string.IsNullOrWhiteSpace(query)
                ? "api/v1/customers?page=1&pageSize=10"
                : $"api/v1/customers?query={Uri.EscapeDataString(query)}&page=1&pageSize=10";

            var result = await Http.GetFromJsonAsync<PagedResponse<CustomerSummaryDto>>(
                url,
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
            index = item.InputIndex,
            fileName = item.File.Name,
            fileSize = item.File.Size
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
            part.ProgressPercent = 100;
            part.AwaitingPreview = true;
            part.StatusText = "Processing geometry...";
            var completedLocally = await TryCompleteBrowserPrimaryViewerLocallyAsync(part);

            // Trigger client-side thumbnail generation. Formats the browser cannot
            // render directly (e.g. STEP) wait for the server viewer GLB instead —
            // the GlbReady handler generates thumbnails from it.
            if (!string.IsNullOrEmpty(completedUpload.StoragePath)
                && CanGenerateThumbnailsLocally(completedUpload.StoragePath))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var signedUrl = await GetSignedDownloadUrlAsync(completedUpload.StoragePath);
                        if (!string.IsNullOrEmpty(signedUrl))
                        {
                            part.SignedDownloadUrl = signedUrl;
                            await ThumbnailService.GenerateAsync(
                                completedUpload.StoragePath,
                                part.ThumbnailVersion,
                                signedUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Thumbnail generation failed for {StoragePath}", completedUpload.StoragePath);
                    }
                    finally
                    {
                        // Ensure AwaitingPreview is cleared even if thumbnail generation fails
                        // so the skeleton doesn't get stuck
                        await InvokeAsync(() =>
                        {
                            part.AwaitingPreview = false;
                            if (string.IsNullOrEmpty(part.ThumbnailSmallUrl) && !HasLocalThumbnails(part))
                            {
                                part.StatusText = "Preview unavailable";
                            }
                            StateHasChanged();
                        });
                    }
                });
            }

            await JoinPartFileGroupsAsync(part);
            if (!completedLocally)
                StartStatusWatchdog(part, completedUpload.StoragePath);

            if (_selectedCustomerId.HasValue &&
                completedUpload.StoragePath.StartsWith("projects/", StringComparison.OrdinalIgnoreCase))
            {
                TriggerDeferredStorageMigration();
            }

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
                if (!ShouldRetainBrowserUploadFile(part))
                    await JS.InvokeVoidAsync("window.projectNewUploads.scheduleClearFile", item.ClientUploadId);
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
                _missingAnalysisStatusPolls.Remove(storagePath);
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
            _missingAnalysisStatusPolls.Remove(storagePath);
            cts.Cancel();
            cts.Dispose();
        }
    }

    private void StartStatusWatchdog(PartViewModel part, string? storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return;

        if (_statusPollCts.ContainsKey(storagePath))
            return;

        var pollCts = new CancellationTokenSource();
        _statusPollCts[storagePath] = pollCts;
        _ = Task.Run(() => StatusWatchdogLoopAsync(part, storagePath, pollCts.Token));
    }

    private void StopStatusWatchdogs(PartViewModel part)
    {
        foreach (var storagePath in part.GetSignalRStoragePaths())
            StopStatusWatchdog(storagePath);
    }

    private void StopStatusWatchdogs(IEnumerable<PartViewModel> parts)
    {
        foreach (var part in parts)
            StopStatusWatchdogs(part);
    }

    private List<PartViewModel> FindPartsByStoragePath(string? storagePath) =>
        string.IsNullOrWhiteSpace(storagePath)
            ? []
            : _parts.Where(part => part.MatchesSourceStoragePath(storagePath)).ToList();

    private async Task JoinPartFileGroupsAsync(PartViewModel part)
    {
        var hubConnection = _hubConnection;
        if (hubConnection?.State != HubConnectionState.Connected)
            return;

        foreach (var storagePath in part.GetSignalRStoragePaths().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await hubConnection.InvokeAsync("JoinFileGroup", storagePath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to join file SignalR group for {StoragePath}", storagePath);
            }
        }
    }

    private async Task LeavePartFileGroupsAsync(PartViewModel part)
    {
        var hubConnection = _hubConnection;
        if (hubConnection?.State != HubConnectionState.Connected)
            return;

        foreach (var storagePath in part.GetSignalRStoragePaths().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await hubConnection.InvokeAsync("LeaveFileGroup", storagePath);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Failed to leave file SignalR group for {StoragePath}", storagePath);
            }
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

            if (statusResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var missingPolls = _missingAnalysisStatusPolls.TryGetValue(storagePath, out var existingMissingPolls)
                    ? existingMissingPolls + 1
                    : 1;
                _missingAnalysisStatusPolls[storagePath] = missingPolls;

                if (missingPolls >= MissingAnalysisStatusMaxPolls)
                    MarkAnalysisStatusUnavailable(part);

                await InvokeAsync(StateHasChanged);
                return;
            }

            if (!statusResponse.IsSuccessStatusCode)
            {
                await ResolveViewerUrlAsync(part);
                await InvokeAsync(StateHasChanged);
                return;
            }

            _missingAnalysisStatusPolls.Remove(storagePath);
            var status = await statusResponse.Content.ReadFromJsonAsync<FileAnalysisStatusDto>();
            if (status == null)
            {
                await ResolveViewerUrlAsync(part);
                await InvokeAsync(StateHasChanged);
                return;
            }

            await ApplyAnalysisStatusAsync(part, status);

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            // Catch-up fetch is a safety net — failures are non-fatal; SignalR will deliver the final state
            await InvokeAsync(() => Snackbar.Add($"Status fetch failed for {part.Name}: {ex.Message}", Severity.Warning));
        }
    }

    private static void MarkAnalysisStatusUnavailable(PartViewModel part)
    {
        if (part.DfmReport != null)
            return;

        if (!string.IsNullOrWhiteSpace(part.ProcessCode)
            && BrowserDfmReportSync.HasActiveLocalAttempt(part, part.ProcessCode))
        {
            var localDeadline = BrowserDfmReportSync.GetActiveLocalAttemptDeadline(part, part.ProcessCode);
            if (!localDeadline.HasValue || DateTimeOffset.UtcNow < localDeadline.Value)
                return;
        }

        part.AwaitingPreview = false;
        part.DfmAnalysisTimedOut = true;
        part.AnalysisErrorCode = "ANALYSIS_STATUS_NOT_FOUND";
        part.StatusText = DfmStatusMessages.GetStatusText(part.AnalysisErrorCode);
    }

    /// <summary>
    /// Shows a snackbar message for DFM terminal failures, suppressing duplicates
    /// for the same upload/storage path/process combination within a time window.
    /// </summary>
    private void ShowDfmFailureSnackbar(PartViewModel part, string errorCode, string message, Severity severity = Severity.Error)
    {
        var key = $"{part.FileId}|{part.StoragePath}|{part.ProcessCode}|{errorCode}";
        if (_recentDfmFailureKeys.Contains(key))
            return;

        _recentDfmFailureKeys.Add(key);

        // Schedule cleanup
        _ = Task.Delay(DfmFailureKeyTtlMs).ContinueWith(_ => _recentDfmFailureKeys.Remove(key));

        Snackbar.Add(message, severity);
    }

    private async Task LoadRequestedCustomerAsync(Guid customerId)
    {
        var customer = await TryLoadCustomerSummaryAsync(customerId);
        if (customer is null)
            return;

        await OnCustomerSelected(customer);
    }

    private async Task<CustomerSummaryDto?> TryLoadCustomerSummaryAsync(Guid customerId)
    {
        if (customerId == Guid.Empty)
            return null;

        try
        {
            var customer = await Http.GetFromJsonAsync<CustomerDetailDto>($"api/v1/customers/{customerId}");
            return customer is null || customer.Id == Guid.Empty
                ? null
                : ToCustomerSummary(customer);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not load customer details for {CustomerId}.", customerId);
            return null;
        }
    }

    private static CustomerSummaryDto ToCustomerSummary(CustomerDetailDto customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        CompanyId = customer.CompanyId,
        CompanyName = customer.CompanyName,
        Email = customer.Email,
        ProfileImageUrl = customer.ProfileImageUrl,
        Mobile = customer.Mobile,
        Extension = customer.Extension,
        Landline = customer.Landline,
        CompanyPhone = customer.CompanyPhone,
        Status = customer.Status,
        Segment = customer.Segment,
        Tier = customer.Tier,
        TotalSpent = customer.TotalSpent,
        CreatedAt = customer.CreatedAt
    };

    private async Task ApplyAnalysisStatusAsync(PartViewModel part, FileAnalysisStatusDto status)
    {
        if (status.Dimensions != null)
        {
            part.Dimensions = status.Dimensions;
            part.VolumeMm3 = status.Dimensions.VolumeMm3;
        }

        if (status.IsManifold.HasValue)
            part.IsManifold = status.IsManifold;
        if (status.NonManifoldReason != null || status.NonManifoldFaceCount.HasValue)
        {
            part.NonManifoldReason = status.NonManifoldReason;
            part.NonManifoldFaceCount = status.NonManifoldFaceCount;
        }

        var normalizedGlbStoragePath = NormalizeMigratedArtifactPath(part, status.GlbStoragePath);
        if (!string.IsNullOrEmpty(normalizedGlbStoragePath))
            part.GlbStoragePath = normalizedGlbStoragePath;
        var normalizedViewerStoragePath = NormalizeMigratedArtifactPath(part, status.ViewerStoragePath);
        if (!string.IsNullOrEmpty(normalizedViewerStoragePath))
            part.ViewerStoragePath = normalizedViewerStoragePath;
        part.ViewerFileExtension = NormalizeViewerFileExtension(status.ViewerFileExtension, part.ViewerStoragePath ?? part.GlbStoragePath);
        if (!string.IsNullOrEmpty(status.GlbSignedUrl))
            part.GlbSignedUrl = status.GlbSignedUrl;

        ApplyDfmStatus(part, status.DfmReport);
        ApplyPreviewStatus(part, status);

        if (status.Status == FileAnalysisStatus.Completed &&
            (status.PreviewProcessingStatus == PreviewProcessingStatus.Completed ||
             status.PreviewProcessingStatus == PreviewProcessingStatus.Failed))
        {
            part.AwaitingPreview = false;
            part.StatusText = "Ready";
            TriggerAutoSave();
            TriggerDeferredStorageMigration();
        }
        else if (status.Status == FileAnalysisStatus.Failed)
        {
            part.AwaitingPreview = false;
            part.Error = $"Geometry analysis failed: {status.ErrorCode}";
            part.StatusText = "Analysis failed";
            TriggerAutoSave();
            TriggerDeferredStorageMigration();
        }

        await ResolveViewerUrlAsync(part);
        await ResolveOverlayUrlsAsync(part);
        EnsureLocalThumbnailSource(part);

        if (part.ProcessId.HasValue && part.MaterialId.HasValue
            && part.AvailableMaterials.Count > 0)
            TriggerPricingAsync(part);
    }

    private readonly HashSet<string> _thumbnailSourceRequests = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ensures a restored or resumed part has a browser-renderable source for local
    /// thumbnail generation. Server previews are no longer published when a viewer
    /// source exists, so a part restored from a draft has neither server preview URLs
    /// nor a signed download URL — without this the thumbnail stays a spinner forever.
    /// </summary>
    private void EnsureLocalThumbnailSource(PartViewModel part)
    {
        var storagePath = part.StoragePath;
        if (string.IsNullOrEmpty(storagePath)
            || !string.IsNullOrEmpty(part.SignedDownloadUrl)
            || !string.IsNullOrEmpty(part.ThumbnailSmallUrl)
            || HasLocalThumbnails(part)
            || !_thumbnailSourceRequests.Add(storagePath))
        {
            return;
        }

        if (CanGenerateThumbnailsLocally(storagePath))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var signedUrl = await GetSignedDownloadUrlAsync(storagePath);
                    if (string.IsNullOrEmpty(signedUrl)) return;
                    part.SignedDownloadUrl = signedUrl;
                    await ThumbnailService.GenerateAsync(storagePath, part.ThumbnailVersion, signedUrl);
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Local thumbnail source recovery failed for {StoragePath}", storagePath);
                }
            });
            return;
        }

        if (!string.IsNullOrEmpty(part.ViewerUrl)
            && CanGenerateThumbnailsLocally(part.ViewerStoragePath ?? part.ViewerFileExtension))
        {
            part.SignedDownloadUrl = part.ViewerUrl;
            TriggerLocalThumbnailsFromViewer(part, part.ViewerUrl);
        }
    }

    private async Task ApplyFileAnalysisCompletedPayloadAsync(SignalRFileAnalysisPayload payload)
    {
        var parts = FindPartsByStoragePath(payload.StoragePath);
        if (parts.Count == 0) return;

        if (payload.Failed)
        {
            if (IsPreviewArtifactFailure(payload.ErrorCode))
            {
                var status = ToFileAnalysisStatus(payload);
                foreach (var part in parts)
                {
                    ApplyPreviewStatus(part, status);
                    part.AwaitingPreview = false;
                    part.StatusText = "Ready";
                }

                TriggerAutoSave();
                TriggerDeferredStorageMigration();
                await InvokeAsync(StateHasChanged);
                return;
            }

            foreach (var p in parts)
            {
                p.AwaitingPreview = false;
                p.AnalysisErrorCode = payload.ErrorCode;
                p.DfmAnalysisTimedOut = true;
                p.StatusText = DfmStatusMessages.GetStatusText(payload.ErrorCode);
            }
            StopStatusWatchdogs(parts);
            TriggerAutoSave();
            TriggerDeferredStorageMigration();
            await InvokeAsync(StateHasChanged);
            return;
        }

        foreach (var part in parts)
        {
            await ApplyAnalysisStatusAsync(part, ToFileAnalysisStatus(payload));

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
        await InvokeAsync(StateHasChanged);
    }

    private static bool IsPreviewArtifactFailure(string? errorCode)
    {
        return string.Equals(errorCode, "preview-generation-failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(errorCode, "preview-url-resolution-failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(errorCode, "thumbnail-url-resolution-failed", StringComparison.OrdinalIgnoreCase);
    }

    private static FileAnalysisStatusDto ToFileAnalysisStatus(SignalRFileAnalysisPayload payload)
    {
        var hasGeometryState = payload.Dimensions != null || payload.BodyCount.HasValue || payload.NonManifoldReason != null;

        return new FileAnalysisStatusDto
        {
            UploadId = payload.StoragePath,
            Status = payload.Failed ? FileAnalysisStatus.Failed : FileAnalysisStatus.Completed,
            Dimensions = payload.Dimensions == null
                ? null
                : new FileAnalysisDimensionsDto
                {
                    X = payload.Dimensions.X,
                    Y = payload.Dimensions.Y,
                    Z = payload.Dimensions.Z,
                    VolumeMm3 = payload.Dimensions.VolumeMm3
                },
            IsManifold = payload.NonManifoldReason != null
                ? false
                : hasGeometryState ? true : null,
            NonManifoldReason = payload.NonManifoldReason,
            NonManifoldFaceCount = payload.NonManifoldFaceCount,
            ThumbnailUrl = payload.ThumbnailUrl,
            HiResThumbnailUrl = payload.HiResThumbnailUrl,
            PreviewUrls = payload.PreviewUrls == null
                ? null
                : new FileAnalysisPreviewUrlsDto
                {
                    FrontSmall = payload.PreviewUrls.FrontSmall,
                    BackSmall = payload.PreviewUrls.BackSmall,
                    LeftSmall = payload.PreviewUrls.LeftSmall,
                    RightSmall = payload.PreviewUrls.RightSmall,
                    TopSmall = payload.PreviewUrls.TopSmall,
                    BottomSmall = payload.PreviewUrls.BottomSmall,
                    ThumbnailSmall = payload.PreviewUrls.ThumbnailSmall,
                    ThumbnailLargeUrl = payload.PreviewUrls.ThumbnailLarge,
                    ThumbnailSmallGcsPath = payload.PreviewUrls.ThumbnailSmallGcsPath,
                    ThumbnailLargeGcsPath = payload.PreviewUrls.ThumbnailLargeGcsPath
                },
            PreviewProcessingStatus = payload.PreviewUrls == null
                ? PreviewProcessingStatus.Pending
                : payload.Failed ? PreviewProcessingStatus.Failed : PreviewProcessingStatus.Completed,
            ErrorCode = payload.ErrorCode
        };
    }

    private void ApplyPreviewStatus(PartViewModel part, FileAnalysisStatusDto status)
    {
        if (status.PreviewUrls != null)
        {
            // Locally generated thumbnails are authoritative for display — never let
            // late server preview URLs replace them. GCS paths are still recorded
            // below for storage-migration bookkeeping.
            var keepLocalThumbnails = HasLocalThumbnails(part);

            if (!keepLocalThumbnails)
            {
                if (!string.IsNullOrEmpty(status.PreviewUrls.ThumbnailSmall))
                    part.ThumbnailSmallUrl = status.PreviewUrls.ThumbnailSmall;
                else if (!string.IsNullOrEmpty(status.ThumbnailUrl))
                    part.ThumbnailSmallUrl = status.ThumbnailUrl;

                if (!string.IsNullOrEmpty(status.PreviewUrls.ThumbnailLargeUrl))
                    part.ThumbnailLargeUrl = status.PreviewUrls.ThumbnailLargeUrl;
                else if (!string.IsNullOrEmpty(status.HiResThumbnailUrl))
                    part.ThumbnailLargeUrl = status.HiResThumbnailUrl;
            }

            part.ThumbnailSmallGcsPath = NormalizeMigratedArtifactPath(part, status.PreviewUrls.ThumbnailSmallGcsPath);
            part.ThumbnailLargeGcsPath = NormalizeMigratedArtifactPath(part, status.PreviewUrls.ThumbnailLargeGcsPath);
            return;
        }

        if (!string.IsNullOrEmpty(status.ThumbnailUrl) && !HasLocalThumbnails(part))
        {
            part.ThumbnailSmallUrl = status.ThumbnailUrl;
            part.ThumbnailLargeUrl = status.HiResThumbnailUrl ?? status.ThumbnailUrl;
        }
    }

    /// <summary>
    /// File extensions the in-browser thumbnail generator can render
    /// (BabylonJS loaders plus the GeometryService runtime worker for 3MF).
    /// Must stay in sync with GeometryInterop.js SUPPORTED_EXTENSIONS.
    /// </summary>
    private static readonly string[] LocallyRenderableThumbnailExtensions =
        [".stl", ".obj", ".glb", ".gltf", ".3mf"];

    private static bool CanGenerateThumbnailsLocally(string? fileNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
            return false;
        var ext = Path.GetExtension(fileNameOrPath);
        return !string.IsNullOrEmpty(ext)
            && LocallyRenderableThumbnailExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private bool HasLocalThumbnails(PartViewModel part) =>
        !string.IsNullOrEmpty(part.StoragePath)
        && ThumbnailService.TryGetCached(part.StoragePath, part.ThumbnailVersion, out var localSet)
        && localSet is { HasAny: true };

    /// <summary>
    /// Generates the part's thumbnail set locally from a browser-renderable viewer
    /// source (the server-exported GLB). Marks the part Ready on success so the
    /// status watchdog does not wait for server previews that will never arrive.
    /// </summary>
    private void TriggerLocalThumbnailsFromViewer(PartViewModel part, string viewerUrl)
    {
        var storagePath = part.StoragePath;
        if (string.IsNullOrEmpty(storagePath) || HasLocalThumbnails(part))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await ThumbnailService.GenerateAsync(storagePath, part.ThumbnailVersion, viewerUrl);
                await InvokeAsync(() =>
                {
                    part.AwaitingPreview = false;
                    if (result is { HasAny: true })
                    {
                        part.StatusText = "Ready";
                        StopStatusWatchdog(storagePath);
                        TriggerAutoSave();
                    }
                    StateHasChanged();
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Local viewer thumbnail generation failed for {StoragePath}", storagePath);
            }
        });
    }

    private void ApplyDfmStatus(PartViewModel part, object? dfmReport)
    {
        if (dfmReport is SignalRDfmAnalysisPayload payload)
        {
            ApplyDfmPayload(part, payload);
            return;
        }

        if (dfmReport is JsonElement je && je.ValueKind == JsonValueKind.Object
            && ContainsDfmReportPayload(je))
        {
            var payloadFromJson = JsonSerializer.Deserialize<SignalRDfmAnalysisPayload>(
                je.GetRawText(),
                SignalRJsonOptions);
            if (payloadFromJson != null)
            {
                ApplyDfmPayload(part, payloadFromJson);
                return;
            }
        }

        part.DfmReport = dfmReport;
        if (dfmReport != null)
            ClearDfmUnavailableState(part);
    }

    private static bool ContainsDfmReportPayload(JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, nameof(SignalRDfmAnalysisPayload.FdmReport), StringComparison.OrdinalIgnoreCase)
                || string.Equals(property.Name, nameof(SignalRDfmAnalysisPayload.SlaReport), StringComparison.OrdinalIgnoreCase)
                || string.Equals(property.Name, nameof(SignalRDfmAnalysisPayload.CncReport), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyDfmPayload(PartViewModel part, SignalRDfmAnalysisPayload payload)
    {
        if (payload.FdmReport != null) part.FdmDfmReport = payload.FdmReport;
        if (payload.SlaReport != null) part.SlaDfmReport = payload.SlaReport;
        if (payload.CncReport != null) part.CncDfmReport = payload.CncReport;
        if (payload.OverlayUrls != null) part.OverlayUrls = payload.OverlayUrls;
        if (payload.OverlayPaths != null)
            part.OverlayPaths = NormalizeMigratedArtifactPaths(part, payload.OverlayPaths);
        if (payload.BodyCount.HasValue)
            part.BodyCount = payload.BodyCount.Value;

        if (payload.NonManifoldReason != null && part.NonManifoldReason == null)
        {
            part.IsManifold = false;
            part.NonManifoldReason = payload.NonManifoldReason;
            part.NonManifoldFaceCount = payload.NonManifoldFaceCount;
        }
        else if (payload.NonManifoldReason == null && payload.BodyCount.HasValue && part.IsManifold == null)
        {
            part.IsManifold = true;
        }

        part.ResolveDfmReport();
        if (part.DfmReport != null)
            ClearDfmUnavailableState(part);
    }

    private async Task RunProcessDfmAnalysisAsync(PartViewModel part, ProcessDto process)
    {
        if (!_parts.Contains(part) || part.FileId == Guid.Empty)
            return;

        var processCode = process.Code;
        try
        {
            var runInteractiveServerDfmFallback = await ShouldRunInteractiveServerDfmFallbackAsync(part, processCode);
            ResetProcessDfmStateForRetry(part, processCode, clearLocalAttempts: runInteractiveServerDfmFallback);
            await InvokeAsync(StateHasChanged);

            if (await BrowserDfmReportSync.WaitForCurrentReportAsync(part, processCode, CancellationToken.None))
                return;

            // Re-check the fallback policy after the wait. A browser-local terminal
            // attempt may have been recorded during the grace period, which grants
            // the server fallback even when the static policy says otherwise.
            if (!runInteractiveServerDfmFallback
                && !(await ShouldRunInteractiveServerDfmFallbackAsync(part, processCode)))
            {
                MarkBrowserPrimaryLocalDfmUnavailable(part, processCode, "interactive_server_dfm_fallback_disabled");
                return;
            }

            using var response = await Http.PostAsJsonAsync(
                $"api/v1/geometry/{part.FileId}/dfm/{Uri.EscapeDataString(processCode)}",
                new GeometryAnalysisRequest { StoragePath = part.StoragePath });

            if (!string.Equals(part.ProcessCode, processCode, StringComparison.OrdinalIgnoreCase))
                return;

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DfmAnalysisResponse>();
                if (result != null && string.Equals(result.Status, "analysis_complete", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyDfmAnalysisResultAsync(part, processCode, result);
                }
                else if (result != null && string.Equals(result.Status, "timeout", StringComparison.OrdinalIgnoreCase))
                {
                    part.DfmAnalysisTimedOut = true;
                    part.AnalysisErrorCode = "GEOMETRY_PHASE2_TIMEOUT";
                }
                else
                {
                    part.DfmAnalysisTimedOut = true;
                    part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                if (BrowserDfmReportSync.HasCurrentReport(part, processCode))
                    return;

                if (BrowserDfmReportSync.HasActiveLocalAttempt(part, processCode))
                    return;

                part.DfmAnalysisTimedOut = false;
                part.AnalysisErrorCode = "FILE_MISSING";
                part.AwaitingPreview = false;
                part.StatusText = DfmStatusMessages.GetStatusText("FILE_MISSING");
                StopStatusWatchdog(part.StoragePath);
                ShowDfmFailureSnackbar(part, "FILE_MISSING", "File expired or missing. Re-upload to run DFM analysis.", Severity.Error);
            }
            else
            {
                var result = await response.Content.ReadFromJsonAsync<DfmAnalysisResponse>();
                if (result?.DfmReport != null)
                    await ApplyDfmAnalysisResultAsync(part, processCode, result);
                else
                {
                    part.DfmAnalysisTimedOut = true;
                    part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";
                }
            }
        }
        catch (Exception)
        {
            if (string.Equals(part.ProcessCode, processCode, StringComparison.OrdinalIgnoreCase))
            {
                part.DfmAnalysisTimedOut = true;
                part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";
            }
        }
        finally
        {
            if (string.Equals(part.ProcessCode, processCode, StringComparison.OrdinalIgnoreCase))
            {
                part.ResolveDfmReport();
                await OnPartChanged(part);
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task ApplyDfmAnalysisResultAsync(
        PartViewModel part,
        string processCode,
        DfmAnalysisResponse result)
    {
        SetDfmReportForProcess(part, processCode, result.DfmReport);

        if (result.BodyCount.HasValue && !part.BodyCount.HasValue)
            part.BodyCount = result.BodyCount.Value;

        if (result.OverlayPaths.Count > 0)
        {
            var normalized = NormalizeMigratedArtifactPaths(part, result.OverlayPaths);
            part.OverlayPaths ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, path) in normalized)
                part.OverlayPaths[key] = path;

            await ResolveOverlayUrlsAsync(part);
        }

        part.DfmAnalysisTimedOut = false;
        part.AnalysisErrorCode = null;
    }

    private async Task<bool> ShouldRunInteractiveServerDfmFallbackAsync(PartViewModel part, string? processCode = null)
    {
        if (!IsBrowserPrimaryDfmPart(part, _runtimeBrowserViewerExtensions ?? DefaultBrowserViewerExtensions))
            return true;

        if (!string.IsNullOrWhiteSpace(processCode) && BrowserDfmReportSync.HasTerminalLocalAttempt(part, processCode))
            return true;

        var enabled = await GetBrowserPrimaryServerDfmFallbackEnabledAsync();
        return enabled.GetValueOrDefault(false);
    }

    private async Task<bool?> GetBrowserPrimaryServerDfmFallbackEnabledAsync()
    {
        if (_browserPrimaryServerDfmFallbackEnabled.HasValue)
            return _browserPrimaryServerDfmFallbackEnabled.Value;

        _browserPrimaryServerDfmFallbackEnabledTask ??= FetchBrowserPrimaryServerDfmFallbackEnabledAsync();
        var enabled = await _browserPrimaryServerDfmFallbackEnabledTask;
        if (enabled.HasValue)
        {
            _browserPrimaryServerDfmFallbackEnabled = enabled.Value;
        }
        else
        {
            _browserPrimaryServerDfmFallbackEnabledTask = null;
        }

        return enabled;
    }

    private async Task<bool?> FetchBrowserPrimaryServerDfmFallbackEnabledAsync()
    {
        try
        {
            using var response = await Http.GetAsync("api/v1/geometry/runtime/manifest");
            if (!response.IsSuccessStatusCode)
                return null;

            using var manifest = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return manifest is null
                ? null
                : ReadInteractiveServerDfmFallbackForBrowserPrimaryUploads(manifest.RootElement);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or OperationCanceledException)
        {
            Logger.LogDebug(ex, "Could not load browser geometry runtime fallback policy.");
            return null;
        }
    }

    private static void MarkBrowserPrimaryLocalDfmUnavailable(
        PartViewModel part,
        string processCode,
        string reason)
    {
        BrowserDfmReportSync.MarkTerminalLocalAttempt(part, processCode, reason);
        part.DfmAnalysisTimedOut = true;
        part.AnalysisErrorCode = DfmStatusMessages.BrowserLocalDfmUnavailable;
        part.StatusText = DfmStatusMessages.GetStatusText(part.AnalysisErrorCode);
        part.ResolveDfmReport();
    }

    private async Task<bool> HandleLocalGeometryRuntimeCompletedAsync(PartLocalGeometryRuntimeResult completion)
    {
        if (!_parts.Contains(completion.Part))
            return false;

        if (!TryApplyLocalGeometryRuntimeResult(completion.Part, completion.Result))
            return false;

        await OnPartChanged(completion.Part);
        await InvokeAsync(StateHasChanged);
        return true;
    }

    private async Task HandleLocalGeometryRuntimeStartedAsync(PartLocalGeometryRuntimeStarted completion)
    {
        if (!_parts.Contains(completion.Part))
            return;

        if (string.IsNullOrWhiteSpace(completion.Part.ProcessCode)
            || string.IsNullOrWhiteSpace(completion.Result.ProcessCode)
            || !ProcessCodeNormalizer.Equals(completion.Part.ProcessCode, completion.Result.ProcessCode))
        {
            return;
        }

        BrowserDfmReportSync.MarkLocalAttemptStarted(
            completion.Part,
            completion.Result.ProcessCode,
            completion.Result.InputByteCount,
            completion.Result.InputTriangleCount);
        completion.Part.DfmAnalysisTimedOut = false;
        completion.Part.AnalysisErrorCode = null;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleLocalGeometryRuntimeUnavailableAsync(PartLocalGeometryRuntimeUnavailable completion)
    {
        if (!_parts.Contains(completion.Part))
            return;

        BrowserDfmReportSync.MarkTerminalLocalAttempt(
            completion.Part,
            completion.Result.ProcessCode,
            completion.Result.Reason);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Replaces a part's 3D file with a newly uploaded revision. The part keeps
    /// its configuration (process, material, finish, tolerance, quantity); all
    /// geometry-derived state (metrics, DFM, viewer, thumbnails) is reset and
    /// recomputed for the new file.
    /// </summary>
    private async Task HandleReplacePartFileAsync(PartReplaceFileRequest request)
    {
        var part = request.Part;
        var file = request.File;
        if (!_parts.Contains(part) || file.Size <= 0)
            return;

        var extension = Path.GetExtension(file.Name);
        if (!FileTypes.Is3DFile(extension))
        {
            Snackbar.Add($"{file.Name}: not a supported 3D file format.", Severity.Warning);
            return;
        }

        var clientUploadId = $"revision-{Guid.NewGuid():N}";

        // Hand the browser File object to the shared upload pipeline.
        await JS.InvokeVoidAsync(
            "window.projectNewUploads.captureFiles",
            request.ContainerId,
            new[] { new { clientUploadId, fileName = file.Name, fileSize = file.Size, index = 0 } });

        await ReplacePartFileCoreAsync(part, file.Name, file.Size, file.ContentType, clientUploadId);
    }

    /// <summary>
    /// Handles a rescale request: scales every vertex of the part's mesh file by
    /// the requested factor in the browser, then replaces the uploaded file with
    /// the scaled result (configuration preserved).
    /// </summary>
    private async Task HandleRescalePartAsync(PartRescaleRequest request)
    {
        var part = request.Part;
        if (!_parts.Contains(part) || string.IsNullOrEmpty(part.StoragePath))
            return;

        var clientUploadId = $"rescale-{Guid.NewGuid():N}";
        try
        {
            var signedUrl = await GetSignedDownloadUrlAsync(part.StoragePath);
            if (string.IsNullOrEmpty(signedUrl))
            {
                Snackbar.Add($"{part.Name}: could not download the file for rescaling.", Severity.Error);
                return;
            }

            var scaled = await JS.InvokeAsync<ScaledFileResult?>(
                "malievPartScaling.scaleAndRegister",
                signedUrl,
                part.Name,
                request.ScaleFactor,
                clientUploadId);
            if (scaled is null || scaled.Size <= 0)
            {
                Snackbar.Add($"{part.Name}: rescaling produced no file.", Severity.Error);
                return;
            }

            await ReplacePartFileCoreAsync(part, scaled.FileName ?? part.Name, scaled.Size, null, clientUploadId);
        }
        catch (JSException ex)
        {
            Snackbar.Add($"{part.Name}: {ex.Message}", Severity.Error);
        }
    }

    private sealed record ScaledFileResult(string? FileName, long Size);

    /// <summary>
    /// Shared revision/rescale upload core: uploads the file already registered
    /// under <paramref name="clientUploadId"/> in the browser, then swaps it into
    /// the part, resetting geometry-derived state while keeping configuration.
    /// </summary>
    private async Task ReplacePartFileCoreAsync(
        PartViewModel part,
        string fileName,
        long fileSize,
        string? contentType,
        string clientUploadId)
    {
        var callbackId = Guid.NewGuid();
        DotNetObjectReference<UploadProgressCallback>? callbackRef = null;

        part.Uploading = true;
        part.ProgressPercent = 0;
        part.Error = null;
        part.StatusText = "Uploading revision…";
        await InvokeAsync(StateHasChanged);

        try
        {
            var browserContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType;
            var initiateRequest = new BffInitiateResumableUploadRequest
            {
                FileName = fileName,
                ContentType = browserContentType,
                FileSize = fileSize,
                ProjectId = _tempProjectId,
                CustomerId = _selectedCustomerId
            };

            var initiateResponse = await Http.PostAsJsonAsync("api/v1/uploads/resumable", initiateRequest);
            if (!initiateResponse.IsSuccessStatusCode)
            {
                await MarkUploadFailedAsync(part, $"Revision upload initiation failed ({(int)initiateResponse.StatusCode}).");
                return;
            }

            var session = await initiateResponse.Content.ReadFromJsonAsync<BffResumableUploadSessionResponse>();
            if (session == null || string.IsNullOrWhiteSpace(session.UploadId) || string.IsNullOrWhiteSpace(session.SessionUri))
            {
                await MarkUploadFailedAsync(part, "Revision upload initiation response was invalid.");
                return;
            }

            callbackRef = DotNetObjectReference.Create(new UploadProgressCallback(part, () => InvokeAsync(StateHasChanged)));
            _uploadCallbacks[callbackId] = callbackRef;

            var uploadResult = await JS.InvokeAsync<UploadResult>(
                "window.projectNewUploads.uploadFile",
                clientUploadId,
                session.SessionUri,
                $"api/v1/uploads/resumable/{Uri.EscapeDataString(session.UploadId)}",
                browserContentType ?? "application/octet-stream",
                fileSize,
                callbackRef);

            if (uploadResult == null || uploadResult.Status < 200 || uploadResult.Status >= 300)
            {
                await MarkUploadFailedAsync(part, $"Revision upload failed ({uploadResult?.Status ?? 0}).");
                return;
            }

            var completeResponse = await Http.PostAsJsonAsync(
                $"api/v1/uploads/resumable/{Uri.EscapeDataString(session.UploadId)}/complete",
                new { });
            if (!completeResponse.IsSuccessStatusCode)
            {
                await MarkUploadFailedAsync(part, $"Revision upload completion failed ({(int)completeResponse.StatusCode}).");
                return;
            }

            var completedUpload = await completeResponse.Content.ReadFromJsonAsync<BffUploadResponse>();
            if (completedUpload == null || string.IsNullOrWhiteSpace(completedUpload.StoragePath))
            {
                await MarkUploadFailedAsync(part, "Revision upload completion response was invalid.");
                return;
            }

            // Detach from the old file before swapping in the revision.
            await LeavePartFileGroupsAsync(part);
            StopStatusWatchdogs(part);
            _thumbnailSourceRequests.Remove(part.StoragePath ?? string.Empty);

            ApplyReplacementFileToPart(part, fileName, completedUpload, clientUploadId);

            part.Uploading = false;
            part.ProgressPercent = 100;
            part.AwaitingPreview = true;
            part.StatusText = "Processing geometry...";

            var completedLocally = await TryCompleteBrowserPrimaryViewerLocallyAsync(part);

            if (CanGenerateThumbnailsLocally(part.StoragePath))
            {
                var storagePath = part.StoragePath!;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var signedUrl = await GetSignedDownloadUrlAsync(storagePath);
                        if (string.IsNullOrEmpty(signedUrl)) return;
                        part.SignedDownloadUrl = signedUrl;
                        await ThumbnailService.GenerateAsync(storagePath, part.ThumbnailVersion, signedUrl);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Revision thumbnail generation failed for {StoragePath}", storagePath);
                    }
                    finally
                    {
                        await InvokeAsync(() =>
                        {
                            part.AwaitingPreview = false;
                            StateHasChanged();
                        });
                    }
                });
            }

            await JoinPartFileGroupsAsync(part);
            if (!completedLocally)
                StartStatusWatchdog(part, part.StoragePath);

            TriggerAutoSave();
            TriggerPricingAsync(part);
            Snackbar.Add($"{fileName}: revision uploaded — configuration kept.", Severity.Success);
        }
        catch (Exception ex)
        {
            await MarkUploadFailedAsync(part, $"Revision upload error: {ex.Message}");
        }
        finally
        {
            if (callbackRef != null)
            {
                _uploadCallbacks.TryRemove(callbackId, out _);
                callbackRef.Dispose();
            }
            await InvokeAsync(StateHasChanged);
        }
    }

    private static void ApplyReplacementFileToPart(
        PartViewModel part,
        string fileName,
        BffUploadResponse completedUpload,
        string clientUploadId)
    {
        part.StoragePath = completedUpload.StoragePath;
        part.StoragePathAliases.Clear();
        part.FileId = Guid.TryParse(completedUpload.UploadId, out var fileId) ? fileId : Guid.NewGuid();
        part.Name = fileName;
        part.ClientUploadId = clientUploadId;
        part.ThumbnailVersion = part.FileId.ToString("N");

        // Geometry-derived state belongs to the OLD file — reset everything the
        // pipelines recompute. Configuration (process/material/finish/tolerance/
        // quantity/lead time) is intentionally preserved.
        part.SignedDownloadUrl = null;
        part.ViewerUrl = null;
        part.ViewerStoragePath = null;
        part.ViewerFileExtension = null;
        part.GlbStoragePath = null;
        part.GlbSignedUrl = null;
        part.Dimensions = null;
        part.VolumeMm3 = null;
        part.SurfaceAreaMm2 = null;
        part.IsManifold = null;
        part.NonManifoldReason = null;
        part.NonManifoldFaceCount = null;
        part.BodyCount = null;
        part.Bodies = [];
        part.ThumbnailSmallUrl = null;
        part.ThumbnailLargeUrl = null;
        part.ThumbnailSmallGcsPath = null;
        part.ThumbnailLargeGcsPath = null;
        part.OverlayPaths = null;
        part.OverlayUrls = null;
        part.DfmReport = null;
        part.FdmDfmReport = null;
        part.SlaDfmReport = null;
        part.CncDfmReport = null;
        part.DfmAnalysisTimedOut = false;
        part.AnalysisErrorCode = null;
        part.LocalDfmRuntimeCompletedForProcessCode = null;
        part.LocalDfmRuntimeTerminalProcessCode = null;
        part.LocalDfmRuntimeTerminalReason = null;
        part.Error = null;
    }

    private static bool TryApplyLocalGeometryRuntimeResult(
        PartViewModel part,
        LocalGeometryRuntimeResult result)
    {
        if (result is not
            {
                Authority: "local_primary",
                ExecutionMode: "primary_interactive",
                IsAuthoritative: false
            })
        {
            return false;
        }

        // Metrics-only probe (no manufacturing process selected yet): apply the
        // mesh metrics so dimensions/volume/integrity show right after upload,
        // but do not record a DFM report — that needs a process-specific run.
        if (string.Equals(result.Operation, "compute_metrics", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(result.ProcessCode))
        {
            if (result.Metrics is null)
                return false;
            ApplyLocalGeometryRuntimeMetrics(part, result.Metrics);
            return true;
        }

        if (string.IsNullOrWhiteSpace(part.ProcessCode)
            || !ProcessCodeNormalizer.Equals(part.ProcessCode, result.ProcessCode))
        {
            return false;
        }

        var processCode = ProcessCodeNormalizer.Normalize(part.ProcessCode) ?? part.ProcessCode;
        var report = BuildDfmReportFromLocalGeometryRuntimeResult(processCode, result);
        SetDfmReportForProcess(part, processCode, report);
        part.ResolveDfmReport();
        ApplyLocalGeometryRuntimeMetrics(part, result.Metrics);
        BrowserDfmReportSync.ClearTerminalLocalAttempt(part, processCode);
        BrowserDfmReportSync.ClearActiveLocalAttempt(part, processCode);
        part.LocalDfmRuntimeCompletedForProcessCode = processCode;
        part.DfmAnalysisTimedOut = false;
        part.AnalysisErrorCode = null;
        return true;
    }

    private static void ApplyLocalGeometryRuntimeMetrics(
        PartViewModel part,
        LocalGeometryRuntimeMetrics? metrics)
    {
        if (metrics is null)
            return;

        if (TryGetFiniteNonNegative(metrics.VolumeMm3, out var volumeMm3))
            part.VolumeMm3 = volumeMm3;

        if (TryGetFiniteNonNegative(metrics.SurfaceAreaMm2, out var surfaceAreaMm2))
            part.SurfaceAreaMm2 = surfaceAreaMm2;

        if (metrics.BoundingBox is { } boundingBox
            && TryGetFiniteNonNegative(boundingBox.X, out var x)
            && TryGetFiniteNonNegative(boundingBox.Y, out var y)
            && TryGetFiniteNonNegative(boundingBox.Z, out var z))
        {
            part.Dimensions = new FileAnalysisDimensionsDto
            {
                X = x,
                Y = y,
                Z = z,
                VolumeMm3 = part.VolumeMm3,
            };
        }

        if (metrics.IsManifold.HasValue)
            part.IsManifold = metrics.IsManifold.Value;

        if (TryGetFiniteNonNegative(metrics.BodyCount, out var bodyCountValue)
            && bodyCountValue >= 1
            && bodyCountValue <= int.MaxValue)
        {
            part.BodyCount = (int)Math.Round(bodyCountValue, MidpointRounding.AwayFromZero);
        }

        if (TryGetFiniteNonNegative(metrics.NonManifoldEdgeCount, out var edgeCountValue)
            && edgeCountValue > 0
            && edgeCountValue <= int.MaxValue)
        {
            var edgeCount = (int)Math.Round(edgeCountValue, MidpointRounding.AwayFromZero);
            part.IsManifold = false;
            part.NonManifoldFaceCount = edgeCount;
            part.NonManifoldReason = string.Create(
                CultureInfo.InvariantCulture,
                $"Found {edgeCount:N0} non-manifold edge(s) shared by more than two faces.");
        }
        else if (TryGetFiniteNonNegative(metrics.OpenEdgeCount, out var openEdgeValue)
            && openEdgeValue > 0
            && openEdgeValue <= int.MaxValue)
        {
            var openEdgeCount = (int)Math.Round(openEdgeValue, MidpointRounding.AwayFromZero);
            part.IsManifold = false;
            part.NonManifoldFaceCount = openEdgeCount;
            part.NonManifoldReason = string.Create(
                CultureInfo.InvariantCulture,
                $"Found {openEdgeCount:N0} open edge(s) — the mesh is not watertight.");
        }
        else if (metrics.IsManifold == true)
        {
            part.NonManifoldFaceCount = null;
            part.NonManifoldReason = null;
        }
    }

    private static bool TryGetFiniteNonNegative(double? value, out double number)
    {
        number = value.GetValueOrDefault();
        return value.HasValue && double.IsFinite(number) && number >= 0;
    }

    private static DfmReport BuildDfmReportFromLocalGeometryRuntimeResult(
        string processCode,
        LocalGeometryRuntimeResult result)
    {
        var issues = result.Issues
            .Select(issue => new Maliev.Intranet.Shared.Dtos.DfmIssue
            {
                Category = issue.Category ?? string.Empty,
                Severity = issue.Severity ?? "warning",
                Title = issue.Title ?? "Local DFM issue",
                Description = issue.Description ?? "Detected by the browser-first local DFM runtime.",
                Value = issue.Value.GetValueOrDefault(),
                Threshold = issue.Threshold.GetValueOrDefault(),
                FaceIndices = issue.FaceIndices,
                Centroid = issue.Centroid,
                Source = "local"
            })
            .ToList();

        var thinWallCount = issues.Count(issue =>
            string.Equals(issue.Category, "thin_wall", StringComparison.OrdinalIgnoreCase));
        var overhangFaceCount = issues
            .Where(issue => string.Equals(issue.Category, "overhang", StringComparison.OrdinalIgnoreCase))
            .Sum(issue => issue.FaceIndices.Count > 0 ? issue.FaceIndices.Count : 1);

        return new DfmReport
        {
            ReportType = processCode,
            Issues = issues,
            AnalysisTimeSeconds = 0,
            ThinWallCount = thinWallCount > 0 ? thinWallCount : null,
            OverhangFaceCount = overhangFaceCount > 0 ? overhangFaceCount : null,
            SupportRequired = overhangFaceCount > 0 ? true : null,
        };
    }

    private static void SetDfmReportForProcess(
        PartViewModel part,
        string processCode,
        object? report)
    {
        var normalizedProcessCode = ProcessCodeNormalizer.Normalize(processCode);
        if (normalizedProcessCode is "SLA_DLP")
            part.SlaDfmReport = report;
        else if (normalizedProcessCode is "CNC_MILL" or "CNC_TURN" or "CNC_5AXIS")
            part.CncDfmReport = report;
        else
            part.FdmDfmReport = report;
    }

    private async Task HandleBulkTableDfmActionAsync(ProjectPartDfmActionRequest request)
    {
        if (!_parts.Contains(request.Part))
            return;

        if (request.Action == ProjectPartDfmAction.Review)
        {
            var index = _parts.IndexOf(request.Part);
            if (index >= 0)
                _selectedPartIndex = index;

            _layoutMode = LayoutMode.Configurator;
            await OpenBabylonViewer(request.Part);
            await InvokeAsync(StateHasChanged);
            return;
        }

        await RetryDfmAnalysisAsync(request.Part);
    }

    private async Task RetryDfmAnalysisAsync(PartViewModel part)
    {
        var process = _processes.FirstOrDefault(item =>
            string.Equals(item.Code, part.ProcessCode, StringComparison.OrdinalIgnoreCase));

        if (process == null)
            return;

        await RunProcessDfmAnalysisAsync(part, process);
    }

    private static void ResetProcessDfmStateForRetry(PartViewModel part, string processCode, bool clearLocalAttempts = true)
    {
        var upperProcessCode = processCode.ToUpperInvariant();
        if (upperProcessCode is "SLA" or "SLA_DLP" or "DLP")
            part.SlaDfmReport = null;
        else if (upperProcessCode is "CNC" or "CNC_MILL" or "CNC_TURN")
            part.CncDfmReport = null;
        else
            part.FdmDfmReport = null;

        part.DfmReport = null;
        if (clearLocalAttempts)
        {
            BrowserDfmReportSync.ClearTerminalLocalAttempt(part, processCode);
            BrowserDfmReportSync.ClearActiveLocalAttempt(part, processCode);
        }
        part.DfmAnalysisTimedOut = false;
        part.AnalysisErrorCode = null;
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

        if (await TryApplyLocalViewerUrlAsync(part))
            return;

        // Fallback: Call viewer-url API for backward compatibility (drafts created before this fix)
        // Use StoragePath (original uploaded file path); the BFF caches status keyed by that path,
        // not by GlbStoragePath which ends in _viewer.glb.
        var storagePath = part.StoragePath;
        if (string.IsNullOrEmpty(storagePath)) return;

        try
        {
            var viewerResp = await Http.GetAsync(
                $"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(storagePath)}");
            if (viewerResp.IsSuccessStatusCode)
            {
                var viewerJson = await viewerResp.Content.ReadFromJsonAsync<JsonDocument>();
                ApplyViewerUrlDocument(part, viewerJson);
            }
        }
        catch
        {
            // Non-fatal — viewer URL resolution is best-effort
        }
    }

    private static void ApplyViewerUrlDocument(PartViewModel part, JsonDocument? viewerJson)
    {
        if (viewerJson == null)
            return;

        var root = viewerJson.RootElement;
        var resolvedUrl = ReadStringProperty(root, "url", "Url");
        if (!string.IsNullOrEmpty(resolvedUrl))
        {
            part.ViewerUrl = resolvedUrl;
            part.GlbSignedUrl = resolvedUrl;
        }

        var viewerStoragePath = ReadStringProperty(root, "viewerStoragePath", "ViewerStoragePath");
        if (!string.IsNullOrWhiteSpace(viewerStoragePath))
        {
            part.ViewerStoragePath = viewerStoragePath;
        }

        part.ViewerFileExtension = NormalizeViewerFileExtension(
            ReadStringProperty(root, "viewerFileExtension", "ViewerFileExtension"),
            part.ViewerStoragePath ?? part.GlbStoragePath ?? part.StoragePath);

        if (string.Equals(part.ViewerFileExtension, ".glb", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(part.ViewerStoragePath))
        {
            part.GlbStoragePath = part.ViewerStoragePath;
        }
    }

    private static string? ReadStringProperty(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
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

    private async Task<bool> TryCompleteBrowserPrimaryViewerLocallyAsync(PartViewModel part)
    {
        if (!await TryApplyLocalViewerUrlAsync(part))
            return false;

        part.AwaitingPreview = false;
        part.StatusText = "Ready";
        part.DfmAnalysisTimedOut = false;
        part.AnalysisErrorCode = null;
        TriggerAutoSave();
        return true;
    }

    private static string? NormalizeMigratedArtifactPath(PartViewModel part, string? storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || string.IsNullOrWhiteSpace(part.StoragePath))
            return storagePath;

        foreach (var alias in part.StoragePathAliases.Where(alias => !string.IsNullOrWhiteSpace(alias)))
        {
            var rewritten = RewriteMigratedStoragePath(storagePath, alias, part.StoragePath);
            if (!string.Equals(rewritten, storagePath, StringComparison.OrdinalIgnoreCase))
                return rewritten;
        }

        return storagePath;
    }

    private static Dictionary<string, string> NormalizeMigratedArtifactPaths(
        PartViewModel part,
        Dictionary<string, string> storagePaths) =>
        storagePaths.ToDictionary(
            item => item.Key,
            item => NormalizeMigratedArtifactPath(part, item.Value) ?? item.Value,
            StringComparer.OrdinalIgnoreCase);

    private static string? RewriteMigratedStoragePath(string? storagePath, string oldBasePath, string newBasePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return storagePath;

        if (string.Equals(storagePath, oldBasePath, StringComparison.OrdinalIgnoreCase))
            return newBasePath;

        if (!storagePath.StartsWith(oldBasePath, StringComparison.OrdinalIgnoreCase))
            return storagePath;

        return newBasePath + storagePath[oldBasePath.Length..];
    }

    private static string? BuildViewerGlbStoragePath(string? sourceStoragePath)
    {
        if (string.IsNullOrWhiteSpace(sourceStoragePath))
            return null;

        return sourceStoragePath.EndsWith("_viewer.glb", StringComparison.OrdinalIgnoreCase)
            ? sourceStoragePath
            : sourceStoragePath + "_viewer.glb";
    }

    private static string? NormalizeViewerFileExtension(string? fileExtension, string? storagePath)
    {
        var ext = !string.IsNullOrWhiteSpace(fileExtension)
            ? fileExtension.Trim()
            : Path.GetExtension(storagePath);

        if (string.IsNullOrWhiteSpace(ext))
            return null;

        return ext.StartsWith('.') ? ext.ToLowerInvariant() : "." + ext.ToLowerInvariant();
    }

    private async Task<bool> TryApplyLocalViewerUrlAsync(PartViewModel part)
    {
        var viewerExtension = await ResolveBrowserFileViewerExtensionAsync(part);
        if (viewerExtension is null || string.IsNullOrWhiteSpace(part.ClientUploadId))
            return false;

        try
        {
            var objectUrl = await JS.InvokeAsync<string?>(
                "window.projectNewUploads.getObjectUrl",
                part.ClientUploadId);
            if (string.IsNullOrWhiteSpace(objectUrl))
                return false;

            part.ViewerUrl = objectUrl;
            part.ViewerStoragePath ??= part.StoragePath;
            part.ViewerFileExtension = viewerExtension;
            return true;
        }
        catch (JSException ex)
        {
            Logger.LogDebug(ex, "Browser local viewer URL was not available for part {PartName}", part.Name);
            return false;
        }
    }

    private bool CanUseBrowserFileViewer(PartViewModel part)
        => ResolveBrowserFileViewerExtension(part, _runtimeBrowserViewerExtensions ?? DefaultBrowserViewerExtensions) is not null;

    private bool ShouldRetainBrowserUploadFile(PartViewModel part) =>
        !string.IsNullOrWhiteSpace(part.ClientUploadId)
        && !string.IsNullOrWhiteSpace(part.StoragePath)
        && string.IsNullOrWhiteSpace(part.Error)
        && CanUseBrowserFileViewer(part);

    private async Task ClearBrowserUploadFileAsync(PartViewModel part, bool force = false)
    {
        if (string.IsNullOrWhiteSpace(part.ClientUploadId))
            return;

        if (!force && _parts.Any(existing =>
            !ReferenceEquals(existing, part)
            && string.Equals(existing.ClientUploadId, part.ClientUploadId, StringComparison.Ordinal)))
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("window.projectNewUploads.clearFile", part.ClientUploadId);
        }
        catch (JSDisconnectedException)
        {
            // Page teardown can race with browser-side cleanup.
        }
        catch (JSException ex)
        {
            Logger.LogDebug(ex, "Failed to clear retained browser upload file reference.");
        }
    }

    private async Task<string?> ResolveBrowserFileViewerExtensionAsync(PartViewModel part)
    {
        var extensions = await GetRuntimeBrowserViewerExtensionsAsync();
        return ResolveBrowserFileViewerExtension(part, extensions ?? DefaultBrowserViewerExtensions);
    }

    private async Task<HashSet<string>?> GetRuntimeBrowserViewerExtensionsAsync()
    {
        if (_runtimeBrowserViewerExtensions is { Count: > 0 })
            return _runtimeBrowserViewerExtensions;

        _runtimeBrowserViewerExtensionsTask ??= FetchRuntimeBrowserViewerExtensionsAsync();
        var extensions = await _runtimeBrowserViewerExtensionsTask;
        if (extensions is { Count: > 0 })
        {
            _runtimeBrowserViewerExtensions = extensions;
        }
        else
        {
            _runtimeBrowserViewerExtensionsTask = null;
        }

        return extensions;
    }

    private async Task<HashSet<string>?> FetchRuntimeBrowserViewerExtensionsAsync()
    {
        try
        {
            using var response = await Http.GetAsync("api/v1/geometry/runtime/manifest");
            if (!response.IsSuccessStatusCode)
                return null;

            using var manifest = await response.Content.ReadFromJsonAsync<JsonDocument>();
            if (manifest is null)
                return null;

            _browserPrimaryServerDfmFallbackEnabled =
                ReadInteractiveServerDfmFallbackForBrowserPrimaryUploads(manifest.RootElement);
            return ReadDirectBrowserViewerExtensions(manifest.RootElement);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or OperationCanceledException)
        {
            Logger.LogDebug(ex, "Could not load browser geometry runtime artifact policy.");
            return null;
        }
    }

    private static HashSet<string>? ReadDirectBrowserViewerExtensions(JsonElement root)
    {
        if (!root.TryGetProperty("artifactPolicy", out var artifactPolicy)
            || !artifactPolicy.TryGetProperty("directBrowserViewerExtensions", out var extensions)
            || extensions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in extensions.EnumerateArray())
        {
            if (extension.ValueKind != JsonValueKind.String)
                continue;

            var normalized = NormalizeViewerFileExtension(extension.GetString(), null);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                result.Add(normalized);
            }
        }

        return result.Count > 0 ? result : null;
    }

    private static bool? ReadInteractiveServerDfmFallbackForBrowserPrimaryUploads(JsonElement root)
    {
        if (root.TryGetProperty("fallbackPolicy", out var fallbackPolicy)
            && fallbackPolicy.TryGetProperty("interactiveServerDfmFallbackForBrowserPrimaryUploads", out var enabled)
            && enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return enabled.GetBoolean();
        }

        return null;
    }

    private static bool IsBrowserPrimaryDfmPart(PartViewModel part, ISet<string> browserViewerExtensions)
        => !string.IsNullOrWhiteSpace(part.ClientUploadId)
        && ResolveBrowserFileViewerExtension(part, browserViewerExtensions) is not null;

    private static string? ResolveBrowserFileViewerExtension(PartViewModel part, ISet<string> browserViewerExtensions)
    {
        var ext = NormalizeViewerFileExtension(null, part.StoragePath ?? part.Name);
        return !string.IsNullOrWhiteSpace(ext) && browserViewerExtensions.Contains(ext) ? ext : null;
    }

    private static void ApplyMigratedStoragePaths(PartViewModel part, string oldBasePath, string newBasePath)
    {
        part.ThumbnailSmallGcsPath = RewriteMigratedStoragePath(part.ThumbnailSmallGcsPath, oldBasePath, newBasePath);
        part.ThumbnailLargeGcsPath = RewriteMigratedStoragePath(part.ThumbnailLargeGcsPath, oldBasePath, newBasePath);

        var rewrittenGlbStoragePath = RewriteMigratedStoragePath(part.GlbStoragePath, oldBasePath, newBasePath);
        if (string.IsNullOrWhiteSpace(rewrittenGlbStoragePath)
            && (!string.IsNullOrWhiteSpace(part.GlbSignedUrl) || !string.IsNullOrWhiteSpace(part.ViewerUrl)))
        {
            rewrittenGlbStoragePath = BuildViewerGlbStoragePath(newBasePath);
        }

        if (!string.Equals(part.GlbStoragePath, rewrittenGlbStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            part.GlbStoragePath = rewrittenGlbStoragePath;
            part.GlbSignedUrl = null;
            part.ViewerUrl = null;
        }

        var rewrittenViewerStoragePath = RewriteMigratedStoragePath(part.ViewerStoragePath, oldBasePath, newBasePath);
        if (string.IsNullOrWhiteSpace(rewrittenViewerStoragePath)
            && string.Equals(part.ViewerFileExtension, ".glb", StringComparison.OrdinalIgnoreCase))
        {
            rewrittenViewerStoragePath = rewrittenGlbStoragePath;
        }
        else if (string.IsNullOrWhiteSpace(rewrittenViewerStoragePath)
            && (!string.IsNullOrWhiteSpace(part.GlbSignedUrl) || !string.IsNullOrWhiteSpace(part.ViewerUrl)))
        {
            rewrittenViewerStoragePath = newBasePath;
        }

        if (!string.Equals(part.ViewerStoragePath, rewrittenViewerStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            part.ViewerStoragePath = rewrittenViewerStoragePath;
            part.ViewerFileExtension = NormalizeViewerFileExtension(part.ViewerFileExtension, rewrittenViewerStoragePath);
            part.GlbSignedUrl = null;
            part.ViewerUrl = null;
        }

        if (part.OverlayPaths is { Count: > 0 })
        {
            part.OverlayPaths = part.OverlayPaths.ToDictionary(
                item => item.Key,
                item => RewriteMigratedStoragePath(item.Value, oldBasePath, newBasePath) ?? item.Value,
                StringComparer.OrdinalIgnoreCase);
            part.OverlayUrls = null;
        }
    }

    /// <summary>
    /// Refreshes the 3D viewer signed URL when the current one has expired.
    /// Called by PartDetailCard when BabylonJS viewer fails to load.
    /// </summary>
    private async Task RequestFreshViewerUrlAsync(string storagePath)
    {
        var part = _parts.FirstOrDefault(p =>
            p.MatchesSourceStoragePath(storagePath)
            || string.Equals(p.GlbStoragePath, storagePath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.ViewerStoragePath, storagePath, StringComparison.OrdinalIgnoreCase));
        if (part == null)
        {
            Snackbar.Add("Part not found for URL refresh.", Severity.Warning);
            return;
        }

        try
        {
            var refreshStoragePath = part.StoragePath ?? storagePath;
            var viewerResp = await Http.GetAsync(
                $"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(refreshStoragePath)}");
            if (viewerResp.IsSuccessStatusCode)
            {
                var viewerJson = await viewerResp.Content.ReadFromJsonAsync<JsonDocument>();
                ApplyViewerUrlDocument(part, viewerJson);
                if (!string.IsNullOrEmpty(part.ViewerUrl))
                {
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
        await LeavePartFileGroupsAsync(part);
        StopStatusWatchdogs(part);

        // Cascade delete all attachment files before removing the part
        foreach (var att in part.DrawingFiles.Concat(part.SupplementaryFiles))
        {
            try { await Http.DeleteAsync($"api/v1/uploads/attachments/{att.FileId}"); } catch { /* non-fatal */ }
        }

        // Delete from server if the project has been cloud-saved
        if (_serverProjectId.HasValue && part.ServerPartId.HasValue)
        {
            try { await Http.DeleteAsync($"api/v1/projects/{_serverProjectId}/parts/{part.ServerPartId.Value}"); } catch { /* non-fatal */ }
        }

        _parts.Remove(part);
        _bulkSelectedParts.Remove(part);
        PruneBulkSelection();
        await ClearBrowserUploadFileAsync(part);

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
        StateHasChanged();
    }

    private void SetLayoutMode(LayoutMode mode)
    {
        _layoutMode = mode;
        PruneBulkSelection();
    }

    private Task SetLayoutModeAsync(LayoutMode mode)
    {
        SetLayoutMode(mode);
        return Task.CompletedTask;
    }

    private void ToggleLayoutMode() =>
        SetLayoutMode(_layoutMode == LayoutMode.SummaryTable ? LayoutMode.Configurator : LayoutMode.SummaryTable);

    private Task OnBulkSelectionChanged(IReadOnlyCollection<PartViewModel> selectedParts)
    {
        _bulkSelectedParts.Clear();
        foreach (var part in selectedParts.Where(_parts.Contains))
            _bulkSelectedParts.Add(part);

        return Task.CompletedTask;
    }

    private void PruneBulkSelection() =>
        _bulkSelectedParts.RemoveWhere(part => !_parts.Contains(part));

    private async Task OnBulkTableProcessChanged(ProjectPartProcessChange change)
    {
        if (!_parts.Contains(change.Part))
            return;

        var changed = ProjectPartBulkEdit.ApplyProcess(change.Part, change.Process);
        if (!changed)
            return;

        await OnPartChanged(change.Part);

        if (change.Process != null)
            _ = RunProcessDfmAnalysisAsync(change.Part, change.Process);
    }

    private async Task ApplyBulkEditAsync(ProjectPartsBulkApplyRequest request)
    {
        var targets = new List<PartViewModel>();
        foreach (var part in request.Parts)
        {
            if (_parts.Contains(part) && targets.All(existing => !ReferenceEquals(existing, part)))
                targets.Add(part);
        }
        if (targets.Count == 0)
            return;

        var appliedParts = 0;
        var skippedFields = 0;

        foreach (var part in targets)
        {
            var partApplied = false;

            if (request.Patch.IncludeProcess)
            {
                if (request.Patch.Process == null)
                {
                    skippedFields++;
                }
                else
                {
                    var processChanged = ProjectPartBulkEdit.ApplyProcess(part, request.Patch.Process);
                    if (processChanged)
                    {
                        partApplied = true;
                        await OnPartChanged(part);
                        _ = RunProcessDfmAnalysisAsync(part, request.Patch.Process);
                    }
                    else if (part.DfmAnalysisTimedOut || part.DfmReport == null)
                    {
                        _ = RunProcessDfmAnalysisAsync(part, request.Patch.Process);
                    }
                }
            }

            var patchWithoutProcess = CopyPatchWithoutProcess(request.Patch);
            var result = ProjectPartBulkEdit.ApplyPatch(part, patchWithoutProcess);
            skippedFields += result.SkippedFields.Count;

            if (result.AppliedFields.Count > 0)
            {
                partApplied = true;
                await OnPartChanged(part);
            }

            if (partApplied)
                appliedParts++;
        }

        if (request.ShowSummary)
        {
            var skippedText = skippedFields == 0 ? string.Empty : $" {skippedFields} incompatible field update(s) skipped.";
            Snackbar.Add($"Bulk edit applied to {appliedParts} of {targets.Count} part(s).{skippedText}", Severity.Info);
        }
    }

    private static PartConfigurationBulkPatch CopyPatchWithoutProcess(PartConfigurationBulkPatch patch) => new()
    {
        IncludeMaterial = patch.IncludeMaterial,
        Material = patch.Material,
        IncludeFinish = patch.IncludeFinish,
        Finish = patch.Finish,
        IncludeTolerance = patch.IncludeTolerance,
        Tolerance = patch.Tolerance,
        IncludeQuantity = patch.IncludeQuantity,
        Quantity = patch.Quantity,
        IncludeInspection = patch.IncludeInspection,
        InspectionLevel = patch.InspectionLevel,
        IncludeRoughness = patch.IncludeRoughness,
        RoughnessCode = patch.RoughnessCode,
        IncludeThreadedHoles = patch.IncludeThreadedHoles,
        HasThreadedHoles = patch.HasThreadedHoles,
        IncludeInserts = patch.IncludeInserts,
        HasInserts = patch.HasInserts,
        IncludeBagAndTag = patch.IncludeBagAndTag,
        BagAndTag = patch.BagAndTag,
        IncludePartNotes = patch.IncludePartNotes,
        PartNotes = patch.PartNotes,
        IncludeProcessOptions = patch.IncludeProcessOptions,
        ProcessOptionValues = new Dictionary<string, string?>(patch.ProcessOptionValues, StringComparer.OrdinalIgnoreCase),
    };

    // ── Task 12: Cascading dropdowns ──────────────────────────────────

    /// <inheritdoc />
    private async Task OnPartChanged(PartViewModel part)
    {
        NormalizeSelectedMaterialForClearPetg(part);

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

                NormalizeSelectedMaterialForClearPetg(part);
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

    private static void NormalizeSelectedMaterialForClearPetg(PartViewModel part)
    {
        if (!part.MaterialId.HasValue)
            return;

        var selectedMaterial = part.AvailableMaterials.FirstOrDefault(material => material.Id == part.MaterialId.Value);
        if (selectedMaterial == null || !IsClearPetgMaterial(selectedMaterial))
            return;

        var basePetgMaterial = part.AvailableMaterials
            .FirstOrDefault(material => IsPetgMaterial(material) && !IsClearPetgMaterial(material));

        if (basePetgMaterial == null)
        {
            part.MaterialCode = "PETG";
            part.MaterialId = selectedMaterial.Id;
            part.ProcessOptionValues[MaterialColorKey] = "Clear";
            return;
        }

        part.MaterialId = basePetgMaterial.Id;
        part.MaterialCode = basePetgMaterial.Code;
        part.ProcessOptionValues[MaterialColorKey] = "Clear";
    }

    private static bool IsClearPetgMaterial(CatalogMaterialDto material)
    {
        var normalized = NormalizeMaterialText(material.Code, material.Name, material.Description);
        return normalized.Contains("petg", StringComparison.Ordinal) && normalized.Contains("clear", StringComparison.Ordinal);
    }

    private static bool IsPetgMaterial(CatalogMaterialDto material)
    {
        var normalized = NormalizeMaterialText(material.Code, material.Name, material.Description);
        return normalized.Contains("petg", StringComparison.Ordinal)
            && !normalized.Contains("clear", StringComparison.Ordinal)
            && !normalized.Contains("transp", StringComparison.Ordinal)
            && !normalized.Contains("transparent", StringComparison.Ordinal);
    }

    private static string NormalizeMaterialText(params string?[] values)
    {
        var combined = string.Concat(values.Where(value => !string.IsNullOrWhiteSpace(value)));
        return new string(combined.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
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
        if (string.Equals(processCode, "FDM", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "FDM_3D_PRINTING", StringComparison.OrdinalIgnoreCase))
        {
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

        if (IsAdditiveManufacturingProcessCode(processCode))
        {
            return tolerances.Where(t => IsAdditiveProcessSpecificTolerance(processCode, t));
        }

        return tolerances;
    }

    private static bool IsAdditiveManufacturingProcessCode(string? processCode)
    {
        if (string.IsNullOrEmpty(processCode))
        {
            return false;
        }

        return string.Equals(processCode, "MJF", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "SLS", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "SLA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "SLA_DLP", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "DLP", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "MJ", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "MATERIAL_JETTING", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "BJ", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "BINDER_JETTING", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "DMLS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdditiveProcessSpecificTolerance(string? processCode, CatalogToleranceDto tolerance)
    {
        var combined = $"{tolerance.Code} {tolerance.Name} {tolerance.IsoStandard} {tolerance.Grade}";
        var normalized = NormalizeForComparison(combined);

        if (string.Equals(processCode, "MJF", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("mjfstandard") || normalized.Contains("mjf_standard");
        }

        if (string.Equals(processCode, "SLS", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("slsstandard") || normalized.Contains("sls_standard");
        }

        if (string.Equals(processCode, "SLA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "SLA_DLP", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "DLP", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("slastandard") || normalized.Contains("sla_standard")
                || normalized.Contains("dlpstandard") || normalized.Contains("dlp_standard");
        }

        if (string.Equals(processCode, "DMLS", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("dmlsstandard") || normalized.Contains("dmls_standard");
        }

        if (string.Equals(processCode, "MJ", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "MATERIAL_JETTING", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("mjstandard") || normalized.Contains("mj_standard")
                || normalized.Contains("materialjetstandard") || normalized.Contains("material_jet_standard");
        }

        if (string.Equals(processCode, "BJ", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processCode, "BINDER_JETTING", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("bjstandard") || normalized.Contains("bj_standard")
                || normalized.Contains("binderjettingstandard") || normalized.Contains("binder_jetting_standard");
        }

        return false;
    }

    private static string NormalizeForComparison(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
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

        // Wait for geometry analysis to complete (VolumeMm3 available) before pricing.
        // For STEP/IGES files, analysis happens asynchronously on the server.
        if (!part.VolumeMm3.HasValue)
        {
            // Wait up to 30 seconds for geometry analysis to complete
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            while (!part.VolumeMm3.HasValue && DateTime.UtcNow - startTime < timeout && !ct.IsCancellationRequested)
            {
                await Task.Delay(500, ct);
            }

            // If still no geometry data after timeout, skip pricing for now
            // It will be retried when SignalR analysis status updates arrive
            if (!part.VolumeMm3.HasValue)
            {
                part.PricingLoading = false;
                part.PricingFailed = false;
                RefreshLeadTimeOptionsFromPricing();
                return;
            }
        }

        part.PricingLoading = true;
        part.PricingFailed = false;

        try
        {
            var geometry = new GeometryMetricsDto
            {
                VolumeCm3 = (decimal)part.VolumeMm3.Value / 1_000m,
                SupportVolumeCm3 = 0m,
                SurfaceAreaCm2 = 0m,
                BoundingBoxX = (decimal)(part.Dimensions?.X ?? 0),
                BoundingBoxY = (decimal)(part.Dimensions?.Y ?? 0),
                BoundingBoxZ = (decimal)(part.Dimensions?.Z ?? 0),
                IsManifold = part.IsManifold ?? false,
                TriangleCount = 0,
            };

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
                Geometry = geometry,
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
                    part.EstimatedBaseUnitPrice = result.UnitPriceBeforeVolumeDiscount is > 0m
                        ? result.UnitPriceBeforeVolumeDiscount
                        : result.TotalUnitPrice;
                    part.EstimatedDiscountedUnitPriceBeforeFinish = result.UnitPriceBeforeFinish ?? result.TotalUnitPrice;
                    part.FinishPricingBaseUnitPrice = result.UnitPriceBeforeFinish ?? result.TotalUnitPrice;
                    part.FinishAdditionalUnitCost = result.FinishAdditionalUnitCost;
                    part.EstimatedLeadTimeDays = result.EstimatedLeadTimeDays ?? 0;
                    part.PricingFailed = false;
                }
            }
            else
            {
                part.PricingFailed = true;
                part.EstimatedUnitPrice = null;
                part.EstimatedTotalAmount = null;
                part.EstimatedBaseUnitPrice = null;
                part.EstimatedDiscountedUnitPriceBeforeFinish = null;
                part.FinishPricingBaseUnitPrice = null;
                part.FinishAdditionalUnitCost = null;
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
    /// True after this editor has just generated a quotation and is navigating away.
    /// Reopened quoted projects are editable again and should be repriced when inputs change.
    /// </summary>
    private bool _projectLocked;

    private AddProjectPartRequest? BuildAddProjectPartRequest(PartViewModel part)
    {
        var processCode = ResolvePartProcessCode(part);
        if (string.IsNullOrWhiteSpace(processCode))
            return null;

        part.ProcessCode ??= processCode;

        return new AddProjectPartRequest
        {
            FileId = part.FileId,
            FileReference = part.StoragePath,
            FileName = part.Name,
            ProcessType = processCode,
            MaterialId = part.MaterialId,
            MaterialName = ResolvePartMaterialName(part),
            MaterialCode = ResolvePartMaterialCode(part),
            Quantity = part.Quantity,
            Finish = part.FinishCode,
            Color = ResolvePartColor(part),
            Tolerance = part.ToleranceCode,
            PartNotes = part.PartNotes,
            ThumbnailSmallGcsPath = part.ThumbnailSmallGcsPath,
            ThumbnailLargeGcsPath = part.ThumbnailLargeGcsPath,
            GlbStoragePath = part.GlbStoragePath,
            OverlayPaths = part.OverlayPaths is null ? [] : new Dictionary<string, string>(part.OverlayPaths),
            RoughnessCode = part.RoughnessCode,
            MarkingType = part.MarkingType,
            MarkingText = part.MarkingText,
            HasDfmWarnings = part.HasProcessRelevantDfmIssues,
            DfmAcknowledged = part.DfmAcknowledged,
            HasThreadedHoles = part.HasThreadedHoles,
            ThreadedHoleSpec = part.ThreadedHoleSpec,
            ThreadedHoleCount = part.ThreadedHoleCount,
            HasInserts = part.HasInserts,
            InsertType = part.InsertType,
            InsertCount = part.InsertCount,
            BagAndTag = part.BagAndTag,
            InspectionLevel = part.InspectionLevel,
            Certificates = [.. part.Certificates],
            DrawingFiles = ToProjectPartAttachments(part.DrawingFiles),
            SupplementaryFiles = ToProjectPartAttachments(part.SupplementaryFiles),
            ProcessConfig = BuildProcessConfig(part),
            BodyCount = part.BodyCount,
            BodiesJson = part.Bodies.Count > 0 ? JsonSerializer.Serialize(part.Bodies) : null,
            SelectedBodyIndex = part.SelectedBodyIndex,
            VolumeCm3 = part.VolumeMm3.HasValue ? (decimal)part.VolumeMm3.Value / 1_000m : null,
            BoundingBoxX = part.Dimensions is null ? null : (decimal)part.Dimensions.X,
            BoundingBoxY = part.Dimensions is null ? null : (decimal)part.Dimensions.Y,
            BoundingBoxZ = part.Dimensions is null ? null : (decimal)part.Dimensions.Z,
            IsManifold = part.IsManifold,
        };
    }

    private UpdateProjectPartRequest? BuildUpdateProjectPartRequest(PartViewModel part)
    {
        var processCode = ResolvePartProcessCode(part);
        if (string.IsNullOrWhiteSpace(processCode))
            return null;

        part.ProcessCode ??= processCode;

        return new UpdateProjectPartRequest
        {
            ProcessType = processCode,
            MaterialId = part.MaterialId,
            MaterialName = ResolvePartMaterialName(part),
            MaterialCode = ResolvePartMaterialCode(part),
            Quantity = part.Quantity,
            Finish = part.FinishCode,
            Color = ResolvePartColor(part),
            Tolerance = part.ToleranceCode,
            PartNotes = part.PartNotes,
            ThumbnailSmallGcsPath = part.ThumbnailSmallGcsPath,
            ThumbnailLargeGcsPath = part.ThumbnailLargeGcsPath,
            GlbStoragePath = part.GlbStoragePath,
            OverlayPaths = part.OverlayPaths is null ? [] : new Dictionary<string, string>(part.OverlayPaths),
            RoughnessCode = part.RoughnessCode,
            MarkingType = part.MarkingType,
            MarkingText = part.MarkingText,
            HasDfmWarnings = part.HasProcessRelevantDfmIssues,
            DfmAcknowledged = part.DfmAcknowledged,
            HasThreadedHoles = part.HasThreadedHoles,
            ThreadedHoleSpec = part.ThreadedHoleSpec,
            ThreadedHoleCount = part.ThreadedHoleCount,
            HasInserts = part.HasInserts,
            InsertType = part.InsertType,
            InsertCount = part.InsertCount,
            BagAndTag = part.BagAndTag,
            InspectionLevel = part.InspectionLevel,
            Certificates = [.. part.Certificates],
            DrawingFiles = ToProjectPartAttachments(part.DrawingFiles),
            SupplementaryFiles = ToProjectPartAttachments(part.SupplementaryFiles),
            ProcessConfig = BuildProcessConfig(part),
            BodyCount = part.BodyCount,
            BodiesJson = part.Bodies.Count > 0 ? JsonSerializer.Serialize(part.Bodies) : null,
            SelectedBodyIndex = part.SelectedBodyIndex,
        };
    }

    private static Dictionary<string, string> BuildProcessConfig(PartViewModel part) =>
        part.ProcessOptionValues
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value!, StringComparer.Ordinal);

    private static List<ProjectPartAttachmentDto> ToProjectPartAttachments(IEnumerable<DraftProjectAttachmentDto> attachments) =>
        attachments.Select(attachment => new ProjectPartAttachmentDto
        {
            FileId = attachment.FileId,
            FileName = attachment.Name,
            StoragePath = attachment.StoragePath,
            SizeBytes = attachment.FileSizeBytes,
            ContentType = attachment.FileType,
            UploadedAt = attachment.UploadedAt,
        }).ToList();

    private string? ResolvePartProcessCode(PartViewModel part)
    {
        if (!string.IsNullOrWhiteSpace(part.ProcessCode))
            return part.ProcessCode;

        return part.ProcessId.HasValue
            ? _processes.FirstOrDefault(process => process.Id == part.ProcessId.Value)?.Code
            : null;
    }

    private static string? ResolvePartMaterialName(PartViewModel part)
    {
        if (part.MaterialId.HasValue)
        {
            var material = part.AvailableMaterials.FirstOrDefault(item => item.Id == part.MaterialId.Value);
            if (!string.IsNullOrWhiteSpace(material?.Name))
                return material.Name;
        }

        return part.MaterialCode;
    }

    private static string? ResolvePartMaterialCode(PartViewModel part)
    {
        if (!string.IsNullOrWhiteSpace(part.MaterialCode))
            return part.MaterialCode;

        return part.MaterialId.HasValue
            ? part.AvailableMaterials.FirstOrDefault(item => item.Id == part.MaterialId.Value)?.Code
            : null;
    }

    private static string? ResolvePartColor(PartViewModel part)
    {
        foreach (var key in new[] { "paint_color_reference", "paint_color", "paint_colour", "material_color", "material_colour", "plastic_color", "plastic_colour", "anodize_color", "anodise_color" })
        {
            if (part.ProcessOptionValues.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return part.ProcessOptionValues.TryGetValue("paint_color_hex", out var hex) && !string.IsNullOrWhiteSpace(hex)
            ? hex
            : null;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private async Task SaveDraftAsync()
    {
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
                CustomerProfileImageUrl = _selectedCustomer?.ProfileImageUrl,
                CustomerMobile = _selectedCustomer?.Mobile,
                CustomerLandline = _selectedCustomer?.Landline,
                CustomerCompanyPhone = _selectedCustomer?.CompanyPhone,
                SelectedLeadTimeCode = _selectedLeadTime?.Code ?? "STANDARD",
                SelectedCurrencyCode = CurrencyService.Code,
                ShippingCost = _shippingCost,
                ManualDiscountAmount = _manualDiscountAmount,
                QuotationTerms = _quotationTerms,
                LastModified = DateTime.UtcNow,
                Parts = _parts.Select(p => p.ToDraftPartState()).ToList(),
            };

            var json = JsonSerializer.Serialize(draft);
            await JS.InvokeVoidAsync("sessionStorage.setItem", DraftStorageKey, json);
            _lastSavedAt = DateTimeOffset.UtcNow;

            if (_selectedCustomerId.HasValue)
            {
                if (_serverSaveInProgress || _storageMigrationInProgress)
                {
                    _serverSavePending = true;
                }
                else
                {
                    _ = SaveDraftToServerAsync();
                }
            }
        }
        catch (Exception)
        {
            Snackbar.Add("Auto-save failed.", Severity.Warning);
        }
        finally
        {
            if (!_serverSaveInProgress && !_serverSavePending)
            {
                StateHasChanged();
            }
        }
    }

    private async Task<bool> SaveDraftToServerAsync()
    {
        _serverSaveInProgress = true;
        var saved = true;
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

                        foreach (var part in _parts.Where(p => p.IsFullyConfigured && !string.IsNullOrEmpty(p.StoragePath)))
                        {
                            try
                            {
                                var addPartRequest = BuildAddProjectPartRequest(part);
                                if (addPartRequest == null)
                                    continue;

                                var partResponse = await Http.PostAsJsonAsync($"api/v1/projects/{_serverProjectId}/parts", addPartRequest);
                                if (partResponse.IsSuccessStatusCode)
                                {
                                    var createdPart = await partResponse.Content.ReadFromJsonAsync<ProjectPartDto>();
                                    if (createdPart != null)
                                    {
                                        part.ServerPartId = createdPart.Id;
                                    }
                                    else
                                    {
                                        saved = false;
                                    }
                                }
                                else
                                {
                                    saved = false;
                                }
                            }
                            catch
                            {
                                // Non-fatal: part sync will retry on next auto-save
                                saved = false;
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
                            CustomerProfileImageUrl = _selectedCustomer?.ProfileImageUrl,
                            CustomerMobile = _selectedCustomer?.Mobile,
                            CustomerLandline = _selectedCustomer?.Landline,
                            CustomerCompanyPhone = _selectedCustomer?.CompanyPhone,
                            SelectedLeadTimeCode = _selectedLeadTime?.Code ?? "STANDARD",
                            SelectedCurrencyCode = CurrencyService.Code,
                            ShippingCost = _shippingCost,
                            ManualDiscountAmount = _manualDiscountAmount,
                            QuotationTerms = _quotationTerms,
                            LastModified = DateTime.UtcNow,
                            Parts = _parts.Select(p => p.ToDraftPartState()).ToList(),
                        };
                        var updatedJson = JsonSerializer.Serialize(updatedDraft);
                        await JS.InvokeVoidAsync("sessionStorage.setItem", DraftStorageKey, updatedJson);
                        _lastSavedAt = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        saved = false;
                    }
                }
                else
                {
                    saved = false;
                }
            }
            else
            {
                var projectUpdateResponse = await Http.PutAsJsonAsync($"api/v1/projects/{_serverProjectId}", new { Title = _title });
                if (!projectUpdateResponse.IsSuccessStatusCode)
                    saved = false;

                foreach (var part in _parts.Where(p => p.IsFullyConfigured))
                {
                    try
                    {
                        // Parts not yet synced to the server need to be created first
                        if (!part.ServerPartId.HasValue)
                        {
                            var addPartRequest = BuildAddProjectPartRequest(part);
                            if (addPartRequest == null)
                                continue;

                            var partResponse = await Http.PostAsJsonAsync($"api/v1/projects/{_serverProjectId}/parts", addPartRequest);
                            if (partResponse.IsSuccessStatusCode)
                            {
                                var createdPart = await partResponse.Content.ReadFromJsonAsync<ProjectPartDto>();
                                if (createdPart != null)
                                {
                                    part.ServerPartId = createdPart.Id;
                                }
                                else
                                {
                                    saved = false;
                                }
                            }
                            else
                            {
                                saved = false;
                            }

                            continue;
                        }

                        var partRequest = BuildUpdateProjectPartRequest(part);
                        if (partRequest == null)
                            continue;

                        var partUpdateResponse = await Http.PutAsJsonAsync(
                            $"api/v1/projects/{_serverProjectId}/parts/{part.ServerPartId}",
                            partRequest);
                        if (!partUpdateResponse.IsSuccessStatusCode)
                            saved = false;
                    }
                    catch
                    {
                        // Non-fatal: part update will retry on next auto-save
                        saved = false;
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
            saved = false;
        }
finally
            {
                _serverSaveInProgress = false;

                if (_serverSavePending && _selectedCustomerId.HasValue && !_storageMigrationInProgress)
            {
                _serverSavePending = false;
                _ = InvokeAsync(SaveDraftAsync);
            }
            else
            {
                _ = InvokeAsync(StateHasChanged);
            }
        }

        return saved;
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
            _shippingCost = Math.Max(0m, draft.ShippingCost);
            _manualDiscountAmount = Math.Max(0m, draft.ManualDiscountAmount);
            _quotationTerms = draft.QuotationTerms;
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
                    ProfileImageUrl = draft.CustomerProfileImageUrl,
                    Mobile = draft.CustomerMobile,
                    Landline = draft.CustomerLandline,
                    CompanyPhone = draft.CustomerCompanyPhone,
                };

                if (string.IsNullOrWhiteSpace(_selectedCustomer.ProfileImageUrl))
                {
                    _selectedCustomer = await TryLoadCustomerSummaryAsync(_selectedCustomer.Id) ?? _selectedCustomer;
                }
            }

            _parts.Clear();
            _bulkSelectedParts.Clear();
            foreach (var partState in draft.Parts)
            {
                var partVm = PartViewModel.FromDraftPartState(partState);
                NormalizePartProcessSelection(partVm);
                if (!string.IsNullOrEmpty(partVm.ProcessCode))
                    _ = ReloadPartCatalogAsync(partVm);

                // Catch-up fetch for any part with a storage path — refreshes thumbnail signed URLs,
                // restores DFM results from BFF cache, and repopulates GlbStoragePath.
                if (!string.IsNullOrEmpty(partVm.StoragePath))
                {
                    var p = partVm;
                    foreach (var path in partVm.GetSignalRStoragePaths())
                    {
                        var statusPath = path;
                        _ = Task.Run(async () => { await Task.Delay(CatchUpDelayMs); await FetchCurrentStatusAsync(p, statusPath); });
                    }
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
            if (!CanResumeProjectForEditing(project.Status)) return;

            await ApplyProjectDetailAsync(project);
            Snackbar.Add("Draft restored from server.", Severity.Info);
        }
        catch (Exception)
        {
            // Server resume failures are non-fatal; sessionStorage draft is the fallback
        }
    }

    private async Task ApplyProjectDetailAsync(ProjectDetailDto project)
    {
        _serverProjectId = project.Id;
        _tempProjectId = ResolveStorageProjectId(project.Parts.Select(p => p.FileReference), project.Id);
        _title = project.Title;
        _parts.Clear();
        _bulkSelectedParts.Clear();

        if (!string.IsNullOrEmpty(project.Currency))
        {
            var c = CurrencyService.Currencies.FirstOrDefault(x => x.Code == project.Currency);
            if (c != null) await CurrencyService.SetCurrencyAsync(c);
        }

        await RestoreQuotationCommercialFieldsAsync(project.QuotationId);

        _selectedCustomer = await TryLoadCustomerSummaryAsync(project.CustomerId) ?? new CustomerSummaryDto
        {
            Id = project.CustomerId,
            Name = project.CustomerName,
        };

        var catalogReloadTasks = new List<Task>();
        foreach (var part in project.Parts.Where(part => !string.IsNullOrEmpty(part.FileName)))
        {
            var partVm = CreatePartViewModelFromProjectPart(part);
            _parts.Add(partVm);

            if (!string.IsNullOrEmpty(partVm.ProcessCode))
                catalogReloadTasks.Add(ReloadPartCatalogAsync(partVm));

            ScheduleStatusCatchUp(partVm);
        }

        if (catalogReloadTasks.Count > 0)
            await Task.WhenAll(catalogReloadTasks);

        _selectedPartIndex = 0;
        await SaveDraftAsync();
        await InvokeAsync(StateHasChanged);
    }

    private PartViewModel CreatePartViewModelFromProjectPart(ProjectPartDto part)
    {
        var unitPrice = part.ConfirmedPrice ?? part.ConfirmedUnitPrice ?? part.EstimatedPrice ?? part.AiSuggestedPrice;
        var bodies = DeserializeBodies(part.BodiesJson);
        var process = FindProcessByCodeOrAlias(part.ProcessType);
        var processCode = process?.Code ?? ProcessCodeNormalizer.Normalize(part.ProcessType) ?? part.ProcessType;

        var partVm = new PartViewModel
        {
            FileId = part.FileId,
            ServerPartId = part.Id,
            Name = part.FileName,
            StoragePath = part.FileReference,
            ProcessCode = processCode,
            ProcessId = process?.Id,
            MaterialId = part.MaterialId,
            MaterialCode = part.MaterialCode,
            Quantity = part.Quantity,
            FinishCode = part.Finish,
            ToleranceCode = part.Tolerance,
            PartNotes = part.PartNotes,
            EstimatedUnitPrice = unitPrice,
            EstimatedTotalAmount = unitPrice * Math.Max(part.Quantity, 1),
            EstimatedDiscountedUnitPriceBeforeFinish = unitPrice,
            FinishPricingBaseUnitPrice = unitPrice,
            Dimensions = part.Dimensions is null
                ? null
                : new FileAnalysisDimensionsDto
                {
                    X = part.Dimensions.X,
                    Y = part.Dimensions.Y,
                    Z = part.Dimensions.Z,
                },
            IsManifold = part.IsManifold,
            ThumbnailSmallUrl = part.ThumbnailUrl,
            ThumbnailSmallGcsPath = part.ThumbnailSmallGcsPath,
            ThumbnailLargeGcsPath = part.ThumbnailLargeGcsPath,
            GlbStoragePath = part.GlbStoragePath,
            ViewerStoragePath = part.ViewerStoragePath ?? part.GlbStoragePath,
            ViewerFileExtension = NormalizeViewerFileExtension(part.ViewerFileExtension, part.ViewerStoragePath ?? part.GlbStoragePath),
            GlbSignedUrl = string.IsNullOrWhiteSpace(part.ModelPreviewUrl) ? null : part.ModelPreviewUrl,
            ViewerUrl = string.IsNullOrWhiteSpace(part.ModelPreviewUrl) ? null : part.ModelPreviewUrl,
            OverlayPaths = part.OverlayPaths.Count == 0 ? null : new Dictionary<string, string>(part.OverlayPaths),
            RoughnessCode = part.RoughnessCode,
            MarkingType = part.MarkingType,
            MarkingText = part.MarkingText,
            DfmAcknowledged = part.DfmAcknowledged,
            HasThreadedHoles = part.HasThreadedHoles,
            ThreadedHoleSpec = part.ThreadedHoleSpec,
            ThreadedHoleCount = part.ThreadedHoleCount,
            HasInserts = part.HasInserts,
            InsertType = part.InsertType,
            InsertCount = part.InsertCount,
            BagAndTag = part.BagAndTag,
            InspectionLevel = part.InspectionLevel,
            Certificates = [.. part.Certificates],
            DrawingFiles = ToDraftAttachments(part.DrawingFiles, DraftAttachmentKind.Drawing),
            SupplementaryFiles = ToDraftAttachments(part.SupplementaryFiles, DraftAttachmentKind.Supplementary),
            ProcessOptionValues = part.ProcessConfig.ToDictionary(pair => pair.Key, pair => (string?)pair.Value),
            BodyCount = part.BodyCount ?? (bodies.Count > 0 ? bodies.Count : null),
            Bodies = bodies,
            SelectedBodyIndex = part.SelectedBodyIndex,
            AwaitingPreview = false,
            StatusText = "Ready",
        };

        partVm.ResolveDfmReport();
        ApplyResumedDfmTerminalState(partVm);
        return partVm;
    }

    private ProcessDto? FindProcessByCodeOrAlias(string? processCode)
    {
        var normalizedProcessCode = ProcessCodeNormalizer.Normalize(processCode);
        if (string.IsNullOrWhiteSpace(normalizedProcessCode))
            return null;

        return _processes.FirstOrDefault(process =>
            string.Equals(process.Code, normalizedProcessCode, StringComparison.OrdinalIgnoreCase)
            || ProcessCodeNormalizer.Equals(process.Code, processCode));
    }

    private void NormalizePartProcessSelection(PartViewModel part)
    {
        var normalizedProcessCode = ProcessCodeNormalizer.Normalize(part.ProcessCode);
        if (string.IsNullOrWhiteSpace(normalizedProcessCode))
            return;

        var process = FindProcessByCodeOrAlias(normalizedProcessCode);
        part.ProcessCode = process?.Code ?? normalizedProcessCode;
        part.ProcessId ??= process?.Id;
    }

    private async Task RestoreQuotationCommercialFieldsAsync(Guid? quotationId)
    {
        if (quotationId is not Guid id)
            return;

        try
        {
            var quotation = await Http.GetFromJsonAsync<QuotationDetailDto>($"api/v1/quotations/{id}");
            var currentVersion = quotation?.Versions?
                .OrderByDescending(version => version.VersionNumber == quotation.CurrentVersionNumber)
                .ThenByDescending(version => version.VersionNumber)
                .FirstOrDefault();

            if (currentVersion is null)
                return;

            _shippingCost = Math.Max(0m, currentVersion.ShippingCost);
            _manualDiscountAmount = Math.Max(0m, currentVersion.ManualDiscountAmount);
            _quotationTerms = currentVersion.SpecialTerms;
        }
        catch
        {
            // Quotation restore is best-effort; the project draft still remains editable.
        }
    }

    private static void ApplyResumedDfmTerminalState(PartViewModel partVm)
    {
        if (partVm.DfmReport != null
            || partVm.DfmAcknowledged
            || string.IsNullOrWhiteSpace(partVm.ProcessCode)
            || !HasViewerArtifactForMigration(partVm))
        {
            return;
        }

        partVm.DfmAnalysisTimedOut = true;
        partVm.AnalysisErrorCode = DfmStatusMessages.PersistedDfmReportUnavailable;
        partVm.StatusText = DfmStatusMessages.GetStatusText(partVm.AnalysisErrorCode);
    }

    private static void ClearDfmUnavailableState(PartViewModel part)
    {
        if (!part.DfmAnalysisTimedOut && part.AnalysisErrorCode == null)
        {
            return;
        }

        part.DfmAnalysisTimedOut = false;
        part.AnalysisErrorCode = null;
    }

    private void ScheduleStatusCatchUp(PartViewModel part)
    {
        if (string.IsNullOrEmpty(part.StoragePath))
            return;

        foreach (var path in part.GetSignalRStoragePaths())
        {
            var statusPath = path;
            _ = Task.Run(async () =>
            {
                await Task.Delay(CatchUpDelayMs);
                await FetchCurrentStatusAsync(part, statusPath);
            });
        }
    }

    private static List<DraftProjectAttachmentDto> ToDraftAttachments(
        IEnumerable<ProjectPartAttachmentDto> attachments,
        DraftAttachmentKind kind) =>
        attachments.Select(attachment => new DraftProjectAttachmentDto
        {
            FileId = attachment.FileId.GetValueOrDefault(),
            StoragePath = attachment.StoragePath ?? string.Empty,
            Name = attachment.FileName,
            FileType = attachment.ContentType ?? string.Empty,
            FileSizeBytes = attachment.SizeBytes.GetValueOrDefault(),
            Kind = kind,
            UploadedAt = attachment.UploadedAt ?? DateTime.UtcNow,
        }).ToList();

    private static List<PartViewModel.BodyInfo> DeserializeBodies(string? bodiesJson)
    {
        if (string.IsNullOrWhiteSpace(bodiesJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<PartViewModel.BodyInfo>>(bodiesJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool CanResumeProjectForEditing(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "draft" or "configuring" or "priced" or "quoted" or "quotationgenerated" or "quotationsent" => true,
            _ => false,
        };
    }

    private async Task ReloadPartCatalogAsync(PartViewModel part)
    {
        NormalizePartProcessSelection(part);
        if (string.IsNullOrEmpty(part.ProcessCode))
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
            NormalizeSelectedMaterialForClearPetg(part);
            RestoreCatalogSelections(part);

            if (part.MaterialId.HasValue)
                await ComputePriceAsync(part, CancellationToken.None);
        }
        catch
        {
            // Catalog reload failures are non-fatal
        }
    }

    private static void RestoreCatalogSelections(PartViewModel part)
    {
        if (part.MaterialId.HasValue)
        {
            var material = part.AvailableMaterials.FirstOrDefault(item => item.Id == part.MaterialId.Value);
            if (material is not null)
                part.MaterialCode = material.Code;
        }

        if (!part.FinishId.HasValue && !string.IsNullOrWhiteSpace(part.FinishCode))
        {
            var finish = part.AvailableFinishes.FirstOrDefault(item =>
                item.Code.Equals(part.FinishCode, StringComparison.OrdinalIgnoreCase)
                || item.Name.Equals(part.FinishCode, StringComparison.OrdinalIgnoreCase));
            if (finish is not null)
            {
                part.FinishId = finish.Id;
                part.FinishCode = finish.Code;
            }
        }

        if (!part.ToleranceId.HasValue && !string.IsNullOrWhiteSpace(part.ToleranceCode))
        {
            var tolerance = part.AvailableTolerances.FirstOrDefault(item =>
                item.Code.Equals(part.ToleranceCode, StringComparison.OrdinalIgnoreCase)
                || item.Name.Equals(part.ToleranceCode, StringComparison.OrdinalIgnoreCase));
            if (tolerance is not null)
            {
                part.ToleranceId = tolerance.Id;
                part.ToleranceCode = tolerance.Code;
            }
        }
    }

    // ── PDF generation ─────────────────────────────────────────────────

    private async Task GenerateDraftPdfAsync()
    {
        try
        {
            var pdfData = await BuildCurrentQuotationPdfDataAsync();
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

    private async Task<QuotationPdfData> BuildCurrentQuotationPdfDataAsync()
    {
        var customerDetail = await GetDraftPdfCustomerDetailAsync();
        var nowUtc = DateTime.UtcNow;

        return ProjectQuotationPdfMapper.BuildDraftPdfData(
            _tempProjectId,
            _selectedCustomer,
            customerDetail,
            CurrencyService.Code,
            nowUtc,
            ProjectQuotationPdfMapper.BuildDeliveryExpectation(_selectedLeadTime),
            _parts,
            _processes,
            _quotationTerms,
            _shippingCost,
            _manualDiscountAmount,
            CurrencyService.ExchangeRate);
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
            var isUpdatingExistingProject = _serverProjectId.HasValue;

            if (isUpdatingExistingProject)
            {
                projectId = _serverProjectId.GetValueOrDefault();

                var updateResponse = await Http.PutAsJsonAsync($"api/v1/projects/{projectId}", new { Title = _title });
                if (!updateResponse.IsSuccessStatusCode)
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    Snackbar.Add($"Failed to update project: {errorContent}", Severity.Error);
                    return;
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
            }

            if (!await SyncProjectPartsForQuoteAsync(projectId))
                return;

            var pdfData = await BuildCurrentQuotationPdfDataAsync();

            if (!await ConfirmProjectPartPricesForQuoteAsync(projectId, pdfData))
                return;

            var quoteRequest = BuildGenerateQuotationRequest(projectId, isUpdatingExistingProject, pdfData);
            using var quoteResponse = await Http.PostAsJsonAsync(
                $"api/v1/projects/{projectId}/generate-quotation",
                quoteRequest);
            if (!quoteResponse.IsSuccessStatusCode)
            {
                var errorContent = await quoteResponse.Content.ReadAsStringAsync();
                Snackbar.Add(
                    string.IsNullOrWhiteSpace(errorContent)
                        ? BuildQuotationGenerationFailureMessage(isUpdatingExistingProject)
                        : $"{BuildQuotationGenerationFailureMessage(isUpdatingExistingProject)} {errorContent}",
                    Severity.Warning);
                Navigation.NavigateTo($"/sales/projects/{projectId}");
                return;
            }

            // Lock pricing before clearing the draft so no in-flight debounced calls reprice
            _projectLocked = true;

            await JS.InvokeVoidAsync("sessionStorage.removeItem", DraftStorageKey);

            Snackbar.Add(BuildQuotationGenerationSuccessMessage(isUpdatingExistingProject), Severity.Success);
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

    private static string BuildQuotationGenerationSuccessMessage(bool isUpdatingExistingProject) =>
        isUpdatingExistingProject
            ? "Quotation regenerated."
            : "Quotation generated.";

    private static string BuildQuotationGenerationFailureMessage(bool isUpdatingExistingProject) =>
        isUpdatingExistingProject
            ? "Project updated but quotation generation failed."
            : "Project created but quotation generation failed.";

    private static GenerateQuotationRequest BuildGenerateQuotationRequest(
        Guid projectId,
        bool isUpdatingExistingProject,
        QuotationPdfData pdfData) => new()
    {
        ValidityDays = ResolveValidityDays(pdfData),
        DeliveryExpectations = pdfData.DeliveryExpectations,
        BulkDiscountAmount = ResolveDiscountAmount(pdfData, "Automatic bulk-order savings"),
        ManualDiscountAmount = ResolveDiscountAmount(pdfData, "Manual discount"),
        ShippingCost = Math.Max(0m, pdfData.ShippingCost),
        TaxAmount = Math.Max(0m, pdfData.TaxAmount),
        QuotationTerms = pdfData.SpecialTerms,
        PdfData = pdfData,
        ChangeSummary = isUpdatingExistingProject
            ? "Regenerated from employee project quote workspace."
            : "Initial quotation generated from employee project quote workspace.",
        IdempotencyKey = $"{projectId:N}:{DateTime.UtcNow:yyyyMMddHHmmssfff}"
    };

    private static int ResolveValidityDays(QuotationPdfData pdfData)
    {
        var days = (pdfData.ValidityEnd.Date - pdfData.ValidityStart.Date).Days;
        return days <= 0 ? 30 : Math.Clamp(days, 1, 365);
    }

    private static decimal ResolveDiscountAmount(QuotationPdfData pdfData, string condition) =>
        pdfData.Discounts
            .Where(discount => string.Equals(discount.Conditions, condition, StringComparison.OrdinalIgnoreCase))
            .Sum(discount => Math.Max(0m, discount.DiscountValue));

    private async Task<bool> SyncProjectPartsForQuoteAsync(Guid projectId)
    {
        foreach (var part in _parts.Where(p => p.IsFullyConfigured))
        {
            if (part.ServerPartId.HasValue)
            {
                var updateRequest = BuildUpdateProjectPartRequest(part);
                if (updateRequest == null)
                    return false;

                using var updateResponse = await Http.PutAsJsonAsync(
                    $"api/v1/projects/{projectId}/parts/{part.ServerPartId.Value}",
                    updateRequest);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    Snackbar.Add($"Failed to update part '{part.Name}'.", Severity.Warning);
                    return false;
                }

                continue;
            }

            var addRequest = BuildAddProjectPartRequest(part);
            if (addRequest == null)
                return false;

            using var addResponse = await Http.PostAsJsonAsync($"api/v1/projects/{projectId}/parts", addRequest);
            if (!addResponse.IsSuccessStatusCode)
            {
                Snackbar.Add($"Failed to add part '{part.Name}'.", Severity.Warning);
                return false;
            }

            var createdPart = await addResponse.Content.ReadFromJsonAsync<ProjectPartDto>();
            if (createdPart == null)
            {
                Snackbar.Add($"Project part '{part.Name}' was saved but returned no part ID.", Severity.Warning);
                return false;
            }

            part.ServerPartId = createdPart.Id;
        }

        return true;
    }

    private async Task<bool> ConfirmProjectPartPricesForQuoteAsync(Guid projectId, QuotationPdfData pdfData)
    {
        var quotedParts = _parts.Where(p => p.IsFullyConfigured).ToList();

        for (var index = 0; index < quotedParts.Count; index++)
        {
            var part = quotedParts[index];
            if (!part.ServerPartId.HasValue)
            {
                Snackbar.Add($"Part '{part.Name}' has not been saved to the project yet.", Severity.Warning);
                return false;
            }

            var pdfItem = ResolvePdfItemForPart(pdfData.Items, index, part);
            var unitPrice = ResolvePartUnitPriceForConfirmation(part, pdfItem);
            if (!unitPrice.HasValue || unitPrice.Value <= 0m)
            {
                Snackbar.Add($"Part '{part.Name}' has no calculated price yet.", Severity.Warning);
                return false;
            }

            using var response = await Http.PostAsJsonAsync(
                $"api/v1/projects/{projectId}/parts/{part.ServerPartId.Value}/confirm-price",
                new ConfirmPartPriceRequest { ConfirmedUnitPrice = unitPrice.Value });

            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add($"Failed to confirm price for '{part.Name}'.", Severity.Warning);
                return false;
            }
        }

        return true;
    }

    private static QuotationPdfItem? ResolvePdfItemForPart(
        IReadOnlyList<QuotationPdfItem> pdfItems,
        int index,
        PartViewModel part)
    {
        if (index < pdfItems.Count && string.Equals(pdfItems[index].PartName, part.Name, StringComparison.Ordinal))
            return pdfItems[index];

        return pdfItems.FirstOrDefault(item => string.Equals(item.PartName, part.Name, StringComparison.Ordinal));
    }

    private static decimal? ResolvePartUnitPriceForConfirmation(PartViewModel part, QuotationPdfItem? pdfItem = null)
    {
        if (pdfItem?.UnitPrice > 0m)
            return pdfItem.UnitPrice;

        var baseUnitPrice = ProjectQuotationPdfMapper.ResolveBaseUnitPrice(part);
        if (baseUnitPrice > 0m)
            return baseUnitPrice;

        return part.Quantity > 0 && part.EstimatedTotalAmount.HasValue
            ? part.EstimatedTotalAmount.Value / part.Quantity
            : null;
    }

    // ── Task 15: Duplicate project ─────────────────────────────────────

    /// <inheritdoc />
    private async Task DuplicateProjectAsync()
    {
        if (_parts.Count == 0)
            return;

        _saving = true;
        try
        {
            var saved = await SaveDraftToServerAsync();
            if (!_serverProjectId.HasValue)
            {
                Snackbar.Add("Select a customer before duplicating this draft.", Severity.Warning);
                return;
            }

            if (!saved)
            {
                Snackbar.Add("Project save did not complete. Please try duplicating again.", Severity.Warning);
                return;
            }

            var duplicateTitle = BuildDuplicateProjectTitle(_title);
            using var response = await Http.PostAsJsonAsync(
                $"api/v1/projects/{_serverProjectId.Value}/duplicate",
                new DuplicateProjectRequest { Title = duplicateTitle });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Snackbar.Add(
                    string.IsNullOrWhiteSpace(error)
                        ? "Project duplicate failed."
                        : $"Project duplicate failed. {error}",
                    Severity.Error);
                return;
            }

            var duplicate = await response.Content.ReadFromJsonAsync<ProjectDetailDto>();
            if (duplicate == null)
            {
                Snackbar.Add("Project duplicate failed: response was invalid.", Severity.Error);
                return;
            }

            await ApplyProjectDetailAsync(duplicate);
            Navigation.NavigateTo($"/sales/projects/new?session={_sessionId}&resume={duplicate.Id}", replace: true);
            Snackbar.Add("Project duplicated.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Project duplicate failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string BuildDuplicateProjectTitle(string? title)
    {
        var baseTitle = string.IsNullOrWhiteSpace(title)
            ? $"Project {DateTime.Today:yyyy-MM-dd}"
            : title.Trim();

        return baseTitle.EndsWith(" (Copy)", StringComparison.OrdinalIgnoreCase)
            ? baseTitle
            : $"{baseTitle} (Copy)";
    }

    /// <summary>Duplicates a single part within the project, reusing the source's analysis artifacts.</summary>
    private async Task DuplicateSinglePart(PartViewModel sourcePart)
    {
        var cloned = new PartViewModel
        {
            // Identity — reuse same physical upload
            Name = sourcePart.Name,
            FileId = sourcePart.FileId,
            ClientUploadId = sourcePart.ClientUploadId,
            StoragePath = sourcePart.StoragePath,
            StoragePathAliases = [.. sourcePart.StoragePathAliases],
            FileSizeBytes = sourcePart.FileSizeBytes,
            UploadedAt = sourcePart.UploadedAt,

            // Completed analysis artifacts — copy, don't re-run
            ThumbnailSmallUrl = sourcePart.ThumbnailSmallUrl,
            ThumbnailLargeUrl = sourcePart.ThumbnailLargeUrl,
            ThumbnailSmallGcsPath = sourcePart.ThumbnailSmallGcsPath,
            ThumbnailLargeGcsPath = sourcePart.ThumbnailLargeGcsPath,
            GlbStoragePath = sourcePart.GlbStoragePath,
            ViewerStoragePath = sourcePart.ViewerStoragePath,
            ViewerFileExtension = sourcePart.ViewerFileExtension,
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
        await JoinPartFileGroupsAsync(cloned);

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

        TriggerDeferredStorageMigration();
        await MigrateTempProjectFilesAsync();
    }

    private async Task<List<ProjectSummaryDto>> LoadRecentProjectsAsync(Guid customerId)
    {
        try
        {
            var result = await Http.GetFromJsonAsync<PagedResponse<ProjectSummaryDto>>(
                $"api/v1/projects?customerId={customerId}&page=1&pageSize=5");
            return result?.Data?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load recent projects for customer {CustomerId}", customerId);
            return [];
        }
    }

    private async Task OnRecentProjectClicked(Guid projectId)
    {
        Navigation.NavigateTo($"/sales/projects/{projectId}");
    }

    private async Task MigrateTempProjectFilesAsync()
    {
        if (!_selectedCustomerId.HasValue)
            return;

        await _storageMigrationSemaphore.WaitAsync();
        try
        {
            _storageMigrationInProgress = true;
            var partsInTemp = _parts
                .Where(p => IsTempProjectStoragePath(p.StoragePath))
                .ToList();
            if (partsInTemp.Count == 0)
                return;

            var processingParts = partsInTemp
                .Where(p => !IsReadyForTempProjectMigration(p))
                .ToList();
            if (processingParts.Count > 0)
            {
                var storageProjectId = ResolveStorageProjectId(partsInTemp.Select(p => p.StoragePath), _tempProjectId);
                Logger.LogInformation(
                    "Deferring storage migration for temp project {ProjectId}; {ProcessingPartCount} of {PartCount} temp part(s) are still processing",
                    storageProjectId,
                    processingParts.Count,
                    partsInTemp.Count);
                return;
            }

            var migrationProjectId = ResolveStorageProjectId(partsInTemp.Select(p => p.StoragePath), _tempProjectId);
            var migrationResult = await Http.PostAsJsonAsync(
                $"api/v1/uploads/migrate-project?projectId={migrationProjectId}&customerId={_selectedCustomerId}",
                (object?)null);

            if (!migrationResult.IsSuccessStatusCode)
            {
                Snackbar.Add("Failed to migrate files to customer storage. Please try again.", Severity.Error);
                return;
            }

            var migrated = await migrationResult.Content.ReadFromJsonAsync<BffMigrateProjectResponseDto>();
            if (migrated == null)
            {
                Snackbar.Add("Migration failed.", Severity.Error);
                return;
            }

            var successfullyMigratedParts = new List<PartViewModel>();

            foreach (var entry in migrated.MigratedFiles)
            {
                var fileId = entry.FileId;
                var newBasePath = entry.NewPath;
                var oldBasePath = entry.OldPath;

                if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(newBasePath) || string.IsNullOrEmpty(oldBasePath))
                    continue;

                var part = _parts.FirstOrDefault(p => p.FileId.ToString() == fileId);
                if (part == null) continue;

                part.StoragePath = newBasePath;
                part.AddStoragePathAlias(oldBasePath);
                ApplyMigratedStoragePaths(part, oldBasePath, newBasePath);

                await JoinPartFileGroupsAsync(part);
                StartStatusWatchdog(part, oldBasePath);
                StartStatusWatchdog(part, newBasePath);
                if (entry.Status != null)
                    await ApplyAnalysisStatusAsync(part, entry.Status);
                successfullyMigratedParts.Add(part);
            }

            if (successfullyMigratedParts.Count > 0)
            {
                TriggerAutoSave();
                await InvokeAsync(StateHasChanged);

                foreach (var part in successfullyMigratedParts.Where(p => p.ProcessId.HasValue && p.MaterialId.HasValue))
                    TriggerPricingAsync(part);
            }

            if (migrated.Errors.Count > 0)
            {
                var errorMessages = migrated.Errors
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                var summary = $"Migration completed with {migrated.Errors.Count} error(s). {successfullyMigratedParts.Count} file(s) migrated successfully.";
                if (errorMessages.Count > 0 && errorMessages.Count <= 3)
                {
                    summary += " Errors: " + string.Join("; ", errorMessages);
                }

                Snackbar.Add(summary, Severity.Warning);
            }
        }
        finally
        {
            _storageMigrationInProgress = false;
            _storageMigrationSemaphore.Release();
            if (_serverSavePending)
                TriggerAutoSave();
        }
    }

    private void TriggerDeferredStorageMigration()
    {
        if (!_selectedCustomerId.HasValue ||
            !_parts.Any(p => IsTempProjectStoragePath(p.StoragePath)))
        {
            return;
        }

        _storageMigrationDebounceTimer?.Dispose();
        _storageMigrationDebounceTimer = new Timer(_ =>
        {
            _ = InvokeAsync(async () =>
            {
                try
                {
                    await MigrateTempProjectFilesAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Deferred temp project storage migration failed");
                }
            });
        }, null, StorageMigrationDebounceMs, Timeout.Infinite);
    }

    private static bool IsTempProjectStoragePath(string? storagePath) =>
        !string.IsNullOrWhiteSpace(storagePath) &&
        storagePath.StartsWith("projects/", StringComparison.OrdinalIgnoreCase);

    private static bool IsReadyForTempProjectMigration(PartViewModel part)
    {
        if (!IsTempProjectStoragePath(part.StoragePath))
            return false;

        if (part.QueuedUpload || part.Uploading || part.AwaitingPreview)
            return false;

        if (!string.IsNullOrWhiteSpace(part.Error) || part.DfmAnalysisTimedOut)
            return true;

        return string.Equals(part.StatusText, "Ready", StringComparison.OrdinalIgnoreCase) &&
               HasViewerArtifactForMigration(part);
    }

    private static bool HasViewerArtifactForMigration(PartViewModel part) =>
        !string.IsNullOrWhiteSpace(part.GlbStoragePath) ||
        !string.IsNullOrWhiteSpace(part.ViewerStoragePath) ||
        !string.IsNullOrWhiteSpace(part.GlbSignedUrl) ||
        !string.IsNullOrWhiteSpace(part.ViewerUrl);

    private static Guid ResolveStorageProjectId(IEnumerable<string?> storagePaths, Guid fallbackProjectId)
    {
        foreach (var storagePath in storagePaths)
        {
            if (TryGetProjectIdFromStoragePath(storagePath, out var projectId))
                return projectId;
        }

        return fallbackProjectId;
    }

    private static bool TryGetProjectIdFromStoragePath(string? storagePath, out Guid projectId)
    {
        projectId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(storagePath))
            return false;

        var segments = storagePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length >= 2 &&
            segments[0].Equals("projects", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(segments[1], out projectId);
        }

        if (segments.Length >= 4 &&
            segments[0].Equals("customers", StringComparison.OrdinalIgnoreCase) &&
            segments[2].Equals("projects", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(segments[3], out projectId);
        }

        return false;
    }

    private async Task OpenBabylonViewer(PartViewModel part)
    {
        if (!string.IsNullOrWhiteSpace(part.ViewerUrl))
            return;

        if (!string.IsNullOrWhiteSpace(part.GlbSignedUrl))
        {
            part.ViewerUrl = part.GlbSignedUrl;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (await TryApplyLocalViewerUrlAsync(part))
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (string.IsNullOrWhiteSpace(part.StoragePath)) return;

        var resp = await Http.GetAsync($"api/v1/uploads/viewer-url?storagePath={Uri.EscapeDataString(part.StoragePath)}");
        if (!resp.IsSuccessStatusCode)
        {
            Snackbar.Add("Failed to load 3D viewer URL.", Severity.Error);
            return;
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        ApplyViewerUrlDocument(part, json);

        if (string.IsNullOrEmpty(part.ViewerUrl))
        {
            Snackbar.Add("3D preview not available yet for this file.", Severity.Warning);
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task<string?> GetSignedDownloadUrlAsync(string storagePath)
    {
        try
        {
            // Call BFF to get signed download URL for the storage path
            // The BFF proxies the UploadService's /upload/v1/files/by-path/signed-url endpoint
            var response = await Http.PostAsJsonAsync(
                "api/v1/uploads/by-path/signed-url",
                new { storagePath },
                CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("Failed to get signed URL for {StoragePath}: {StatusCode}", storagePath, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SignedUrlResponse>();
            return result?.SignedUrl;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error getting signed URL for {StoragePath}", storagePath);
            return null;
        }
    }

    private sealed record SignedUrlResponse(string? SignedUrl);

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

