# BillingIdentitySelector Component

A reusable Blazor component for selecting billing identity (Personal vs Corporate) when creating invoices, quotations, or receipts.

## Features

- **Automatic Detection**: Detects available identities (Thai National ID and/or Company)
- **Smart UI**: Shows appropriate UI based on available identities:
  - Both identities → Radio buttons for selection
  - Only one identity → Auto-selects and shows info alert
  - No identities → Warning message
- **Two-way Binding**: Supports `@bind-SelectedIdentityType` for easy integration

## Usage Example

### In Invoice/Quotation/Receipt Creation Form:

```razor
@page "/sales/invoices/create"
@using Maliev.Intranet.Shared
@using Maliev.Intranet.Client.Components

<MudForm @ref="_form">
    <!-- Customer Selection -->
    <MudAutocomplete T="CustomerDto"
                     @bind-Value="_selectedCustomer"
                     Label="Customer"
                     SearchFunc="SearchCustomers"
                     ToStringFunc="c => c?.Name ?? string.Empty"
                     ValueChanged="OnCustomerChanged" />

    @if (_selectedCustomer != null && _customerIdentity != null)
    {
        <!-- Billing Identity Selector -->
        <BillingIdentitySelector Customer="_customerIdentity"
                                 @bind-SelectedIdentityType="_billingIdentityType" />

        <!-- Rest of the invoice/quotation form -->
        <MudTextField @bind-Value="_invoiceNumber" Label="Invoice Number" />
        <MudDatePicker @bind-Date="_issueDate" Label="Issue Date" />
        <!-- ... more fields ... -->
    }
</MudForm>

@code {
    private CustomerDto? _selectedCustomer;
    private CustomerIdentityDto? _customerIdentity;
    private BillingIdentityType _billingIdentityType = BillingIdentityType.Corporate;

    private async Task OnCustomerChanged(CustomerDto? customer)
    {
        if (customer == null)
        {
            _customerIdentity = null;
            return;
        }

        // Fetch full customer identity info from CustomerService API
        _customerIdentity = await Http.GetFromJsonAsync<CustomerIdentityDto>(
            $"api/customers/{customer.Id}/identity");
    }

    private async Task CreateInvoice()
    {
        var request = new CreateInvoiceRequest
        {
            CustomerId = _selectedCustomer.Id,
            BillingIdentityType = _billingIdentityType, // Pass selected identity
            IssueDate = _issueDate.Value,
            // ... other fields
        };

        await Http.PostAsJsonAsync("api/invoices", request);
    }
}
```

## Component Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Customer` | `CustomerIdentityDto?` | Customer information including identity details |
| `SelectedIdentityType` | `BillingIdentityType` | Currently selected billing identity (supports two-way binding) |
| `SelectedIdentityTypeChanged` | `EventCallback<BillingIdentityType>` | Event fired when selection changes |

## Backend Integration

The selected `BillingIdentityType` should be included in document creation requests:

```csharp
public class CreateInvoiceRequest
{
    public Guid CustomerId { get; set; }
    public BillingIdentityType BillingIdentityType { get; set; } = BillingIdentityType.Corporate;
    // ... other fields
}
```

The backend services (InvoiceService, QuotationService, etc.) will validate the selection and use the appropriate identity for the document.

## Validation

The component automatically handles validation:
- ✅ Shows warning if customer has no identities
- ✅ Auto-selects if only one identity is available
- ✅ Allows choice if both identities are available

Backend services also validate that:
- Personal identity requires Thai National ID
- Corporate identity requires linked company
