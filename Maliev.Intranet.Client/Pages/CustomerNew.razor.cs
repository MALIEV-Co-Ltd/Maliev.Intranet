using System.Net.Http.Json;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Microsoft.Extensions.Logging;

namespace Maliev.Intranet.Client.Pages;

/// <summary>Page for creating a new customer with optional company onboarding, addresses, and NDA.</summary>
public partial class CustomerNew : ComponentBase
{
    /// <summary>HTTP client for API calls.</summary>
    [Inject] public HttpClient Http { get; set; } = null!;
    /// <summary>Navigation manager for routing.</summary>
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    /// <summary>Snackbar service for transient notifications.</summary>
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    /// <summary>Dialog service for modal dialogs.</summary>
    [Inject] public IDialogService DialogService { get; set; } = null!;
    /// <summary>JavaScript runtime for browser interop.</summary>
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    
    
    private MudForm? _form;

    // Customer data
    private CustomerOnboardingRequest _customerData = new()
    {
        Customer = new CreateCustomerRequest
        {
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }
    };
    
    // Company data
    private CreateCompanyRequest _newCompany = new() { Segment = "Retail", Tier = "Bronze" };
    private string _companyOfficeType = "head";
    private string _branchNumber = "00001";
    private bool _showCompanyFields = false;
    private bool _companyIsManualEntry = false;
    private RegistryCompanyProfile? _selectedCompany;
    
    // Address data
    private Dictionary<int, CountryDto?> _selectedCountries = new();
    private Dictionary<int, RegistryThaiLocation?> _selectedLocations = new();
    private List<CountryDto> _allCountries = new();
    
    // Communication preferences
    private bool _emailOptIn = true;
    private bool _smsOptIn = false;
    private bool _marketingOptIn = false;
    
    // NDA
    private bool _includeNda = false;
    private DateTime? _ndaExpiryDate = DateTime.Now.AddYears(1);
    private List<IBrowserFile> _ndaFiles = new();
    private List<CreateDocumentRequest> _uploadedNdaDocs = new();
    private bool _isUploadingNda = false;
    
    // Customer documents
    private List<IBrowserFile> _customerDocFiles = new();
    private List<CreateDocumentRequest> _uploadedCustomerDocs = new();
    private string _customerDocCategory = "General";
    private bool _isUploadingCustomerDocs = false;
    
    // AI Extraction
    private bool _chatbotServiceAvailable = false;
    
    // Submission
    private bool _isSubmitting = false;

