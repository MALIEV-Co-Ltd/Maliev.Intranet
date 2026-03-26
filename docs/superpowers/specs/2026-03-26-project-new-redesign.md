# ProjectNew Page Redesign — Spec
**Date:** 2026-03-26
**Status:** Approved

---

## Overview

Rebuild `ProjectNew.razor` from a simple submission form into a full-screen, self-contained quoting workspace. The page uses `@layout EmptyLayout` (no app shell chrome). The reference design is `_designs/new-project-design/new-project-page-reference-design.html`.

The user creates a project, configures each uploaded part (process / material / finish / tolerance), gets live AI-driven pricing per part, selects a queue-aware lead time, then submits — which creates the project, adds all parts, generates a quotation, and navigates to `QuotationDetail`.

---

## Layout

Three-panel layout filling the full viewport height (no scroll on the outer shell):

```
┌─────────────────────────────────────────────────────────────┐
│  ProjectTopBar  (fixed height ~52px)                        │
├──────────┬──────────────────────────────────┬───────────────┤
│  Left    │  Center                          │  Right        │
│  Panel   │  ┌──────────────────────────┐    │  Quote        │
│  340px   │  │  PartCarousel (row)      │    │  Panel        │
│  resiz.  │  └──────────────────────────┘    │  280px        │
│          │  ┌──────────────────────────┐    │  resiz.       │
│          │  │  PartDetailCard          │    │               │
│          │  │  (60% viewer | 40% cfg)  │    │               │
│          │  └──────────────────────────┘    │               │
└──────────┴──────────────────────────────────┴───────────────┘
```

Panels are fixed width (no drag handles). Two layout modes toggled from topbar:
- **Configurator** — 3-panel as above
- **Summary Table** — left panel collapses to icon strip; center becomes `MudDataGrid` of all parts; right panel stays

---

## File Structure

```
Pages/
  ProjectNew.razor              @page "/sales/projects/new", @layout EmptyLayout
  ProjectNew.razor.cs           All C# state, API calls, event handlers

Components/Project/
  ProjectTopBar.razor
  ProjectLeftPanel.razor
  PartCarousel.razor
  PartMiniCard.razor
  PartDetailCard.razor
  ProjectQuotePanel.razor
```

`ProjectNew.razor` is a layout-only shell. All business logic lives in `ProjectNew.razor.cs`.

---

## State Model (`ProjectNew.razor.cs`)

### Project-level
```csharp
Guid _tempProjectId                        // non-readonly; reassigned on Duplicate action
string _title, string? _description
CustomerSummaryDto? _selectedCustomer
bool _showCustomerSearch
CurrencyDto? _selectedCurrency             // defaulted to THB on init from CurrencyService
List<CurrencyDto> _currencies
List<LeadTimeOptionDto> _leadTimeOptions   // loaded once from catalog on init
LeadTimeOptionDto? _selectedLeadTime       // defaulted to IsDefault option
List<ProcessDto> _processes                // loaded once from catalog on init
bool _saving, bool _autoSaving
DateTimeOffset? _lastSavedAt
LayoutMode _layoutMode                     // Configurator | SummaryTable
bool _dragOver
MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload
```

Note: `_tempProjectId` must **not** be `readonly` — the Duplicate action reassigns it.

### Per-part state — `PartViewModel`
Defined in `ProjectNew.razor.cs`. Passed to child components as `[Parameter] PartViewModel Part` (or `List<PartViewModel> Parts`). All configuration mutations flow back to the parent via `EventCallback<PartViewModel> OnPartChanged` so the parent's pricing debounce fires centrally. Children never call `StateHasChanged` directly for config changes.

```csharp
// Persisted to DraftPartState:
string Name
Guid FileId
string? StoragePath
string? ProcessCode, Guid? ProcessId
string? MaterialCode, Guid? MaterialId
string? FinishCode, Guid? FinishId
string? ToleranceCode, Guid? ToleranceId
int Quantity
string? PartNotes
bool DfmAcknowledged     // resets to false when ProcessCode changes

// Upload / preview (populated by polling — see source fields below):
bool Uploading
bool AwaitingPreview
string? ThumbnailSmallUrl    // from: status.PreviewUrls?.ThumbnailSmall ?? status.ThumbnailUrl
string? ThumbnailLargeUrl    // from: status.HiResThumbnailUrl ?? status.PreviewUrls?.ThumbnailLargeUrl
string? GlbStoragePath       // from: status.GlbStoragePath
bool PreviewLoadFailed
string StatusText
FileAnalysisDimensionsDto? Dimensions
double? VolumeMm3
bool? IsManifold
string? Error

// Pricing (UI-only, not persisted):
decimal? EstimatedUnitPrice
decimal? EstimatedTotalAmount
int EstimatedLeadTimeDays    // from last PricingResult; used for lead time day calculation
bool PricingLoading
bool PricingFailed

// Catalog (UI-only, not persisted):
List<CatalogMaterialDto> AvailableMaterials
List<CatalogSurfaceFinishDto> AvailableFinishes
List<CatalogToleranceDto> AvailableTolerances
bool CatalogLoading          // skeleton state while cascading dropdowns load
```

