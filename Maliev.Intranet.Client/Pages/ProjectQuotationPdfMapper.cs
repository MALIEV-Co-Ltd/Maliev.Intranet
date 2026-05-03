using System.Globalization;
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
        IReadOnlyList<ProcessDto> processes,
        string? quotationTerms = null,
        decimal shippingCost = 0m,
        decimal manualDiscountAmount = 0m,
        decimal currencyExchangeRate = 1m)
    {
        var exchangeRate = currencyExchangeRate <= 0m ? 1m : currencyExchangeRate;
        var itemSubtotal = parts.Sum(part => ConvertCurrency(part.EstimatedTotalAmount ?? 0m, exchangeRate));
        var discount = Math.Min(Math.Max(0m, manualDiscountAmount), itemSubtotal);
        var normalizedShippingCost = Math.Max(0m, shippingCost);
        var subtotal = itemSubtotal - discount + normalizedShippingCost;
        var taxAmount = Math.Round(subtotal * ThailandVatRate, 2, MidpointRounding.AwayFromZero);
        var customerType = string.IsNullOrWhiteSpace(selectedCustomer?.CompanyName) && string.IsNullOrWhiteSpace(customerDetail?.CompanyName)
            ? "Individual"
            : "Corporate";
        var billingAddress = ResolveBillingAddress(customerDetail);
        var shippingAddress = ResolveShippingAddress(customerDetail) ?? billingAddress;
        var billingAddressLines = FormatAddressLines(billingAddress);
        var shippingAddressLines = FormatAddressLines(shippingAddress);
        var isThaiCustomer = ContainsThai(ResolveCustomerName(selectedCustomer, customerDetail))
            || billingAddressLines.Any(ContainsThai);
        var customerBranch = ResolveCustomerBranch(customerType, isThaiCustomer);
        var customerPhone = ResolveCustomerPhone(selectedCustomer, customerDetail, customerType);
        var customerEmail = ResolveCustomerEmail(selectedCustomer, customerDetail, customerType);

        return new QuotationPdfData
        {
            QuotationNumber = TruncateQuotationNumber($"DRAFT-{projectId:N}"),
            CustomerName = ResolveCustomerName(selectedCustomer, customerDetail),
            CustomerType = customerType,
            CustomerBranch = customerBranch,
            CustomerTaxId = customerDetail?.CompanyVatNumber ?? customerDetail?.CompanyRegistrationNumber,
            CustomerPhone = customerPhone,
            CustomerDisplayLines = BuildCustomerDisplayLines(selectedCustomer, customerDetail, customerType, customerBranch, customerPhone, customerEmail),
            CustomerAddress = FormatAddressText(billingAddressLines),
            BillingAddress = FormatAddressText(billingAddressLines),
            BillingAddressLines = billingAddressLines,
            ShippingAddress = FormatAddressText(shippingAddressLines),
            ShippingAddressLines = shippingAddressLines,
            ContactPerson = customerDetail?.Name ?? selectedCustomer?.Name,
            QuotationDate = nowUtc,
            ValidityStart = nowUtc,
            ValidityEnd = nowUtc.AddDays(30),
            Currency = string.IsNullOrWhiteSpace(currency) ? "THB" : currency,
            SubtotalBeforeDiscount = itemSubtotal,
            TotalDiscount = discount,
            ManualDiscountAmount = discount,
            ShippingCost = normalizedShippingCost,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = subtotal + taxAmount,
            DeliveryExpectations = deliveryExpectations,
            SpecialTerms = string.IsNullOrWhiteSpace(quotationTerms) ? null : quotationTerms.Trim(),
            Items = parts.Select((part, index) => new QuotationPdfItem
            {
                Index = index + 1,
                PartName = part.Name,
                MaterialName = ResolveMaterialName(part),
                ManufacturingProcess = ResolveProcessName(part, processes),
                DetailLines = BuildLineItemDetailLines(part),
                Quantity = part.Quantity,
                QuantityUnit = "pcs",
                UnitPrice = ConvertCurrency(part.EstimatedUnitPrice ?? 0m, exchangeRate),
                LineTotal = ConvertCurrency(part.EstimatedTotalAmount ?? 0m, exchangeRate),
                Notes = BuildLineItemNotes(part),
                ThumbnailUrl = FirstNonEmpty(part.ThumbnailSmallUrl, part.ThumbnailLargeUrl),
            }).ToList(),
        };
    }

    /// <summary>
    /// Builds human-readable line item notes for the quotation PDF.
    /// </summary>
    public static string? BuildLineItemNotes(PartViewModel part)
    {
        var notes = BuildLineItemDetailLines(part);

        return notes.Count == 0 ? null : string.Join(Environment.NewLine, notes);
    }

    /// <summary>
    /// Builds human-readable line item detail rows for the quotation PDF.
    /// </summary>
    public static List<string> BuildLineItemDetailLines(PartViewModel part)
    {
        var notes = new List<string>();

        if (FormatBoundingBox(part.Dimensions) is { Length: > 0 } boundingBox)
            notes.Add($"Bounding box: {boundingBox}");

        if (ResolveFinishName(part) is { Length: > 0 } finishName)
            notes.Add($"Surface finish: {finishName}");

        if (ResolveToleranceName(part) is { Length: > 0 } toleranceName)
            notes.Add($"Tolerance: {toleranceName}");

        if (ResolveRoughnessName(part) is { Length: > 0 } roughness)
            notes.Add($"Surface roughness: {roughness}");

        if (ResolveColor(part) is { Length: > 0 } color)
            notes.Add($"Color: {color}");

        if (part.HasThreadedHoles)
        {
            if (part.ThreadedHoleCount > 0 && !string.IsNullOrWhiteSpace(part.ThreadedHoleSpec))
            {
                notes.Add($"Tapped holes: {part.ThreadedHoleCount:N0} x {part.ThreadedHoleSpec}");
            }
            else
            {
                notes.Add(part.DrawingFiles.Count > 0 ? "Tapped holes: Yes (see attached drawings)" : "Tapped holes: Yes");
            }
        }

        if (part.HasInserts && part.InsertType != InsertType.None)
        {
            var count = part.InsertCount > 0 ? part.InsertCount.ToString("N0") : "TBD";
            notes.Add($"Inserts: {count} x {part.InsertType}");
        }

        if (ResolveDeburring(part) is { Length: > 0 } deburring)
            notes.Add(deburring);

        notes.Add($"Inspection: {part.InspectionLevel}");

        if (part.DrawingFiles.Count > 0)
            notes.Add($"Drawing: {string.Join(", ", part.DrawingFiles.Select(file => file.Name).Where(name => !string.IsNullOrWhiteSpace(name)))}");

        if (!string.IsNullOrWhiteSpace(part.PartNotes))
            notes.Add(part.PartNotes);

        return notes;
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

    private static decimal ConvertCurrency(decimal amount, decimal exchangeRate) =>
        Math.Round(amount * exchangeRate, 2, MidpointRounding.AwayFromZero);

    private static string ResolveCustomerName(CustomerSummaryDto? selectedCustomer, CustomerDetailDto? customerDetail)
    {
        if (!string.IsNullOrWhiteSpace(customerDetail?.CompanyName))
            return customerDetail.CompanyName;

        if (!string.IsNullOrWhiteSpace(selectedCustomer?.CompanyName))
            return selectedCustomer.CompanyName;

        return customerDetail?.Name ?? selectedCustomer?.Name ?? "N/A";
    }

    private static string? ResolveCustomerBranch(string customerType, bool isThaiCustomer)
    {
        if (customerType != "Corporate")
            return null;

        return isThaiCustomer ? "สำนักงานใหญ่" : "Head Office";
    }

    private static List<string> BuildCustomerDisplayLines(
        CustomerSummaryDto? selectedCustomer,
        CustomerDetailDto? customerDetail,
        string customerType,
        string? customerBranch,
        string? customerPhone,
        string? customerEmail)
    {
        var lines = new List<string>();
        var contactName = FirstNonEmpty(customerDetail?.Name, selectedCustomer?.Name);
        var customerName = ResolveCustomerName(selectedCustomer, customerDetail);

        if (customerType == "Corporate")
        {
            var branch = string.IsNullOrWhiteSpace(customerBranch) ? string.Empty : $" ({customerBranch})";
            lines.Add($"{customerName}{branch}");

            if (!string.IsNullOrWhiteSpace(contactName))
            {
                var contactLine = string.IsNullOrWhiteSpace(customerPhone)
                    ? contactName
                    : $"{contactName} ({customerPhone})";
                lines.Add($"Attn: {contactLine}");
            }

            if (!string.IsNullOrWhiteSpace(customerEmail))
                lines.Add($"Email: {customerEmail}");
        }
        else
        {
            lines.Add(customerName);
            if (!string.IsNullOrWhiteSpace(customerPhone))
                lines.Add(customerPhone);
            if (!string.IsNullOrWhiteSpace(customerEmail))
                lines.Add($"Email: {customerEmail}");
        }

        return lines.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
    }

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

    private static string? ResolveCustomerEmail(CustomerSummaryDto? selectedCustomer, CustomerDetailDto? customerDetail, string customerType)
    {
        if (customerType == "Corporate")
        {
            return FirstNonEmpty(
                customerDetail?.CompanyContactEmail,
                customerDetail?.Email,
                selectedCustomer?.Email);
        }

        return FirstNonEmpty(
            customerDetail?.Email,
            selectedCustomer?.Email,
            customerDetail?.CompanyContactEmail);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string ResolveMaterialName(PartViewModel part)
    {
        var materialName = part.MaterialId.HasValue
            ? part.AvailableMaterials.FirstOrDefault(material => material.Id == part.MaterialId.Value)?.Name
            : null;

        return string.IsNullOrWhiteSpace(materialName)
            ? part.MaterialCode ?? string.Empty
            : materialName;
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

        return finish?.Name ?? FormatKnownFinishCode(part.FinishCode);
    }

    private static string? ResolveToleranceName(PartViewModel part)
    {
        var tolerance = part.ToleranceId.HasValue
            ? part.AvailableTolerances.FirstOrDefault(item => item.Id == part.ToleranceId.Value)
            : null;

        if (tolerance == null)
            return FormatKnownToleranceCode(part.ToleranceCode, IsFdmProcess(part));

        if (IsFdmProcess(part) && !string.IsNullOrWhiteSpace(tolerance.ToleranceRange))
            return $"{tolerance.Name} {NormalizeToleranceRange(tolerance.ToleranceRange)}";

        var name = StripToleranceRange(tolerance.Name);
        if (!string.IsNullOrWhiteSpace(name) && HasIsoReference(name))
            return name;

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(tolerance.IsoStandard)
            && !name.Contains(tolerance.IsoStandard, StringComparison.OrdinalIgnoreCase)
            && !name.Contains(tolerance.IsoStandard.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase))
        {
            return $"{name} ({NormalizeIsoStandard(tolerance.IsoStandard)})";
        }

        return name;
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

    private static string? ResolveRoughnessName(PartViewModel part)
    {
        if (string.IsNullOrWhiteSpace(part.RoughnessCode))
            return null;

        return part.RoughnessCode.ToUpperInvariant() switch
        {
            "RA_3_2" => "Ra 3.2 um",
            "RA_1_6" => "Ra 1.6 um",
            "RA_0_8" => "Ra 0.8 um",
            "RA_0_4" => "Ra 0.4 um",
            _ => part.RoughnessCode.Replace("_", " ", StringComparison.Ordinal),
        };
    }

    private static string? FormatBoundingBox(FileAnalysisDimensionsDto? dimensions)
    {
        if (dimensions is not { X: > 0, Y: > 0, Z: > 0 })
            return null;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatDimension(dimensions.X)} x {FormatDimension(dimensions.Y)} x {FormatDimension(dimensions.Z)} mm");
    }

    private static string FormatDimension(double value) =>
        value.ToString(value >= 10 ? "0.#" : "0.##", CultureInfo.InvariantCulture);

    private static string? FormatKnownFinishCode(string? finishCode)
    {
        if (string.IsNullOrWhiteSpace(finishCode))
            return null;

        var normalized = NormalizeCode(finishCode);
        return normalized switch
        {
            "AS_PRNTED" or "AS_PRINTED" or "ASPRINTED" => "As-printed",
            "AS_MACHINED" or "ASMACHINED" => "As-machined",
            _ => FormatOptionName(finishCode),
        };
    }

    private static string? FormatKnownToleranceCode(string? toleranceCode, bool isFdmProcess)
    {
        if (string.IsNullOrWhiteSpace(toleranceCode))
            return null;

        var normalized = NormalizeCode(toleranceCode);
        if (isFdmProcess)
        {
            return normalized switch
            {
                "FDM_STD" or "FDM_STANDARD" or "FDMSTANDARD" => "FDM Standard +-0.3mm",
                "FDM_FINE" or "FDMFINE" => "FDM Fine +-0.15mm",
                _ => FormatOptionName(toleranceCode),
            };
        }

        return normalized switch
        {
            "ISO2768_M" or "ISO_2768_M" or "MEDIUM" => "Medium (ISO2768-m)",
            "ISO2768_F" or "ISO_2768_F" or "FINE" => "Fine (ISO2768-f)",
            _ => FormatOptionName(toleranceCode),
        };
    }

    private static string StripToleranceRange(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cutIndex = value.IndexOfAny(['±', '+']);
        if (cutIndex > 0)
            value = value[..cutIndex];

        return value.Trim();
    }

    private static string NormalizeToleranceRange(string value) =>
        value.Replace("±", "+-", StringComparison.Ordinal)
            .Replace("+/-", "+-", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();

    private static string NormalizeIsoStandard(string value) =>
        value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();

    private static bool HasIsoReference(string value)
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal);
        return normalized.Contains("ISO2768", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFdmProcess(PartViewModel part) =>
        part.ProcessCode?.Contains("FDM", StringComparison.OrdinalIgnoreCase) == true;

    private static string NormalizeCode(string value) =>
        value.Trim().ToUpperInvariant();

    private static string FormatOptionName(string value) =>
        string.Join(
            " ",
            value.Replace("_", " ", StringComparison.Ordinal)
                .Replace("-", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant() switch
            {
                var text when string.IsNullOrWhiteSpace(text) => value,
                var text => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text),
            };

    private static string? ResolveDeburring(PartViewModel part)
    {
        var value = part.ProcessOptionValues
            .FirstOrDefault(pair => pair.Key.Contains("deburr", StringComparison.OrdinalIgnoreCase))
            .Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("included", StringComparison.OrdinalIgnoreCase)
            ? "Deburring"
            : $"Deburring: {value}";
    }

    private static AddressResponse? ResolveBillingAddress(CustomerDetailDto? customer)
    {
        return customer?.CompanyBillingAddress
            ?? customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Billing"))
            ?? customer?.Addresses.FirstOrDefault(address => address.IsDefault);
    }

    private static AddressResponse? ResolveShippingAddress(CustomerDetailDto? customer)
    {
        return customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Shipping") && address.IsDefault)
            ?? customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Shipping"));
    }

    private static bool IsAddressType(AddressResponse address, string type) =>
        address.Type.Contains(type, StringComparison.OrdinalIgnoreCase);

    private static List<string> FormatAddressLines(AddressResponse? address)
    {
        if (address == null)
            return [];

        var isThai = ContainsThai(address.AddressLine1)
            || ContainsThai(address.AddressLine2)
            || ContainsThai(address.AddressLine3)
            || ContainsThai(address.District)
            || ContainsThai(address.City)
            || ContainsThai(address.StateProvince);

        if (isThai)
            return FormatThaiAddressLines(address);

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

        return parts.ToList()!;
    }

    private static List<string> FormatThaiAddressLines(AddressResponse address)
    {
        var lines = new List<string>();
        AddIfPresent(lines, address.AddressLine1);
        AddIfPresent(lines, address.AddressLine2);
        AddIfPresent(lines, address.AddressLine3);
        AddThaiAdministrativeLine(lines, address.District, "ตำบล");
        AddThaiAdministrativeLine(lines, address.City, "อำเภอ");

        var provinceLine = WithThaiPrefix(address.StateProvince, "จังหวัด");
        if (!string.IsNullOrWhiteSpace(provinceLine) && !string.IsNullOrWhiteSpace(address.PostalCode))
            lines.Add($"{provinceLine} {address.PostalCode}");
        else
        {
            AddIfPresent(lines, provinceLine);
            AddIfPresent(lines, address.PostalCode);
        }

        return lines;
    }

    private static string? FormatAddressText(IReadOnlyList<string> lines) =>
        lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);

    private static void AddThaiAdministrativeLine(List<string> lines, string? value, string prefix)
    {
        var line = WithThaiPrefix(value, prefix);
        AddIfPresent(lines, line);
    }

    private static string? WithThaiPrefix(string? value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.StartsWith(prefix, StringComparison.Ordinal) ? trimmed : $"{prefix}{trimmed}";
    }

    private static void AddIfPresent(List<string> lines, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            lines.Add(value.Trim());
    }

    private static bool ContainsThai(string? value) =>
        value?.Any(ch => ch >= '\u0E00' && ch <= '\u0E7F') == true;
}
