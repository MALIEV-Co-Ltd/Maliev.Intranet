using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Constants;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Client.Pages;

/// <summary>
/// Builds customer-facing quotation PDF data from the ProjectNew draft state.
/// </summary>
public static class ProjectQuotationPdfMapper
{
    private const decimal ThailandVatRate = 0.07m;

    /// <summary>
    /// Builds a draft quotation PDF payload with VAT-inclusive totals and display labels.
    /// </summary>
    public static QuotationPdfData BuildDraftPdfData(
        Guid projectId,
        CustomerSummaryDto? selectedCustomer,
        CustomerDetailDto? customerDetail,
        string? currency,
        DateTime nowUtc,
        string deliveryExpectations,
        IReadOnlyList<PartViewModel> parts,
        IReadOnlyList<ProcessDto> processes)
    {
        var subtotal = parts.Sum(part => part.EstimatedTotalAmount ?? 0m);
        var taxAmount = Math.Round(subtotal * ThailandVatRate, 2, MidpointRounding.AwayFromZero);
        var customerType = string.IsNullOrWhiteSpace(selectedCustomer?.CompanyName) && string.IsNullOrWhiteSpace(customerDetail?.CompanyName)
            ? "Individual"
            : "Corporate";
        var billingAddress = ResolveBillingAddress(customerDetail);
        var shippingAddress = ResolveShippingAddress(customerDetail);

        return new QuotationPdfData
        {
            QuotationNumber = TruncateQuotationNumber($"DRAFT-{projectId:N}"),
            CustomerName = ResolveCustomerName(selectedCustomer, customerDetail),
            CustomerType = customerType,
            CustomerBranch = ResolveCustomerBranch(customerType),
            CustomerTaxId = customerDetail?.CompanyVatNumber ?? customerDetail?.CompanyRegistrationNumber,
            CustomerPhone = ResolveCustomerPhone(selectedCustomer, customerDetail, customerType),
            CustomerAddress = billingAddress,
            BillingAddress = billingAddress,
            ShippingAddress = shippingAddress,
            ContactPerson = customerDetail?.Name ?? selectedCustomer?.Name,
            QuotationDate = nowUtc,
            ValidityStart = nowUtc,
            ValidityEnd = nowUtc.AddDays(30),
            Currency = string.IsNullOrWhiteSpace(currency) ? "THB" : currency,
            SubtotalBeforeDiscount = subtotal,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = subtotal + taxAmount,
            DeliveryExpectations = deliveryExpectations,
            SpecialTerms = "Prices are indicative until project review is completed and all files are confirmed manufacturable.",
            Items = parts.Select((part, index) => new QuotationPdfItem
            {
                Index = index + 1,
                MaterialName = ResolveMaterialName(part),
                ManufacturingProcess = ResolveProcessName(part, processes),
                Quantity = part.Quantity,
                QuantityUnit = "pcs",
                UnitPrice = part.EstimatedUnitPrice ?? 0m,
                LineTotal = part.EstimatedTotalAmount ?? 0m,
                Notes = BuildLineItemNotes(part),
            }).ToList(),
        };
    }

    /// <summary>
    /// Builds human-readable line item notes for the quotation PDF.
    /// </summary>
    public static string? BuildLineItemNotes(PartViewModel part)
    {
        var notes = new List<string>();

        if (ResolveFinishName(part) is { Length: > 0 } finishName)
            notes.Add($"Finish: {finishName}");

        if (ResolveToleranceName(part) is { Length: > 0 } toleranceName)
            notes.Add($"Tolerance: {toleranceName}");

        if (ResolveColor(part) is { Length: > 0 } color)
            notes.Add($"Color: {color}");

        if (part.HasThreadedHoles)
        {
            var specification = string.IsNullOrWhiteSpace(part.ThreadedHoleSpec) ? "specified thread" : part.ThreadedHoleSpec;
            var count = part.ThreadedHoleCount > 0 ? part.ThreadedHoleCount.ToString("N0") : "TBD";
            notes.Add($"Tapped holes: {count} x {specification}");
        }

        if (part.HasInserts && part.InsertType != InsertType.None)
        {
            var count = part.InsertCount > 0 ? part.InsertCount.ToString("N0") : "TBD";
            notes.Add($"Inserts: {count} x {part.InsertType}");
        }

        if (part.InspectionLevel != InspectionLevel.Standard)
            notes.Add($"Inspection: {part.InspectionLevel}");

        if (part.DrawingFiles.Count > 0)
            notes.Add($"Drawing: {string.Join(", ", part.DrawingFiles.Select(file => file.Name).Where(name => !string.IsNullOrWhiteSpace(name)))}");

        if (!string.IsNullOrWhiteSpace(part.PartNotes))
            notes.Add(part.PartNotes);

        return notes.Count == 0 ? null : string.Join(" | ", notes);
    }

