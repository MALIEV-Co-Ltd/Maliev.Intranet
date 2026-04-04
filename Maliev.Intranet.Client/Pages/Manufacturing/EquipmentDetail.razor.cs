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

namespace Maliev.Intranet.Client.Pages.Manufacturing;

/// <summary>Displays detailed information about a piece of equipment, including notes, loans, and maintenance logs.</summary>
/// <summary>Displays detailed information about a piece of equipment, including notes, loans, and maintenance logs.</summary>
public partial class EquipmentDetail : ComponentBase
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
    
    
    /// <summary>The equipment identifier from the route.</summary>
    [Parameter] public Guid Id { get; set; }

    private bool _isLoading = true;
    private EquipmentDetailDto? _equipment;
    private List<EquipmentNoteDto> _notes = [];
    private List<EquipmentLoanDto> _loans = [];
    private List<MaintenanceLogDto> _maintenanceLogs = [];
    private List<EquipmentAttachmentDto> _attachments = [];

    /// <summary>Loads all equipment details including notes, loans, and maintenance logs.</summary>
    /// <summary>Loads all equipment details including notes, loans, and maintenance logs.</summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadAll();
    }

    private async Task LoadAll()
    {
        _isLoading = true;
        try
        {
            _equipment = await Http.GetFromJsonAsync<EquipmentDetailDto>($"api/equipments/{Id}");

            if (_equipment is not null)
            {
                var notesTask = Http.GetFromJsonAsync<List<EquipmentNoteDto>>($"api/equipments/{Id}/notes");
                var loansTask = Http.GetFromJsonAsync<List<EquipmentLoanDto>>($"api/equipments/{Id}/loans");
                var maintenanceTask = Http.GetFromJsonAsync<List<MaintenanceLogDto>>($"api/equipments/{Id}/maintenance");

                await Task.WhenAll(notesTask, loansTask, maintenanceTask);

                _notes = await notesTask ?? [];
                _loans = await loansTask ?? [];
                _maintenanceLogs = await maintenanceTask ?? [];

                if (_equipment.Category == "CncMachine")
                {
                    _attachments = await Http.GetFromJsonAsync<List<EquipmentAttachmentDto>>(
                        $"api/equipments/{Id}/attachments") ?? [];
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading equipment: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OpenChangeStatusDialog()
    {
        if (_equipment is null) return;

        var parameters = new DialogParameters<ChangeEquipmentStatusDialog>
        {
            { x => x.EquipmentId, Id },
            { x => x.CurrentStatus, _equipment.Status }
        };
        var dialog = await DialogService.ShowAsync<ChangeEquipmentStatusDialog>(
            "Change Equipment Status",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadAll();
    }

    private async Task OpenAddNoteDialog()
    {
        var parameters = new DialogParameters<AddEquipmentNoteDialog>
        {
            { x => x.EquipmentId, Id }
        };
        var dialog = await DialogService.ShowAsync<AddEquipmentNoteDialog>(
            "Add Note",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadAll();
    }

    private async Task OpenAddMaintenanceDialog()
    {
        var parameters = new DialogParameters<AddMaintenanceLogDialog>
        {
            { x => x.EquipmentId, Id }
        };
        var dialog = await DialogService.ShowAsync<AddMaintenanceLogDialog>(
            "Log Maintenance",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadAll();
    }

    private async Task OpenCreateLoanDialog()
    {
        var parameters = new DialogParameters<CreateLoanDialog>
        {
            { x => x.EquipmentId, Id }
        };
        var dialog = await DialogService.ShowAsync<CreateLoanDialog>(
            "Create Equipment Loan",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadAll();
    }

    private async Task OpenApproveLoanDialog(EquipmentLoanDto loan)
    {
        var parameters = new DialogParameters<ApproveLoanDialog>
        {
            { x => x.LoanId, loan.Id },
            { x => x.RowVersion, 0u }
        };
        var dialog = await DialogService.ShowAsync<ApproveLoanDialog>(
            "Approve Loan",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadAll();
    }

    private async Task RejectLoan(EquipmentLoanDto loan)
    {
        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "Reject Loan",
            "Are you sure you want to reject this loan request?",
            yesText: "Reject", cancelText: "Cancel");
        if (confirmed != true) return;

        var request = new RejectLoanRequest { RowVersion = 0u };
        var response = await Http.PatchAsJsonAsync($"api/equipments/loans/{loan.Id}/reject", request);
        if (response.IsSuccessStatusCode)
        {
            Snackbar.Add("Loan rejected.", Severity.Success);
            await LoadAll();
        }
        else
        {
            Snackbar.Add("Failed to reject loan.", Severity.Error);
        }
    }

    private async Task ReturnLoan(EquipmentLoanDto loan)
    {
        var request = new ReturnLoanRequest
        {
            ActualReturnDate = DateOnly.FromDateTime(DateTime.Today),
            RowVersion = 0u
        };
        var response = await Http.PatchAsJsonAsync($"api/equipments/loans/{loan.Id}/return", request);
        if (response.IsSuccessStatusCode)
        {
            Snackbar.Add("Equipment return recorded.", Severity.Success);
            await LoadAll();
        }
        else
        {
            Snackbar.Add("Failed to record return.", Severity.Error);
        }
    }

    private async Task OpenAddAttachmentDialog()
    {
        var parameters = new DialogParameters<AddAttachmentDialog>
        {
            { x => x.EquipmentId, Id }
        };
        var dialog = await DialogService.ShowAsync<AddAttachmentDialog>(
            "Add CNC Attachment",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadAll();
    }

    private async Task DeleteEquipment()
    {
        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Equipment",
            $"Are you sure you want to permanently delete '{_equipment?.Name}'? This cannot be undone.",
            yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true) return;

        var response = await Http.DeleteAsync($"api/equipments/{Id}");
        if (response.IsSuccessStatusCode)
        {
            Snackbar.Add("Equipment deleted.", Severity.Success);
            Navigation.NavigateTo("/mfg/equipment");
        }
        else
        {
            var statusCode = (int)response.StatusCode;
            Snackbar.Add(statusCode == 409
                ? "Cannot delete: this equipment has job history."
                : statusCode == 503
                    ? "Cannot delete: Job Service is currently unavailable."
                    : "Failed to delete equipment.",
                Severity.Error);
        }
    }

    private static Color GetStatusColor(string status) => status switch
    {
        "Active" => Color.Success,
        "UnderMaintenance" => Color.Warning,
        "OnLoan" => Color.Info,
        "Lost" => Color.Error,
        "Decommissioned" => Color.Dark,
        _ => Color.Default
    };

    private static Color GetCategoryColor(string category) => category switch
    {
        "FdmPrinter" or "SlaPrinter" or "InjectionMolding" => Color.Default,
        "CncMachine" => Color.Primary,
        "Scanner3D" => Color.Tertiary,
        _ => Color.Default
    };

    private static Color GetLoanStatusColor(string status) => status switch
    {
        "Pending" => Color.Warning,
        "Approved" or "Active" => Color.Success,
        "Rejected" => Color.Error,
        "Returned" => Color.Default,
        "Overdue" => Color.Error,
        _ => Color.Default
    };

    private static Color GetMaintenanceTypeColor(string type) => type switch
    {
        "Preventive" => Color.Info,
        "Corrective" or "Repair" => Color.Warning,
        "Calibration" => Color.Default,
        "Inspection" => Color.Tertiary,
        _ => Color.Default
    };

    private static Color GetWarrantyColor(DateOnly? date)
    {
        if (date is null) return Color.Default;
        return date.Value < DateOnly.FromDateTime(DateTime.Today) ? Color.Error : Color.Success;
    }

    private static Color GetServiceDueColor(DateOnly? date)
    {
        if (date is null) return Color.Default;
        var daysUntil = date.Value.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
        return daysUntil < 0 ? Color.Error : daysUntil <= 14 ? Color.Warning : Color.Default;
    }

    private static string FormatStatus(string status) => status switch
    {
        "UnderMaintenance" => "Under Maintenance",
        "OnLoan" => "On Loan",
        _ => status
    };

    private static string FormatCategory(string category) => category switch
    {
        "FdmPrinter" => "FDM Printer",
        "SlaPrinter" => "SLA/DLP Printer",
        "CncMachine" => "CNC Machine",
        "Scanner3D" => "3D Scanner",
        "InjectionMolding" => "Injection Molding",
        "OfficeEquipment" => "Office Equipment",
        "MeasuringEquipment" => "Measuring Equipment",
        "ITEquipment" => "IT Equipment",
        "HandTool" => "Hand Tool",
        _ => category
    };

    private static string FormatSpecKey(string key)
    {
        // Convert PascalCase/camelCase spec keys to readable labels
        // e.g., "BuildVolumeX" → "Build Volume X", "NozzleDiameterMm" → "Nozzle Diameter (mm)"
        var result = System.Text.RegularExpressions.Regex.Replace(key, "([A-Z])", " $1").Trim();
        result = result.Replace(" Mm", " (mm)").Replace(" C", " (°C)").Replace(" Rpm", " (RPM)");
        return result;
    }

}
