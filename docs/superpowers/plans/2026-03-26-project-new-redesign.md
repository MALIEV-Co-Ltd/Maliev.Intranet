# ProjectNew Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `ProjectNew.razor` into a full-screen 3-panel quoting workspace with live AI pricing, BabylonJS 3D viewer, per-part configuration, and draft auto-save.

**Architecture:** `ProjectNew.razor` is a layout-only shell using `@layout EmptyLayout`; all state and API calls live in `ProjectNew.razor.cs` (partial class). Six focused sub-components under `Components/Project/` receive `PartViewModel` instances as parameters and fire `EventCallback`s back to the parent for all mutations.

**Tech Stack:** .NET 10, Blazor WebAssembly, MudBlazor, `IJSRuntime` (sessionStorage), `HttpClient` (BFF APIs at `/api/pricing/calculate`, `/api/catalog/`, `/api/referencedata/currencies`, `/api/projects`, `/api/quotations`)

**Spec:** `docs/superpowers/specs/2026-03-26-project-new-redesign.md`
**Reference design:** `_designs/new-project-design/new-project-page-reference-design.html`
**Existing file to replace:** `Maliev.Intranet.Client/Pages/ProjectNew.razor` (912 lines — keep open for reference during migration)

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `Components/AnimatedCubeIcon.razor` | Fix `Size` parameter CSS |
| Create | `Components/Project/PartViewModel.cs` | Per-part state wrapper class |
| Create | `Pages/ProjectNew.razor` | Layout shell only — `@layout EmptyLayout`, 3-panel grid |
| Create | `Pages/ProjectNew.razor.cs` | All C# state, API calls, event handlers (partial class) |
| Create | `Components/Project/ProjectTopBar.razor` | Title, currency, duplicate, layout toggle |
| Create | `Components/Project/ProjectLeftPanel.razor` | Customer, project fields, upload zone |
| Create | `Components/Project/PartMiniCard.razor` | Single part card in carousel |
| Create | `Components/Project/PartCarousel.razor` | Horizontal scrolling carousel |
| Create | `Components/Project/PartDetailCard.razor` | Viewer + config tabs for selected part |
| Create | `Components/Project/ProjectQuotePanel.razor` | Lead time, totals, quote button |
| Modify | `Client/_Imports.razor` | Add `@using Maliev.Intranet.Client.Components.Project` |

---

## Task 1: Fix AnimatedCubeIcon Size CSS

**Files:**
- Modify: `Maliev.Intranet.Client/Components/AnimatedCubeIcon.razor`

The `Size` parameter is declared but all dimensions are hardcoded. Replace with a `--cube-size` CSS variable driven by an inline `style` attribute.

- [ ] **Open** `Components/AnimatedCubeIcon.razor`. Note: `Size` parameter defaults to 22. The CSS has hardcoded `18px`/`16px` values throughout.

- [ ] **Replace the `<span>` opening tag** to inject the CSS variable:
```razor
<span class="animated-cube-icon @Class"
      style="display:inline-flex; align-items:center; cursor:pointer; --cube-size:@(Size)px;"
      title="View in 3D"
      @onclick="OnClickHandler">
```

- [ ] **Update `.cube-card` dimensions:**
```css
.animated-cube-icon .cube-card {
    width: var(--cube-size);
    height: var(--cube-size);
    /* keep existing gradient, border-radius, transform, transform-style */
    box-shadow:
        calc(var(--cube-size) * 0.11) calc(var(--cube-size) * 0.11) 0 0 #d0d0d0,
        calc(var(--cube-size) * 0.17) calc(var(--cube-size) * 0.17) 0 0 #c0c0c0,
        calc(var(--cube-size) * 0.22) calc(var(--cube-size) * 0.22) calc(var(--cube-size) * 0.44) rgba(0,0,0,0.25),
        calc(var(--cube-size) * 0.33) calc(var(--cube-size) * 0.33) calc(var(--cube-size) * 0.67) rgba(0,0,0,0.15);
    position: relative;
}
```

- [ ] **Update hover transform** (was `translate3d(0px, -16px, 8px)`):
```css
.animated-cube-icon:hover .cube-card {
    transform: translate3d(0, calc(var(--cube-size) * -0.89), calc(var(--cube-size) * 0.44)) rotateX(51deg) rotateZ(43deg);
    box-shadow:
        calc(var(--cube-size) * 0.11) calc(var(--cube-size) * 0.11) 0 0 #d0d0d0,
        calc(var(--cube-size) * 0.17) calc(var(--cube-size) * 0.17) 0 0 #c0c0c0,
        calc(var(--cube-size) * 0.22) calc(var(--cube-size) * 0.22) calc(var(--cube-size) * 0.89) rgba(0,0,0,0.35),
        calc(var(--cube-size) * 0.44) calc(var(--cube-size) * 0.67) calc(var(--cube-size) * 1.78) rgba(0,0,0,0.25),
        calc(var(--cube-size) * 0.67) calc(var(--cube-size) * 1.11) calc(var(--cube-size) * 2.67) rgba(0,0,0,0.15);
}
```

- [ ] **Update `::after` pseudo-element** (was `bottom: -4px; left: 4px`):
```css
.animated-cube-icon .cube-card::after {
    bottom: calc(var(--cube-size) * -0.22);
    left: calc(var(--cube-size) * 0.22);
    width: 100%;
    height: 100%;
    /* keep existing background, border-radius, transform, transform-origin */
}
```

- [ ] **Build and verify:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Commit:**
```bash
cd B:/maliev && git add Maliev.Intranet/Maliev.Intranet.Client/Components/AnimatedCubeIcon.razor && git commit -m "fix: AnimatedCubeIcon Size parameter drives CSS via --cube-size variable"
```

---

## Task 2: PartViewModel + Components/Project directory + _Imports update

**Files:**
- Create: `Maliev.Intranet.Client/Components/Project/PartViewModel.cs`
- Modify: `Maliev.Intranet.Client/_Imports.razor`

- [ ] **Create directory** `Maliev.Intranet.Client/Components/Project/` (create any file inside it).

- [ ] **Create `PartViewModel.cs`:**
```csharp
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// UI state wrapper for a single uploaded part. Combines upload state,
/// file analysis results, per-part configuration, and live pricing.
/// All config mutations flow through EventCallback to the parent page.
/// </summary>
public class PartViewModel
{
    // ── Persisted to DraftPartState ────────────────────────────────────
    public string Name { get; set; } = string.Empty;
    public Guid FileId { get; set; }
    public string? StoragePath { get; set; }
    public string? ProcessCode { get; set; }
    public Guid? ProcessId { get; set; }
    public string? MaterialCode { get; set; }
    public Guid? MaterialId { get; set; }
    public string? FinishCode { get; set; }
    public Guid? FinishId { get; set; }
    public string? ToleranceCode { get; set; }
    public Guid? ToleranceId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? PartNotes { get; set; }
    public bool DfmAcknowledged { get; set; }

    // ── Upload / preview (from polling) ───────────────────────────────
    public bool Uploading { get; set; }
    public bool AwaitingPreview { get; set; }
    /// <summary>Small thumbnail (~256px). Source: status.PreviewUrls?.ThumbnailSmall ?? status.ThumbnailUrl</summary>
    public string? ThumbnailSmallUrl { get; set; }
    /// <summary>Large thumbnail (~1200px). Source: status.HiResThumbnailUrl ?? status.PreviewUrls?.ThumbnailLargeUrl</summary>
    public string? ThumbnailLargeUrl { get; set; }
    /// <summary>GLB artifact for BabylonJS viewer. Source: status.GlbStoragePath</summary>
    public string? GlbStoragePath { get; set; }
    public bool PreviewLoadFailed { get; set; }
    public string StatusText { get; set; } = "Processing...";
    public FileAnalysisDimensionsDto? Dimensions { get; set; }
    public double? VolumeMm3 { get; set; }
    public bool? IsManifold { get; set; }
    public string? Error { get; set; }

    // ── Pricing (UI-only, not persisted) ──────────────────────────────
    public decimal? EstimatedUnitPrice { get; set; }
    public decimal? EstimatedTotalAmount { get; set; }
    /// <summary>Queue-aware lead time from last pricing result. Drives lead time panel day calculations.</summary>
    public int EstimatedLeadTimeDays { get; set; }
    public bool PricingLoading { get; set; }
    public bool PricingFailed { get; set; }

    // ── Catalog (UI-only, not persisted) ──────────────────────────────
    public List<CatalogMaterialDto> AvailableMaterials { get; set; } = [];
    public List<CatalogSurfaceFinishDto> AvailableFinishes { get; set; } = [];
    public List<CatalogToleranceDto> AvailableTolerances { get; set; } = [];
    public bool CatalogLoading { get; set; }

    // ── Helpers ───────────────────────────────────────────────────────
    public bool IsFullyConfigured =>
        ProcessId.HasValue && MaterialId.HasValue && FileId != Guid.Empty && Error == null;

    public DraftPartState ToDraftPartState() => new()
    {
        FileId = FileId.ToString(),
        StoragePath = StoragePath,
        FileName = Name,
        Quantity = Quantity,
        ProcessCode = ProcessCode,
        ProcessId = ProcessId,
        MaterialCode = MaterialCode,
        MaterialId = MaterialId,
        FinishCode = FinishCode,
        FinishId = FinishId,
        ToleranceCode = ToleranceCode,
        ToleranceId = ToleranceId,
        PartNotes = PartNotes,
        DfmAcknowledged = DfmAcknowledged,
    };

    public static PartViewModel FromDraftPartState(DraftPartState s) => new()
    {
        FileId = Guid.TryParse(s.FileId, out var id) ? id : Guid.Empty,
        StoragePath = s.StoragePath,
        Name = s.FileName ?? string.Empty,
        Quantity = s.Quantity,
        ProcessCode = s.ProcessCode,
        ProcessId = s.ProcessId,
        MaterialCode = s.MaterialCode,
        MaterialId = s.MaterialId,
        FinishCode = s.FinishCode,
        FinishId = s.FinishId,
        ToleranceCode = s.ToleranceCode,
        ToleranceId = s.ToleranceId,
        PartNotes = s.PartNotes,
        DfmAcknowledged = s.DfmAcknowledged,
    };
}
```

