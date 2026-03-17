using Asp.Versioning;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// BFF Controller for Credit Terms.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/credit-terms")]
public class CreditTermsController(InvoiceServiceClient invoiceClient) : ControllerBase
{
    /// <summary>
    /// Gets all credit terms.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CreditTermDto>>> GetCreditTerms(CancellationToken ct)
    {
        var result = await invoiceClient.GetCreditTermsAsync(ct);
        return Ok(result ?? []);
    }
}
