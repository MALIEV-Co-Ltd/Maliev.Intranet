using System.Diagnostics;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Default system health probe implementation backed by Aspire service discovery.
/// </summary>
public sealed class SystemHealthProbeService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ISystemHealthProbeService
{
    /// <inheritdoc />
    public IReadOnlyList<SystemHealthTarget> Targets { get; } =
    [
        new("AuthService", "Platform", "auth", true),
        new("IAMService", "Platform", "iam", true),
        new("CustomerService", "CRM", "customer", true),
        new("ProjectService", "CRM", "project", true),
        new("QuotationService", "CRM", "quotation", true),
        new("OrderService", "Operations", "order", true),
        new("JobService", "Operations", "job", true),
        new("FacilityService", "Operations", "facility", false),
        new("MaterialService", "Operations", "material", false),
        new("InventoryService", "Operations", "inventory", false),
        new("SupplierService", "CRM", "supplier", false),
        new("PurchaseOrderService", "Purchasing", "purchase-order", true),
        new("AccountingService", "Finance", "accounting", true),
        new("InvoiceService", "Finance", "invoice", true),
        new("PaymentService", "Finance", "payment", true),
        new("ReceiptService", "Finance", "receipt", false),
        new("DeliveryService", "Finance", "delivery", false),
        new("UploadService", "Platform", "upload", true),
        new("PdfService", "Platform", "pdf", false),
        new("GeometryService", "Manufacturing", "geometry", true),
        new("PricingService", "Manufacturing", "pricing", true),
        new("PredictionService", "Manufacturing", "prediction", false),
        new("CurrencyService", "Reference", "currency", false),
        new("CountryService", "Reference", "country", false),
        new("RegistryService", "Reference", "registry", false),
        new("EmployeeService", "HR", "employee", true),
        new("LeaveService", "HR", "leave", false),
        new("LifecycleService", "HR", "lifecycle", false),
        new("CareerService", "HR", "career", false),
        new("CompensationService", "HR", "compensation", true),
        new("PerformanceService", "HR", "performance", false),
        new("ComplianceService", "HR", "compliance", false),
        new("ContactService", "CRM", "contact", false),
        new("NotificationService", "Platform", "notification", false),
        new("ChatbotService", "Platform", "chatbot", false)
    ];

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceHealthStatus>> CheckAllAsync(CancellationToken ct)
    {
        var healthStatuses = await Task.WhenAll(Targets.Select(target => CheckServiceHealthAsync(target, ct)));
        return healthStatuses.OrderBy(s => s.DomainGroup).ThenBy(s => s.ServiceName).ToList();
    }

    private async Task<ServiceHealthStatus> CheckServiceHealthAsync(SystemHealthTarget target, CancellationToken ct)
    {
        var status = new ServiceHealthStatus
        {
            ServiceName = target.ServiceName,
            DomainGroup = target.DomainGroup,
            RoutePrefix = target.RoutePrefix,
            HealthPath = target.ReadinessPath,
            LivenessPath = target.LivenessPath,
            ReadinessPath = target.ReadinessPath,
            IsCritical = target.IsCritical,
            LastCheck = DateTime.UtcNow,
            Status = "Unknown"
        };

        var baseUrl = configuration[$"Services:{target.ServiceName}:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"http://{target.ServiceName}";
        }

        try
        {
            var client = httpClientFactory.CreateClient("ServiceHealthCheck");
            client.BaseAddress = new Uri(baseUrl);

            var liveness = await ProbeAsync(client, target.LivenessPath, TimeSpan.FromSeconds(5), ct);
            status.LivenessResponseTimeMs = liveness.ResponseTimeMs;
            if (!liveness.IsSuccess)
            {
                status.Status = "Unreachable";
                status.ErrorMessage = liveness.ErrorMessage;
                status.ErrorBody = liveness.ErrorBody;
                return status;
            }

            var readiness = await ProbeAsync(client, target.ReadinessPath, TimeSpan.FromSeconds(10), ct);
            status.ReadinessResponseTimeMs = readiness.ResponseTimeMs;
            status.ResponseTimeMs = readiness.ResponseTimeMs;
            if (readiness.IsSuccess)
            {
                status.Status = "Healthy";
                return status;
            }

            status.Status = "Unhealthy";
            status.ErrorMessage = readiness.ErrorMessage;
            status.ErrorBody = readiness.ErrorBody;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            status.Status = "Unreachable";
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    private static async Task<ProbeResult> ProbeAsync(HttpClient client, string path, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var responseTask = client.GetAsync(path, timeoutCts.Token);
            var completedTask = await Task.WhenAny(responseTask, Task.Delay(timeout, ct));
            if (completedTask != responseTask)
            {
                sw.Stop();
                await timeoutCts.CancelAsync();
                _ = responseTask.ContinueWith(
                    task =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion)
                        {
                            task.Result.Dispose();
                        }

                        _ = task.Exception;
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                return new ProbeResult(
                    false,
                    sw.Elapsed.TotalMilliseconds,
                    $"Health probe {path} timed out after {timeout.TotalSeconds:N0} seconds.");
            }

            using var response = await responseTask;
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new ProbeResult(true, sw.Elapsed.TotalMilliseconds);
            }

            return new ProbeResult(
                false,
                sw.Elapsed.TotalMilliseconds,
                $"Health probe {path} returned HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                TrimErrorBody(await response.Content.ReadAsStringAsync(ct)));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            return new ProbeResult(
                false,
                sw.Elapsed.TotalMilliseconds,
                $"Health probe {path} timed out after {timeout.TotalSeconds:N0} seconds.");
        }
    }

    private static string? TrimErrorBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        return body.Length <= 500 ? body : body[..500];
    }

    private sealed record ProbeResult(
        bool IsSuccess,
        double ResponseTimeMs,
        string? ErrorMessage = null,
        string? ErrorBody = null);
}
