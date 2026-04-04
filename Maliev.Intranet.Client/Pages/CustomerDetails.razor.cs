using System.Net.Http.Json;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;

namespace Maliev.Intranet.Client.Pages;

/// <summary>Displays detailed information about a customer, including addresses, documents, NDAs, activities, and internal notes.</summary>
/// <summary>Displays detailed information about a customer, including addresses, documents, NDAs, activities, and internal notes.</summary>
public partial class CustomerDetails : ComponentBase, IAsyncDisposable
{
    /// <summary>HTTP client for API calls.</summary>
    [Inject] public HttpClient Http { get; set; } = null!;
    /// <summary>Navigation manager for routing.</summary>
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    /// <summary>Snackbar service for transient notifications.</summary>
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    /// <summary>Dialog service for modal dialogs.</summary>
    [Inject] public IDialogService DialogService { get; set; } = null!;
    /// <summary>SignalR service for real-time customer updates.</summary>
    [Inject] public ISignalRCustomerService SignalRService { get; set; } = null!;
    /// <summary>JavaScript runtime for browser interop.</summary>
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    /// <summary>Layout service for theme management.</summary>
    [Inject] public LayoutService LayoutService { get; set; } = null!;
    /// <summary>Service for managing breadcrumb navigation.</summary>
    [Inject] public BreadcrumbService BreadcrumbService { get; set; } = null!;
    /// <summary>Logger for this component.</summary>
    [Inject] public ILogger<CustomerDetails> Logger { get; set; } = null!;


    /// <summary>The customer identifier from the route.</summary>
    [Parameter] public Guid Id { get; set; }
    private CustomerDetailDto? _customer;
    private List<CustomerActivityResponse> _activities = [];
    private bool _loading = true;
    private bool _isEditMode = false;
    private bool _isSaving = false;
    private bool _canChangeEmail = false;
    private int _activeTabIndex = 0;
    
    private int _activityPage = 1;
    private bool _activityHasNextPage = false;
    private bool _loadingMoreActivities = false;
    private MudTable<CustomerActivityResponse>? _virtualizeComponent;

    private MudDropContainer<AddressResponse>? _dropContainer;
    private MudForm? _editForm;
    private UpdateCustomerRequest _updateRequest = new();
    private List<CountryDto> _allCountries = new();

    private string _noteSearchString = "";
    private bool _noteDrawerOpen = false;
    private Guid? _editingNoteId;
    private string _editingNoteText = "";
    private string _originalNoteText = "";
    private bool _isNewNote = true;
    private NoteEditor? _noteEditorRef;

