using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing and displaying the health status of microservices.
/// </summary>
[RequirePermission(MalievPermissions.System.HealthRead, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/system-health")]
public class SystemHealthController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private static readonly string[] ServicesToCheck =
    [
        "AuthService", "IAMService", "CustomerService", "OrderService",
        "QuotationService", "AccountingService", "SupplierService",
        "NotificationService", "EmployeeService", "CareerService",
        "CompensationService", "ComplianceService", "LeaveService",
        "LifecycleService", "PerformanceService", "ContactService",
        "CurrencyService", "InvoiceService", "MaterialService",
        "PaymentService", "PdfService", "PurchaseOrderService",
        "ReceiptService", "UploadService", "CountryService",
        "RegistryService", "PricingService", "PredictionService",
        "ChatbotService", "GeometryService"
    ];

    /// <summary>
    /// Aggregates and returns the health status of all registered microservices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A SystemHealthDto containing status of all services.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SystemHealthDto), 200)]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth(CancellationToken ct)
    {
        var result = new SystemHealthDto
        {
            OverallTimestamp = DateTime.UtcNow,
            OverallStatus = "Healthy"
        };

        var tasks = ServicesToCheck.Select(serviceName => CheckServiceHealth(serviceName, ct));
        var healthStatuses = await Task.WhenAll(tasks);

        result.Services.AddRange(healthStatuses);

        if (result.Services.Any(s => s.Status == "Unhealthy"))
        {
            result.OverallStatus = "Degraded";
        }

        if (result.Services.All(s => s.Status == "Unhealthy" || s.Status == "Unreachable"))
        {
            result.OverallStatus = "Unhealthy";
        }

        return Ok(result);
    }

    private async Task<ServiceHealthStatus> CheckServiceHealth(string serviceName, CancellationToken ct)
    {
        var status = new ServiceHealthStatus
        {
            ServiceName = serviceName,
            LastCheck = DateTime.UtcNow,
            Status = "Unknown"
        };

        var baseUrl = configuration[$"Services:{serviceName}:BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            status.Status = "Misconfigured";
            status.ErrorMessage = "Base URL not found in configuration";
            return status;
        }

        // Standard service prefix is usually the first part of the service name in lowercase
        // or we can just try /liveness or /readiness
        var servicePrefix = serviceName.Replace("Service", "").ToLower();
        if (serviceName == "PurchaseOrderService") servicePrefix = "purchase-order";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);

            var sw = Stopwatch.StartNew();
            // Try the standard Aspire-mapped liveness endpoint
            var response = await client.GetAsync($"/{servicePrefix}/liveness", ct);
            sw.Stop();

            status.ResponseTimeMs = sw.ElapsedMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                status.Status = "Healthy";
            }
            else
            {
                status.Status = "Unhealthy";
                status.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            status.Status = "Unreachable";
            status.ErrorMessage = ex.Message;
        }

        return status;
    }
}
