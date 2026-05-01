using System.Net.Http.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Application-wide currency selection and exchange rate service.
/// Base currency is always THB; user may select a display currency which
/// triggers an exchange-rate fetch and notifies all subscribers.
/// </summary>
public sealed class CurrencyService : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<CurrencyService> _logger;
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
    public CurrencyService(HttpClient http, ILogger<CurrencyService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Loads currencies from the API on first call (idempotent).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

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
    public void Dispose() { }
}