> **Note:** Check `DraftPartState` fields in `Maliev.Intranet.Shared/Dtos/ProjectDraftDtos.cs` and adjust property names in `ToDraftPartState`/`FromDraftPartState` to match exactly.

- [ ] **Add namespace to `_Imports.razor`** — append:
```razor
@using Maliev.Intranet.Client.Components.Project
```

- [ ] **Build and verify:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
```

- [ ] **Commit:**
```bash
cd B:/maliev && git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/ Maliev.Intranet/Maliev.Intranet.Client/_Imports.razor && git commit -m "feat: add PartViewModel and Components/Project namespace"
```

---

## Task 3: ProjectNew.razor shell + ProjectNew.razor.cs skeleton

**Files:**
- Create (replace): `Maliev.Intranet.Client/Pages/ProjectNew.razor`
- Create: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

- [ ] **Create `ProjectNew.razor`** (layout shell only — no `@code` block, all logic is in `.razor.cs`):
```razor
@page "/sales/projects/new"
@layout EmptyLayout
@rendermode InteractiveAuto
@using Maliev.Intranet.Shared
@using Maliev.Intranet.Shared.Dtos
@using Microsoft.AspNetCore.Components.Authorization
@inject HttpClient Http
@inject NavigationManager Navigation
@inject ISnackbar Snackbar
@inject IJSRuntime JS
@inject AuthenticationStateProvider AuthStateProvider
@implements IAsyncDisposable

<PageTitle>New Project — Maliev</PageTitle>

<div class="pn-root">
    <ErrorBoundary>
        <ChildContent>
            <ProjectTopBar
                Title="@_title"
                OnTitleChanged="@(v => { _title = v; TriggerAutoSave(); })"
                SelectedCurrency="@_selectedCurrency"
                Currencies="@_currencies"
                OnCurrencyChanged="@(c => { _selectedCurrency = c; TriggerAutoSave(); })"
                LastSavedAt="@_lastSavedAt"
                AutoSaving="@_autoSaving"
                LayoutMode="@_layoutMode"
                OnLayoutModeChanged="@(m => _layoutMode = m)"
                OnDuplicate="DuplicateProject" />

            <div class="pn-body">
                <ErrorBoundary>
                    <ChildContent>
                        <ProjectLeftPanel
                            SelectedCustomer="@_selectedCustomer"
                            ShowCustomerSearch="@_showCustomerSearch"
                            OnCustomerSelected="OnCustomerSelected"
                            OnShowSearchChanged="@(v => _showCustomerSearch = v)"
                            Title="@_title"
                            OnTitleChanged="@(v => { _title = v; ValidateTitle(); TriggerAutoSave(); })"
                            TitleHasError="@_titleHasError"
                            TitleError="@_titleError"
                            Description="@_description"
                            OnDescriptionChanged="@(v => { _description = v; ValidateDescription(); TriggerAutoSave(); })"
                            DescriptionHasError="@_descriptionHasError"
                            DescriptionError="@_descriptionError"
                            DragOver="@_dragOver"
                            OnDragEnter="@(() => _dragOver = true)"
                            OnDragLeave="@(() => _dragOver = false)"
                            OnOpenFilePicker="OpenFilePicker"
                            SearchCustomersFunc="SearchCustomersAsync" />
                    </ChildContent>
                    <ErrorContent><MudAlert Severity="Severity.Error" Class="ma-2">Left panel error.</MudAlert></ErrorContent>
                </ErrorBoundary>

                <div class="pn-center">
                    @if (_layoutMode == LayoutMode.Configurator)
                    {
                        <PartCarousel
                            Parts="@_parts"
                            SelectedIndex="@_selectedPartIndex"
                            OnSelectPart="@(i => _selectedPartIndex = i)"
                            OnRemovePart="RemovePart"
                            OnAddPart="OpenFilePicker" />

                        @if (_parts.Count > 0 && _selectedPartIndex < _parts.Count)
                        {
                            <ErrorBoundary>
                                <ChildContent>
                                    <PartDetailCard
                                        Part="@_parts[_selectedPartIndex]"
                                        Processes="@_processes"
                                        OnPartChanged="OnPartChanged"
                                        OnOpenViewer="OpenBabylonViewer" />
                                </ChildContent>
                                <ErrorContent><MudAlert Severity="Severity.Error" Class="ma-2">Detail panel error.</MudAlert></ErrorContent>
                            </ErrorBoundary>
                        }
                        else
                        {
                            <div class="pn-empty-detail">
                                <MudText Color="Color.Secondary">Upload a part to get started</MudText>
                            </div>
                        }
                    }
                    else
                    {
                        @* Summary table mode — Task 18 *@
                        <div class="pn-table-placeholder">
                            <MudText Color="Color.Secondary">Summary table — coming in Task 18</MudText>
                        </div>
                    }
                </div>

                <ErrorBoundary>
                    <ChildContent>
                        <ProjectQuotePanel
                            Parts="@_parts"
                            LeadTimeOptions="@_leadTimeOptions"
                            SelectedLeadTime="@_selectedLeadTime"
                            OnLeadTimeChanged="@(lt => { _selectedLeadTime = lt; TriggerAutoSave(); })"
                            SelectedCurrency="@_selectedCurrency"
                            CanQuote="@CanSubmit"
                            Saving="@_saving"
                            OnQuote="CreateProjectAndQuoteAsync" />
                    </ChildContent>
                    <ErrorContent><MudAlert Severity="Severity.Error" Class="ma-2">Quote panel error.</MudAlert></ErrorContent>
                </ErrorBoundary>
            </div>
        </ChildContent>
        <ErrorContent>
            <MudAlert Severity="Severity.Error" Class="ma-4">An unexpected error occurred. Please refresh.</MudAlert>
        </ErrorContent>
    </ErrorBoundary>
</div>

<MudFileUpload T="IReadOnlyList<IBrowserFile>"
               @ref="_fileUpload"
               FilesChanged="HandleFileSelected"
               Accept=".stl,.step,.stp,.3mf,.obj,.igs,.iges,.blend,.fbx,.gltf,.glb,.pdf,.dxf,.dwg,.png,.jpg,.jpeg,.tiff,.bmp,.webp,.doc,.docx,.xls,.xlsx,.zip,.rar,.7z"
               MaximumFileCount="20"
               Hidden="true"
               Style="display:none;" />

<style>
    .pn-root {
        display: flex;
        flex-direction: column;
        height: 100vh;
        overflow: hidden;
        background: var(--mud-palette-background-grey);
    }
    .pn-body {
        flex: 1;
        display: flex;
        overflow: hidden;
        min-height: 0;
    }
    .pn-center {
        flex: 1;
        display: flex;
        flex-direction: column;
        overflow: hidden;
        min-width: 0;
        gap: 12px;
        padding: 14px;
    }
    .pn-empty-detail {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .pn-table-placeholder {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
    }
</style>
```

- [ ] **Create `ProjectNew.razor.cs`** with the partial class skeleton:
```csharp
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace Maliev.Intranet.Client.Pages;

public partial class ProjectNew : IAsyncDisposable
{
    // ── Project-level state ───────────────────────────────────────────
    private Guid _tempProjectId = Guid.NewGuid();  // non-readonly; reassigned on Duplicate
    private string _title = string.Empty;
    private string? _description;
    private CustomerSummaryDto? _selectedCustomer;
    private bool _showCustomerSearch = true;
    private CurrencyDto? _selectedCurrency;
    private List<CurrencyDto> _currencies = [];
    private List<LeadTimeOptionDto> _leadTimeOptions = [];
    private LeadTimeOptionDto? _selectedLeadTime;
    private List<ProcessDto> _processes = [];
    private bool _saving;
    private bool _autoSaving;
    private DateTimeOffset? _lastSavedAt;
    private LayoutMode _layoutMode = LayoutMode.Configurator;
    private bool _dragOver;
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;

    // ── Validation ────────────────────────────────────────────────────
    private string? _titleError;
    private bool _titleHasError;
    private string? _descriptionError;
    private bool _descriptionHasError;

    // ── Parts ─────────────────────────────────────────────────────────
    private readonly List<PartViewModel> _parts = [];
    private int _selectedPartIndex;

    // ── Upload / polling (preserved from original) ────────────────────
    private readonly Dictionary<string, CancellationTokenSource> _pollingTokens = new();
    private const int PollingIntervalMs = 2000;

    // ── Pricing debounce ──────────────────────────────────────────────
    private readonly Dictionary<Guid, CancellationTokenSource> _pricingTokens = new();
    private const int PricingDebounceMs = 300;

    // ── Auto-save debounce ────────────────────────────────────────────
    private CancellationTokenSource? _autoSaveCts;
    private const int AutoSaveDebounceMs = 1000;
    private const string DraftStorageKey = "project-new-draft";

    // ── File type sets (preserved from original) ──────────────────────
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
        !_parts.Any(p => p.Uploading || p.PricingLoading);

    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _pollingTokens.Values) { await cts.CancelAsync(); cts.Dispose(); }
        foreach (var cts in _pricingTokens.Values) { await cts.CancelAsync(); cts.Dispose(); }
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
    }

    // ── Stubs — implemented in later tasks ────────────────────────────
    protected override async Task OnInitializedAsync() { await Task.CompletedTask; }

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
        if (string.IsNullOrWhiteSpace(_title)) { _titleError = "Project title is required"; _titleHasError = true; }
        else if (_title.Length > 500) { _titleError = "Must be 500 characters or fewer"; _titleHasError = true; }
        else { _titleError = null; _titleHasError = false; }
    }

    private void ValidateDescription()
    {
        if (_description?.Length > 2000) { _descriptionError = "Must be 2000 characters or fewer"; _descriptionHasError = true; }
        else { _descriptionError = null; _descriptionHasError = false; }
    }

    private Task HandleFileSelected(IReadOnlyList<IBrowserFile> files) => Task.CompletedTask; // Task 12
    private void RemovePart(PartViewModel part) { } // Task 12
    private void OnPartChanged(PartViewModel part) { } // Task 15
    private void TriggerAutoSave() { } // Task 16
    private void DuplicateProject() { } // Task 17
    private Task OpenBabylonViewer(PartViewModel part) => Task.CompletedTask; // Task 9
    private Task CreateProjectAndQuoteAsync() => Task.CompletedTask; // Task 17
    private Task<IEnumerable<CustomerSummaryDto>> SearchCustomersAsync(string value, CancellationToken ct)
        => Task.FromResult(Enumerable.Empty<CustomerSummaryDto>()); // Task 6
    private void OnCustomerSelected(CustomerSummaryDto? customer)
    { _selectedCustomer = customer; if (customer != null) _showCustomerSearch = false; }
}