### Selection
```csharp
int _selectedPartIndex       // which PartViewModel is shown in PartDetailCard
```

---

## Component Designs

### ProjectTopBar
- **Left:** back arrow → `/sales/projects`, inline-editable project title (click → `<input>`, blur → styled text), `Draft` chip
- **Center:** `MudAutocomplete<CurrencyDto>` searching currency list; default THB
- **Right:** "Saved X sec ago" label, Duplicate icon button, layout toggle `MudToggleIconButton` (grid ↔ table icons)

**Duplicate action:** generates new `_tempProjectId`, deep-copies all `PartViewModel` list, appends " (copy)" to `_title`, clears `_lastSavedAt`, triggers draft auto-save.

### ProjectLeftPanel
Three sections separated by dividers:
1. **Customer** — existing `MudAutocomplete` search / selected customer card (preserved exactly)
2. **Project** — title + description `MudTextField`s with validation (preserved)
3. **Upload** — existing drop zone + hidden `MudFileUpload` (preserved)

### PartCarousel
Horizontal `overflow-x: auto` row of `PartMiniCard` items + "Add Part" dashed card at end (triggers file picker).

### PartMiniCard
Receives `PartViewModel Part`, `bool IsActive`, `EventCallback OnSelect`, `EventCallback OnRemove`.
- Left: `ThumbnailSmallUrl` image (or `GetFileIcon` fallback)
- Right: filename truncated, `×qty · ฿price` (MudSkeleton while `PricingLoading`), process name
- DFM badge: warning count (amber) or ✓ (green) or upload spinner
- Active card: primary colour border
- Click → fires `OnSelect`

### PartDetailCard
Receives `PartViewModel Part`, `EventCallback<PartViewModel> OnPartChanged`, `List<ProcessDto> Processes`.
Split **60% left / 40% right**:

**Left — thumbnail viewer:**
- While `ThumbnailLargeUrl` is null and `AwaitingPreview` is true: show `ThumbnailSmallUrl` with looping CSS blur animation (`blur(8px) ↔ blur(3px)`, 1.8s `ease-in-out` `alternate infinite`). If `ThumbnailSmallUrl` is also null, show a grey background placeholder.
- When `ThumbnailLargeUrl` becomes non-null: CSS opacity crossfade — large image fades in over blurred small; blur animation stops.
- Hover state: `AnimatedCubeIcon` (`Size="48"`) fades in centered over the image → click fires callback to open BabylonJS `MudDialog` passing `GlbStoragePath`.
- Below image: filename label, dimensions string (`X × Y × Z mm`) when `Dimensions` is available.

**Right — configuration tabs:**
- Tab bar: **Configuration** | **Drawings** | **Bulk Pricing**
- **Configuration tab:**
  - Process `MudSelect` always rendered first.
  - Before process selected: Material, Finish, Tolerance each show two `MudSkeleton` wave lines (inactive placeholder — not "loading", just locked).
  - On process selected: `CatalogLoading = true` → load materials; on arrival show Material `MudSelect`. Then load Finish + Tolerance in parallel → show each with skeleton while in-flight.
  - Quantity `MudNumericField` (min 1, max 999).
  - Part Notes `MudTextField` multiline.
  - DFM `MudExpansionPanel` (warning colours) at bottom — visible only when `IsManifold == false` or DFM issues present; contains acknowledge checkbox that sets `DfmAcknowledged = true`.
- **Drawings tab:** reuses existing `DocumentUploadSection` component.
- **Bulk Pricing tab:** read-only quantity tier table from `/api/pricing/bulk`.

### ProjectQuotePanel
Receives `List<PartViewModel> Parts`, `List<LeadTimeOptionDto> LeadTimeOptions`, `LeadTimeOptionDto? SelectedLeadTime`, `EventCallback<LeadTimeOptionDto> OnLeadTimeChanged`, `EventCallback OnQuote`.

**Lead time section:**
- Loaded from `/api/pricing/lead-times` on init (parent loads, passes in).
- `EstimatedLeadTimeDays` for the panel = **maximum** `EstimatedLeadTimeDays` across all `Parts` where `EstimatedTotalAmount` is non-null. Zero if no parts have been priced yet.
- Days displayed per option (let `B` = panel `EstimatedLeadTimeDays`):
  - Standard = `B` days
  - Economy = `B + ceil(B × 0.5)` days, capped at `catalog.MaxDays`
  - Express = `max(ceil(B × 0.6), catalog.MinDays)` days
- When `B == 0` (no parts priced yet): show `MudSkeleton` for days on all options; show "Add and configure parts to see pricing" placeholder in place of the summary rows.
- Price per option = `sum(Parts.EstimatedTotalAmount ?? 0) × option.PriceMultiplier`.

**Summary rows:** one row per part — filename truncated, `×qty`, `MudSkeleton` while `PricingLoading`, otherwise formatted price.

**Totals:** subtotal, min-order top-up warning row if applicable, bold **Total**.

**Quote button:** disabled when any part is `Uploading`, any `PricingLoading`, or `SelectedLeadTime` is null.

