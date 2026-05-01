using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maliev.Intranet.Tests.Client.Services;

public class CurrencyServiceTests
{
    private static HttpClient MakeHttpClient(HttpStatusCode status, object? body = null)
    {
        var handler = new FakeHandler(status, body);
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    [Fact]
    public async Task SetCurrencyAsync_UpdatesExchangeRate_WhenApiFetchSucceeds()
    {
        var response = new ExchangeRateResponse("THB", "USD", 0.027m);
        var http = MakeHttpClient(HttpStatusCode.OK, response);
        var svc = new CurrencyService(http, NullLogger<CurrencyService>.Instance);
        svc.Currencies.Add(new CurrencyDto { Code = "THB", Symbol = "฿", Name = "Thai Baht", IsPrimary = true, IsActive = true });
        svc.Currencies.Add(new CurrencyDto { Code = "USD", Symbol = "$", Name = "US Dollar", IsActive = true });

        await svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "THB"));
        await svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "USD"));

        Assert.Equal("USD", svc.Code);
        Assert.Equal(0.027m, svc.ExchangeRate);
    }

    [Fact]
    public async Task SetCurrencyAsync_ResetsExchangeRateToOne_OnApiFetchFailure()
    {
        var http = MakeHttpClient(HttpStatusCode.InternalServerError);
        var svc = new CurrencyService(http, NullLogger<CurrencyService>.Instance);
        svc.Currencies.Add(new CurrencyDto { Code = "THB", Symbol = "฿", Name = "Thai Baht", IsPrimary = true, IsActive = true });
        svc.Currencies.Add(new CurrencyDto { Code = "EUR", Symbol = "€", Name = "Euro", IsActive = true });

        await svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "THB"));
        await svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "EUR"));

        Assert.Equal("EUR", svc.Code);
        Assert.Equal(1m, svc.ExchangeRate);
    }

    [Fact]
    public async Task SetCurrencyAsync_FlipsIsConvertingAroundFetch()
    {
        var tcs = new TaskCompletionSource<HttpResponseMessage>();
        var handler = new FakeHandler(HttpStatusCode.OK, new ExchangeRateResponse("THB", "USD", 0.027m), tcs.Task);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var svc = new CurrencyService(http, NullLogger<CurrencyService>.Instance);
        svc.Currencies.Add(new CurrencyDto { Code = "THB", Symbol = "฿", Name = "Thai Baht", IsPrimary = true, IsActive = true });
        svc.Currencies.Add(new CurrencyDto { Code = "USD", Symbol = "$", Name = "US Dollar", IsActive = true });

        await svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "THB"));

        var convertingStates = new List<bool>();
        svc.Changed += (_, _) => convertingStates.Add(svc.IsConverting);

        var task = svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "USD"));
        // Allow the task to reach the HTTP call
        await Task.Delay(20);
        tcs.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ExchangeRateResponse("THB", "USD", 0.027m))
        });
        await task;

        // First Changed event: IsConverting = true; second: IsConverting = false
        Assert.Equal(2, convertingStates.Count);
        Assert.True(convertingStates[0]);
        Assert.False(convertingStates[1]);
        Assert.False(svc.IsConverting);
    }

    [Fact]
    public async Task SetCurrencyAsync_ThbSelection_SetsExchangeRateToOneWithoutHttpCall()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, new ExchangeRateResponse("THB", "THB", 1m));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var svc = new CurrencyService(http, NullLogger<CurrencyService>.Instance);
        svc.Currencies.Add(new CurrencyDto { Code = "USD", Symbol = "$", Name = "US Dollar", IsActive = true });
        svc.Currencies.Add(new CurrencyDto { Code = "THB", Symbol = "฿", Name = "Thai Baht", IsPrimary = true, IsActive = true });

        // First select USD so we have a non-THB state
        await svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "USD"));
        handler.ResetCallCount();
        await svc.SetCurrencyAsync(svc.Currencies.First(c => c.Code == "THB"));

        Assert.Equal(1m, svc.ExchangeRate);
        Assert.Equal(0, handler.CallCount);
    }

    private sealed class FakeHandler(HttpStatusCode status, object? body, Task<HttpResponseMessage>? blocker = null)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public void ResetCallCount() => CallCount = 0;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (blocker != null)
                await blocker;
            if (status != HttpStatusCode.OK)
                return new HttpResponseMessage(status);
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            if (body != null)
                resp.Content = JsonContent.Create(body);
            return resp;
        }
    }
}
