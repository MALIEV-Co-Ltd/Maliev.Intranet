using System.Net.Http.Json;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Helpers;
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

/// <summary>Displays detailed information about a project, including parts, pricing, and customer details.</summary>
public partial class ProjectDetail : ComponentBase
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
    
    
    /// <summary>The project identifier from the route.</summary>
    [Parameter]
    public Guid Id { get; set; }

    private ProjectDetailDto? _project;
    private CustomerDetailDto? _customer;
    private bool _loading = true;
    private bool _actioning = false;
    private string? _notes;
    private List<ProjectNoteDto>? _projectNotes;

    private bool AllPartsConfirmed =>
        _project?.Parts.Count > 0 && _project.Parts.All(p => p.Status == "Confirmed");

    private decimal TotalConfirmedPrice =>
        _project?.Parts.Sum(p => (p.ConfirmedPrice ?? p.EstimatedPrice ?? 0) * p.Quantity) ?? 0;

    /// <summary>Returns true when the project is in a state where parts cannot be deleted.</summary>
    private bool IsProjectLocked() =>
        _project?.Status is "Quoted" or "In Production" or "Completed";

    /// <summary>Loads the project detail on render.</summary>
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _project = await Http.GetFromJsonAsync<ProjectDetailDto>($"api/projects/{Id}");
            if (_project != null)
            {
                _projectNotes = _project.Notes;
                await LoadCustomerAsync(_project.CustomerId);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load project: {ex.Message}", Severity.Error);
        }
        finally { _loading = false; }
    }

    private async Task LoadCustomerAsync(Guid customerId)
    {
        try
        {
            _customer = await Http.GetFromJsonAsync<CustomerDetailDto>($"api/customers/{customerId}");
        }
        catch
        {
            _customer = null;
        }
    }

    private async Task SavePartConfigAsync(ProjectPartDto part)
    {
        var request = new UpdateProjectPartRequest
        {
            ProcessType = part.ProcessType,
            MaterialId  = part.MaterialId,
            Quantity    = part.Quantity,
            Finish      = part.Finish,
            Color       = part.Color,
            Tolerance   = part.Tolerance
        };
        var response = await Http.PutAsJsonAsync($"api/projects/{Id}/parts/{part.Id}", request);
        Snackbar.Add(response.IsSuccessStatusCode ? "Part configuration saved." : "Failed to save configuration.", response.IsSuccessStatusCode ? Severity.Success : Severity.Error);
    }

    private async Task GetAiPriceAsync(ProjectPartDto part)
    {
        var response = await Http.PostAsync($"api/projects/{Id}/parts/{part.Id}/price", null);
        if (response.IsSuccessStatusCode)
        {
            var breakdown = await response.Content.ReadFromJsonAsync<ProjectPriceBreakdownDto>();
            if (breakdown != null)
            {
                part.PriceBreakdown  = breakdown;
                part.EstimatedPrice  = breakdown.TotalPerUnit;
                part.Status          = "Priced";
                Snackbar.Add($"AI price estimate: {CurrencyFormatter.Format(breakdown.TotalPerUnit, "THB", 2)}/unit", Severity.Info);
            }
        }
        else
        {
            Snackbar.Add("AI pricing request failed. Is the part fully configured?", Severity.Warning);
        }
        StateHasChanged();
    }

    private async Task ConfirmPriceAsync(ProjectPartDto part, decimal price)
    {
        var request = new ConfirmPartPriceRequest { ConfirmedPrice = price };
        var response = await Http.PostAsJsonAsync($"api/projects/{Id}/parts/{part.Id}/confirm-price", request);
        if (response.IsSuccessStatusCode)
        {
            part.ConfirmedPrice = price;
            part.Status         = "Confirmed";
            _project!.TotalPrice = TotalConfirmedPrice;
            Snackbar.Add("Price confirmed.", Severity.Success);
        }
        else
        {
            Snackbar.Add("Failed to confirm price.", Severity.Error);
        }
        StateHasChanged();
    }

    private async Task DeletePartAsync(Guid partId)
    {
        var response = await Http.DeleteAsync($"api/projects/{Id}/parts/{partId}");
        if (response.IsSuccessStatusCode)
        {
            _project!.Parts.RemoveAll(p => p.Id == partId);
            Snackbar.Add("Part removed.", Severity.Success);
        }
        else Snackbar.Add("Failed to remove part.", Severity.Error);
        StateHasChanged();
    }

    private async Task GenerateQuotationAsync()
    {
        _actioning = true;
        var response = await Http.PostAsync($"api/projects/{Id}/generate-quotation", null);
        if (response.IsSuccessStatusCode)
        {
            Snackbar.Add("Quotation generated successfully.", Severity.Success);
            await LoadAsync();
        }
        else Snackbar.Add("Failed to generate quotation. Check that all parts have confirmed prices.", Severity.Error);
        _actioning = false;
    }

    private async Task AcceptQuotationAsync()
    {
        _actioning = true;
        var response = await Http.PostAsync($"api/projects/{Id}/accept-quotation", null);
        if (response.IsSuccessStatusCode)
        {
            Snackbar.Add("Quotation accepted — orders and jobs are being created.", Severity.Success);
            await LoadAsync();
        }
        else Snackbar.Add("Failed to accept quotation.", Severity.Error);
        _actioning = false;
    }

    private async Task SaveNotesAsync()
    {
        var response = await Http.PutAsJsonAsync($"api/projects/{Id}", new { notes = _notes });
        Snackbar.Add(response.IsSuccessStatusCode ? "Notes saved." : "Failed to save notes.", response.IsSuccessStatusCode ? Severity.Success : Severity.Error);
    }

    private static Color GetStatusColor(string status) => status switch
    {
        "Draft"         => Color.Default,
        "Configuring"   => Color.Warning,
        "Priced"        => Color.Info,
        "Quoted"        => Color.Primary,
        "In Production" => Color.Default,
        "Completed"     => Color.Success,
        "Cancelled"     => Color.Error,
        _               => Color.Default
    };

    private static Color GetPartStatusColor(string status) => status switch
    {
        "Configuring" => Color.Warning,
        "Priced"      => Color.Info,
        "Confirmed"   => Color.Success,
        _             => Color.Default
    };

}
