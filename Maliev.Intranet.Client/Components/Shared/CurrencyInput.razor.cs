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
