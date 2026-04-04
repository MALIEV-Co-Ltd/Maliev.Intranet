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

/// <summary>Displays detailed information about an invoice, including billing notes and payment actions.</summary>
public partial class InvoiceDetail : ComponentBase
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
    
    
    /// <summary>The invoice identifier from the route.</summary>
    [Parameter]
    public Guid Id { get; set; }

    private bool _isLoading = true;
    private InvoiceDetailDto? _invoice;
    private List<BillingNoteDto> _billingNotes = [];
    private List<BreadcrumbItem> _breadcrumbs = [];

    /// <summary>Loads the invoice details and initializes breadcrumbs.</summary>
    /// <summary>Loads the invoice details and initializes breadcrumbs.</summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadInvoice();

        _breadcrumbs = 
        [
            new BreadcrumbItem("Home", href: "/"),
            new BreadcrumbItem("Finance", href: null, disabled: true),
            new BreadcrumbItem("Invoices", href: "/finance/invoices"),
            new BreadcrumbItem(_invoice?.InvoiceNumber ?? "Detail", href: null, disabled: true)
        ];
    }

    private async Task LoadInvoice()
    {
        try
        {
            _isLoading = true;
            _invoice = await Http.GetFromJsonAsync<InvoiceDetailDto>($"api/invoices/{Id}");
            if (_invoice != null)
            {
                var pagedNotes = await Http.GetFromJsonAsync<PagedResponse<BillingNoteDto>>($"api/billing-notes?invoiceId={Id}");
                _billingNotes = pagedNotes?.Data?.ToList() ?? [];
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading invoice: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task FinalizeInvoice()
    {
        try
        {
            var response = await Http.PostAsync($"api/invoices/{Id}/finalize", null);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Invoice finalized successfully", Severity.Success);
                await LoadInvoice();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task OpenSplitDialog()
    {
        var parameters = new DialogParameters<InvoiceSplitDialog>
        {
            { x => x.InvoiceId, _invoice!.Id },
            { x => x.InvoiceNumber, _invoice.InvoiceNumber ?? "Draft" },
            { x => x.TotalAmount, _invoice.Total }
        };

        var dialog = await DialogService.ShowAsync<InvoiceSplitDialog>("Split Invoice", parameters);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            await LoadInvoice();
        }
    }

    private async Task GeneratePdf()
    {
        if (_invoice == null) return;
        try
        {
            Snackbar.Add($"Generating PDF for invoice {_invoice.InvoiceNumber}...", Severity.Info);
            
            var response = await Http.PostAsync($"api/invoices/{Id}/pdf", null);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("storageUrl", out var urlElement))
                {
                    var pdfUrl = urlElement.GetString();
                    if (!string.IsNullOrEmpty(pdfUrl))
                    {
                        await JSRuntime.InvokeVoidAsync("open", pdfUrl, "_blank");
                        Snackbar.Add("PDF generated successfully", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add("PDF generated but URL is empty", Severity.Warning);
                    }
                }
                else
                {
                    Snackbar.Add("Invalid response from server", Severity.Error);
                }
            }
            else
            {
                Snackbar.Add("Failed to generate PDF", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
        }
    }

    private async Task OpenRecordPaymentDialog()
    {
        if (_invoice == null) return;
        var parameters = new DialogParameters<RecordPaymentDialog>
        {
            { x => x.InvoiceId, _invoice.Id },
            { x => x.InvoiceNumber, _invoice.InvoiceNumber ?? string.Empty },
            { x => x.OutstandingAmount, _invoice.Total - _invoice.Payments.Sum(p => p.Amount) }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<RecordPaymentDialog>("Record Payment", parameters, options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadInvoice(); // reload the invoice to show updated payments
            Snackbar.Add("Payment recorded successfully", Severity.Success);
        }
    }

    private async Task OpenEditDialog()
    {
        if (_invoice == null) return;

        var parameters = new DialogParameters<EditInvoiceDialog>
        {
            { x => x.InvoiceId, Id },
            { x => x.Invoice, _invoice }
        };
        var dialog = await DialogService.ShowAsync<EditInvoiceDialog>("Edit Invoice", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadInvoice();
        }
    }

    private async Task CancelInvoice()
    {
        var parameters = new DialogParameters();
        // Here we'd usually have a small dialog to capture reason
        bool? result = await DialogService.ShowMessageBoxAsync(
            "Cancel Invoice", 
            "Are you sure you want to cancel this invoice? This action cannot be undone.", 
            yesText: "Cancel Invoice", cancelText: "Go Back");

        if (result == true)
        {
            var response = await Http.PostAsJsonAsync($"api/invoices/{Id}/cancel", new { Reason = "User request" });
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Invoice cancelled", Severity.Success);
                await LoadInvoice();
            }
        }
    }

    private Color GetStatusColor(string status) => status switch
    {
        "Draft" => Color.Default,
        "Sent" => Color.Info,
        "Paid" => Color.Success,
        "Overdue" => Color.Error,
        "Cancelled" => Color.Dark,
        "Split" => Color.Default,
        "Partial" => Color.Warning,
        _ => Color.Default
    };

}
