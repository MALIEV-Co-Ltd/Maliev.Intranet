using Asp.Versioning;
using System.Text.Json;
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
/// <param name="customerClient">The customer service client.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AiProcessingController(
    ChatbotServiceClient chatbotClient,
    UploadServiceClient uploadClient,
    RegistryServiceClient registryClient,
    CustomerServiceClient customerClient,
    ILogger<AiProcessingController> logger) : ControllerBase
{
    private readonly ILogger<AiProcessingController> _logger = logger;

    /// <summary>
    /// Health check endpoint to verify chatbot service availability without creating a session.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken = default)
    {
        try
        {
            if (await chatbotClient.CheckHealthAsync(cancellationToken))
            {
                return Ok(new
                {
                    status = "healthy",
                    service = "ai-processing",
                    canInitiateSession = true
                });
            }

            return StatusCode(503, new
            {
                status = "unavailable",
                service = "ai-processing",
                canInitiateSession = false,
                message = "Chatbot service health check failed"
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

        // Standardize Head Office Branch Number
        if (!string.IsNullOrWhiteSpace(extracted.BranchNumber))
        {
            var branch = extracted.BranchNumber.Trim();
            if (branch == "0" || branch == "00000" ||
                branch.Equals("Head Office", StringComparison.OrdinalIgnoreCase) ||
                branch.Equals("สำนักงานใหญ่", StringComparison.OrdinalIgnoreCase))
            {
                extracted.BranchNumber = "00000";
            }
        }

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
    /// Extracts NDA-related dates (expiration, effective, signed) from an uploaded PDF using AI.
    /// </summary>
    /// <param name="file">The NDA PDF file to scan.</param>
    /// <returns>Extracted dates, or empty result if extraction fails.</returns>
    [RequirePermission(MalievPermissions.Customer.Write)]
    [HttpPost("extract-nda-dates")]
    public async Task<ActionResult<ExtractedNdaDatesResponse>> ExtractNdaDates([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        try
        {
            // 1. Initiate a chatbot session
            var session = await chatbotClient.InitiateSessionAsync("intranet", "en");
            if (session == null)
            {
                return StatusCode(503, new { message = "AI service is currently unavailable." });
            }

            // 2. Read the file as base64
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            // 3. Build attachment and structured schema for Gemini JSON output
            var attachments = new List<ChatbotAttachment>
            {
                new()
                {
                    Type = file.ContentType.StartsWith("image/") ? "Image" : "PDF",
                    Url = $"data:{file.ContentType};base64,{base64}",
                    MimeType = file.ContentType,
                    SizeBytes = file.Length
                }
            };

            var responseSchema = new
            {
                type = "object",
                properties = new
                {
                    expiration_date = new { type = "string", description = "The NDA expiration date in ISO 8601 format (YYYY-MM-DD), or null if not found." },
                    effective_date = new { type = "string", description = "The NDA effective/start date in ISO 8601 format (YYYY-MM-DD), or null if not found." },
                    signed_date = new { type = "string", description = "The date the NDA was signed in ISO 8601 format (YYYY-MM-DD), or null if not found." }
                }
            };

            const string prompt = """
                Analyze this NDA (Non-Disclosure Agreement) document and extract the following dates:
                1. expiration_date - When the NDA expires or terminates
                2. effective_date - When the NDA becomes effective or starts
                3. signed_date - When the NDA was signed

                Return dates in ISO 8601 format (YYYY-MM-DD). If a date cannot be found, return null for that field.
                Look for terms like "expiration", "termination", "effective date", "commencement", "signed on", "executed on", "valid until", "term of", etc.
                """;

            // 4. Send message with structured output
            var response = await chatbotClient.SendMessageAsync(
                session.SessionId,
                prompt,
                attachments,
                responseMimeType: "application/json",
                responseSchema: responseSchema);

            if (response == null || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("AI returned no content for NDA date extraction.");
                return Ok(new ExtractedNdaDatesResponse());
            }

            // 5. Parse JSON response
            _logger.LogInformation("AI NDA date extraction response: {Content}", response.Content);

            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            var result = new ExtractedNdaDatesResponse
            {
                ExpirationDate = TryParseDate(root, "expiration_date"),
                EffectiveDate = TryParseDate(root, "effective_date"),
                SignedDate = TryParseDate(root, "signed_date")
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NDA date extraction failed.");
            return Ok(new ExtractedNdaDatesResponse());
        }
    }

    /// <summary>
    /// Summarizes an uploaded NDA document using AI, extracting key terms and provisions.
    /// </summary>
    /// <param name="file">The NDA document file to summarize.</param>
    /// <returns>A structured summary of the NDA document.</returns>
    [RequirePermission(MalievPermissions.Customer.Write)]
    [HttpPost("summarize-nda")]
    public async Task<ActionResult<NdaSummaryResponse>> SummarizeNda([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        try
        {
            var session = await chatbotClient.InitiateSessionAsync("intranet", "en");
            if (session == null)
            {
                return StatusCode(503, new { message = "AI service is currently unavailable." });
            }

            using var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            var attachments = new List<ChatbotAttachment>
            {
                new()
                {
                    Type = file.ContentType.StartsWith("image/") ? "Image" : "PDF",
                    Url = $"data:{file.ContentType};base64,{base64}",
                    MimeType = file.ContentType,
                    SizeBytes = file.Length
                }
            };

            var responseSchema = new
            {
                type = "object",
                properties = new
                {
                    summary = new { type = "string", description = "A concise 2-4 sentence summary of the NDA document, covering its purpose and main obligations." },
                    key_terms = new { type = "array", items = new { type = "string" }, description = "List of 3-6 key terms or notable provisions (e.g. 'Non-compete clause for 2 years', 'Covers trade secrets and client lists')." },
                    confidentiality_scope = new { type = "string", description = "What information is covered as confidential, or null if not specified." },
                    duration = new { type = "string", description = "The duration/term of the NDA (e.g. '3 years from effective date'), or null if not specified." },
                    governing_law = new { type = "string", description = "The governing law/jurisdiction, or null if not specified." }
                }
            };

            const string prompt = """
                Analyze this NDA (Non-Disclosure Agreement) document and provide a structured summary.
                Focus on:
                1. A concise overall summary (2-4 sentences)
                2. Key terms and notable provisions (3-6 bullet points)
                3. The scope of confidential information covered
                4. The duration/term of the agreement
                5. The governing law or jurisdiction

                If any field cannot be determined from the document, return null for that field.
                For key_terms, provide short actionable descriptions (e.g. "Non-compete clause for 2 years").
                """;

            var response = await chatbotClient.SendMessageAsync(
                session.SessionId,
                prompt,
                attachments,
                responseMimeType: "application/json",
                responseSchema: responseSchema);

            if (response == null || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("AI returned no content for NDA summarization.");
                return Ok(new NdaSummaryResponse { Summary = "Unable to generate summary from this document." });
            }

            _logger.LogInformation("AI NDA summary response: {Content}", response.Content);

            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            var result = new NdaSummaryResponse
            {
                Summary = root.TryGetProperty("summary", out var sProp) && sProp.ValueKind == JsonValueKind.String
                    ? sProp.GetString() ?? "" : "",
                KeyTerms = root.TryGetProperty("key_terms", out var ktProp) && ktProp.ValueKind == JsonValueKind.Array
                    ? ktProp.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList() : [],
                ConfidentialityScope = root.TryGetProperty("confidentiality_scope", out var csProp) && csProp.ValueKind == JsonValueKind.String
                    ? csProp.GetString() : null,
                Duration = root.TryGetProperty("duration", out var dProp) && dProp.ValueKind == JsonValueKind.String
                    ? dProp.GetString() : null,
                GoverningLaw = root.TryGetProperty("governing_law", out var glProp) && glProp.ValueKind == JsonValueKind.String
                    ? glProp.GetString() : null
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NDA summarization failed.");
            return Ok(new NdaSummaryResponse { Summary = "Unable to generate summary due to a processing error." });
        }
    }

    private static DateTime? TryParseDate(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(prop.GetString(), out var date))
        {
            return date;
        }
        return null;
    }

    /// <summary>
    /// Uploads a single document to the central upload service.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="category">The category (e.g. NDA, General).</param>
    /// <param name="customerId">The ID of the customer owning the document.</param>
    [RequirePermission(MalievPermissions.Customer.Write)]
    [HttpPost("upload-document")]
    public async Task<ActionResult<BffUploadResponse>> UploadDocument(IFormFile file, [FromQuery] string category = "General", [FromQuery] Guid? customerId = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var standardizedCategory = category.Trim();
        if (standardizedCategory.Equals("nda", StringComparison.OrdinalIgnoreCase)) standardizedCategory = "NDA";
        else if (standardizedCategory.Equals("general", StringComparison.OrdinalIgnoreCase)) standardizedCategory = "General";

        using var stream = file.OpenReadStream();
        var storagePathId = customerId.HasValue ? customerId.Value.ToString() : "{id}";
        var path = $"customer-onboarding/{standardizedCategory}/{storagePathId}/{file.FileName}";

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
    /// <param name="category">Document category (e.g., 'NDA', 'General').</param>
    /// <param name="subType">Optional sub-type for categorization.</param>
    /// <param name="customerId">The ID of the customer owning the documents.</param>
    /// <returns>List of upload metadata for all files.</returns>
    [RequirePermission(MalievPermissions.Customer.Write)]
    [HttpPost("upload-documents")]
    public async Task<ActionResult<List<BffUploadResponse>>> UploadDocuments(
        [FromForm] IFormFileCollection files,
        [FromQuery] string category = "General",
        [FromQuery] string? subType = null,
        [FromQuery] Guid? customerId = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded.");
        }

        // Standardize category casing for storage paths (General, NDA, etc.)
        var standardizedCategory = category.Trim();
        if (standardizedCategory.Equals("nda", StringComparison.OrdinalIgnoreCase)) standardizedCategory = "NDA";
        else if (standardizedCategory.Equals("general", StringComparison.OrdinalIgnoreCase)) standardizedCategory = "General";

        var results = new List<BffUploadResponse>();

        foreach (var file in files)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest($"File '{file.FileName}' exceeds the 10 MB limit.");
            }

            using var stream = file.OpenReadStream();
            var storagePathId = customerId.HasValue ? customerId.Value.ToString() : "{id}";
            var path = $"customer-onboarding/{standardizedCategory}/{storagePathId}/{file.FileName}";

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

    /// <summary>
    /// Links uploaded documents to an owner (Customer or Company).
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Write)]
    [HttpPost("link-documents")]
    public async Task<IActionResult> LinkDocuments(
        [FromQuery] string ownerType,
        [FromQuery] Guid ownerId,
        [FromBody] List<CreateDocumentRequest> documents)
    {
        if (documents == null || documents.Count == 0) return BadRequest("No documents to link.");

        var created = await customerClient.CreateDocumentsAsync(ownerType, ownerId, documents);
        return Ok(created);
    }

    /// <summary>
    /// Gets a signed download URL for a file.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("download-url/{fileReference}")]
    public async Task<ActionResult<string>> GetDownloadUrl(string fileReference)
    {
        var url = await uploadClient.GetDownloadUrlAsync(fileReference);
        if (string.IsNullOrEmpty(url)) return NotFound("File not found or URL generation failed.");
        return Ok(new { url });
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