public enum LayoutMode { Configurator, SummaryTable }
```

- [ ] **Build and verify** (will have warnings about unused stubs — that is expected):
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | grep -E "error|Error|succeeded"
```
Expected: `Build succeeded` (components referenced in `.razor` don't exist yet — they'll error). If errors appear about missing components, add placeholder `@code {}` stubs temporarily.

> **Note:** The `.razor` references components that don't exist yet. The fastest fix is to create empty placeholder files for all 6 components in Task 4 so the shell compiles, then flesh them out in Tasks 5–11.

- [ ] **Create empty placeholder stubs** for all 6 components (to allow Task 3 to compile):
```bash
# Create each with minimal content — exact implementation follows in Tasks 5–11
```
Each stub looks like:
```razor
@namespace Maliev.Intranet.Client.Components.Project
@* Placeholder — implemented in Task N *@
```
Add the minimum `[Parameter]` attributes needed to satisfy the `.razor` bindings.

- [ ] **Build confirms green, commit:**
```bash
cd B:/maliev && git add Maliev.Intranet/Maliev.Intranet.Client/Pages/ProjectNew.razor Maliev.Intranet/Maliev.Intranet.Client/Pages/ProjectNew.razor.cs Maliev.Intranet/Maliev.Intranet.Client/Components/Project/ && git commit -m "feat: ProjectNew shell + code-behind skeleton with placeholder components"
```

---

## Task 4: ProjectLeftPanel

**Files:**
- Create: `Maliev.Intranet.Client/Components/Project/ProjectLeftPanel.razor`

Port all content from the current `ProjectNew.razor` left column (customer search/card, project title/description fields, upload drop zone). The existing logic is fully preserved — just moved to a component.

- [ ] **Create `ProjectLeftPanel.razor`** with these parameters:
```csharp
[Parameter] public CustomerSummaryDto? SelectedCustomer { get; set; }
[Parameter] public bool ShowCustomerSearch { get; set; }
[Parameter] public EventCallback<CustomerSummaryDto?> OnCustomerSelected { get; set; }
[Parameter] public EventCallback<bool> OnShowSearchChanged { get; set; }
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public EventCallback<string> OnTitleChanged { get; set; }
[Parameter] public bool TitleHasError { get; set; }
[Parameter] public string? TitleError { get; set; }
[Parameter] public string? Description { get; set; }
[Parameter] public EventCallback<string?> OnDescriptionChanged { get; set; }
[Parameter] public bool DescriptionHasError { get; set; }
[Parameter] public string? DescriptionError { get; set; }
[Parameter] public bool DragOver { get; set; }
[Parameter] public EventCallback OnDragEnter { get; set; }
[Parameter] public EventCallback OnDragLeave { get; set; }
[Parameter] public EventCallback OnOpenFilePicker { get; set; }
[Parameter] public Func<string, CancellationToken, Task<IEnumerable<CustomerSummaryDto>>>? SearchCustomersFunc { get; set; }
```

- [ ] **Copy the HTML markup** from the current `ProjectNew.razor` sections 1 and 3 (customer panel, project details). Keep all existing MudBlazor components unchanged.

- [ ] **Style the panel** — fixed width, full height, scrollable, right border:
```css
.pnl-left {
    width: 340px;
    min-width: 340px;
    height: 100%;
    overflow-y: auto;
    background: var(--mud-palette-surface);
    border-right: 1px solid var(--mud-palette-lines-default);
    display: flex;
    flex-direction: column;
}
```

- [ ] **Add `SearchCustomersFunc` invocation** — replace `SearchFunc="SearchCustomersAsync"` on `MudAutocomplete` with `SearchFunc="@(SearchCustomersFunc ?? ((_,__) => Task.FromResult(Enumerable.Empty<CustomerSummaryDto>())))"`

- [ ] **Build and verify, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/ProjectLeftPanel.razor && git commit -m "feat: ProjectLeftPanel component (ported from ProjectNew)"
```

---

## Task 5: ProjectTopBar

**Files:**
- Create: `Maliev.Intranet.Client/Components/Project/ProjectTopBar.razor`

- [ ] **Create `ProjectTopBar.razor`** with parameters:
```csharp
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public EventCallback<string> OnTitleChanged { get; set; }
[Parameter] public CurrencyDto? SelectedCurrency { get; set; }
[Parameter] public List<CurrencyDto> Currencies { get; set; } = [];
[Parameter] public EventCallback<CurrencyDto?> OnCurrencyChanged { get; set; }
[Parameter] public DateTimeOffset? LastSavedAt { get; set; }
[Parameter] public bool AutoSaving { get; set; }
[Parameter] public LayoutMode LayoutMode { get; set; }
[Parameter] public EventCallback<LayoutMode> OnLayoutModeChanged { get; set; }
[Parameter] public EventCallback OnDuplicate { get; set; }
```

- [ ] **Markup** (reference design topbar pattern):
```razor
<div class="pn-topbar">
    <MudIconButton Icon="@Icons.Material.Outlined.ArrowBack"
                   Href="/sales/projects"
                   Size="Size.Small"
                   Color="Color.Default" />

    @* Inline-editable title *@
    @if (_editingTitle)
    {
        <MudTextField Value="@Title"
                      ValueChanged="@OnTitleChanged"
                      Immediate="true"
                      AutoFocus="true"
                      Variant="Variant.Text"
                      Margin="Margin.Dense"
                      @onblur="@(() => _editingTitle = false)"
                      Style="font-weight:600; font-size:14px; min-width:200px;" />
    }
    else
    {
        <span class="pn-topbar-title" @onclick="@(() => _editingTitle = true)">
            @(string.IsNullOrWhiteSpace(Title) ? "Untitled Project" : Title)
        </span>
    }
    <MudChip T="string" Size="Size.Small" Color="Color.Default" Label="true" Class="ml-1">Draft</MudChip>

    <MudSpacer />

    <MudAutocomplete T="CurrencyDto"
                     Value="@SelectedCurrency"
                     ValueChanged="@OnCurrencyChanged"
                     SearchFunc="@SearchCurrencies"
                     ToStringFunc="@(c => c == null ? "" : $"{c.Code} — {c.Name}")"
                     Label="Currency"
                     Variant="Variant.Outlined"
                     Margin="Margin.Dense"
                     Style="width:180px;"
                     Clearable="false" />

    <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mx-2" Style="white-space:nowrap;">
        @if (AutoSaving) { <text>Saving...</text> }
        else if (LastSavedAt.HasValue) { <text>Saved @LastSavedAt.Value.ToLocalTime().ToString("HH:mm")</text> }
    </MudText>

    <MudTooltip Text="Duplicate project">
        <MudIconButton Icon="@Icons.Material.Outlined.ContentCopy"
                       Size="Size.Small"
                       OnClick="@OnDuplicate" />
    </MudTooltip>

    <MudTooltip Text="@(LayoutMode == LayoutMode.Configurator ? "Switch to table view" : "Switch to configurator")">
        <MudIconButton Icon="@(LayoutMode == LayoutMode.Configurator ? Icons.Material.Outlined.TableRows : Icons.Material.Outlined.GridView)"
                       Size="Size.Small"
                       OnClick="@ToggleLayout" />
    </MudTooltip>
</div>
```

- [ ] **Add `@code` block:**
```csharp
private bool _editingTitle;