**DFM alert banner:** shown when any part has `DfmAcknowledged == false` and has DFM issues.

---

## Key Flows

### Pricing recalculation trigger
Any change to `ProcessCode`, `MaterialCode`, `FinishCode`, `ToleranceCode`, or `Quantity` on a part:
1. 300ms debounce per part — cancels previous in-flight CancellationTokenSource for that part
2. `PricingLoading = true`, `PricingFailed = false`
3. `POST /api/pricing/calculate` with geometry metrics (`VolumeMm3`, `Dimensions`, `IsManifold`) + new config
4. On result: update `EstimatedUnitPrice`, `EstimatedTotalAmount`, `EstimatedLeadTimeDays`; `PricingLoading = false`
5. Quote panel totals and lead time options re-render (panel recalculates max `EstimatedLeadTimeDays` from all parts)
6. On failure: `PricingFailed = true`, `PricingLoading = false` — show `—` with ⚠ tooltip; silent retry on next config change

### Process change side effects
When `ProcessCode` changes on a part:
- `DfmAcknowledged` resets to `false`
- `MaterialCode/Id`, `FinishCode/Id`, `ToleranceCode/Id` clear to null
- `AvailableMaterials`, `AvailableFinishes`, `AvailableTolerances` clear and reload for new process

### Part removal
When a part is removed from `_parts`:
- Its `EstimatedTotalAmount` is immediately excluded from quote panel totals
- Lead time panel recalculates max `EstimatedLeadTimeDays` from remaining parts
- If no parts remain: quote panel shows zero-parts placeholder
- Cancel and dispose its polling `CancellationTokenSource` (existing logic preserved)
- `_selectedPartIndex` clamps to `min(_selectedPartIndex, _parts.Count - 1)`

### Draft auto-save
Every meaningful state change (title, customer, currency, lead time, any part config) → 1s debounce → serialize `DraftProjectState` + `List<DraftPartState>` → `sessionStorage.setItem("project-new-draft", json)` via JS interop → topbar: "Saving..." then "Saved X sec ago".

On `OnInitializedAsync`:
1. `sessionStorage.getItem("project-new-draft")` → if found, deserialize
2. If restored draft's `TempProjectId` matches current `_tempProjectId`: hydrate all fields, rebuild `PartViewModel` list, re-trigger pricing for fully-configured parts
3. If `TempProjectId` differs: show `MudDialog` — "Resume previous draft?" (Yes restores + uses saved `TempProjectId`) / "Start fresh" (clears sessionStorage, begins blank)

### Quote submission
1. Validate: `_selectedCustomer != null`, `_title` not empty, no parts `Uploading`, `_selectedLeadTime != null`
2. `POST /api/projects` with title, description, customer ID, currency code → `projectId`
3. For each `PartViewModel` where `FileId != Guid.Empty` and `Error == null` → `POST /api/projects/{projectId}/parts`
4. `POST` quotation creation endpoint with `projectId` + `_selectedLeadTime.Code` → `quotationId`
5. On success: `sessionStorage.removeItem("project-new-draft")` → navigate to `/sales/quotations/{quotationId}`
6. On any failure: snackbar error, `_saving = false`, all state preserved

---

## AnimatedCubeIcon Update

Current issue: `Size` parameter declared in `@code` but not applied to CSS (hardcoded `18px`/`16px` values throughout).

Fix — replace all hardcoded size-dependent values with expressions of `--cube-size` (set as inline style from `Size` parameter):

| Current hardcoded value | Becomes |
|---|---|
| `width: 18px; height: 18px` on `.cube-card` | `calc(var(--cube-size) * 1px)` both |
| `translate3d(0px, -16px, 8px)` on hover | `translate3d(0, calc(var(--cube-size) * -0.89px), calc(var(--cube-size) * 0.44px))` |
| `2px 2px 0 0` shadow offsets | scale proportionally to `--cube-size` |
| `bottom: -4px; left: 4px` on `::after` | `calc(var(--cube-size) * -0.22px)` / `calc(var(--cube-size) * 0.22px)` |

The hover overlay on `PartDetailCard` viewer uses `Size="48"`.

---

## Error Boundaries

- Outer `<ErrorBoundary>` wraps entire page body (preserved from current)
- Inner `<ErrorBoundary>` wrapping each of the three panels independently

---

## Preserved from Current Implementation

- All upload logic: `HandleFileSelected`, `UploadFileAsync`, `RemoveFile`
- All polling logic: `PollAnalysisStatusAsync`, `_pollingTokens` — extended to populate `ThumbnailSmallUrl`, `ThumbnailLargeUrl`, `GlbStoragePath` on `PartViewModel`
- Customer search: `SearchCustomersAsync`, `OnCustomerSelected`
- `ResolvePreviewUrl` helper
- Title / description validation: `ValidateTitle`, `ValidateDescription`
- `IAsyncDisposable` / `DisposeAsync` cancelling all polling tokens
- `ThreeDExtensions`, `AllowedExtensions`, `Is3DFile`, `GetFileIcon`
- `CanSubmit` logic (extended: also blocks when any part has `PricingLoading == true` or `_selectedLeadTime == null`)
