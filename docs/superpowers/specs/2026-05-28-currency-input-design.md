# CurrencyInput Component — Design Spec

**Date:** 2026-05-28
**Status:** Approved
**Workstream:** 1 of 2 (UI Redesign)

## Problem

The `QuoteSummaryBar.razor` Shipping and Discount `MudNumericField` inputs display a currency symbol from `CurrencyService.Symbol`, but the numeric value is always in THB. When the user switches the appbar currency to USD, the symbol changes to `$` but the numeric value is still interpreted as THB — causing confusion. Users expect to type amounts in the currency they selected.

## Solution

Create a reusable `CurrencyInput` Blazor component that:
- Accepts a **THB value** (bound two-way via `@bind-ThbValue`)
- **Displays and accepts input** in the currently selected appbar currency
- Shows a **THB equivalent** label below the input
- Handles two-way conversion internally using `CurrencyService`

## Component API

```razor
<CurrencyInput Label="Shipping"
               HelperText="Freight, courier, or delivery charge"
               @bind-ThbValue="_shippingCost"
               Min="0" />
```

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Label` | `string` | Yes | Field label (uppercased automatically) |
| `HelperText` | `string?` | No | Helper text below the field |
| `ThbValue` | `decimal` | Yes | Two-way bound value in THB |
| `ThbValueChanged` | `EventCallback<decimal>` | Yes | Callback when THB value changes |
| `Min` | `decimal?` | No | Minimum value (default null) |
| `Max` | `decimal?` | No | Maximum value (default null) |

### Internal Behavior
- Injects `CurrencyService` for exchange rate and symbol
- On `OnParametersSet`: converts `ThbValue` → display value via `CurrencyService.ConvertFromThb()`
- On user input: converts display value → THB via `value * CurrencyService.ExchangeRate`
- Subscribes to `CurrencyService.Changed` event to refresh display when appbar currency changes

## Files

### New Files
| File | Description |
|------|-------------|
| `Maliev.Intranet.Client/Components/Shared/CurrencyInput.razor` | Razor template with `MudNumericField` |
| `Maliev.Intranet.Client/Components/Shared/CurrencyInput.razor.cs` | Code-behind with conversion logic |

### Modified Files
| File | Change |
|------|--------|
| `Maliev.Intranet.Client/Components/Project/QuoteSummaryBar.razor` | Replace `MudNumericField` for Shipping and Discount with `CurrencyInput` |

## What Does NOT Change
- Internal storage: `decimal ShippingCost` (THB), `decimal ManualDiscountAmount` (THB) — unchanged
- `DraftProjectState`: `ShippingCost`, `ManualDiscountAmount` fields — unchanged (THB)
- `GenerateQuotationRequest`: `ShippingCost` field — unchanged (THB)
- BFF backend controllers — no changes
- PDF generation (`QuotationPdfData`) — receives THB as before
- Tax calculation: `taxableSubtotal - discount + shippingCost` in THB — unchanged
- `CurrencyService` itself — no changes needed

## Rendering

```
┌──────────────────────────────────────────────┐
│ SHIPPING                                     │
│  $ [____14.29____] USD                       │
│  Freight, courier, or delivery charge  =฿500 │
└──────────────────────────────────────────────┘
```

## Currency Change Behavior
When the user switches currency in the appbar:
1. `CurrencyService.Changed` event fires
2. `CurrencyInput` subscribes to the event
3. Display value recalculates: `ConvertFromThb(_thbValue)`
4. `StateHasChanged()` triggers re-render

## Edge Cases
- **Exchange rate = 0 or null**: fall back to 1.0 (identity — display THB directly)
- **Negative input**: clamped to `Min` (default 0)
- **Currency has no symbol fallback**: use ISO code (e.g., "CNY")
- **Rapid currency switching**: debounce not needed — conversion is a simple multiplication