private Task<IEnumerable<CurrencyDto>> SearchCurrencies(string value, CancellationToken ct)
{
    var results = string.IsNullOrWhiteSpace(value)
        ? Currencies
        : Currencies.Where(c => c.Code.Contains(value, StringComparison.OrdinalIgnoreCase)
                              || c.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    return Task.FromResult(results);
}

private void ToggleLayout() =>
    OnLayoutModeChanged.InvokeAsync(
        LayoutMode == LayoutMode.Configurator ? LayoutMode.SummaryTable : LayoutMode.Configurator);
```

- [ ] **Add topbar CSS:**
```css
.pn-topbar {
    height: 52px;
    min-height: 52px;
    background: var(--mud-palette-surface);
    border-bottom: 1px solid var(--mud-palette-lines-default);
    display: flex;
    align-items: center;
    padding: 0 8px;
    gap: 6px;
    flex-shrink: 0;
}
.pn-topbar-title {
    font-weight: 600;
    font-size: 14px;
    cursor: pointer;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 300px;
}
.pn-topbar-title:hover { text-decoration: underline; }
```

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/ProjectTopBar.razor && git commit -m "feat: ProjectTopBar with inline title editing, currency autocomplete, layout toggle"
```

---

## Task 6: PartMiniCard + PartCarousel

**Files:**
- Create: `Maliev.Intranet.Client/Components/Project/PartMiniCard.razor`
- Create: `Maliev.Intranet.Client/Components/Project/PartCarousel.razor`

- [ ] **Create `PartMiniCard.razor`** with parameters:
```csharp
[Parameter] public PartViewModel Part { get; set; } = null!;
[Parameter] public bool IsActive { get; set; }
[Parameter] public EventCallback OnSelect { get; set; }
[Parameter] public EventCallback OnRemove { get; set; }
```

Markup structure (reference design `.part-card` pattern):
```razor
<div class="pmc-card @(IsActive ? "pmc-active" : "")" @onclick="OnSelect">
    <div class="pmc-thumb">
        @if (!string.IsNullOrEmpty(Part.ThumbnailSmallUrl) && !Part.PreviewLoadFailed)
        {
            <img src="@Part.ThumbnailSmallUrl" alt="@Part.Name"
                 @onerror="@(() => { Part.PreviewLoadFailed = true; StateHasChanged(); })" />
        }
        else
        {
            <MudIcon Icon="@GetFileIcon(Part.Name)" Size="Size.Medium" Color="Color.Default" />
        }
        @* DFM / status badge *@
        <span class="pmc-badge @GetBadgeClass()">@GetBadgeText()</span>
    </div>
    <div class="pmc-info">
        <MudTooltip Text="@Part.Name">
            <div class="pmc-name">@Part.Name</div>
        </MudTooltip>
        <div class="pmc-qp">
            ×@Part.Quantity
            @if (Part.PricingLoading) { <MudSkeleton Width="50px" Height="12px" /> }
            else if (Part.EstimatedTotalAmount.HasValue) { <text> · @FormatPrice(Part.EstimatedTotalAmount.Value)</text> }
        </div>
        @if (!string.IsNullOrEmpty(Part.ProcessCode))
        {
            <div class="pmc-process">@Part.ProcessCode</div>
        }
    </div>
    <MudIconButton Icon="@Icons.Material.Outlined.Close"
                   Size="Size.Small"
                   Color="Color.Error"
                   Class="pmc-remove"
                   OnClick="@(e => { e.StopPropagation(); OnRemove.InvokeAsync(); })"
                   Disabled="@Part.Uploading" />
</div>
```

- [ ] **Add CSS and `@code` for badge/price helpers** in `PartMiniCard.razor`.

- [ ] **Create `PartCarousel.razor`** with parameters:
```csharp
[Parameter] public List<PartViewModel> Parts { get; set; } = [];
[Parameter] public int SelectedIndex { get; set; }
[Parameter] public EventCallback<int> OnSelectPart { get; set; }
[Parameter] public EventCallback<PartViewModel> OnRemovePart { get; set; }
[Parameter] public EventCallback OnAddPart { get; set; }
```

```razor
<div class="pc-row">
    @for (var i = 0; i < Parts.Count; i++)
    {
        var idx = i;
        <PartMiniCard Part="@Parts[idx]"
                      IsActive="@(idx == SelectedIndex)"
                      OnSelect="@(() => OnSelectPart.InvokeAsync(idx))"
                      OnRemove="@(() => OnRemovePart.InvokeAsync(Parts[idx]))" />
    }
    <div class="pc-add" @onclick="OnAddPart">
        <MudIcon Icon="@Icons.Material.Outlined.Add" Size="Size.Large" />
        <MudText Typo="Typo.caption">Add Part</MudText>
    </div>
</div>
```

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/PartMiniCard.razor Maliev.Intranet/Maliev.Intranet.Client/Components/Project/PartCarousel.razor && git commit -m "feat: PartMiniCard and PartCarousel components"
```

---

## Task 7: PartDetailCard — Viewer Column

**Files:**
- Create/modify: `Maliev.Intranet.Client/Components/Project/PartDetailCard.razor`

Focus on the **left 60%**: thumbnail display with blur animation, AnimatedCubeIcon hover overlay, and BabylonJS dialog trigger.

- [ ] **Check `Components/ModelViewer.razor`** — this component likely wraps `babylon-viewer.js`. Read it to understand its parameters (`GlbPath`, `StoragePath`, or similar). You will pass `Part.GlbStoragePath` to it.

- [ ] **Create `PartDetailCard.razor`** — start with viewer column only (right column is Task 8):
```csharp
[Parameter] public PartViewModel Part { get; set; } = null!;
[Parameter] public List<ProcessDto> Processes { get; set; } = [];
[Parameter] public EventCallback<PartViewModel> OnPartChanged { get; set; }
[Parameter] public EventCallback<PartViewModel> OnOpenViewer { get; set; }
```

- [ ] **Viewer markup:**
```razor
<div class="pdc-root">
    <div class="pdc-viewer-col">
        <div class="pdc-thumb-area" @onmouseenter="@(() => _showViewer3dBtn = true)"
                                     @onmouseleave="@(() => _showViewer3dBtn = false)">
            @* Blurred small thumbnail while large is loading *@
            @if (!string.IsNullOrEmpty(Part.ThumbnailSmallUrl) && string.IsNullOrEmpty(Part.ThumbnailLargeUrl) && Part.AwaitingPreview)
            {
                <img src="@Part.ThumbnailSmallUrl" class="pdc-thumb-blurred" alt="@Part.Name" />
            }
            @* Sharp large thumbnail once ready *@
            @if (!string.IsNullOrEmpty(Part.ThumbnailLargeUrl))
            {
                <img src="@Part.ThumbnailLargeUrl"
                     class="pdc-thumb-sharp @(string.IsNullOrEmpty(Part.ThumbnailSmallUrl) ? "pdc-thumb-fade-in" : "")"
                     alt="@Part.Name"
                     @onerror="@(() => { Part.PreviewLoadFailed = true; StateHasChanged(); })" />
            }
            @* Fallback — no preview available *@
            @if (string.IsNullOrEmpty(Part.ThumbnailSmallUrl) && string.IsNullOrEmpty(Part.ThumbnailLargeUrl) && !Part.AwaitingPreview)
            {
                <MudIcon Icon="@GetFileIcon()" Size="Size.Large" Color="Color.Default" />
            }
            @* AnimatedCubeIcon — fades in on hover when GLB is available *@
            @if (!string.IsNullOrEmpty(Part.GlbStoragePath) && _showViewer3dBtn)
            {
                <div class="pdc-cube-overlay">
                    <AnimatedCubeIcon Size="48" OnClick="@(() => OnOpenViewer.InvokeAsync(Part))" />
                </div>
            }
        </div>

        <div class="pdc-viewer-meta">
            <MudText Typo="Typo.caption" Class="pdc-filename">@Part.Name</MudText>
            @if (Part.Dimensions != null)
            {
                <MudText Typo="Typo.caption" Color="Color.Secondary">
                    @($"{Part.Dimensions.X:N1} × {Part.Dimensions.Y:N1} × {Part.Dimensions.Z:N1} mm")
                </MudText>
            }
        </div>
    </div>

    <div class="pdc-config-col">
        @* Task 8 — configuration column *@
    </div>
</div>
```

- [ ] **Add blur animation CSS:**
```css
@@keyframes blur-pulse {
    0%   { filter: blur(8px); }
    100% { filter: blur(3px); }
}
.pdc-thumb-blurred {
    width: 100%;
    height: 100%;
    object-fit: cover;
    animation: blur-pulse 1.8s ease-in-out alternate infinite;
}
.pdc-thumb-sharp {
    width: 100%;
    height: 100%;
    object-fit: cover;
}
.pdc-thumb-fade-in {
    animation: fade-in 0.4s ease-in-out forwards;
}
@@keyframes fade-in {
    from { opacity: 0; }
    to   { opacity: 1; }
}
.pdc-thumb-area {
    position: relative;
    width: 100%;
    flex: 1;
    background: var(--mud-palette-background-grey);
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
    min-height: 200px;
}
.pdc-cube-overlay {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    animation: fade-in 0.2s ease-in-out;
    z-index: 2;
}
```

- [ ] **Add `@code` block** (`private bool _showViewer3dBtn;` + `GetFileIcon()` helper).

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/PartDetailCard.razor && git commit -m "feat: PartDetailCard viewer column with blur animation and AnimatedCubeIcon overlay"
```

---

## Task 8: PartDetailCard — Configuration Column

**Files:**
- Modify: `Maliev.Intranet.Client/Components/Project/PartDetailCard.razor`

Fill in the right 40% with the tab bar and configuration form. Config mutations fire `OnPartChanged` — **no direct API calls here**.

- [ ] **Add tab state to `@code`:**
```csharp
private int _activeTab = 0;  // 0=Configuration, 1=Drawings, 2=Bulk Pricing
```

- [ ] **Fill `pdc-config-col`:**
```razor
<div class="pdc-config-col">
    <MudTabs @bind-ActivePanelIndex="_activeTab" Rounded="false" Border="true" PanelClass="pa-3">

        <MudTabPanel Text="Configuration">
            @* Process — always shown first *@
            <div class="pdc-field-group">
                <MudSelect T="Guid?" Label="Process *"
                           Value="@Part.ProcessId"
                           ValueChanged="@OnProcessChanged"
                           Variant="Variant.Outlined"
                           Margin="Margin.Dense">
                    @foreach (var p in Processes)
                    {
                        <MudSelectItem T="Guid?" Value="@((Guid?)p.Id)">@p.Name</MudSelectItem>
                    }
                </MudSelect>
            </div>

            @* Material — skeleton until process selected and catalog loaded *@
            <div class="pdc-field-group">
                @if (Part.ProcessId == null)
                {
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="40px" Width="100%" Animation="Animation.Wave" />
                    <MudSkeleton Width="60%" Height="12px" Animation="Animation.Wave" Class="mt-1" />
                }
                else if (Part.CatalogLoading)
                {
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="40px" Width="100%" Animation="Animation.Wave" />
                }
                else
                {
                    <MudSelect T="Guid?" Label="Material *"
                               Value="@Part.MaterialId"
                               ValueChanged="@OnMaterialChanged"
                               Variant="Variant.Outlined"
                               Margin="Margin.Dense"
                               Disabled="@(Part.AvailableMaterials.Count == 0)">
                        @foreach (var m in Part.AvailableMaterials)
                        {
                            <MudSelectItem T="Guid?" Value="@((Guid?)m.Id)">@m.Name</MudSelectItem>
                        }
                    </MudSelect>
                }
            </div>

            @* Surface Finish *@
            <div class="pdc-field-group">
                @if (Part.ProcessId == null || Part.CatalogLoading)
                {
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="40px" Width="100%" Animation="Animation.Wave" />
                }
                else
                {
                    <MudSelect T="Guid?" Label="Surface Finish"
                               Value="@Part.FinishId"
                               ValueChanged="@OnFinishChanged"
                               Variant="Variant.Outlined"
                               Margin="Margin.Dense">
                        <MudSelectItem T="Guid?" Value="@((Guid?)null)">None</MudSelectItem>
                        @foreach (var f in Part.AvailableFinishes)
                        {
                            <MudSelectItem T="Guid?" Value="@((Guid?)f.Id)">@f.Name</MudSelectItem>
                        }
                    </MudSelect>
                }
            </div>

            @* Tolerance *@
            <div class="pdc-field-group">
                @if (Part.ProcessId == null || Part.CatalogLoading)
                {
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="40px" Width="100%" Animation="Animation.Wave" />
                }
                else
                {
                    <MudSelect T="Guid?" Label="Tolerance"
                               Value="@Part.ToleranceId"
                               ValueChanged="@OnToleranceChanged"
                               Variant="Variant.Outlined"
                               Margin="Margin.Dense">
                        <MudSelectItem T="Guid?" Value="@((Guid?)null)">Standard</MudSelectItem>
                        @foreach (var t in Part.AvailableTolerances)
                        {
                            <MudSelectItem T="Guid?" Value="@((Guid?)t.Id)">@t.Name</MudSelectItem>
                        }
                    </MudSelect>
                }
            </div>

            @* Quantity *@
            <div class="pdc-field-group">
                <MudNumericField T="int" Label="Quantity"
                                 Value="@Part.Quantity"
                                 ValueChanged="@OnQuantityChanged"
                                 Min="1" Max="999"
                                 Variant="Variant.Outlined"
                                 Margin="Margin.Dense" />
            </div>

            @* Part Notes *@
            <MudTextField Value="@Part.PartNotes"
                          ValueChanged="@((string? v) => NotifyChanged(p => p.PartNotes = v))"
                          Label="Part Notes"
                          Lines="3"
                          Variant="Variant.Outlined"
                          Margin="Margin.Dense"
                          MaxLength="1000" />

            @* DFM collapsible *@
            @if (Part.IsManifold == false)
            {
                <MudExpansionPanel Class="mt-2" Style="background:#fff7ed; border:1px solid #fed7aa;">
                    <TitleContent>
                        <MudText Color="Color.Warning" Typo="Typo.body2" Style="font-weight:600;">
                            ⚠ Non-manifold mesh detected
                        </MudText>
                    </TitleContent>
                    <ChildContent>
                        <MudText Typo="Typo.body2">This mesh may have geometry issues affecting manufacturing.</MudText>
                        <MudCheckBox T="bool" Value="@Part.DfmAcknowledged"
                                     ValueChanged="@((bool v) => NotifyChanged(p => p.DfmAcknowledged = v))"
                                     Label="I understand and want to proceed" />
                    </ChildContent>
                </MudExpansionPanel>
            }
        </MudTabPanel>

        <MudTabPanel Text="Drawings">
            @* Reuse DocumentUploadSection if it accepts a project/part ID parameter.
               Check Components/DocumentUploadSection.razor for its parameter signature. *@
            <MudText Color="Color.Secondary">Drawing uploads — wire to DocumentUploadSection</MudText>
        </MudTabPanel>

        <MudTabPanel Text="Bulk Pricing">
            <MudText Color="Color.Secondary">Bulk pricing tiers — implemented in Task 16</MudText>
        </MudTabPanel>
    </MudTabs>
</div>
```

- [ ] **Add event handlers to `@code`** that call `OnPartChanged` — **never mutate Part directly without notifying the parent**:
```csharp
private void NotifyChanged(Action<PartViewModel> mutate)
{
    mutate(Part);
    OnPartChanged.InvokeAsync(Part);
}

private void OnProcessChanged(Guid? processId)
{
    NotifyChanged(p =>
    {
        p.ProcessId = processId;
        p.ProcessCode = Processes.FirstOrDefault(x => x.Id == processId)?.Code;
        // Reset downstream selections and DFM acknowledgement on process change
        p.MaterialId = null; p.MaterialCode = null;
        p.FinishId = null; p.FinishCode = null;
        p.ToleranceId = null; p.ToleranceCode = null;
        p.DfmAcknowledged = false;
        p.AvailableMaterials = [];
        p.AvailableFinishes = [];
        p.AvailableTolerances = [];
    });
}

private void OnMaterialChanged(Guid? materialId)
    => NotifyChanged(p => {
        p.MaterialId = materialId;
        p.MaterialCode = p.AvailableMaterials.FirstOrDefault(x => x.Id == materialId)?.Code;
    });

private void OnFinishChanged(Guid? finishId)
    => NotifyChanged(p => {
        p.FinishId = finishId;
        p.FinishCode = p.AvailableFinishes.FirstOrDefault(x => x.Id == finishId)?.Code;
    });

private void OnToleranceChanged(Guid? toleranceId)
    => NotifyChanged(p => {
        p.ToleranceId = toleranceId;
        p.ToleranceCode = p.AvailableTolerances.FirstOrDefault(x => x.Id == toleranceId)?.Code;
    });

private void OnQuantityChanged(int qty)
    => NotifyChanged(p => p.Quantity = Math.Max(1, qty));
```

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/PartDetailCard.razor && git commit -m "feat: PartDetailCard configuration column with cascading skeleton selects"
```

---

## Task 9: ProjectQuotePanel

**Files:**
- Create: `Maliev.Intranet.Client/Components/Project/ProjectQuotePanel.razor`

- [ ] **Create `ProjectQuotePanel.razor`** with parameters:
```csharp
[Parameter] public List<PartViewModel> Parts { get; set; } = [];
[Parameter] public List<LeadTimeOptionDto> LeadTimeOptions { get; set; } = [];
[Parameter] public LeadTimeOptionDto? SelectedLeadTime { get; set; }
[Parameter] public EventCallback<LeadTimeOptionDto> OnLeadTimeChanged { get; set; }
[Parameter] public CurrencyDto? SelectedCurrency { get; set; }
[Parameter] public bool CanQuote { get; set; }
[Parameter] public bool Saving { get; set; }
[Parameter] public EventCallback OnQuote { get; set; }
```

- [ ] **Add computed helpers in `@code`:**
```csharp
// Maximum EstimatedLeadTimeDays across all priced parts
private int MaxLeadTimeDays =>
    Parts.Where(p => p.EstimatedTotalAmount.HasValue)
         .Select(p => p.EstimatedLeadTimeDays)
         .DefaultIfEmpty(0)
         .Max();

private bool HasPricedParts => Parts.Any(p => p.EstimatedTotalAmount.HasValue);

private decimal Subtotal => Parts.Sum(p => p.EstimatedTotalAmount ?? 0m);

private decimal QuoteTotal =>
    SelectedLeadTime == null ? Subtotal : Subtotal * SelectedLeadTime.PriceMultiplier;

// Derive displayed days for a lead time option from MaxLeadTimeDays
private (int min, int max) GetDisplayDays(LeadTimeOptionDto option)
{
    if (MaxLeadTimeDays == 0)
        return (option.MinDays, option.MaxDays);  // fall back to catalog ranges when no data

    return option.Code.ToUpperInvariant() switch
    {
        "EXPRESS" => (Math.Max((int)Math.Ceiling(MaxLeadTimeDays * 0.6), option.MinDays),
                      Math.Max((int)Math.Ceiling(MaxLeadTimeDays * 0.75), option.MinDays + 1)),
        "ECONOMY"  => (MaxLeadTimeDays + (int)Math.Ceiling(MaxLeadTimeDays * 0.3),
                       Math.Min(MaxLeadTimeDays + (int)Math.Ceiling(MaxLeadTimeDays * 0.5), option.MaxDays)),
        _          => (MaxLeadTimeDays, (int)Math.Ceiling(MaxLeadTimeDays * 1.2))  // STANDARD
    };
}

private string FormatPrice(decimal amount) =>
    $"{SelectedCurrency?.Symbol ?? "฿"}{amount:N2}";
```

- [ ] **Markup:**
```razor
<div class="pqp-root">
    <MudText Typo="Typo.subtitle2" Style="font-weight:600;" Class="mb-2">Project Quote</MudText>

    @* Lead time cards *@
    <MudText Typo="Typo.caption" Color="Color.Secondary" Style="text-transform:uppercase;letter-spacing:0.5px;font-weight:600;">
        Lead Time
    </MudText>
    <div class="pqp-lt-options mt-1 mb-3">
        @foreach (var opt in LeadTimeOptions)
        {
            var (dMin, dMax) = GetDisplayDays(opt);
            var isSelected = SelectedLeadTime?.Code == opt.Code;
            <div class="pqp-lt-card @(isSelected ? "pqp-lt-selected" : "")"
                 @onclick="@(() => OnLeadTimeChanged.InvokeAsync(opt))">
                <div>
                    <div class="pqp-lt-name">@opt.Name</div>
                    <div class="pqp-lt-days">
                        @if (!HasPricedParts) { <MudSkeleton Width="60px" Height="11px" Animation="Animation.Wave" /> }
                        else { <text>@dMin–@dMax business days</text> }
                    </div>
                </div>
                <div class="pqp-lt-price">
                    @if (!HasPricedParts) { <MudSkeleton Width="50px" Height="14px" Animation="Animation.Wave" /> }
                    else { @FormatPrice(Subtotal * opt.PriceMultiplier) }
                </div>
            </div>
        }
    </div>

    <MudDivider Class="mb-2" />

    @* Per-part summary rows *@
    <MudText Typo="Typo.caption" Color="Color.Secondary" Style="text-transform:uppercase;letter-spacing:0.5px;font-weight:600;" Class="mb-1">
        Summary
    </MudText>
    @if (Parts.Count == 0)
    {
        <MudText Typo="Typo.body2" Color="Color.Secondary" Class="py-3" Style="text-align:center;">
            Add and configure parts to see pricing
        </MudText>
    }
    else
    {
        @foreach (var part in Parts)
        {
            <div class="pqp-part-row">
                <MudTooltip Text="@part.Name">
                    <span class="pqp-part-name">@part.Name</span>
                </MudTooltip>
                <span class="pqp-part-qty">×@part.Quantity</span>
                <span class="pqp-part-price">
                    @if (part.PricingLoading) { <MudSkeleton Width="55px" Height="12px" Animation="Animation.Wave" /> }
                    else if (part.PricingFailed) { <MudIcon Icon="@Icons.Material.Outlined.Warning" Size="Size.Small" Color="Color.Warning" /> }
                    else if (part.EstimatedTotalAmount.HasValue) { @FormatPrice(part.EstimatedTotalAmount.Value) }
                    else { <text>—</text> }
                </span>
            </div>
        }
    }

    <MudDivider Class="my-2" />

    @* Totals *@
    <div class="pqp-totals">
        <div class="pqp-total-row"><span>Subtotal</span><span>@FormatPrice(Subtotal)</span></div>
        @if (SelectedLeadTime != null && SelectedLeadTime.PriceMultiplier != 1.0m)
        {
            <div class="pqp-total-row">
                <span>Lead time adjustment</span>
                <span>×@SelectedLeadTime.PriceMultiplier.ToString("N2")</span>
            </div>
        }
        <div class="pqp-total-grand"><span>Total</span><span>@FormatPrice(QuoteTotal)</span></div>
    </div>

    @* DFM warning banner *@
    @if (Parts.Any(p => p.IsManifold == false && !p.DfmAcknowledged))
    {
        <MudAlert Severity="Severity.Warning" Class="mt-2" Dense="true">
            ⚠ @(Parts.Count(p => p.IsManifold == false && !p.DfmAcknowledged)) part(s) have unacknowledged DFM warnings
        </MudAlert>
    }

    @* Quote button *@
    <MudButton Variant="Variant.Filled"
               Color="Color.Primary"
               FullWidth="true"
               Class="mt-3"
               Disabled="@(!CanQuote)"
               OnClick="OnQuote">
        @if (Saving)
        {
            <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
            <text>Creating...</text>
        }
        else { <text>Quote</text> }
    </MudButton>
</div>
```

- [ ] **Add panel CSS** (fixed width, right border, scrollable, matches reference design right panel).

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/ProjectQuotePanel.razor && git commit -m "feat: ProjectQuotePanel with queue-aware lead time display and live totals"
```

---

## Task 10: Upload & Polling Migration

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

Port the full upload + polling logic from the old `ProjectNew.razor` into `ProjectNew.razor.cs`, adapted to use `PartViewModel` instead of `UploadedFileState`.

- [ ] **Implement `HandleFileSelected`** — same logic as old version but creates `PartViewModel`:
```csharp
private async Task HandleFileSelected(IReadOnlyList<IBrowserFile> files)
{
    _dragOver = false;
    if (files.Count == 0) return;

    const long maxBytes = 200L * 1024 * 1024;
    var newFiles = files.Where(f =>
    {
        var ext = Path.GetExtension(f.Name);
        if (!AllowedExtensions.Contains(ext)) { Snackbar.Add($"'{ext}' not supported.", Severity.Error); return false; }
        if (_parts.Any(u => u.Name == f.Name)) { Snackbar.Add($"'{f.Name}' already added.", Severity.Warning); return false; }
        return true;
    }).ToList();

    var newParts = newFiles.Select(f => new PartViewModel
    {
        Name = f.Name,
        Uploading = true,
        StatusText = "Uploading...",
    }).ToList();

    _parts.AddRange(newParts);
    if (_selectedPartIndex >= _parts.Count - newParts.Count)
        _selectedPartIndex = _parts.Count - newParts.Count;
    StateHasChanged();

    await Task.WhenAll(newFiles.Zip(newParts, (file, part) => UploadFileAsync(file, part, maxBytes)));
    StateHasChanged();
}
```

- [ ] **Implement `UploadFileAsync`:**
```csharp
private async Task UploadFileAsync(IBrowserFile browserFile, PartViewModel part, long maxBytes)
{
    try
    {
        await using var stream = browserFile.OpenReadStream(maxBytes);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", browserFile.Name);
        var response = await Http.PostAsync($"api/uploads?projectId={_tempProjectId}", content);
        if (response.IsSuccessStatusCode)
        {
            var uploaded = await response.Content.ReadFromJsonAsync<BffUploadResponse>();
            part.FileId = Guid.TryParse(uploaded?.UploadId, out var id) ? id : Guid.Empty;
            part.StoragePath = uploaded?.StoragePath;
            part.Uploading = false;
            if (Is3DFile(browserFile.Name)) { part.AwaitingPreview = true; part.StatusText = "Processing 3D file..."; _ = PollAnalysisStatusAsync(part); }
        }
        else
        {
            part.Error = $"HTTP {(int)response.StatusCode}";
            part.Uploading = false;
        }
    }
    catch (Exception ex) { part.Error = ex.Message; part.Uploading = false; }
    await InvokeAsync(StateHasChanged);
}
```

- [ ] **Implement `PollAnalysisStatusAsync`** — same logic as original but populates `PartViewModel` fields:
```csharp
private async Task PollAnalysisStatusAsync(PartViewModel part)
{
    if (string.IsNullOrEmpty(part.StoragePath)) return;
    var cts = new CancellationTokenSource();
    _pollingTokens[part.StoragePath] = cts;
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var status = await Http.GetFromJsonAsync<FileAnalysisStatusDto>(
                    $"api/uploads/analysis-status?storagePath={Uri.EscapeDataString(part.StoragePath)}", cts.Token);
                if (status == null) break;

                // Populate preview URLs
                part.ThumbnailSmallUrl = ResolvePreviewUrl(status.PreviewUrls?.ThumbnailSmall ?? status.ThumbnailUrl);
                part.ThumbnailLargeUrl = ResolvePreviewUrl(status.HiResThumbnailUrl ?? status.PreviewUrls?.ThumbnailLargeUrl);
                part.GlbStoragePath = status.GlbStoragePath;
                part.Dimensions = status.Dimensions;
                part.VolumeMm3 = status.Dimensions?.VolumeMm3;
                part.IsManifold = status.IsManifold;

                switch (status.Status)
                {
                    case FileAnalysisStatus.Completed:
                        if (status.PreviewProcessingStatus is PreviewProcessingStatus.Completed or PreviewProcessingStatus.Failed)
                        {
                            part.AwaitingPreview = false;
                            part.StatusText = "Ready";
                            cts.Cancel();
                        }
                        else { part.StatusText = "Generating preview..."; }
                        break;
                    case FileAnalysisStatus.Failed:
                        part.AwaitingPreview = false;
                        part.Error = status.ErrorCode;
                        cts.Cancel();
                        break;
                    default:
                        part.StatusText = "Processing 3D file...";
                        break;
                }
                await InvokeAsync(StateHasChanged);
            }
            catch (OperationCanceledException) { break; }
            catch { /* continue on network errors */ }
            await Task.Delay(PollingIntervalMs, cts.Token);
        }
    }
    finally { _pollingTokens.Remove(part.StoragePath!); cts.Dispose(); }
}
```

- [ ] **Implement `RemovePart`:**
```csharp
private void RemovePart(PartViewModel part)
{
    if (!string.IsNullOrEmpty(part.StoragePath) && _pollingTokens.TryGetValue(part.StoragePath, out var cts))
    { cts.Cancel(); cts.Dispose(); _pollingTokens.Remove(part.StoragePath); }
    if (_pricingTokens.TryGetValue(part.FileId, out var pcts))
    { pcts.Cancel(); pcts.Dispose(); _pricingTokens.Remove(part.FileId); }
    _parts.Remove(part);
    _selectedPartIndex = Math.Min(_selectedPartIndex, Math.Max(0, _parts.Count - 1));
    TriggerAutoSave();
    StateHasChanged();
}
```

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Pages/ProjectNew.razor.cs && git commit -m "feat: upload and polling migrated to PartViewModel in ProjectNew.razor.cs"
```

---

## Task 11: Init — Currencies, Processes, Lead Times + Draft Restore

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

- [ ] **Implement `OnInitializedAsync`:**
```csharp
protected override async Task OnInitializedAsync()
{
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    if (authState.User.Identity?.IsAuthenticated != true) return;

    // Load catalogs + currency + draft in parallel
    await Task.WhenAll(
        LoadCurrenciesAsync(),
        LoadProcessesAsync(),
        LoadLeadTimesAsync(),
        RestoreDraftAsync());
}

private async Task LoadCurrenciesAsync()
{
    try
    {
        _currencies = await Http.GetFromJsonAsync<List<CurrencyDto>>("api/referencedata/currencies") ?? [];
        // Default to primary (THB) if not restored from draft
        if (_selectedCurrency == null)
        {
            _selectedCurrency = _currencies.FirstOrDefault(c => c.IsPrimary)
                ?? await Http.GetFromJsonAsync<CurrencyDto>("api/referencedata/currencies/primary");
        }
    }
    catch { Snackbar.Add("Failed to load currencies", Severity.Warning); }
}

private async Task LoadProcessesAsync()
{
    try { _processes = await Http.GetFromJsonAsync<List<ProcessDto>>("api/catalog/processes") ?? []; }
    catch { Snackbar.Add("Failed to load processes", Severity.Warning); }
}

private async Task LoadLeadTimesAsync()
{
    try
    {
        _leadTimeOptions = await Http.GetFromJsonAsync<List<LeadTimeOptionDto>>("api/pricing/lead-times") ?? [];
        if (_selectedLeadTime == null)
            _selectedLeadTime = _leadTimeOptions.FirstOrDefault(l => l.IsDefault) ?? _leadTimeOptions.FirstOrDefault();
    }
    catch { Snackbar.Add("Failed to load lead time options", Severity.Warning); }
}
```

- [ ] **Implement `RestoreDraftAsync`** (sessionStorage — no separate .js file needed):
```csharp
private async Task RestoreDraftAsync()
{
    try
    {
        var json = await JS.InvokeAsync<string?>("sessionStorage.getItem", DraftStorageKey);
        if (string.IsNullOrEmpty(json)) return;

        var draft = System.Text.Json.JsonSerializer.Deserialize<DraftProjectState>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (draft == null) return;

        var savedId = Guid.TryParse(draft.TempProjectId, out var sid) ? sid : Guid.Empty;
        if (savedId != Guid.Empty && savedId != _tempProjectId)
        {
            // Different session — prompt user
            var resume = await ShowResumeDraftDialogAsync();
            if (!resume) { await JS.InvokeVoidAsync("sessionStorage.removeItem", DraftStorageKey); return; }
            _tempProjectId = savedId;
        }

        _title = draft.Title ?? string.Empty;
        _description = draft.Description;
        if (draft.CustomerId.HasValue)
        {
            _selectedCustomer = new CustomerSummaryDto { Id = draft.CustomerId.Value, Name = draft.CustomerName ?? string.Empty };
            _showCustomerSearch = false;
        }
        if (!string.IsNullOrEmpty(draft.SelectedLeadTimeCode))
            _selectedLeadTime = _leadTimeOptions.FirstOrDefault(l => l.Code == draft.SelectedLeadTimeCode) ?? _selectedLeadTime;

        _parts.AddRange((draft.Parts ?? []).Select(PartViewModel.FromDraftPartState));

        // Re-trigger pricing for fully configured parts
        foreach (var part in _parts.Where(p => p.IsFullyConfigured))
            _ = TriggerPricingAsync(part);
    }
    catch { /* corrupt draft — ignore */ }
}

private async Task<bool> ShowResumeDraftDialogAsync()
{
    var result = await DialogService.ShowMessageBox(
        "Resume draft?",
        "A previous draft was found. Resume it or start fresh?",
        yesText: "Resume", noText: "Start fresh");
    return result == true;
}
```

> **Note:** Add `[Inject] IDialogService DialogService { get; set; } = null!;` to `ProjectNew.razor` directives, or inject via `@inject IDialogService DialogService` in the `.razor` file.

> **Note:** Check `DraftProjectState` fields in `ProjectDraftDtos.cs`. Adjust property names (`TempProjectId`, `CustomerId`, `CustomerName`, `SelectedLeadTimeCode`, `Parts`) to match exactly.

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Pages/ProjectNew.razor.cs && git commit -m "feat: init loads currencies/processes/lead-times and restores sessionStorage draft"
```

---

## Task 12: Cascading Catalog Dropdowns

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

When `OnPartChanged` fires and `ProcessId` has changed, load materials. When `MaterialId` changes, optionally reload finishes filtered by material. All loading happens in the parent.

- [ ] **Implement `OnPartChanged`:**
```csharp
private void OnPartChanged(PartViewModel part)
{
    TriggerAutoSave();
    // Load catalog if process changed and materials not yet loaded
    if (part.ProcessId.HasValue && part.ProcessCode != null && part.AvailableMaterials.Count == 0 && !part.CatalogLoading)
        _ = LoadPartCatalogAsync(part);
    // Trigger pricing if fully configured
    if (part.IsFullyConfigured)
        _ = TriggerPricingAsync(part);
    StateHasChanged();
}

private async Task LoadPartCatalogAsync(PartViewModel part)
{
    if (string.IsNullOrEmpty(part.ProcessCode)) return;
    part.CatalogLoading = true;
    StateHasChanged();
    try
    {
        // Load materials, finishes, tolerances in parallel
        var (materials, finishes, tolerances) = await (
            Http.GetFromJsonAsync<List<CatalogMaterialDto>>($"api/catalog/processes/{part.ProcessCode}/materials") ?? Task.FromResult(new List<CatalogMaterialDto>()),
            Http.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>($"api/catalog/processes/{part.ProcessCode}/finishes") ?? Task.FromResult(new List<CatalogSurfaceFinishDto>()),
            Http.GetFromJsonAsync<List<CatalogToleranceDto>>($"api/catalog/processes/{part.ProcessCode}/tolerances") ?? Task.FromResult(new List<CatalogToleranceDto>())
        ).WhenAll();
        part.AvailableMaterials = materials ?? [];
        part.AvailableFinishes = finishes ?? [];
        part.AvailableTolerances = tolerances ?? [];
    }
    catch { Snackbar.Add($"Failed to load catalog for {part.ProcessCode}", Severity.Warning); }
    finally { part.CatalogLoading = false; await InvokeAsync(StateHasChanged); }
}
```

> **Note:** The tuple destructuring with `WhenAll` requires a helper or use `Task.WhenAll` with separate awaits. Simplest correct version:
```csharp
var t1 = Http.GetFromJsonAsync<List<CatalogMaterialDto>>($"api/catalog/processes/{part.ProcessCode}/materials");
var t2 = Http.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>($"api/catalog/processes/{part.ProcessCode}/finishes");
var t3 = Http.GetFromJsonAsync<List<CatalogToleranceDto>>($"api/catalog/processes/{part.ProcessCode}/tolerances");
await Task.WhenAll(t1, t2, t3);
part.AvailableMaterials = t1.Result ?? [];
part.AvailableFinishes = t2.Result ?? [];
part.AvailableTolerances = t3.Result ?? [];
```

- [ ] **Build, commit.**

---

## Task 13: Debounced Pricing Integration

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

- [ ] **Implement `TriggerPricingAsync`:**
```csharp
private async Task TriggerPricingAsync(PartViewModel part)
{
    if (!part.IsFullyConfigured || part.Dimensions == null) return;
    if (part.MaterialId == null || part.ProcessId == null || _selectedCustomer == null) return;

    // Cancel previous debounce for this part
    if (_pricingTokens.TryGetValue(part.FileId, out var old)) { await old.CancelAsync(); old.Dispose(); }
    var cts = new CancellationTokenSource();
    _pricingTokens[part.FileId] = cts;

    try
    {
        await Task.Delay(PricingDebounceMs, cts.Token);

        part.PricingLoading = true;
        part.PricingFailed = false;
        await InvokeAsync(StateHasChanged);

        var request = new PricingRequestDto
        {
            FileId = part.FileId,
            CustomerId = _selectedCustomer.Id,
            MaterialId = part.MaterialId.Value,
            MaterialCode = part.MaterialCode ?? string.Empty,
            ManufacturingProcessId = part.ProcessId.Value,
            ManufacturingProcessName = part.ProcessCode ?? string.Empty,
            Quantity = part.Quantity,
            StoragePath = part.StoragePath,
            CorrelationId = _tempProjectId.ToString(),
            Geometry = new GeometryMetricsDto
            {
                // VolumeMm3 → VolumeCm3 conversion (÷ 1000)
                VolumeCm3 = part.VolumeMm3.HasValue ? (decimal)(part.VolumeMm3.Value / 1000.0) : 0m,
                SupportVolumeCm3 = 0m,   // not available from file analysis
                SurfaceAreaCm2 = 0m,     // not available from file analysis
                BoundingBoxX = (decimal)(part.Dimensions?.X ?? 0),
                BoundingBoxY = (decimal)(part.Dimensions?.Y ?? 0),
                BoundingBoxZ = (decimal)(part.Dimensions?.Z ?? 0),
                IsManifold = part.IsManifold ?? true,
                TriangleCount = 0,       // not available from file analysis
            },
        };

        var result = await Http.PostAsJsonAsync("api/pricing/calculate", request, cts.Token)
            .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<PricingResultDto>(cancellationToken: cts.Token));
        var pricing = await result;

        if (pricing != null)
        {
            part.EstimatedUnitPrice = pricing.TotalUnitPrice;
            part.EstimatedTotalAmount = pricing.TotalPrice;
            part.EstimatedLeadTimeDays = pricing.EstimatedLeadTimeDays ?? 0;
            part.PricingLoading = false;
        }
    }
    catch (OperationCanceledException) { /* superseded by newer call */ }
    catch { part.PricingFailed = true; part.PricingLoading = false; }
    finally { _pricingTokens.Remove(part.FileId); await InvokeAsync(StateHasChanged); }
}
```

- [ ] **Build, commit.**

---

## Task 14: Draft Auto-Save

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

- [ ] **Implement `TriggerAutoSave`:**
```csharp
private void TriggerAutoSave()
{
    _autoSaveCts?.Cancel();
    _autoSaveCts?.Dispose();
    _autoSaveCts = new CancellationTokenSource();
    var token = _autoSaveCts.Token;
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(AutoSaveDebounceMs, token);
            await InvokeAsync(async () =>
            {
                _autoSaving = true;
                StateHasChanged();
                await SaveDraftAsync();
                _autoSaving = false;
                _lastSavedAt = DateTimeOffset.Now;
                StateHasChanged();
            });
        }
        catch (OperationCanceledException) { }
    }, token);
}

private async Task SaveDraftAsync()
{
    try
    {
        var draft = new DraftProjectState
        {
            TempProjectId = _tempProjectId.ToString(),
            Title = _title,
            Description = _description,
            CustomerId = _selectedCustomer?.Id,
            CustomerName = _selectedCustomer?.Name,
            SelectedLeadTimeCode = _selectedLeadTime?.Code ?? "STANDARD",
            Parts = _parts.Select(p => p.ToDraftPartState()).ToList(),
        };
        var json = System.Text.Json.JsonSerializer.Serialize(draft);
        await JS.InvokeVoidAsync("sessionStorage.setItem", DraftStorageKey, json);
    }
    catch { /* non-fatal */ }
}
```

> **Note:** Check `DraftProjectState` property names in `ProjectDraftDtos.cs` and align them in `SaveDraftAsync`.

- [ ] **Build, commit.**

---

## Task 15: Quote Submission

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

- [ ] **Implement `CreateProjectAndQuoteAsync`:**
```csharp
private async Task CreateProjectAndQuoteAsync()
{
    if (_selectedCustomer == null || string.IsNullOrWhiteSpace(_title) || _selectedLeadTime == null) return;
    _saving = true;
    StateHasChanged();
    try
    {
        // 1. Create project
        var projectRequest = new CreateProjectRequest
        {
            CustomerId = _selectedCustomer.Id,
            CustomerName = _selectedCustomer.Name,
            Title = _title,
            Description = _description,
            // Currency field — check CreateProjectRequest in ProjectDtos.cs for exact field name
        };
        var projectResponse = await Http.PostAsJsonAsync("api/projects", projectRequest);
        if (!projectResponse.IsSuccessStatusCode)
        {
            Snackbar.Add("Failed to create project.", Severity.Error);
            return;
        }
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDetailDto>();
        if (project == null) { Snackbar.Add("Unexpected response from server.", Severity.Error); return; }

        // 2. Add parts
        var validParts = _parts.Where(p => p.FileId != Guid.Empty && p.Error == null).ToList();
        foreach (var part in validParts)
        {
            await Http.PostAsJsonAsync($"api/projects/{project.Id}/parts", new AddProjectPartRequest
            {
                FileId = part.FileId,
                FileName = part.Name,
                Quantity = part.Quantity,
                // Check AddProjectPartRequest for process/material/finish/tolerance fields
            });
        }

        // 3. Create quotation
        // Check CreateQuotationDialog.razor for the existing quotation creation pattern.
        // CreateQuotationRequest has: CustomerId, BillingIdentityType, ValidityPeriodStart/End, Items, DeliveryExpectations
        var quotationRequest = new CreateQuotationRequest
        {
            CustomerId = _selectedCustomer.Id,
            BillingIdentityType = BillingIdentityType.Company,  // check enum values
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
            DeliveryExpectations = _selectedLeadTime.Name,
            Items = validParts.Select(p => new QuotationItemDto
            {
                Description = $"{p.Name} ×{p.Quantity}",
                Quantity = p.Quantity,
                UnitPrice = p.EstimatedUnitPrice ?? 0m,
            }).ToList(),
        };
        var quotationResponse = await Http.PostAsJsonAsync("api/quotations", quotationRequest);
        if (!quotationResponse.IsSuccessStatusCode)
        {
            Snackbar.Add("Project created but failed to generate quotation.", Severity.Warning);
            Navigation.NavigateTo($"/sales/projects/{project.Id}");
            return;
        }
        var quotation = await quotationResponse.Content.ReadFromJsonAsync<QuotationSummaryDto>();

        // 4. Success
        await JS.InvokeVoidAsync("sessionStorage.removeItem", DraftStorageKey);
        Snackbar.Add($"Project {project.ProjectNumber} created.", Severity.Success);
        Navigation.NavigateTo($"/sales/quotations/{quotation?.Id ?? project.Id}");
    }
    catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
    finally { _saving = false; StateHasChanged(); }
}
```

> **IMPORTANT:** `CreateQuotationRequest` does not have a `ProjectId` field. Check `CreateQuotationDialog.razor` to see how existing code links a project to a quotation after creation (there may be a separate `PATCH` or a field on the request not visible here). Replicate that pattern.

- [ ] **Implement `DuplicateProject`:**
```csharp
private void DuplicateProject()
{
    _tempProjectId = Guid.NewGuid();
    _title = string.IsNullOrWhiteSpace(_title) ? "Copy" : $"{_title} (copy)";
    _lastSavedAt = null;
    // Deep-copy parts (reset upload/pricing state; keep config)
    var copies = _parts.Select(p => new PartViewModel
    {
        Name = p.Name, FileId = p.FileId, StoragePath = p.StoragePath,
        ProcessCode = p.ProcessCode, ProcessId = p.ProcessId,
        MaterialCode = p.MaterialCode, MaterialId = p.MaterialId,
        FinishCode = p.FinishCode, FinishId = p.FinishId,
        ToleranceCode = p.ToleranceCode, ToleranceId = p.ToleranceId,
        Quantity = p.Quantity, PartNotes = p.PartNotes,
        ThumbnailSmallUrl = p.ThumbnailSmallUrl, ThumbnailLargeUrl = p.ThumbnailLargeUrl,
        Dimensions = p.Dimensions, VolumeMm3 = p.VolumeMm3, IsManifold = p.IsManifold,
        AvailableMaterials = p.AvailableMaterials, AvailableFinishes = p.AvailableFinishes,
        AvailableTolerances = p.AvailableTolerances,
    }).ToList();
    _parts.Clear();
    _parts.AddRange(copies);
    TriggerAutoSave();
    StateHasChanged();
}
```

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Pages/ProjectNew.razor.cs && git commit -m "feat: quote submission, duplicate project, auto-save draft"
```

---

## Task 16: BabylonJS Viewer Dialog

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor`
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

- [ ] **Check `Components/ModelViewer.razor`** — read it to understand its exact `[Parameter]` names (e.g. `StoragePath`, `GlbPath`, `GlbUrl`). This component wraps `wwwroot/js/babylon-viewer.js`.

- [ ] **Add `OpenBabylonViewer` method** to `ProjectNew.razor.cs`:
```csharp
private async Task OpenBabylonViewer(PartViewModel part)
{
    if (string.IsNullOrEmpty(part.GlbStoragePath)) return;
    var parameters = new DialogParameters { ["GlbStoragePath"] = part.GlbStoragePath };
    // Use MudDialog with ModelViewer inside, or check if a viewer dialog already exists
    await DialogService.ShowAsync<BabylonViewerDialog>(part.Name, parameters,
        new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true });
}
```

- [ ] **Create `Components/Project/BabylonViewerDialog.razor`** wrapping `ModelViewer`:
```razor
@namespace Maliev.Intranet.Client.Components.Project

<MudDialog>
    <TitleContent>@Title</TitleContent>
    <DialogContent>
        <ModelViewer GlbStoragePath="@GlbStoragePath" Style="width:100%; height:70vh;" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@(() => MudDialog.Close())">Close</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? GlbStoragePath { get; set; }
}
```

> Adjust `ModelViewer` parameter names to match the actual component signature.

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Components/Project/BabylonViewerDialog.razor Maliev.Intranet/Maliev.Intranet.Client/Pages/ProjectNew.razor.cs && git commit -m "feat: BabylonJS viewer dialog via AnimatedCubeIcon hover overlay"
```

---

## Task 17: Summary Table Mode

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor`

Fill in the `SummaryTable` mode branch (currently a placeholder in Task 3).

- [ ] **Replace the `pn-table-placeholder` div** with a `MudDataGrid`:
```razor
<MudDataGrid T="PartViewModel"
             Items="@_parts"
             Dense="true"
             Hover="true"
             Filterable="false"
             SortMode="SortMode.None">
    <Columns>
        <TemplateColumn Title="Part" HeaderStyle="width:40%">
            <CellTemplate>
                <div style="display:flex;align-items:center;gap:8px;">
                    @if (!string.IsNullOrEmpty(context.Item.ThumbnailSmallUrl))
                    {
                        <img src="@context.Item.ThumbnailSmallUrl" style="width:32px;height:32px;object-fit:cover;border-radius:4px;" />
                    }
                    <span>@context.Item.Name</span>
                </div>
            </CellTemplate>
        </TemplateColumn>
        <PropertyColumn Property="p => p.ProcessCode" Title="Process" />
        <PropertyColumn Property="p => p.MaterialCode" Title="Material" />
        <PropertyColumn Property="p => p.FinishCode" Title="Finish" />
        <PropertyColumn Property="p => p.ToleranceCode" Title="Tolerance" />
        <PropertyColumn Property="p => p.Quantity" Title="Qty" />
        <TemplateColumn Title="Unit Price">
            <CellTemplate>
                @if (context.Item.PricingLoading) { <MudSkeleton Width="60px" /> }
                else { @($"{_selectedCurrency?.Symbol ?? "฿"}{context.Item.EstimatedUnitPrice:N2}") }
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="Total">
            <CellTemplate>
                @if (context.Item.PricingLoading) { <MudSkeleton Width="70px" /> }
                else { @($"{_selectedCurrency?.Symbol ?? "฿"}{context.Item.EstimatedTotalAmount:N2}") }
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="" HeaderStyle="width:50px">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Outlined.Delete" Size="Size.Small" Color="Color.Error"
                               OnClick="@(() => RemovePart(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

- [ ] **Build, commit:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore 2>&1 | tail -5
git add Maliev.Intranet/Maliev.Intranet.Client/Pages/ProjectNew.razor && git commit -m "feat: summary table layout mode with MudDataGrid"
```

---

## Task 18: Final Build & Smoke Check

- [ ] **Full clean build:**
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Client/Maliev.Intranet.Client.csproj 2>&1 | tail -10
```
Expected: `Build succeeded. 0 Error(s)` (warnings are acceptable).

- [ ] **Also build the BFF** to ensure no shared DTO mismatches:
```bash
cd B:/maliev && dotnet build Maliev.Intranet/Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj 2>&1 | tail -5
```

- [ ] **Manual smoke checklist** (run the app and verify):
  - [ ] Page loads at `/sales/projects/new` with no app shell chrome
  - [ ] Currency autocomplete defaults to THB
  - [ ] Uploading an STL file → card appears in carousel with upload bar → thumbnail appears blurred → sharpens
  - [ ] Selecting a process → Material/Finish/Tolerance load and appear (skeletons show while loading)
  - [ ] Configuring a part fully → price appears in carousel card and quote panel
  - [ ] Lead time cards show queue-aware days once pricing is returned
  - [ ] Refreshing page → draft restore dialog appears (if TempProjectId differs)
  - [ ] Layout toggle switches to table view showing all parts
  - [ ] Duplicate button creates a copy with "(copy)" suffix
  - [ ] Hovering large thumbnail shows AnimatedCubeIcon (when GLB available)
  - [ ] Quote button creates project + quotation + navigates to QuotationDetail

- [ ] **Final commit:**
```bash
cd B:/maliev && git add -A && git commit -m "feat: ProjectNew full quoting workspace — complete"
```
