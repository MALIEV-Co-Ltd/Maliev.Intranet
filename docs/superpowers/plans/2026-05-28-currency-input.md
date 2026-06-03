# CurrencyInput Component Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a reusable `CurrencyInput` Blazor component that accepts input in the appbar-selected currency, displays THB equivalent, and stores values in THB internally.

**Architecture:** New Razor component in `Components/Shared/` that wraps `MudNumericField` with two-way currency conversion via `CurrencyService`. Subscribes to `CurrencyService.Changed` for live updates. Replaces inline `MudNumericField` usage in `QuoteSummaryBar.razor`.

**Tech Stack:** Blazor WASM, MudBlazor, C# 13

---

### Task 1: Create CurrencyInput component

**Files:**
- Create: `Maliev.Intranet.Client/Components/Shared/CurrencyInput.razor`
- Create: `Maliev.Intranet.Client/Components/Shared/CurrencyInput.razor.cs`

- [ ] **Step 1: Create CurrencyInput.razor.cs (code-behind)**

```csharp
using Maliev.Intranet.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Client.Components.Shared;

/// <summary>
/// A numeric input field that accepts values in the currently selected appbar currency
/// and stores them internally as THB via two-way binding.
/// </summary>
public partial class CurrencyInput : ComponentBase, IDisposable
{
    [Inject]
    private CurrencyService CurrencyService { get; set; } = null!;

    /// <summary>
    /// The field label displayed above the input.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional helper text displayed below the input.
    /// </summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>
    /// The value in THB, bound two-way to the parent.
    /// </summary>
    [Parameter]
    public decimal ThbValue { get; set; }

    /// <summary>
    /// Callback invoked when <see cref="ThbValue"/> changes.
    /// </summary>
    [Parameter]
    public EventCallback<decimal> ThbValueChanged { get; set; }

    /// <summary>
    /// Minimum allowed value (default 0).
    /// </summary>
    [Parameter]
    public decimal? Min { get; set; }

    /// <summary>
    /// Maximum allowed value (optional).
    /// </summary>
    [Parameter]
    public decimal? Max { get; set; }

    private decimal _displayValue;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        CurrencyService.Changed += OnCurrencyChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _displayValue = CurrencyService.ConvertFromThb(ThbValue);
    }

    private async Task OnInputChanged(decimal value)
    {
        _displayValue = Math.Max(Min ?? 0m, value);
        if (Max.HasValue)
        {
            _displayValue = Math.Min(_displayValue, Max.Value);
        }

        var rate = CurrencyService.ExchangeRate <= 0 ? 1m : CurrencyService.ExchangeRate;
        var thbValue = _displayValue / rate;
        await ThbValueChanged.InvokeAsync(thbValue);
    }

    private void OnCurrencyChanged(object? sender, EventArgs e)
    {
        _displayValue = CurrencyService.ConvertFromThb(ThbValue);
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CurrencyService.Changed -= OnCurrencyChanged;
    }
}
```

- [ ] **Step 2: Create CurrencyInput.razor (template)**

```razor
@namespace Maliev.Intranet.Client.Components.Shared

<div class="currency-input-root">
    <MudNumericField T="decimal"
                     Label="@Label"
                     Value="@_displayValue"
                     ValueChanged="OnInputChanged"
                     Min="@(Min ?? 0m)"
                     Max="@Max"
                     Immediate="true"
                     Variant="Variant.Outlined"
                     Margin="Margin.Dense"
                     HideSpinButtons="true" />
    @if (!string.IsNullOrWhiteSpace(HelperText))
    {
        <div class="currency-input-helper">
            <span>@HelperText</span>
            <span class="currency-input-thb">= ฿@ThbValue.ToString("N2")</span>
        </div>
    }
</div>

<style>
    .currency-input-root {
        display: flex;
        flex-direction: column;
        gap: 4px;
        min-width: 0;
    }

    .currency-input-root ::deep .mud-input-control {
        margin-top: 0;
        margin-bottom: 0;
    }

    .currency-input-helper {
        display: flex;
        justify-content: space-between;
        align-items: center;
        font-size: var(--mud-typography-caption-size);
        color: var(--maliev-muted);
    }

    .currency-input-thb {
        color: var(--mud-palette-primary);
        font-family: 'JetBrains Mono', monospace;
        font-size: 11px;
        background: color-mix(in oklch, var(--mud-palette-primary) 8%, transparent);
        padding: 1px 6px;
        border-radius: 4px;
    }
</style>
```