    private IEnumerable<InternalNoteResponse> FilteredNotes =>
        (_customer?.Notes ?? [])
        .Where(n => string.IsNullOrEmpty(_noteSearchString) || n.NoteText.Contains(_noteSearchString, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(n => n.CreatedAt);

    /// <summary>Initializes the component by loading customer data, activities, and starting SignalR.</summary>
    /// <summary>Initializes the component by loading customer data, activities, and starting SignalR.</summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadCountries();
        await LoadCustomerData();
        await LoadInitialActivities();

        // Push customer name into breadcrumb so it shows e.g. "Customers > Acme Corp" instead of "Customers > Details"
        if (_customer is not null)
            BreadcrumbService.SetPageLabel(_customer.Name);

        await SignalRService.StartAsync();
        SignalRService.OnCustomerChanged += HandleCustomerChanged;
    }

    private async Task HandleCustomerChanged()
    {
        await InvokeAsync(async () =>
        {
            if (!_isEditMode) 
            {
                await LoadCustomerData();
                await LoadInitialActivities();
            }
        });
    }

    private string GetAddressZone(AddressResponse address) => address.IsDefault ? (address.Type == "Billing" ? "billing-default" : "shipping-default") : "others";

    private async Task AddressItemDropped(MudItemDropInfo<AddressResponse> dropInfo)
    {
        if (dropInfo.Item == null || _customer == null) return;
        var address = dropInfo.Item;
        string newType = address.Type;
        bool newIsDefault = address.IsDefault;
        
        if (dropInfo.DropzoneIdentifier == "billing-default") { newType = "Billing"; newIsDefault = true; }
        else if (dropInfo.DropzoneIdentifier == "shipping-default") { newType = "Shipping"; newIsDefault = true; }
        else if (dropInfo.DropzoneIdentifier == "others") { newIsDefault = false; }

        if (newType != address.Type || newIsDefault != address.IsDefault)
        {
            try
            {
                // If we are setting a new default, we should update the entire collection to ensure 
                // other addresses of the same type are no longer marked as default.
                if (newIsDefault)
                {
                    var updatedAddresses = _customer.Addresses.Select(a => {
                        if (a.Id == address.Id) 
                            return new CreateAddressRequest { Id = a.Id, Type = newType, IsDefault = true, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, AddressLine3 = a.AddressLine3, District = a.District, City = a.City, StateProvince = a.StateProvince, PostalCode = a.PostalCode, CountryId = a.CountryId, RecipientName = a.RecipientName, RecipientPhone = a.RecipientPhone, Version = a.Version };
                        
                        if (a.Type == newType)
                            return new CreateAddressRequest { Id = a.Id, Type = a.Type, IsDefault = false, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, AddressLine3 = a.AddressLine3, District = a.District, City = a.City, StateProvince = a.StateProvince, PostalCode = a.PostalCode, CountryId = a.CountryId, RecipientName = a.RecipientName, RecipientPhone = a.RecipientPhone, Version = a.Version };
                        
                        return new CreateAddressRequest { Id = a.Id, Type = a.Type, IsDefault = a.IsDefault, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, AddressLine3 = a.AddressLine3, District = a.District, City = a.City, StateProvince = a.StateProvince, PostalCode = a.PostalCode, CountryId = a.CountryId, RecipientName = a.RecipientName, RecipientPhone = a.RecipientPhone, Version = a.Version };
                    }).ToList();

                    var onboardingRequest = new CustomerOnboardingRequest { Customer = new CreateCustomerRequest { FirstName = _customer.FirstName, LastName = _customer.LastName, Email = _customer.Email, Mobile = _customer.Mobile, Landline = _customer.Landline, Extension = _customer.Extension, Segment = _customer.Segment, Tier = _customer.Tier, PreferredLanguage = _customer.PreferredLanguage, Timezone = _customer.Timezone, CompanyId = _customer.CompanyId, CommunicationPreferences = _customer.CommunicationPreferences }, Addresses = updatedAddresses };
                    var response = await Http.PutAsJsonAsync($"api/customers/{Id}/full", onboardingRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        Snackbar.Add("Address updated successfully", Severity.Success);
                        await LoadCustomerData();
                        await LoadInitialActivities();
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        Snackbar.Add($"Failed to update address ({(int)response.StatusCode}): {errorBody}", Severity.Error);
                    }
                }
                else
                {
                    // Just unsetting default or changing type without setting default can be a simple PATCH
                    var updateRequest = new UpdateAddressRequest { Type = newType, IsDefault = newIsDefault, Version = address.Version } ;
                    var response = await Http.PatchAsJsonAsync($"api/customers/addresses/{address.Id}", updateRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        Snackbar.Add("Address updated successfully", Severity.Success);
                        await LoadCustomerData();
                        await LoadInitialActivities();
                    }
                    else Snackbar.Add("Failed to update address", Severity.Error);
                }
            }
            catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
            finally { _dropContainer?.Refresh(); }
        }
    }

    private async Task LoadCountries() { try { _allCountries = await Http.GetFromJsonAsync<List<CountryDto>>("api/customers/countries") ?? []; } catch { } }
    
    private async ValueTask<ItemsProviderResult<CustomerActivityResponse>> LoadActivitiesProvider(ItemsProviderRequest request)
    {
        try
        {
            var response = await Http.GetFromJsonAsync<PagedResponse<CustomerActivityResponse>>($"api/customers/{Id}/history?skip={request.StartIndex}&take={request.Count}", request.CancellationToken);
            if (response != null)
            {
                return new ItemsProviderResult<CustomerActivityResponse>(response.Data, response.Meta.TotalItems);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading activities");
        }

        return new ItemsProviderResult<CustomerActivityResponse>(Enumerable.Empty<CustomerActivityResponse>(), 0);
    }

    private async Task LoadInitialActivities()
    {
        _activityPage = 1;
        _activities.Clear();
        await LoadActivities();
    }

    private async Task LoadMoreActivities()
    {
        _activityPage++;
        await LoadActivities();
    }

    private async Task LoadActivities()
    {
        _loadingMoreActivities = true;
        try
        {
            var take = 10;
            var skip = (_activityPage - 1) * take;
            var response = await Http.GetFromJsonAsync<PagedResponse<CustomerActivityResponse>>($"api/customers/{Id}/history?skip={skip}&take={take}");
            
            if (response != null)
            {
                if (_activityPage == 1) _activities = response.Data.ToList();
                else _activities.AddRange(response.Data);
                
                _activityHasNextPage = response.Data.Count() == take; // Simple check, assumption: if full page returned, likely more exists
                // Or better calculate based on total items if available in PagedResponse
                if (response.Meta != null) 
                {
                   _activityHasNextPage = _activities.Count < response.Meta.TotalItems;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading activities");
            Snackbar.Add("Failed to load activities", Severity.Error);
        }
        finally
        {
            _loadingMoreActivities = false;
            StateHasChanged();
        }
    }

    private string GetActivityIcon(string action) => action switch { "Create" => Icons.Material.Filled.AddCircle, "Update" => Icons.Material.Filled.Edit, "SoftDelete" => Icons.Material.Filled.Delete, _ => Icons.Material.Filled.History };
    private Color GetActivityColor(string action) => action switch { "Create" => Color.Success, "Update" => Color.Primary, "SoftDelete" => Color.Error, _ => Color.Default };

    private async Task LoadCustomerData()
    {
        // Don't clear _customer here to avoid "Customer not found" flash
        _loading = _customer == null; 
        try
        {
            var result = await Http.GetFromJsonAsync<CustomerDetailDto>($"api/customers/{Id}");
            if (result != null)
            {
                _customer = result;
                MapToUpdateRequest();
            }
        }
        catch (Exception ex) { Snackbar.Add($"Error loading customer: {ex.Message}", Severity.Error); }
        finally { _loading = false; StateHasChanged(); }
    }

    private void MapToUpdateRequest()
    {
        if (_customer == null) return;
        _updateRequest = new UpdateCustomerRequest { FirstName = _customer.FirstName, LastName = _customer.LastName, Email = _customer.Email, Mobile = _customer.Mobile, Landline = _customer.Landline, Extension = _customer.Extension, Segment = _customer.Segment, Tier = _customer.Tier, PreferredLanguage = _customer.PreferredLanguage, Timezone = _customer.Timezone, CommunicationPreferences = _customer.CommunicationPreferences, Version = _customer.Version };
    }

    private void ToggleEditMode() { if (_isEditMode) MapToUpdateRequest(); else _activeTabIndex = 0; _isEditMode = !_isEditMode; }

    private async Task SaveChanges()
    {
        if (_editForm != null) { await _editForm.ValidateAsync(); if (!_editForm.IsValid) { Snackbar.Add("Please correct validation errors first.", Severity.Warning); return; } }
        _isSaving = true;
        try
        {
            var onboardingRequest = new CustomerOnboardingRequest
            {
                Customer = new CreateCustomerRequest
                {
                    FirstName = _updateRequest.FirstName,
                    LastName = _updateRequest.LastName,
                    Email = _updateRequest.Email,
                    Mobile = _updateRequest.Mobile,
                    Landline = _updateRequest.Landline,
                    Extension = _updateRequest.Extension,
                    Segment = _updateRequest.Segment,
                    Tier = _updateRequest.Tier,
                    PreferredLanguage = _updateRequest.PreferredLanguage,
                    Timezone = _updateRequest.Timezone,
                    CompanyId = _customer?.CompanyId,
                    CommunicationPreferences = _updateRequest.CommunicationPreferences
                },
                // Preserve existing addresses — omitting them causes BFF to delete all
                Addresses = _customer?.Addresses?.Select(a => new CreateAddressRequest
                {
                    Id = a.Id,
                    Type = a.Type,
                    IsDefault = a.IsDefault,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    AddressLine3 = a.AddressLine3,
                    District = a.District,
                    City = a.City,
                    StateProvince = a.StateProvince,
                    PostalCode = a.PostalCode,
                    CountryId = a.CountryId,
                    RecipientName = a.RecipientName,
                    RecipientPhone = a.RecipientPhone,
                    Version = a.Version
                }).ToList() ?? []
            };
            var response = await Http.PutAsJsonAsync($"api/customers/{Id}/full", onboardingRequest);
            if (response.IsSuccessStatusCode) { Snackbar.Add("Customer updated successfully!", Severity.Success); _isEditMode = false; await LoadCustomerData(); await LoadInitialActivities(); }
            else { var err = await response.Content.ReadAsStringAsync(); Snackbar.Add($"Failed to save: {err}", Severity.Error); }
        }
        catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        finally { _isSaving = false; }
    }

    private async Task AddAddressDialog()
    {
        if (_customer == null) return;
        var parameters = new DialogParameters<AddressDialog> { { x => x.Address, new CreateAddressRequest { Type = "Shipping", IsDefault = !_customer.Addresses.Any(a => a.Type == "Shipping"), CountryId = _allCountries.FirstOrDefault(c => c.Code == "TH")?.Id ?? Guid.Empty } }, { x => x.IsNew, true }, { x => x.HasCompany, _customer.CompanyId.HasValue }, { x => x.CompanyName, _customer.CompanyName }, { x => x.CustomerName, _customer.Name }, { x => x.CustomerMobile, _customer.Mobile }, { x => x.AllCountries, _allCountries }, { x => x.SearchLocations, (Func<string?, CancellationToken, Task<IEnumerable<RegistryThaiLocation>>>)SearchThaiLocations } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<AddressDialog>("Add New Address", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled && result.Data is CreateAddressRequest newAddress)
        {
            try
            {
                var response = await Http.PostAsJsonAsync($"api/customers/{Id}/addresses", new List<CreateAddressRequest> { newAddress });
                if (response.IsSuccessStatusCode) { Snackbar.Add("Address added successfully", Severity.Success); await LoadCustomerData(); _dropContainer?.Refresh(); await LoadInitialActivities(); }
            }
            catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private async Task EditAddressDialog(AddressResponse currentAddress)
    {
        if (_customer == null) return;
        var editRequest = new CreateAddressRequest { Id = currentAddress.Id, Type = currentAddress.Type, IsDefault = currentAddress.IsDefault, AddressLine1 = currentAddress.AddressLine1, AddressLine2 = currentAddress.AddressLine2, AddressLine3 = currentAddress.AddressLine3, District = currentAddress.District, City = currentAddress.City, StateProvince = currentAddress.StateProvince, PostalCode = currentAddress.PostalCode, CountryId = currentAddress.CountryId, RecipientName = currentAddress.RecipientName, RecipientPhone = currentAddress.RecipientPhone, Version = currentAddress.Version };
        var parameters = new DialogParameters<AddressDialog> { { x => x.Address, editRequest }, { x => x.IsNew, false }, { x => x.HasCompany, _customer.CompanyId.HasValue }, { x => x.CompanyName, _customer.CompanyName }, { x => x.CustomerName, _customer.Name }, { x => x.CustomerMobile, _customer.Mobile }, { x => x.AllCountries, _allCountries }, { x => x.SearchLocations, (Func<string?, CancellationToken, Task<IEnumerable<RegistryThaiLocation>>>)SearchThaiLocations } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<AddressDialog>("Edit Address", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled && result.Data is CreateAddressRequest updatedAddress)
        {
            try
            {
                var addresses = _customer.Addresses.Select(a => {
                    if (a.Id == currentAddress.Id) return updatedAddress;
                    // If updated is set to default, unset others of same type
                    if (updatedAddress.IsDefault && a.Type == updatedAddress.Type)
                    {
                        return new CreateAddressRequest { Id = a.Id, Type = a.Type, IsDefault = false, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, AddressLine3 = a.AddressLine3, District = a.District, City = a.City, StateProvince = a.StateProvince, PostalCode = a.PostalCode, CountryId = a.CountryId, RecipientName = a.RecipientName, RecipientPhone = a.RecipientPhone, Version = a.Version };
                    }
                    return new CreateAddressRequest { Id = a.Id, Type = a.Type, IsDefault = a.IsDefault, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, AddressLine3 = a.AddressLine3, District = a.District, City = a.City, StateProvince = a.StateProvince, PostalCode = a.PostalCode, CountryId = a.CountryId, RecipientName = a.RecipientName, RecipientPhone = a.RecipientPhone, Version = a.Version };
                }).ToList();

                var onboardingRequest = new CustomerOnboardingRequest { Customer = new CreateCustomerRequest { FirstName = _customer.FirstName, LastName = _customer.LastName, Email = _customer.Email, Mobile = _customer.Mobile, Landline = _customer.Landline, Extension = _customer.Extension, Segment = _customer.Segment, Tier = _customer.Tier, PreferredLanguage = _customer.PreferredLanguage, Timezone = _customer.Timezone, CompanyId = _customer.CompanyId, CommunicationPreferences = _customer.CommunicationPreferences }, Addresses = addresses };
                var response = await Http.PutAsJsonAsync($"api/customers/{Id}/full", onboardingRequest);
                if (response.IsSuccessStatusCode) { Snackbar.Add("Address updated successfully", Severity.Success); await LoadCustomerData(); _dropContainer?.Refresh(); await LoadInitialActivities(); }
            } catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private async Task DeleteAddress(AddressResponse address)
    {
        if (_customer == null) return;
        var result = await DialogService.ShowMessageBoxAsync("Delete Address", "Are you sure you want to delete this address?", yesText: "Delete", noText: "Cancel");
        if (result == true)
        {
            try
            {
                var onboardingRequest = new CustomerOnboardingRequest { Customer = new CreateCustomerRequest { FirstName = _customer.FirstName, LastName = _customer.LastName, Email = _customer.Email, Mobile = _customer.Mobile, Landline = _customer.Landline, Extension = _customer.Extension, Segment = _customer.Segment, Tier = _customer.Tier, PreferredLanguage = _customer.PreferredLanguage, Timezone = _customer.Timezone, CompanyId = _customer.CompanyId }, Addresses = _customer.Addresses.Where(a => a.Id != address.Id).Select(a => new CreateAddressRequest { Id = a.Id, Type = a.Type, IsDefault = a.IsDefault, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, AddressLine3 = a.AddressLine3, District = a.District, City = a.City, StateProvince = a.StateProvince, PostalCode = a.PostalCode, CountryId = a.CountryId, RecipientName = a.RecipientName, RecipientPhone = a.RecipientPhone, Version = a.Version }).ToList() };
                var response = await Http.PutAsJsonAsync($"api/customers/{Id}/full", onboardingRequest);
                if (response.IsSuccessStatusCode) { Snackbar.Add("Address deleted successfully", Severity.Success); await LoadCustomerData(); _dropContainer?.Refresh(); await LoadInitialActivities(); }
            } catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private async Task OpenDocumentUpload()
    {
        var parameters = new DialogParameters<UploadDocumentDialog> { { x => x.CustomerId, Id }, { x => x.Http, Http }, { x => x.Category, "General" } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<UploadDocumentDialog>("Upload Documents", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled) { await LoadCustomerData(); await LoadInitialActivities(); }
    }

    private async Task OpenNdaUpload()
    {
        var parameters = new DialogParameters<UploadDocumentDialog> { { x => x.CustomerId, Id }, { x => x.Http, Http }, { x => x.Category, "NDA" }, { x => x.Accept, ".pdf" }, { x => x.HideCategory, true } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<UploadDocumentDialog>("Upload NDA Document", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data != null)
        {
            try
            {
                // Reload customer to get the newly linked document with its real ID
                await LoadCustomerData();

                var dataList = (IEnumerable<dynamic>)result.Data;
                var firstUpload = dataList.FirstOrDefault();

                if (firstUpload != null)
                {
                    string uploadId = firstUpload.UploadId;

                    // Find the document reference that was created by link-documents
                    var ndaDoc = _customer?.Documents?.FirstOrDefault(d =>
                        d.FileReference == uploadId && d.DocumentCategory == "NDA");

                    await Http.PostAsJsonAsync("api/customers/ndas", new {
                        customerId = Id,
                        status = "Draft",
                        documentReferenceId = ndaDoc?.Id
                    });

                    Snackbar.Add("NDA uploaded and draft created", Severity.Success);
                    await LoadCustomerData();
                    await LoadInitialActivities();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error creating NDA record: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task EditNdaDialog()
    {
        await LoadCustomerData();
        if (_customer?.Nda == null) return;
        var parameters = new DialogParameters<AddNdaDialog> { { x => x.CustomerId, Id }, { x => x.ExistingNda, _customer.Nda } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<AddNdaDialog>("NDA Settings", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled && result.Data != null)
        {
            try
            {
                var data = result.Data;
                var expiresAt = (DateTime?)data.GetType().GetProperty("ExpiresAt")?.GetValue(data);
                var status = (string?)data.GetType().GetProperty("Status")?.GetValue(data);
                var version = (byte[]?)data.GetType().GetProperty("Version")?.GetValue(data);
                var signedAt = (DateTime?)data.GetType().GetProperty("SignedAt")?.GetValue(data);
                var signedBy = (string?)data.GetType().GetProperty("SignedBy")?.GetValue(data);
                var revokeReason = (string?)data.GetType().GetProperty("RevokeReason")?.GetValue(data);
                var revokedAt = (DateTime?)data.GetType().GetProperty("RevokedAt")?.GetValue(data);

                // Update status and expiration date via /status endpoint (now supports expiresAt)
                var response = await Http.PatchAsJsonAsync($"api/customers/ndas/{_customer.Nda.Id}/status", new { status, expiresAt, version, signedAt, signedBy, revokeReason, revokedAt });

                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("NDA updated successfully", Severity.Success);
                    await LoadCustomerData();
                    await LoadInitialActivities();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Snackbar.Add($"Failed to update NDA: {error}", Severity.Error);
                }
            } catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private async Task<string?> GetDocumentPreviewUrl(string fileReference) { try { var response = await Http.GetFromJsonAsync<DownloadUrlResponse>($"api/aiprocessing/download-url/{fileReference}"); return response?.Url; } catch { return null; } }

    private async Task OpenSignedNdaUpload(NDAResponse nda)
    {
        var parameters = new DialogParameters<SignNdaDialog> { { x => x.CustomerId, Id }, { x => x.Http, Http } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<SignNdaDialog>("Sign NDA", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data != null)
        {
            try
            {
                var data = result.Data;
                var expiresAt = (DateTime?)data.GetType().GetProperty("ExpiresAt")?.GetValue(data);
                var fileReference = (string?)data.GetType().GetProperty("FileReference")?.GetValue(data);

                if (!string.IsNullOrEmpty(fileReference))
                {
                    // Reload customer to get the new document ID
                    await LoadCustomerData();
                    var newDoc = _customer?.Documents.FirstOrDefault(d => d.FileReference == fileReference);

                    if (newDoc != null)
                    {
                        var response = await Http.PatchAsJsonAsync($"api/customers/ndas/{nda.Id}/status", new 
                        { 
                            status = "Signed", 
                            version = nda.Version, 
                            signedAt = DateTime.UtcNow, 
                            signedBy = _customer?.Name, 
                            expiresAt = expiresAt,
                            documentReferenceId = newDoc.Id
                        });

                        if (response.IsSuccessStatusCode) 
                        { 
                            Snackbar.Add("NDA signed and uploaded successfully", Severity.Success); 
                            await LoadCustomerData(); 
                            await LoadInitialActivities(); 
                        }
                        else
                        {
                            Snackbar.Add("Failed to update NDA status", Severity.Error);
                        }
                    }
                }
            } 
            catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private async Task OpenNdaRevisionUpload(NDAResponse nda)
    {
        var parameters = new DialogParameters<UploadDocumentDialog> { { x => x.CustomerId, Id }, { x => x.Http, Http }, { x => x.Category, "NDA" }, { x => x.Accept, ".pdf" }, { x => x.HideCategory, true } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<UploadDocumentDialog>("Upload Revised Draft", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data != null)
        {
            try
            {
                await LoadCustomerData();
                var dataList = (IEnumerable<dynamic>)result.Data;
                var firstUpload = dataList.FirstOrDefault();

                if (firstUpload != null)
                {
                    string uploadId = firstUpload.UploadId;
                    var ndaDoc = _customer?.Documents?.FirstOrDefault(d => d.FileReference == uploadId && d.DocumentCategory == "NDA");

                    if (ndaDoc != null)
                    {
                         var response = await Http.PatchAsJsonAsync($"api/customers/ndas/{nda.Id}/status", new 
                        { 
                            status = nda.Status, // Keep existing status (Draft)
                            version = nda.Version, 
                            documentReferenceId = ndaDoc.Id
                        });

                        if (response.IsSuccessStatusCode)
                        {
                            Snackbar.Add("NDA revision uploaded successfully", Severity.Success);
                            await LoadCustomerData();
                            await LoadInitialActivities();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error updating NDA revision: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteNda(NDAResponse nda)
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete NDA", "Are you sure you want to delete this NDA record?", yesText: "Delete", noText: "Cancel");
        if (result == true)
        {
            try
            {
                var versionBase64 = Convert.ToBase64String(nda.Version ?? []);
                var response = await Http.DeleteAsync($"api/customers/ndas/{nda.Id}?version={Uri.EscapeDataString(versionBase64)}");
                if (response.IsSuccessStatusCode) { Snackbar.Add("NDA deleted", Severity.Success); await LoadCustomerData(); await LoadInitialActivities(); }
            } catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private async Task HandleDeleteDocument(DocumentResponse doc)
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete Document", $"Are you sure you want to delete '{doc.FileName}'?", yesText: "Delete", noText: "Cancel");
        if (result == true)
        {
            try
            {
                var response = await Http.DeleteAsync($"api/customers/documents/{doc.Id}?version={doc.Version}");
                if (response.IsSuccessStatusCode) { Snackbar.Add("Document deleted", Severity.Success); await LoadCustomerData(); await LoadInitialActivities(); }
            } catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private async Task DownloadDocument(string fileReference)
    {
        try
        {
            var response = await Http.GetFromJsonAsync<DownloadUrlResponse>($"api/aiprocessing/download-url/{fileReference}");
            if (response?.Url != null) await JSRuntime.InvokeVoidAsync("open", response.Url, "_blank");
        } catch (Exception ex) { Snackbar.Add($"Download error: {ex.Message}", Severity.Error); }
    }

    private class DownloadUrlResponse { public string? Url { get; set; } }

    private async Task<IEnumerable<RegistryThaiLocation>> SearchThaiLocations(string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return [];
        try { return await Http.GetFromJsonAsync<List<RegistryThaiLocation>>($"api/customers/locations/thai?query={Uri.EscapeDataString(query)}&limit=10") ?? []; }
        catch { return []; }
    }

    private async Task CopyAddressToClipboard(AddressResponse address)
    {
        var lines = new List<string>();
        if (address.Type == "Billing") { if (_customer?.CompanyId != null) { lines.Add(_customer.CompanyName ?? ""); if (!string.IsNullOrEmpty(_customer.CompanyVatNumber)) lines.Add($"เลขประจำตัวผู้เสียภาษี {_customer.CompanyVatNumber}"); } else { lines.Add(_customer?.Name ?? ""); if (!string.IsNullOrEmpty(_customer?.Mobile)) lines.Add(_customer.Mobile); } }
        else if (address.Type == "Shipping") { var recipient = string.IsNullOrEmpty(address.RecipientName) ? _customer?.Name : address.RecipientName; var phone = string.IsNullOrEmpty(address.RecipientPhone) ? _customer?.Mobile : address.RecipientPhone; lines.Add(recipient ?? ""); if (!string.IsNullOrEmpty(phone)) lines.Add(phone); }
        lines.Add(address.AddressLine1); if (!string.IsNullOrEmpty(address.AddressLine2)) lines.Add(address.AddressLine2); if (!string.IsNullOrEmpty(address.AddressLine3)) lines.Add(address.AddressLine3); lines.Add($"{address.District}, {address.City}"); lines.Add($"{address.StateProvince} {address.PostalCode}");
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", string.Join("\n", lines.Where(l => !string.IsNullOrWhiteSpace(l))));
        Snackbar.Add("Address copied to clipboard!", Severity.Info);
    }

    private void AddInternalNote()
    {
        _isNewNote = true;
        _editingNoteId = null;
        _editingNoteText = "";
        _originalNoteText = "";
        _noteDrawerOpen = true;
    }

    private void EditInternalNote(InternalNoteResponse note)
    {
        _isNewNote = false;
        _editingNoteId = note.Id;
        _editingNoteText = note.NoteText;
        _originalNoteText = note.NoteText;
        _noteDrawerOpen = true;
    }

    private void CloseNoteDrawer()
    {
        _noteDrawerOpen = false;
        _editingNoteId = null;
        _editingNoteText = "";
    }

    private async Task SaveNote()
    {
        if (string.IsNullOrWhiteSpace(_editingNoteText)) return;

        try
        {
            if (_isNewNote)
            {
                var response = await Http.PostAsJsonAsync($"api/customers/{Id}/notes", new { ownerType = "Customer", ownerId = Id, noteText = _editingNoteText });
                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Note added successfully", Severity.Success);
                    _noteDrawerOpen = false;
                    await LoadCustomerData();
                    await LoadInitialActivities();
                }
            }
            else if (_editingNoteId.HasValue)
            {
                var note = _customer?.Notes?.FirstOrDefault(n => n.Id == _editingNoteId.Value);
                if (note != null)
                {
                    if (note.NoteText == _editingNoteText) return;

                    var response = await Http.PatchAsJsonAsync($"api/customers/notes/{note.Id}", new { noteText = _editingNoteText, version = note.Version });
                    if (response.IsSuccessStatusCode)
                    {
                        Snackbar.Add("Note updated successfully", Severity.Success);
                        _originalNoteText = _editingNoteText;
                        await LoadCustomerData();
                        await LoadInitialActivities();
                        if (_noteEditorRef != null)
                            await _noteEditorRef.SwitchToPreview();
                    }
                }
            }
        }
        catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
    }

    private async Task DeleteInternalNote(InternalNoteResponse note)
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete Note", "Are you sure you want to delete this internal note?", yesText: "Delete", noText: "Cancel");
        if (result == true)
        {
            try { var response = await Http.DeleteAsync($"api/customers/notes/{note.Id}"); if (response.IsSuccessStatusCode) { Snackbar.Add("Note deleted successfully", Severity.Success); await LoadCustomerData(); await LoadInitialActivities(); } }
            catch (Exception ex) { Snackbar.Add($"Error: {ex.Message}", Severity.Error); }
        }
    }

    private string SanitizeActivityDescription(string description)
    {
        if (description.Contains("changed note content from", StringComparison.OrdinalIgnoreCase))
            return "Updated an internal note";
        return description;
    }

    private List<DocumentResponse> GetNonNdaDocuments()
    {
        if (_customer?.Documents == null) return [];
        var ndaDocIds = _customer.Ndas?.Where(n => n.DocumentReferenceId.HasValue).Select(n => n.DocumentReferenceId!.Value).ToHashSet() ?? [];
        return _customer.Documents
            .Where(d => !string.Equals(d.DocumentCategory, "NDA", StringComparison.OrdinalIgnoreCase) && !ndaDocIds.Contains(d.Id))
            .ToList();
    }

    private List<DocumentResponse> GetNdaDocuments()
    {
        if (_customer?.Documents == null) return [];
        var ndaDocIds = _customer.Ndas?.Where(n => n.DocumentReferenceId.HasValue).Select(n => n.DocumentReferenceId!.Value).ToHashSet() ?? [];
        return _customer.Documents
            .Where(d => string.Equals(d.DocumentCategory, "NDA", StringComparison.OrdinalIgnoreCase) || ndaDocIds.Contains(d.Id))
            .ToList();
    }

    private Color GetStatusColor(string status) => status switch { "Active" => Color.Success, "Inactive" => Color.Warning, "Blocked" => Color.Error, _ => Color.Default };
    private Color GetNdaColor(string? status) => status switch { "Signed" => Color.Success, "Expired" => Color.Error, "Draft" => Color.Warning, "Revoked" => Color.Default, _ => Color.Default };
    private string GetDocumentIcon(string mimeType) => mimeType.Contains("pdf") ? Icons.Custom.FileFormats.FilePdf : mimeType.Contains("image") ? Icons.Custom.FileFormats.FileImage : Icons.Custom.FileFormats.FileDocument;
    private Color GetDocumentColor(string mimeType) => mimeType.Contains("pdf") ? Color.Error : mimeType.Contains("image") ? Color.Success : Color.Info;
    private string FormatFileSize(long bytes) => $"{(bytes / 1024.0 / 1024.0):F2} MB";

    private async Task ViewNdaHistory(Guid ndaId)
    {
        var parameters = new DialogParameters<NdaHistoryDialog> { { x => x.NdaId, ndaId }, { x => x.Http, Http } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        await DialogService.ShowAsync<NdaHistoryDialog>("NDA History", parameters, options);
    }

    private async Task ViewNdaDocument(Guid docId)
    {
        var doc = _customer?.Documents.FirstOrDefault(d => d.Id == docId);
        if (doc != null)
        {
            var url = await GetDocumentPreviewUrl(doc.FileReference);
            if (!string.IsNullOrEmpty(url)) await JSRuntime.InvokeVoidAsync("open", url, "_blank");
        }
    }

    // File upload functionality for docs tab
    private bool _uploadingNda = false;
    private bool _uploadingCustomerDoc = false;
    private int _activeDocTabIndex = 0;
    private DocumentResponse? _selectedPreviewDoc = null;
    private string? _previewUrl = null;
    private bool _loadingPreview = false;
    private bool _ndaDragOver = false;
    private bool _customerDocDragOver = false;
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _ndaFileUpload;
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _customerDocFileUpload;
    private List<FileUploadProgress> _ndaUploadFiles = new();
    private List<FileUploadProgress> _customerDocUploadFiles = new();

    private class FileUploadProgress
    {
        public IBrowserFile BrowserFile { get; set; } = default!;
        public string Name => BrowserFile.Name;
        public long Size => BrowserFile.Size;
        public string MimeType => BrowserFile.ContentType;
        public int Progress { get; set; }
        public bool IsUploading { get; set; }
        public bool IsCompleted { get; set; }
        public string? Error { get; set; }
    }


    private async Task OnNdaFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        if (!files.Any()) return;
        const int maxFiles = 20;
        var accepted = files.Take(maxFiles).ToList();
        if (files.Count > maxFiles)
        {
            Snackbar.Add($"Only the first {maxFiles} files will be uploaded ({files.Count} selected).", Severity.Warning);
        }
        _ndaUploadFiles = accepted.Select(f => new FileUploadProgress { BrowserFile = f }).ToList();
        StateHasChanged();
        await UploadFilesAsync(accepted, "NDA", isNda: true);
    }

    private async Task OnCustomerDocFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        if (!files.Any()) return;
        const int maxFiles = 20;
        var accepted = files.Take(maxFiles).ToList();
        if (files.Count > maxFiles)
        {
            Snackbar.Add($"Only the first {maxFiles} files will be uploaded ({files.Count} selected).", Severity.Warning);
        }
        _customerDocUploadFiles = accepted.Select(f => new FileUploadProgress { BrowserFile = f }).ToList();
        StateHasChanged();
        await UploadFilesAsync(accepted, "General", isNda: false);
    }

    private async Task RevokeNda(NDAResponse nda)
    {
        var parameters = new DialogParameters<RevokeNdaDialog>();
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<RevokeNdaDialog>("Revoke NDA", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is string reason)
        {
            try
            {
                var request = new UpdateNDAStatusRequest
                {
                    Status = "Revoked",
                    RevokedAt = DateTime.UtcNow,
                    RevokeReason = reason,
                    Version = nda.Version
                };

                var response = await Http.PatchAsJsonAsync($"api/customer/v1/ndas/{nda.Id}/status", request);
                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("NDA revoked successfully", Severity.Success);
                    await LoadCustomerData();
                }
                else
                {
                    var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    Snackbar.Add($"Failed to revoke NDA: {error?.Message ?? "Unknown error"}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error revoking NDA: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task UploadFilesAsync(IReadOnlyList<IBrowserFile> files, string category, bool isNda)
    {
        if (isNda) _uploadingNda = true;
        else _uploadingCustomerDoc = true;

        var progressList = isNda ? _ndaUploadFiles : _customerDocUploadFiles;
        
        try
        {
            var uploadTasks = progressList.Select(fileProgress => UploadSingleFileAsync(fileProgress, category, isNda)).ToList();
            await Task.WhenAll(uploadTasks);

            var completedCount = progressList.Count(f => f.IsCompleted);
            if (completedCount > 0)
            {
                Snackbar.Add(isNda
                    ? $"NDA Draft created for {completedCount} file(s)"
                    : $"Successfully uploaded {completedCount} file(s)", Severity.Success);
                await LoadCustomerData();
                await LoadInitialActivities();
            }
        }
        finally
        {
            if (isNda) _uploadingNda = false;
            else _uploadingCustomerDoc = false;

            // Clear completed files from list after a short delay
            _ = Task.Delay(2000).ContinueWith(_ =>
            {
                InvokeAsync(() =>
                {
                    progressList.RemoveAll(f => f.IsCompleted);
                    StateHasChanged();
                });
            });
        }
    }

    private async Task UploadSingleFileAsync(FileUploadProgress fileProgress, string category, bool isNda)
    {
        fileProgress.IsUploading = true;
        fileProgress.Progress = 10;
        await InvokeAsync(StateHasChanged);

        try
        {
            using var content = new MultipartFormDataContent();
            var stream = fileProgress.BrowserFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fileProgress.BrowserFile.ContentType);
            content.Add(fileContent, "files", fileProgress.Name);

            fileProgress.Progress = 30;
            await InvokeAsync(StateHasChanged);

            var url = $"api/aiprocessing/upload-documents?category={Uri.EscapeDataString(category)}";
            var response = await Http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                fileProgress.Progress = 60;
                await InvokeAsync(StateHasChanged);

                var results = await response.Content.ReadFromJsonAsync<List<BffUploadResponse>>();
                var result = results?.FirstOrDefault();

                if (result != null)
                {
                    var document = new CreateDocumentRequest
                    {
                        DocumentCategory = category,
                        FileReference = result.UploadId,
                        FileName = result.FileName,
                        FileSize = result.FileSize,
                        MimeType = GetMimeTypeFromFileName(result.FileName)
                    };

                    fileProgress.Progress = 80;
                    await InvokeAsync(StateHasChanged);

                    var linkUrl = $"/api/aiprocessing/link-documents?ownerType=Customer&ownerId={Id}";
                    var linkResponse = await Http.PostAsJsonAsync(linkUrl, new List<CreateDocumentRequest> { document });

                    if (linkResponse.IsSuccessStatusCode)
                    {
                        if (isNda)
                        {
                            // For NDA, we need to create the NDA record after linking
                            // We can't easily wait for LoadCustomerData here in parallel, 
                            // so we'll do it once in the main loop or handle it specifically.
                            // But we need the Document ID from the linked document.
                            // The link endpoint should return the created documents with IDs.
                            
                            var linkedDocs = await linkResponse.Content.ReadFromJsonAsync<List<DocumentResponse>>();
                            var createdDoc = linkedDocs?.FirstOrDefault();
                            
                            if (createdDoc != null)
                            {
                                await Http.PostAsJsonAsync("api/customers/ndas", new {
                                    customerId = Id,
                                    status = "Draft",
                                    documentReferenceId = createdDoc.Id
                                });
                            }
                        }

                        fileProgress.IsCompleted = true;
                        fileProgress.Progress = 100;
                    }
                    else
                    {
                        fileProgress.Error = "Linking failed";
                    }
                }
                else
                {
                    fileProgress.Error = "No upload result returned";
                }
            }
            else
            {
                fileProgress.Error = $"Upload failed: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            fileProgress.Error = ex.Message;
        }
        finally
        {
            fileProgress.IsUploading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string GetMimeTypeFromFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    private async Task PreviewDocument(DocumentResponse doc)
    {
        var parameters = new DialogParameters<DocumentViewer>
        {
            { x => x.Documents, new List<DocumentResponse> { doc } },
            { x => x.OnDownload, EventCallback.Factory.Create<string>(this, DownloadDocument) },
            { x => x.OnDelete, EventCallback.Factory.Create<DocumentResponse>(this, HandleDeleteDocument) },
            { x => x.GetPreviewUrlFunc, new Func<string, Task<string?>>(GetDocumentPreviewUrl) }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };
        await DialogService.ShowAsync<DocumentViewer>("Document Preview", parameters, options);
    }

    private async Task SelectDocumentForPreview(DocumentResponse doc)
    {
        _selectedPreviewDoc = doc;
        _loadingPreview = true;
        _previewUrl = null;

        try
        {
            _previewUrl = await GetDocumentPreviewUrl(doc.FileReference);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load preview: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loadingPreview = false;
        }
    }

    // NDA drag and drop handlers
    private void HandleNdaDragOver() => _ndaDragOver = true;
    private void HandleNdaDragLeave() => _ndaDragOver = false;
    private void HandleNdaDrop()
    {
        _ndaDragOver = false;
        // The MudFileUpload component will handle the actual file drop
    }

    // Customer Documents drag and drop handlers
    private void HandleCustomerDocDragOver() => _customerDocDragOver = true;
    private void HandleCustomerDocDragLeave() => _customerDocDragOver = false;
    private void HandleCustomerDocDrop()
    {
        _customerDocDragOver = false;
        // The MudFileUpload component will handle the actual file drop
    }

    private Color GetDocumentIconColor(string mimeType) => mimeType.Contains("pdf") ? Color.Error :
        mimeType.Contains("image") ? Color.Success :
        mimeType.Contains("word") || mimeType.Contains("doc") ? Color.Info :
        Color.Default;

    /// <summary>Unsubscribes from SignalR customer change events.</summary>
    public async ValueTask DisposeAsync()
    {
        SignalRService.OnCustomerChanged -= HandleCustomerChanged;
    }
}
