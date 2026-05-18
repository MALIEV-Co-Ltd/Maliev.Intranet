using System.Net.Http.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Application-wide currency selection and exchange rate service.
/// Base currency is always THB; user may select a display currency which
/// triggers an exchange-rate fetch and notifies all subscribers.
/// </summary>
public sealed class CurrencyService : IDisposable
{
    private const string CurrencyStateKey = "CurrencyService.InitialState";

    private readonly HttpClient _http;
    private readonly ILogger<CurrencyService> _logger;
    private readonly PersistentComponentState? _componentState;
    private PersistingComponentStateSubscription? _persistingSubscription;
    private bool _initialized;

    /// <summary>All available currencies loaded from the API.</summary>
    public List<CurrencyDto> Currencies { get; private set; } = [];

    /// <summary>Currently selected display currency (defaults to THB).</summary>
    public CurrencyDto? SelectedCurrency { get; private set; }

    /// <summary>Multiplier to convert a THB amount to the selected display currency.</summary>
    public decimal ExchangeRate { get; private set; } = 1m;

    /// <summary>True while an exchange-rate fetch is in-flight; price displays should show skeletons.</summary>
    public bool IsConverting { get; private set; }

    /// <summary>Convenience: symbol of the selected currency (e.g. "฿", "$").</summary>
    public string Symbol => SelectedCurrency?.Symbol ?? "฿";

    /// <summary>Convenience: ISO code of the selected currency (e.g. "THB", "USD").</summary>
    public string Code => SelectedCurrency?.Code ?? "THB";

    /// <summary>Fired whenever <see cref="SelectedCurrency"/> or <see cref="ExchangeRate"/> changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Initializes a new instance of <see cref="CurrencyService"/>.</summary>
    public CurrencyService(HttpClient http, ILogger<CurrencyService> logger, PersistentComponentState? componentState = null)
    {
        _http = http;
        _logger = logger;
        _componentState = componentState;
    }

    /// <summary>
    /// Loads currencies from the API on first call (idempotent).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        _persistingSubscription ??= _componentState?.RegisterOnPersisting(PersistStateAsync);

        if (_componentState?.TryTakeFromJson<CurrencyState>(CurrencyStateKey, out var state) == true &&
            state?.Currencies is { Count: > 0 })
        {
            Currencies = state.Currencies;
            SelectedCurrency = state.SelectedCurrencyCode == null
                ? Currencies.FirstOrDefault(c => c.IsPrimary) ?? Currencies.FirstOrDefault(c => c.Code == "THB") ?? Currencies.First()
                : Currencies.FirstOrDefault(c => c.Code == state.SelectedCurrencyCode)
                    ?? Currencies.FirstOrDefault(c => c.IsPrimary)
                    ?? Currencies.FirstOrDefault(c => c.Code == "THB")
                    ?? Currencies.First();
            ExchangeRate = state.ExchangeRate <= 0 ? 1m : state.ExchangeRate;
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        try
        {
            var currencies = await _http.GetFromJsonAsync<List<CurrencyDto>>("api/v1/referenceData/currencies");
            if (currencies is { Count: > 0 })
            {
                Currencies = currencies;
                SelectedCurrency = currencies.FirstOrDefault(c => c.IsPrimary)
                                ?? currencies.FirstOrDefault(c => c.Code == "THB")
                                ?? currencies.First();
                ExchangeRate = 1m;
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CurrencyService: failed to load currencies");
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Switches the display currency and fetches the corresponding THB exchange rate.
    /// No-op if <paramref name="currency"/> is already selected.
    /// </summary>
    public async Task SetCurrencyAsync(CurrencyDto? currency)
    {
        if (currency == null) return;
        if (currency.Code == SelectedCurrency?.Code) return;

        IsConverting = true;
        Changed?.Invoke(this, EventArgs.Empty);

        try
        {
            decimal rate;
            if (currency.Code == "THB")
            {
                rate = 1m;
            }
            else
            {
                try
                {
                    var resp = await _http.GetFromJsonAsync<ExchangeRateResponse>(
                        $"api/v1/referenceData/currencies/rate?from=THB&to={Uri.EscapeDataString(currency.Code)}");
                    rate = resp?.Rate ?? 1m;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CurrencyService: failed to fetch rate for {Code}", currency.Code);
                    rate = 1m;
                }
            }

            SelectedCurrency = currency;
            ExchangeRate = rate;
        }
        finally
        {
            IsConverting = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Switches the display currency by ISO code when the currency is available in the catalog.
    /// </summary>
    /// <param name="currencyCode">The ISO currency code to select.</param>
    public async Task SetCurrencyCodeAsync(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return;
        }

        if (!_initialized)
        {
            await InitializeAsync();
        }

        var target = Currencies.FirstOrDefault(currency =>
            string.Equals(currency.Code, currencyCode, StringComparison.OrdinalIgnoreCase));

        if (target is not null)
        {
            await SetCurrencyAsync(target);
        }
    }

    /// <summary>Converts a THB amount to the selected display currency.</summary>
    public decimal ConvertFromThb(decimal thbAmount) => thbAmount * ExchangeRate;

    /// <summary>Formats a THB amount in the selected display currency with symbol and correct decimal places.</summary>
    public string Format(decimal thbAmount)
    {
        var converted = thbAmount * ExchangeRate;
        var decimals = SelectedCurrency?.DecimalPlaces ?? 2;
        return $"{Symbol} {converted.ToString("N" + decimals)}";
    }

    /// <inheritdoc/>
    public void Dispose() => _persistingSubscription?.Dispose();

    private Task PersistStateAsync()
    {
        if (_componentState != null && Currencies.Count > 0)
        {
            _componentState.PersistAsJson(
                CurrencyStateKey,
                new CurrencyState(Currencies, SelectedCurrency?.Code, ExchangeRate));
        }

        return Task.CompletedTask;
    }

    private sealed record CurrencyState(
        List<CurrencyDto> Currencies,
        string? SelectedCurrencyCode,
        decimal ExchangeRate);
}