- [ ] **Step 3: Verify the project compiles**

```
dotnet build Maliev.Intranet.slnx --no-restore
```

Expected: Build succeeds with zero warnings.

---

### Task 2: Replace MudNumericField with CurrencyInput in QuoteSummaryBar

**Files:**
- Modify: `Maliev.Intranet.Client/Components/Project/QuoteSummaryBar.razor`

- [ ] **Step 1: Replace Shipping MudNumericField with CurrencyInput**

Find lines 165-176 in `QuoteSummaryBar.razor`:

```razor
                        <div class="qsb-money-entry">
                            <span class="qsb-money-prefix" aria-hidden="true">@CurrencyService.Symbol</span>
                            <MudNumericField T="decimal"
                                             Label="Shipping"
                                             Value="@ShippingCost"
                                             ValueChanged="OnShippingCostChanged"
                                             Min="0m"
                                             Immediate="true"
                                             Variant="Variant.Outlined"
                                             Margin="Margin.Dense"
                                             HideSpinButtons="true" />
                        </div>
```

Replace with:

```razor
                        <div class="qsb-money-entry">
                            <CurrencyInput Label="Shipping"
                                           HelperText="Freight, courier, or delivery charge"
                                           @bind-ThbValue="ShippingCost"
                                           Min="0m" />
                        </div>
```

- [ ] **Step 2: Replace Discount MudNumericField with CurrencyInput**

Find lines 188-199 in `QuoteSummaryBar.razor`:

```razor
                        <div class="qsb-money-entry">
                            <span class="qsb-money-prefix" aria-hidden="true">@CurrencyService.Symbol</span>
                            <MudNumericField T="decimal"
                                             Label="Discount"
                                             Value="@ManualDiscountAmount"
                                             ValueChanged="OnManualDiscountAmountChanged"
                                             Min="0m"
                                             Immediate="true"
                                             Variant="Variant.Outlined"
                                             Margin="Margin.Dense"
                                             HideSpinButtons="true" />
                        </div>
```

Replace with:

```razor
                        <div class="qsb-money-entry">
                            <CurrencyInput Label="Discount"
                                           HelperText="Manual reduction before VAT"
                                           @bind-ThbValue="ManualDiscountAmount"
                                           Min="0m" />
                        </div>
```

- [ ] **Step 3: Remove unused handler methods**

Remove the now-unused handler methods at lines 904-908 in the `@code` block:

```csharp
    private async Task OnShippingCostChanged(decimal value) =>
        await ShippingCostChanged.InvokeAsync(Math.Max(0m, value));

    private async Task OnManualDiscountAmountChanged(decimal value) =>
        await ManualDiscountAmountChanged.InvokeAsync(Math.Max(0m, value));
```

- [ ] **Step 4: Build and verify**

```
dotnet build Maliev.Intranet.slnx
```

Expected: Build succeeds with zero warnings.

- [ ] **Step 5: Verify the existing tests pass**

```
dotnet test Maliev.Intranet.slnx --verbosity normal
```

Expected: All tests pass (no new test failures should be introduced since only the input wrapper changed — backing fields and callbacks are unchanged).

- [ ] **Step 6: Commit**

```
git add Maliev.Intranet.Client/Components/Shared/CurrencyInput.razor Maliev.Intranet.Client/Components/Shared/CurrencyInput.razor.cs Maliev.Intranet.Client/Components/Project/QuoteSummaryBar.razor
git commit -m "feat: add CurrencyInput component with appbar currency conversion"
```
