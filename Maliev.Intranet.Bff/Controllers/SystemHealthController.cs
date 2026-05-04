using System.Diagnostics;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing and displaying the health status of microservices.
/// </summary>
[RequirePermission(MalievPermissions.System.HealthRead, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system-health")]
public class SystemHealthController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private static readonly IReadOnlyList<ServiceHealthTarget> ServiceRegistry =
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
        new("PredictionService", "Manufacturing", "predictionservice", false),
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

    /// <summary>
    /// Aggregates and returns the health status of all registered HTTP microservices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A SystemHealthDto containing status of all services.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth(CancellationToken ct)
    {
        var checkedAt = DateTime.UtcNow;
        var healthStatuses = await Task.WhenAll(ServiceRegistry.Select(target => CheckServiceHealth(target, ct)));
        var services = healthStatuses.OrderBy(s => s.DomainGroup).ThenBy(s => s.ServiceName).ToList();

        var result = new SystemHealthDto
        {
            OverallTimestamp = checkedAt,
            CheckedAt = checkedAt,
            OverallStatus = ResolveOverallStatus(services),
            Services = services
        };

        return Ok(result);
    }

    private async Task<ServiceHealthStatus> CheckServiceHealth(ServiceHealthTarget target, CancellationToken ct)
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

            var liveness = await ProbeAsync(client, target.LivenessPath, TimeSpan.FromSeconds(2), ct);
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
            timeoutCts.CancelAfter(timeout);
            using var response = await client.GetAsync(path, timeoutCts.Token);
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

    private static string ResolveOverallStatus(IReadOnlyList<ServiceHealthStatus> services)
    {
        if (services.Any(s => s.IsCritical && s.Status is "Unhealthy" or "Unreachable"))
        {
            return "Unhealthy";
        }

        return services.Any(s => s.Status is "Unhealthy" or "Unreachable")
            ? "Degraded"
            : "Healthy";
    }

    private static string? TrimErrorBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        return body.Length <= 500 ? body : body[..500];
    }

    private sealed record ServiceHealthTarget(
        string ServiceName,
        string DomainGroup,
        string RoutePrefix,
        bool IsCritical)
    {
        public string LivenessPath { get; } = $"/{RoutePrefix}/liveness";

        public string ReadinessPath { get; } = $"/{RoutePrefix}/readiness";
    }

    private sealed record ProbeResult(
        bool IsSuccess,
        double ResponseTimeMs,
        string? ErrorMessage = null,
        string? ErrorBody = null);
}
