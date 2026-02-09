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
            Addresses = result.Addresses?.Select(a => 
            {
                _logger.LogInformation("Mapping address from AI: Type={Type}, Line1={Line1}, District={District}, City={City}, PC={PC}", 
                    a.Type, a.AddressLine1, a.District, a.City, a.PostalCode);
                
                return new ExtractedAddress
                {
                    Type = a.Type,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    AddressLine3 = a.AddressLine3,
                    District = a.District,
                    City = a.City,
                    StateProvince = a.StateProvince,
                    PostalCode = a.PostalCode,
                    RecipientName = a.RecipientName,
                    RecipientPhone = a.RecipientPhone
                };
            }).ToList()
        };

        // 4. Validate Company via Registry if Tax ID (VatNumber) is available
        if (!string.IsNullOrWhiteSpace(extracted.VatNumber))
        {
            try
            {
                var companyProfiles = await registryClient.SearchCompaniesAsync(extracted.VatNumber, 1);
                if (companyProfiles.Count > 0)
                {
                    var profile = companyProfiles[0];
                    _logger.LogInformation("Validated company name via Registry for Tax ID {TaxId}: {OldName} -> {NewName}", 
                        extracted.VatNumber, extracted.CompanyName, profile.CompanyNameTh);
                    extracted.CompanyName = profile.CompanyNameTh;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate company via Registry for Tax ID {TaxId}", extracted.VatNumber);
            }
        }

        // 5. Resolve Thai locations via Registry service with multi-field composite scoring
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
                        
                        // Detect if we should use Thai or English for corrections
                        // Check ALL extracted fields for Thai characters to be robust
                        var hasThai = IsThai(addr.District) || IsThai(addr.City) || IsThai(addr.StateProvince) || IsThai(addr.AddressLine1);
                        var useThai = hasThai;
                        
                        // Default to Thai if it's ambiguous but we have a match in the Thai registry
                        if (!useThai && string.IsNullOrWhiteSpace(addr.District) && string.IsNullOrWhiteSpace(addr.City))
                        {
                            useThai = true;
                        }

                        _logger.LogInformation("Registry match found. Correcting address fields. useThai={UseThai}", useThai);

                        // Correct administrative fields ONLY if Registry has a non-empty value
                        // This prevents wiping out AI data with empty Registry fields (especially for English)
                        var correctedDistrict = useThai ? loc.SubDistrictTh : loc.SubDistrictEn;
                        var correctedCity = useThai ? loc.DistrictTh : loc.DistrictEn;
                        var correctedProvince = useThai ? loc.ProvinceTh : loc.ProvinceEn;

                        _logger.LogInformation("Registry matched: District={D}, City={C}, Prov={P}, PC={PC}", 
                            correctedDistrict, correctedCity, correctedProvince, loc.PostalCode);

                        // Only update if we actually got a value from Registry
                        // This ensures we keep the AI values if Registry lookup was partial or failed to provide better data
                        if (!string.IsNullOrWhiteSpace(correctedDistrict)) addr.District = correctedDistrict;
                        if (!string.IsNullOrWhiteSpace(correctedCity)) addr.City = correctedCity;
                        if (!string.IsNullOrWhiteSpace(correctedProvince)) addr.StateProvince = correctedProvince;
                        if (!string.IsNullOrWhiteSpace(loc.PostalCode)) addr.PostalCode = loc.PostalCode;
                        
                        // Pass the full location object back to the client for better UI binding
                        addr.Location = loc;
                        
                        _logger.LogInformation(
                            "Final address state (Thai:{Thai}): District:{District}, City:{City}, Province:{Province}, PostalCode:{PostalCode}",
                            useThai, addr.District, addr.City, addr.StateProvince, addr.PostalCode);
                    }
                    else
                    {
                        _logger.LogWarning("No Registry match found for address. Keeping AI values: District:{District}, City:{City}, Province:{Province}, PostalCode:{PostalCode}",
                            addr.District, addr.City, addr.StateProvince, addr.PostalCode);
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

        // 6. Compute confidence server-side based on filled fields
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