    /// <summary>Initializes the component by loading countries and checking service availability.</summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadCountries();
        await CheckChatbotServiceAvailability();
    }

    private async Task CheckChatbotServiceAvailability()
    {
        try
        {
            var response = await Http.GetAsync("api/aiprocessing/health");
            if (response.IsSuccessStatusCode)
            {
                var healthData = await response.Content.ReadFromJsonAsync<AiHealthResponse>();
                _chatbotServiceAvailable = healthData?.CanInitiateSession ?? false;
            }
            else
            {
                _chatbotServiceAvailable = false;
            }
        }
        catch
        {
            _chatbotServiceAvailable = false;
        }
    }

    private class AiHealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public Guid? SessionId { get; set; }
        public bool CanInitiateSession { get; set; }
        public string? Message { get; set; }
    }

    private async Task LoadCountries()
    {
        try
        {
            _allCountries = await Http.GetFromJsonAsync<List<CountryDto>>("api/customers/countries") ?? new();
            
            var thailand = _allCountries.FirstOrDefault(c => c.Code == "TH");
            if (thailand != null)
            {
                _selectedCountries[0] = thailand;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading countries: {ex.Message}", Severity.Error);
        }
    }

    #region AI Extraction
    
    private async Task OpenExtractionDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ExtractionDialog>("AI Data Extraction", options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is ExtractedCustomerDataResponse extracted)
        {
            PopulateFromExtraction(extracted);
            Snackbar.Add($"AI Extraction completed with {(extracted.Confidence * 100):F0}% confidence. Please review the populated fields.", Severity.Success);
        }
    }

    private void PopulateFromExtraction(ExtractedCustomerDataResponse extracted)
    {
        // Customer fields
        if (!string.IsNullOrWhiteSpace(extracted.FirstName)) _customerData.Customer.FirstName = extracted.FirstName;
        if (!string.IsNullOrWhiteSpace(extracted.LastName)) _customerData.Customer.LastName = extracted.LastName;
        if (!string.IsNullOrWhiteSpace(extracted.Email)) _customerData.Customer.Email = extracted.Email;
        if (!string.IsNullOrWhiteSpace(extracted.Mobile)) _customerData.Customer.Mobile = extracted.Mobile;
        if (!string.IsNullOrWhiteSpace(extracted.Landline)) _customerData.Customer.Landline = extracted.Landline;
        if (!string.IsNullOrWhiteSpace(extracted.Extension)) _customerData.Customer.Extension = extracted.Extension;
        if (!string.IsNullOrWhiteSpace(extracted.Segment)) _customerData.Customer.Segment = extracted.Segment;
        
        // Company fields
        if (!string.IsNullOrWhiteSpace(extracted.CompanyName))
        {
            _showCompanyFields = true;
            _companyIsManualEntry = true; // Extracted data is not verified from BDEX
            _newCompany.Name = extracted.CompanyName;
            if (!string.IsNullOrWhiteSpace(extracted.VatNumber)) _newCompany.VatNumber = extracted.VatNumber;
            if (!string.IsNullOrWhiteSpace(extracted.CompanyPhone)) _newCompany.ContactPhone = extracted.CompanyPhone;
            
            if (!string.IsNullOrWhiteSpace(extracted.BranchNumber))
            {
                var branch = extracted.BranchNumber.Trim();
                var isHeadOffice = branch == "00000" || 
                                   branch.Equals("Head Office", StringComparison.OrdinalIgnoreCase) || 
                                   branch.Equals("สำนักงานใหญ่", StringComparison.OrdinalIgnoreCase);
                
                if (isHeadOffice)
                {
                    _companyOfficeType = "head";
                    _branchNumber = "00000";
                }
                else
                {
                    _companyOfficeType = "branch";
                    _branchNumber = branch;
                }
            }
        }
        
        // Addresses
        if (extracted.Addresses?.Any() == true)
        {
            _customerData.Addresses.Clear();
            var thailand = _allCountries.FirstOrDefault(c => c.Code == "TH");
            
            foreach (var addr in extracted.Addresses)
            {
                var type = addr.Type ?? "Billing";
                var newAddr = new CreateAddressRequest
                {
                    Type = type,
                    // Default logic will be applied by UpdateAddressDefaults() below
                    IsDefault = false,
                    AddressLine1 = addr.AddressLine1 ?? string.Empty,
                    AddressLine2 = addr.AddressLine2,
                    AddressLine3 = addr.AddressLine3,
                    District = addr.District ?? string.Empty,
                    City = addr.City ?? string.Empty,
                    StateProvince = addr.StateProvince ?? string.Empty,
                    PostalCode = addr.PostalCode ?? string.Empty,
                    RecipientName = addr.RecipientName,
                    RecipientPhone = addr.RecipientPhone
                };
                
                if (addr.Location != null)
                {
                    _selectedLocations[_customerData.Addresses.Count] = addr.Location;
                }
                
                if (thailand != null)
                {
                    newAddr.CountryId = thailand.Id;
                    _selectedCountries[_customerData.Addresses.Count] = thailand;
                }
                
                _customerData.Addresses.Add(newAddr);
            }

            UpdateAddressDefaults();
        }
    }
    
    #endregion

    #region Address Management
    
    private async Task<IEnumerable<RegistryThaiLocation>> SearchThaiLocations(string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Array.Empty<RegistryThaiLocation>();

        try
        {
            var results = await Http.GetFromJsonAsync<List<RegistryThaiLocation>>(
                $"api/customers/locations/thai?query={Uri.EscapeDataString(query)}&limit=10", cancellationToken);
            return results ?? new List<RegistryThaiLocation>();
        }
        catch
        {
            return Array.Empty<RegistryThaiLocation>();
        }
    }

    private void OnThaiLocationSelected(int addressIndex, RegistryThaiLocation? location)
    {
        if (addressIndex >= _customerData.Addresses.Count) return;

        var address = _customerData.Addresses[addressIndex];
        
        // Update the selected location object for binding
        _selectedLocations[addressIndex] = location;

        if (location == null) return;

        var useThai = IsThai(address.District ?? "") || IsThai(address.City ?? "") || IsThai(address.StateProvince ?? "") || IsThai(address.AddressLine1);
        
        if (!useThai && string.IsNullOrEmpty(address.District) && string.IsNullOrEmpty(address.City) && string.IsNullOrEmpty(address.StateProvince))
        {
            useThai = true;
        }
        
        address.District = useThai ? location.SubDistrictTh : location.SubDistrictEn;
        address.City = useThai ? location.DistrictTh : location.DistrictEn;
        address.StateProvince = useThai ? location.ProvinceTh : location.ProvinceEn;
        address.PostalCode = location.PostalCode;
        
        var thailand = _allCountries.FirstOrDefault(c => c.Code == "TH");
        if (thailand != null)
        {
            address.CountryId = thailand.Id;
            _selectedCountries[addressIndex] = thailand;
        }
    }

    private async Task<IEnumerable<CountryDto>> SearchCountries(string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query)) return _allCountries;

        return _allCountries.Where(c => 
            c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Code.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void OnCountrySelected(int addressIndex, CountryDto? country)
    {
        _selectedCountries[addressIndex] = country;
        if (country != null && addressIndex < _customerData.Addresses.Count)
        {
            _customerData.Addresses[addressIndex].CountryId = country.Id;
        }
    }

    private void OnAddressTypeChanged(int addressIndex, string type)
    {
        if (addressIndex < _customerData.Addresses.Count)
        {
            _customerData.Addresses[addressIndex].Type = type;
            UpdateAddressDefaults();
        }
    }

    private void OnDefaultChanged(int addressIndex, bool isDefault)
    {
        if (!isDefault || addressIndex >= _customerData.Addresses.Count) return;

        var address = _customerData.Addresses[addressIndex];
        
        foreach (var addr in _customerData.Addresses.Where(a => a != address && a.Type == address.Type))
        {
            addr.IsDefault = false;
        }
        
        address.IsDefault = true;
    }

    private void UpdateAddressDefaults()
    {
        var hasDefaultBilling = false;
        var hasDefaultShipping = false;
        
        foreach (var addr in _customerData.Addresses)
        {
            if (addr.Type == "Billing")
            {
                addr.IsDefault = !hasDefaultBilling;
                if (addr.IsDefault) hasDefaultBilling = true;
            }
            else if (addr.Type == "Shipping")
            {
                addr.IsDefault = !hasDefaultShipping;
                if (addr.IsDefault) hasDefaultShipping = true;
            }
        }
    }

    private void AddAddress()
    {
        var type = _customerData.Addresses.Count(a => a.Type == "Billing") >= 1 ? "Shipping" : "Billing";
        
        var newAddress = new CreateAddressRequest
        {
            Type = type,
            IsDefault = !_customerData.Addresses.Any(a => a.Type == type)
        };
        
        var thailand = _allCountries.FirstOrDefault(c => c.Code == "TH");
        if (thailand != null)
        {
            newAddress.CountryId = thailand.Id;
            _selectedCountries[_customerData.Addresses.Count] = thailand;
        }
        
        _customerData.Addresses.Add(newAddress);
        UpdateAddressDefaults();
    }

    private void RemoveAddress(int index)
    {
        _customerData.Addresses.RemoveAt(index);
        _selectedCountries.Remove(index);
        UpdateAddressDefaults();
    }

    private static bool IsThai(string? value) => value?.Any(c => c >= 0x0E00 && c <= 0x0E7F) ?? false;
    
    #endregion

    #region File Upload
    
    private void OnNdaFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        _ndaFiles.Clear();
        const int maxFiles = 20;
        if (files.Count > maxFiles)
        {
            Snackbar.Add($"Only the first {maxFiles} files will be accepted ({files.Count} selected).", Severity.Warning);
            _ndaFiles.AddRange(files.Take(maxFiles));
        }
        else
        {
            _ndaFiles.AddRange(files);
        }
    }

    private async Task UploadNdaFiles()
    {
        await UploadDocuments(_ndaFiles, "nda", _uploadedNdaDocs, isNda: true);
    }

    private void OnCustomerDocsSelected(IReadOnlyList<IBrowserFile> files)
    {
        _customerDocFiles.Clear();
        const int maxFiles = 20;
        if (files.Count > maxFiles)
        {
            Snackbar.Add($"Only the first {maxFiles} files will be accepted ({files.Count} selected).", Severity.Warning);
            _customerDocFiles.AddRange(files.Take(maxFiles));
        }
        else
        {
            _customerDocFiles.AddRange(files);
        }
    }

    private async Task UploadCustomerDocuments()
    {
        await UploadDocuments(_customerDocFiles, _customerDocCategory, _uploadedCustomerDocs, isNda: false);
    }

    private async Task UploadDocuments(List<IBrowserFile> files, string category, List<CreateDocumentRequest> uploadedList, bool isNda)
    {
        if (isNda) _isUploadingNda = true;
        else _isUploadingCustomerDocs = true;

        try
        {
            using var content = new MultipartFormDataContent();
            
            foreach (var file in files)
            {
                var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "files", file.Name);
            }

            var response = await Http.PostAsync($"api/aiprocessing/upload-documents?category={Uri.EscapeDataString(category)}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var results = await response.Content.ReadFromJsonAsync<List<BffUploadResponse>>();
                
                foreach (var result in results ?? [])
                {
                    var doc = new CreateDocumentRequest
                    {
                        DocumentCategory = category,
                        FileReference = result.UploadId,
                        FileName = result.FileName,
                        FileSize = result.FileSize,
                        MimeType = GetMimeType(result.FileName)
                    };
                    uploadedList.Add(doc);
                    _customerData.Documents.Add(doc);
                }
                
                Snackbar.Add($"Uploaded {results?.Count ?? 0} file(s)", Severity.Success);
                files.Clear();
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Snackbar.Add($"Upload failed: {response.StatusCode}. {errorBody}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            if (isNda) _isUploadingNda = false;
            else _isUploadingCustomerDocs = false;
        }
    }

    private static string GetMimeType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };
    
    #endregion

    #region Form Validation & Submission
    
    private async Task<string?> ValidateEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "Email is required";
        if (!email.Contains("@") || !email.Contains(".")) return "Invalid email format";
        
        try
        {
            var exists = await Http.GetFromJsonAsync<bool>($"api/customers/check-email?email={Uri.EscapeDataString(email)}");
            if (exists) return "A customer with this email already exists";
        }
        catch { }
        
        return null;
    }
    
    private bool IsFormValid()
    {
        // Basic customer validation
        var basicValid = !string.IsNullOrWhiteSpace(_customerData.Customer.FirstName) &&
                         !string.IsNullOrWhiteSpace(_customerData.Customer.LastName) &&
                         !string.IsNullOrWhiteSpace(_customerData.Customer.Email);

        // Company validation (only if Tax ID is provided)
        var hasCompanyData = !string.IsNullOrWhiteSpace(_newCompany.VatNumber);
        var companyValid = !hasCompanyData || !string.IsNullOrWhiteSpace(_newCompany.Name);

        // Address validation
        var addressesValid = _customerData.Addresses.All(a =>
            !string.IsNullOrWhiteSpace(a.AddressLine1) &&
            !string.IsNullOrWhiteSpace(a.City) &&
            !string.IsNullOrWhiteSpace(a.StateProvince) &&
            !string.IsNullOrWhiteSpace(a.PostalCode) &&
            a.CountryId != Guid.Empty);

        return basicValid && companyValid && addressesValid;
    }

    private async Task Submit()
    {
        if (_isSubmitting) return;
        _isSubmitting = true;

        try
        {
            if (_form != null)
            {
                await _form.ValidateAsync();
                if (!_form.IsValid)
                {
                    Snackbar.Add("Please fix validation errors", Severity.Warning);
                    return;
                }
            }

            if (!IsFormValid())
            {
                Snackbar.Add("Please fill in all required fields", Severity.Warning);
                return;
            }

            // Build request data
            _customerData.Customer.CommunicationPreferences = new Dictionary<string, bool>
            {
                ["email_opt_in"] = _emailOptIn,
                ["sms_opt_in"] = _smsOptIn,
                ["marketing_opt_in"] = _marketingOptIn
            };

            // Include company data only if Tax ID is provided
            var hasCompanyData = !string.IsNullOrWhiteSpace(_newCompany.VatNumber);
            _customerData.NewCompany = hasCompanyData ? _newCompany : null;
            _customerData.Customer.CompanyId = null;

            if (_includeNda)
            {
                _customerData.Nda = new CreateNDARequest
                {
                    ExpiresAt = _ndaExpiryDate,
                    IsActive = true
                };
            }

            // Open progress dialog
            var parameters = new DialogParameters<CustomerOnboardingDialog>
            {
                { x => x.Request, _customerData },
                { x => x.HasCompany, hasCompanyData },
                { x => x.IncludeNda, _includeNda }
            };
            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
                BackdropClick = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var dialog = await DialogService.ShowAsync<CustomerOnboardingDialog>("Creating Customer", parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is Guid customerId)
            {
                Navigation.NavigateTo($"/sales/customers/{customerId}");
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void RemoveUploadedDoc(CreateDocumentRequest doc, bool isNda)
    {
        if (isNda) _uploadedNdaDocs.Remove(doc);
        else _uploadedCustomerDocs.Remove(doc);
        
        _customerData.Documents.Remove(doc);
        StateHasChanged();
    }

    private void Cancel() => Navigation.NavigateTo("/sales/customers");

    #endregion

    #region Company Lookup

    private async Task<IEnumerable<RegistryCompanyProfile>> SearchCompaniesAsync(string value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            return [];

        var isTaxId = new string(value.Where(char.IsDigit).ToArray()).Length == 13;
        var limit = isTaxId ? 1 : 10;

        try
        {
            var response = await Http.GetFromJsonAsync<List<RegistryCompanyProfile>>(
                $"api/customers/companies/search?query={Uri.EscapeDataString(value)}&limit={limit}", ct);
            return response ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void OnCompanySelected(RegistryCompanyProfile? company)
    {
        if (company == null) return;

        // Populate all company fields from the selected result
        _newCompany.VatNumber = company.TaxId;
        _newCompany.Name = company.CompanyNameTh;
        _newCompany.FullNameTh = company.FullNameTh;
        _newCompany.CompanyStatus = company.StatusCode;
        _newCompany.CompanyStatusNameTh = company.StatusNameTh;
        _newCompany.CompanyTypeCode = company.CompanyTypeCode;
        _newCompany.BusinessObjectives = company.BusinessObjectives;
        _newCompany.StockSymbol = company.StockName;
        _newCompany.IsVerifiedFromBdex = true;
        _companyIsManualEntry = false;
        _showCompanyFields = true;
        _selectedCompany = company;

        var statusInfo = !string.IsNullOrWhiteSpace(company.StatusNameTh)
            ? $" - {company.StatusNameTh}"
            : string.Empty;
        Snackbar.Add($"✓ Company verified: {company.CompanyNameTh}{statusInfo}", Severity.Success);
    }

    private void ResetCompanyVerification()
    {
        var taxId = _newCompany.VatNumber;
        _newCompany = new CreateCompanyRequest
        {
            VatNumber = taxId,
            Segment = _newCompany.Segment,
            Tier = _newCompany.Tier
        };
        _selectedCompany = null;
        _companyIsManualEntry = false;
        _showCompanyFields = true;
        Snackbar.Add("Company verification reset. Search again to verify.", Severity.Info);
    }

    private async Task RemoveCompany()
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.Content, "Are you sure you want to remove the company information?" },
            { x => x.ConfirmButtonText, "Remove" },
            { x => x.ConfirmButtonColor, Color.Error }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Remove Company", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            _newCompany = new CreateCompanyRequest { Segment = "Retail", Tier = "Bronze" };
            _companyOfficeType = "head";
            _branchNumber = "00001";
            _companyIsManualEntry = false;
            _showCompanyFields = false;
            _selectedCompany = null;
            Snackbar.Add("Company information removed", Severity.Success);
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    private async Task<string> GetBdexErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            // Try to parse as API error response
            if (!string.IsNullOrWhiteSpace(errorContent))
            {
                try
                {
                    var apiError = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(errorContent);
                    if (apiError?.Message != null)
                    {
                        return apiError.Message;
                    }
                }
                catch
                {
                    // Failed to parse error response, will use generic message
                }
            }

            // Return generic error based on status code
            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "Invalid request: Please check the Tax ID format",
                System.Net.HttpStatusCode.Unauthorized => "Authentication failed: Please contact system administrator",
                System.Net.HttpStatusCode.Forbidden => "Access denied: Insufficient permissions to access company registry",
                System.Net.HttpStatusCode.NotFound => "Company registry service not available",
                System.Net.HttpStatusCode.TooManyRequests => "Too many requests: Please wait a moment and try again",
                System.Net.HttpStatusCode.ServiceUnavailable => "Company registry service is temporarily unavailable",
                System.Net.HttpStatusCode.GatewayTimeout => "Request timeout: Company registry service is taking too long to respond",
                _ => $"Company lookup failed (Status: {(int)response.StatusCode})"
            };
        }
        catch
        {
            return $"Company lookup failed (Status: {(int)response.StatusCode})";
        }
    }

    #endregion

}
