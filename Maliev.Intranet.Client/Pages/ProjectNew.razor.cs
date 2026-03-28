using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
    private List<CurrencyDto> _currencies = [];
    private List<LeadTimeOptionDto> _leadTimeOptions = [];
    private LeadTimeOptionDto? _selectedLeadTime;
    private List<ProcessDto> _processes = [];
#pragma warning disable CS0649
    private bool _saving;
    private bool _autoSaving;
    private DateTimeOffset? _lastSavedAt;
#pragma warning restore CS0649
    private LayoutMode _layoutMode = LayoutMode.Configurator;
    private bool _dragOver;
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;

    // ── Validation ────────────────────────────────────────────────────
    private bool _titleHasError;
    private string? _descriptionError;
    private bool _descriptionHasError;

    // ── Parts ─────────────────────────────────────────────────────────
    private readonly List<PartViewModel> _parts = [];
    private int _selectedPartIndex;

    // ── Customer search ────────────────────────────────────────────────
    private CancellationTokenSource? _searchCts;

    // ── Upload catch-up ────────────────────────────────────────────────
    private const int CatchUpDelayMs = 5000; // one-shot fetch after SignalR group join

    // ── Pricing debounce ───────────────────────────────────────────────
    private readonly Dictionary<Guid, CancellationTokenSource> _pricingTokens = new();
    private const int PricingDebounceMs = 300;

    // ── Session ────────────────────────────────────────────────────────
    private Guid _sessionId;

    // ── Auto-save debounce ─────────────────────────────────────────────
    private const int AutoSaveDebounceMs = 1000;
    private string DraftStorageKey => $"project-draft-{_sessionId}";
    private Timer? _autoSaveDebounceTimer;

    // ── SignalR ────────────────────────────────────────────────────────
    private HubConnection? _hubConnection;

    // ── File type sets ────────────────────────────────────────────────
    private static readonly HashSet<string> ThreeDExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".step", ".stp", ".3mf", ".obj", ".igs", ".iges", ".blend", ".fbx", ".gltf", ".glb" };

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".stl", ".step", ".stp", ".3mf", ".obj", ".igs", ".iges", ".blend", ".fbx", ".gltf", ".glb",
            ".pdf", ".dxf", ".dwg",
            ".png", ".jpg", ".jpeg", ".tiff", ".bmp", ".webp",
            ".doc", ".docx", ".xls", ".xlsx",
            ".zip", ".rar", ".7z",
        };

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
        _autoSaveDebounceTimer?.Dispose();
        await _searchCts?.CancelAsync()!;
        _searchCts?.Dispose();
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

        if (string.IsNullOrEmpty(sessionParam) || !Guid.TryParse(sessionParam, out _sessionId))
        {
            _sessionId = Guid.NewGuid();
            // Redirect to URL with session param. In SSR this throws NavigationException (stops execution).
            // In WASM, NavigateTo updates the URL in-place without recreating the component, so we must
            // NOT return — data loading must continue immediately with the newly assigned _sessionId.
            Navigation.NavigateTo($"/sales/projects/new?session={_sessionId}", replace: true);
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
                _processes = result;
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

        // ── SignalR hub connection ─────────────────────────────────────
        if (OperatingSystem.IsBrowser()) // client-side only; skip on SSR prerender
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/hubs/notifications"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<SignalRFileAnalysisPayload>("FileAnalysisCompleted", async payload =>
            {
                var part = _parts.FirstOrDefault(p => p.StoragePath == payload.StoragePath);
                if (part == null) return;

                if (payload.Failed)
                {
                    part.AwaitingPreview = false;
                    part.StatusText = string.IsNullOrEmpty(payload.ErrorCode) ? "Preview unavailable" : $"Preview failed: {payload.ErrorCode}";
                    TriggerAutoSave();
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                // Small thumbnail arrives first (ThumbnailUrl only, PreviewUrls null)
                if (!string.IsNullOrEmpty(payload.ThumbnailUrl) && string.IsNullOrEmpty(part.ThumbnailSmallUrl))
                {
                    part.ThumbnailSmallUrl = payload.ThumbnailUrl;
                    await InvokeAsync(StateHasChanged);
                }

                // Full preview from PreviewImagesGeneratedConsumer
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
                }
                await InvokeAsync(StateHasChanged);
            });

            await _hubConnection.StartAsync();

            // Join groups for any parts already in the list (restored from draft)
            foreach (var part in _parts.Where(p => !string.IsNullOrEmpty(p.StoragePath)))
                await _hubConnection.InvokeAsync("JoinFileGroup", part.StoragePath);
        }
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

    // ── Task 10: File upload / polling ─────────────────────────────────

    /// <inheritdoc />
    private async Task HandleFileSelected(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.Name);
            if (!AllowedExtensions.Contains(ext))
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

            var url = $"api/uploads?projectId={_tempProjectId}";
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

            _ = Task.Run(async () => { await Task.Delay(CatchUpDelayMs); await FetchCurrentStatusAsync(part, storagePath); });
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
    /// Single catch-up fetch: applied once after upload to handle the case where the
    /// SignalR event fired before the hub group join completed.
    /// Live updates arrive via SignalR; this is only a safety net.
    /// </summary>
    private async Task FetchCurrentStatusAsync(PartViewModel part, string storagePath)
    {
        try
        {
            var statusResponse = await Http.GetAsync(
                $"api/uploads/analysis-status?storagePath={Uri.EscapeDataString(storagePath)}");

            if (!statusResponse.IsSuccessStatusCode) return;

            var status = await statusResponse.Content.ReadFromJsonAsync<FileAnalysisStatusDto>();
            if (status == null) return;

            part.Dimensions = status.Dimensions;
            part.VolumeMm3 = status.Dimensions?.VolumeMm3;
            part.IsManifold = status.IsManifold;
            part.GlbStoragePath = status.GlbStoragePath;
            part.DfmReport = status.DfmReport;

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

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            // Catch-up fetch is a safety net — failures are non-fatal; SignalR will deliver the final state
            await InvokeAsync(() => Snackbar.Add($"Status fetch failed for {part.Name}: {ex.Message}", Severity.Warning));
        }
    }

    // ── Task 10: Remove part ───────────────────────────────────────────

    /// <inheritdoc />
    private void RemovePart(PartViewModel part)
    {
        if (!string.IsNullOrEmpty(part.StoragePath) && _hubConnection?.State == HubConnectionState.Connected)
            _ = _hubConnection.InvokeAsync("LeaveFileGroup", part.StoragePath);

        _parts.Remove(part);

        if (_selectedPartIndex >= _parts.Count)
            _selectedPartIndex = Math.Max(0, _parts.Count - 1);

        TriggerAutoSave();
    }

    // ── Task 12: Cascading dropdowns ──────────────────────────────────

    /// <inheritdoc />
    private async Task OnPartChanged(PartViewModel part)
    {
        if (part.ProcessId.HasValue && !string.IsNullOrEmpty(part.ProcessCode)
            && part.AvailableMaterials.Count == 0) // only reload catalog on process change (OnProcessChanged clears AvailableMaterials before invoking this)
        {
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
        if (_pricingTokens.TryGetValue(part.FileId, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
            _pricingTokens.Remove(part.FileId);
        }

        var cts = new CancellationTokenSource();
        _pricingTokens[part.FileId] = cts;

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
        if (!_selectedCustomerId.HasValue)
            return;

        part.PricingLoading = true;
        part.PricingFailed = false;

        try
        {
            var geometry = part.VolumeMm3.HasValue
                ? new GeometryMetricsDto
                {
                    VolumeCm3 = (decimal)part.VolumeMm3.Value / 1_000_000m,
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
                CustomerId = _selectedCustomerId.Value,
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
        }
    }

    // ── Task 14: Auto-save ─────────────────────────────────────────────

    /// <inheritdoc />
    private void TriggerAutoSave()
    {
        _autoSaveDebounceTimer?.Dispose();
        _autoSaveDebounceTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () => await SaveDraftAsync());
        }, null, AutoSaveDebounceMs, Timeout.Infinite);
    }

    private Guid? _selectedCustomerId => _selectedCustomer?.Id;

    private async Task SaveDraftAsync()
    {
        _autoSaving = true;
        try
        {
            var draft = new DraftProjectState
            {
                TempProjectId = _tempProjectId,
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
                LastModified = DateTime.UtcNow,
                Parts = _parts.Select(p => p.ToDraftPartState()).ToList(),
            };

            var json = JsonSerializer.Serialize(draft);
            await JS.InvokeVoidAsync("sessionStorage.setItem", DraftStorageKey, json);
            _lastSavedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception)
        {
            Snackbar.Add("Auto-save failed.", Severity.Warning);
        }
        finally
        {
            _autoSaving = false;
            StateHasChanged();
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
            _title = draft.Title;
            _description = draft.Description;
            _selectedLeadTime = _leadTimeOptions.FirstOrDefault(lt => lt.Code == draft.SelectedLeadTimeCode)
                                ?? _leadTimeOptions.FirstOrDefault();

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

                // Catch-up fetch for any part that is still missing its thumbnail (analysis completed
                // while the page was closed, SignalR event was missed).
                if (!string.IsNullOrEmpty(partVm.StoragePath) && string.IsNullOrEmpty(partVm.ThumbnailSmallUrl))
                {
                    var p = partVm; var path = partVm.StoragePath!;
                    _ = Task.Run(async () => { await Task.Delay(CatchUpDelayMs); await FetchCurrentStatusAsync(p, path); });
                }

                _parts.Add(partVm);
            }

            _selectedPartIndex = 0;
            _lastSavedAt = draft.LastModified;
            Snackbar.Add("Draft restored from session storage.", Severity.Info);
        }
        catch (Exception)
        {
            // Draft restore failures are non-fatal
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

                using var partResponse = await Http.PostAsJsonAsync($"api/projects/{project.Id}/parts", addPartRequest);
                if (!partResponse.IsSuccessStatusCode)
                    Snackbar.Add($"Failed to add part '{part.Name}'.", Severity.Warning);
            }

            using var quoteResponse = await Http.PostAsync($"api/projects/{project.Id}/generate-quotation", null);
            if (!quoteResponse.IsSuccessStatusCode)
            {
                Snackbar.Add("Project created but quotation generation failed.", Severity.Warning);
                Navigation.NavigateTo($"/sales/projects/{project.Id}");
                return;
            }

            await JS.InvokeVoidAsync("sessionStorage.removeItem", DraftStorageKey);

            Snackbar.Add("Project and quotation created successfully!", Severity.Success);
            Navigation.NavigateTo($"/sales/projects/{project.Id}");
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

    private static bool Is3DFile(string name) =>
        ThreeDExtensions.Contains(Path.GetExtension(name));

    private static string GetFileIcon(string name) =>
        Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".stl" or ".step" or ".stp" or ".3mf" or ".obj" or ".igs" or ".iges"
            or ".blend" or ".fbx" or ".gltf" or ".glb" => Icons.Material.Outlined.ViewInAr,
            ".pdf" => Icons.Material.Outlined.PictureAsPdf,
            ".dxf" or ".dwg" => Icons.Material.Outlined.Architecture,
            ".png" or ".jpg" or ".jpeg" or ".tiff" or ".bmp" or ".webp" => Icons.Material.Outlined.Image,
            ".doc" or ".docx" => Icons.Material.Outlined.Description,
            ".xls" or ".xlsx" => Icons.Material.Outlined.TableChart,
            ".zip" or ".rar" or ".7z" => Icons.Material.Outlined.FolderZip,
            _ => Icons.Material.Outlined.InsertDriveFile,
        };

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
        if (_description?.Length > 2000) { _descriptionError = "Must be 2000 characters or fewer"; _descriptionHasError = true; }
        else { _descriptionError = null; _descriptionHasError = false; }
    }

    private void OnCustomerSelected(CustomerSummaryDto? customer)
    { _selectedCustomer = customer; if (customer != null) _showCustomerSearch = false; }

    private async Task OpenBabylonViewer(PartViewModel part)
    {
        // Use pre-resolved signed URL from GlbReady SignalR event when available
        if (!string.IsNullOrEmpty(part.GlbSignedUrl))
        {
            part.ViewerUrl = part.GlbSignedUrl;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (string.IsNullOrEmpty(part.GlbStoragePath)) return;
        var resp = await Http.GetAsync($"api/uploads/viewer-url?storagePath={Uri.EscapeDataString(part.GlbStoragePath)}");
        if (!resp.IsSuccessStatusCode)
        {
            Snackbar.Add("Failed to load 3D viewer URL.", Severity.Error);
            return;
        }
        var json = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        part.ViewerUrl = json?.RootElement.GetProperty("url").GetString();
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

