using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for AI-driven data extraction and file processing.
/// </summary>
/// <param name="chatbotClient">The chatbot service client.</param>
/// <param name="uploadClient">The upload service client.</param>
/// <param name="registryClient">The registry service client for Thai location resolution.</param>
/// <param name="logger">The logger.</param>
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class AiProcessingController(
    ChatbotServiceClient chatbotClient, 
    UploadServiceClient uploadClient, 
    RegistryServiceClient registryClient,
    ILogger<AiProcessingController> logger) : ControllerBase
{
    private readonly ILogger<AiProcessingController> _logger = logger;
    
    /// <summary>
    /// Health check endpoint to verify chatbot service availability by initiating a session.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to initiate a session with the chatbot service to verify LLM connectivity
            var sessionResponse = await chatbotClient.InitiateSessionAsync("intranet", "en", cancellationToken);
            
            if (sessionResponse?.SessionId != null)
            {
                return Ok(new 
                { 
                    status = "healthy", 
                    service = "ai-processing", 
                    sessionId = sessionResponse.SessionId,
                    canInitiateSession = true
                });
            }
            
            return StatusCode(503, new 
            { 
                status = "unavailable", 
                service = "ai-processing", 
                canInitiateSession = false,
                message = "Failed to initiate session with chatbot service"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI processing health check failed");
            return StatusCode(503, new 
            { 
                status = "unavailable", 
                service = "ai-processing", 
                canInitiateSession = false,
                message = "Chatbot service is unreachable"
            });
        }
    }
    
    /// <summary>
    /// Processes uploaded documents and text to extract customer data using AI.
    /// </summary>
    /// <param name="files">The list of uploaded files.</param>
    /// <param name="rawText">Additional text context.</param>
    /// <returns>Extracted customer data.</returns>
    [RequirePermission(MalievPermissions.Prediction.Extract)]
    [HttpPost("extract-customer")]
    public async Task<ActionResult<ExtractedCustomerDataResponse>> ExtractCustomerFromDocument(
        [FromForm] IFormFileCollection files,
        [FromForm] string? rawText)
    {
        if ((files == null || files.Count == 0) && string.IsNullOrWhiteSpace(rawText))
        {
            return BadRequest("No files or text provided for processing.");
        }

        // 1. Read files as base64 for multimodal AI extraction
        var fileDataList = new List<ChatbotExtractionFileData>();

        if (files != null)
        {
            foreach (var file in files)
            {
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());

                fileDataList.Add(new ChatbotExtractionFileData
                {
                    FileName = file.FileName,
                    Base64Data = base64,
                    MimeType = file.ContentType
                });
            }
        }

        // 2. Call ChatbotService extraction endpoint with file data
        var result = await chatbotClient.ExtractCustomerAsync([], rawText, fileDataList.Count > 0 ? fileDataList : null);
        if (result == null)
        {
            return StatusCode(500, "Failed to extract data from provided inputs.");
        }

        // Log data received from ChatbotService
        _logger.LogInformation("Received from ChatbotService: {@Result}", result);

        // 3. Map to BFF response
        var extracted = new ExtractedCustomerDataResponse
        {
            FirstName = result.FirstName,
            LastName = result.LastName,
            Email = result.Email,
            Mobile = result.Mobile,
            Landline = result.Landline,
            Extension = result.Extension,
            Segment = result.Segment,
            CompanyName = result.CompanyName,
            CompanyPhone = result.CompanyPhone,
            VatNumber = result.VatNumber,
            BranchNumber = result.BranchNumber,
            Addresses = result.Addresses?.Select(a => new ExtractedAddress
            {
                Type = a.Type,
                AddressLine1 = a.AddressLine1,
                District = a.District,
                City = a.City,
                StateProvince = a.StateProvince,
                PostalCode = a.PostalCode,
                RecipientName = a.RecipientName,
                RecipientPhone = a.RecipientPhone
            }).ToList()
        };

        // 4. Resolve Thai locations via Registry service with multi-field composite scoring
        if (extracted.Addresses != null)
        {
            foreach (var addr in extracted.Addresses)
            {
                try
                {
                    // Use multi-field matching for best results (composite scoring)
                    var locations = await registryClient.AutocompleteLocationsMultiFieldAsync(
                        postalCode: addr.PostalCode,
                        district: addr.District,
                        city: addr.City,
                        province: addr.StateProvince,
                        limit: 3);
                    
                    _logger.LogInformation(
                        "Multi-field Registry query (PC:{PC}, D:{D}, C:{C}, P:{P}): {Count} results",
                        addr.PostalCode, addr.District, addr.City, addr.StateProvince, locations.Count);
                    
                    // Apply best match if found
                    if (locations.Count > 0)
                    {
                        var loc = locations[0];  // Top match by composite similarity score
                        var useThai = IsThai(addr.District ?? addr.City ?? addr.StateProvince ?? "");
                        
                        addr.District = useThai ? loc.SubDistrictTh : loc.SubDistrictEn;
                        addr.City = useThai ? loc.DistrictTh : loc.DistrictEn;
                        addr.StateProvince = useThai ? loc.ProvinceTh : loc.ProvinceEn;
                        addr.PostalCode = loc.PostalCode;
                        
                        _logger.LogInformation(
                            "Applied Registry correction: {District}, {City}, {Province} {PostalCode}",
                            addr.District, addr.City, addr.StateProvince, addr.PostalCode);
                    }
                    else
                    {
                        _logger.LogWarning("No Registry match found for address, using AI extraction");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Registry location resolution failed for address (best-effort)");
                    // Continue with AI-extracted values
                }
            }
            
            // Log final address state after Registry correction
            _logger.LogInformation("After Registry correction: {@Addresses}", extracted.Addresses);
        }

        // 5. Compute confidence server-side based on filled fields
        extracted.Confidence = ComputeConfidence(extracted);

        return Ok(extracted);
    }

    /// <summary>
    /// Uploads a document to the central upload service.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="category">Optional category for the storage path (e.g., 'nda').</param>
    /// <returns>The upload metadata.</returns>
    [RequirePermission(MalievPermissions.Customer.Write)]
    [HttpPost("upload-document")]
    public async Task<ActionResult<BffUploadResponse>> UploadDocument(IFormFile file, [FromQuery] string category = "general")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using var stream = file.OpenReadStream();
        var path = $"customer-onboarding/{category}/{Guid.NewGuid()}/{file.FileName}";

        var uploadResult = await uploadClient.UploadFileAsync(file.FileName, stream, file.ContentType, path);
        if (uploadResult == null)
        {
            return StatusCode(500, "Failed to upload file.");
        }

        return Ok(uploadResult);
    }

    /// <summary>
    /// Uploads multiple documents with category specification.
    /// </summary>
    /// <param name="files">The files to upload.</param>
    /// <param name="category">Document category (e.g., 'nda', 'general').</param>
    /// <param name="subType">Optional sub-type for categorization.</param>
    /// <returns>List of upload metadata for all files.</returns>
    [RequirePermission(MalievPermissions.Customer.Write)]
    [HttpPost("upload-documents")]
    public async Task<ActionResult<List<BffUploadResponse>>> UploadDocuments(
        [FromForm] IFormFileCollection files,
        [FromQuery] string category = "general",
        [FromQuery] string? subType = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded.");
        }

        var results = new List<BffUploadResponse>();

        foreach (var file in files)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest($"File '{file.FileName}' exceeds the 10 MB limit.");
            }

            using var stream = file.OpenReadStream();
            var path = $"customer-onboarding/{category}/{Guid.NewGuid()}/{file.FileName}";

            var uploadResult = await uploadClient.UploadFileAsync(
                file.FileName, stream, file.ContentType, path);

            if (uploadResult != null)
            {
                results.Add(uploadResult);
            }
        }

        if (results.Count == 0)
        {
            return StatusCode(500, "Failed to upload any files.");
        }

        return Ok(results);
    }

    private static bool IsThai(string? value) => value?.Any(c => c >= 0x0E00 && c <= 0x0E7F) ?? false;

    private static double ComputeConfidence(ExtractedCustomerDataResponse data)
    {
        var fields = new[]
        {
            data.FirstName, data.LastName, data.Email, data.Mobile,
            data.Landline, data.Extension, data.Segment,
            data.CompanyName, data.CompanyPhone, data.VatNumber
        };

        int filled = fields.Count(f => !string.IsNullOrWhiteSpace(f));
        int total = fields.Length;

        if (data.Addresses != null)
        {
            foreach (var addr in data.Addresses)
            {
                var addrFields = new[] { addr.AddressLine1, addr.District, addr.City, addr.StateProvince, addr.PostalCode };
                filled += addrFields.Count(f => !string.IsNullOrWhiteSpace(f));
                total += addrFields.Length;
            }
        }
        else
        {
            // Count the 5 address fields as empty
            total += 5;
        }

        return total > 0 ? Math.Round((double)filled / total, 2) : 0;
    }
}