    /// <summary>
    /// Builds the PDF delivery expectation from the selected lead-time option shown on ProjectNew.
    /// </summary>
    public static string BuildDeliveryExpectation(LeadTimeOptionDto? selectedLeadTime)
    {
        if (selectedLeadTime is not { MinDays: > 0 })
            return "To be confirmed after project review";

        var days = selectedLeadTime.MaxDays > selectedLeadTime.MinDays
            ? $"{selectedLeadTime.MinDays} - {selectedLeadTime.MaxDays}"
            : selectedLeadTime.MinDays.ToString();

        return $"{selectedLeadTime.Name}: {days} business days after order confirmation";
    }

    private static string TruncateQuotationNumber(string value) => value[..Math.Min(24, value.Length)];

    private static string ResolveCustomerName(CustomerSummaryDto? selectedCustomer, CustomerDetailDto? customerDetail)
    {
        if (!string.IsNullOrWhiteSpace(customerDetail?.CompanyName))
            return customerDetail.CompanyName;

        if (!string.IsNullOrWhiteSpace(selectedCustomer?.CompanyName))
            return selectedCustomer.CompanyName;

        return customerDetail?.Name ?? selectedCustomer?.Name ?? "N/A";
    }

    private static string? ResolveCustomerBranch(string customerType) =>
        customerType == "Corporate" ? "Head Office / สำนักงานใหญ่" : null;

    private static string? ResolveCustomerPhone(CustomerSummaryDto? selectedCustomer, CustomerDetailDto? customerDetail, string customerType)
    {
        if (customerType == "Corporate")
        {
            return FirstNonEmpty(
                customerDetail?.CompanyPhone,
                selectedCustomer?.CompanyPhone,
                customerDetail?.Mobile,
                selectedCustomer?.Mobile,
                customerDetail?.Landline,
                selectedCustomer?.Landline);
        }

        return FirstNonEmpty(
            customerDetail?.Mobile,
            selectedCustomer?.Mobile,
            customerDetail?.Landline,
            selectedCustomer?.Landline);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string ResolveMaterialName(PartViewModel part)
    {
        var materialName = part.MaterialId.HasValue
            ? part.AvailableMaterials.FirstOrDefault(material => material.Id == part.MaterialId.Value)?.Name
            : null;

        return string.IsNullOrWhiteSpace(materialName)
            ? part.Name
            : $"{materialName} - {part.Name}";
    }

    private static string ResolveProcessName(PartViewModel part, IReadOnlyList<ProcessDto> processes)
    {
        var process = part.ProcessId.HasValue
            ? processes.FirstOrDefault(item => item.Id == part.ProcessId.Value)
            : null;

        process ??= processes.FirstOrDefault(item => string.Equals(item.Code, part.ProcessCode, StringComparison.OrdinalIgnoreCase));

        if (process != null)
            return process.Name;

        if (part.ProcessId.HasValue)
        {
            var processName = ManufacturingProcesses.GetName(part.ProcessId.Value);
            if (processName != "Unknown Process")
                return processName;
        }

        return part.ProcessCode ?? string.Empty;
    }

    private static string? ResolveFinishName(PartViewModel part)
    {
        var finish = part.FinishId.HasValue
            ? part.AvailableFinishes.FirstOrDefault(item => item.Id == part.FinishId.Value)
            : null;

        return finish?.Name ?? part.FinishCode;
    }

    private static string? ResolveToleranceName(PartViewModel part)
    {
        var tolerance = part.ToleranceId.HasValue
            ? part.AvailableTolerances.FirstOrDefault(item => item.Id == part.ToleranceId.Value)
            : null;

        if (tolerance == null)
            return part.ToleranceCode;

        return string.IsNullOrWhiteSpace(tolerance.ToleranceRange)
            ? tolerance.Name
            : $"{tolerance.Name} {tolerance.ToleranceRange}";
    }

    private static string? ResolveColor(PartViewModel part)
    {
        foreach (var key in new[] { "paint_color_reference", "paint_color", "paint_colour", "material_color", "material_colour", "plastic_color", "plastic_colour", "anodize_color", "anodise_color" })
        {
            if (part.ProcessOptionValues.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        if (part.ProcessOptionValues.TryGetValue("paint_color_hex", out var hex) && !string.IsNullOrWhiteSpace(hex))
            return hex;

        return null;
    }

    private static string? ResolveBillingAddress(CustomerDetailDto? customer)
    {
        return FormatAddress(customer?.CompanyBillingAddress)
            ?? FormatAddress(customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Billing")))
            ?? FormatAddress(customer?.Addresses.FirstOrDefault(address => address.IsDefault));
    }

    private static string? ResolveShippingAddress(CustomerDetailDto? customer)
    {
        return FormatAddress(customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Shipping") && address.IsDefault))
            ?? FormatAddress(customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Shipping")));
    }

    private static bool IsAddressType(AddressResponse address, string type) =>
        address.Type.Contains(type, StringComparison.OrdinalIgnoreCase);

    private static string? FormatAddress(AddressResponse? address)
    {
        if (address == null)
            return null;

        var parts = new[]
        {
            address.AddressLine1,
            address.AddressLine2,
            address.AddressLine3,
            address.District,
            address.City,
            address.StateProvince,
            address.PostalCode,
        }.Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(", ", parts);
    }
}
