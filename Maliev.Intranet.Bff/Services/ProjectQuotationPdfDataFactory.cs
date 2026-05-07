using System.Globalization;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Services;

internal static class ProjectQuotationPdfDataFactory
{
    public static QuotationPdfData Build(ProjectDetailDto project, QuotationDetailDto quotation, CustomerDetailDto? customerDetail = null)
    {
        var currentVersion = ResolveCurrentVersion(quotation);
        var lineItems = currentVersion?.LineItems ?? [];
        var subtotal = ResolveLineSubtotal(project, lineItems, quotation);
        var structuredDiscount = ResolveDiscountAmount(currentVersion?.DiscountStructure, subtotal);
        var manualDiscount = Math.Max(0m, currentVersion?.ManualDiscountAmount ?? 0m);
        var discount = Math.Min(subtotal, structuredDiscount + manualDiscount);
        var shippingCost = Math.Max(0m, currentVersion?.ShippingCost ?? 0m);
        var taxableSubtotal = Math.Max(0m, subtotal - discount + shippingCost);
        var total = currentVersion?.TotalPrice > 0m
            ? currentVersion.TotalPrice
            : quotation.Total > 0m
                ? quotation.Total
                : project.TotalPrice;

        return new QuotationPdfData
        {
            QuotationNumber = FirstNonEmpty(quotation.QuotationNumber, project.QuotationNumber) ?? string.Empty,
            VersionNumber = currentVersion?.VersionNumber ?? quotation.CurrentVersionNumber,
            CustomerName = ResolveCustomerName(project, quotation, customerDetail),
            CustomerType = ResolveCustomerType(project, customerDetail),
            CustomerBranch = FirstNonEmpty(project.CustomerBranch, ResolveCustomerBranch(project, customerDetail)),
            CustomerTaxId = FirstNonEmpty(project.CustomerTaxId, customerDetail?.CompanyVatNumber, customerDetail?.CompanyRegistrationNumber),
            CustomerPhone = FirstNonEmpty(project.CustomerCompanyPhone, project.CustomerPhone, customerDetail?.CompanyPhone, customerDetail?.Mobile, customerDetail?.Landline),
            CustomerDisplayLines = BuildCustomerDisplayLines(project, customerDetail),
            CustomerAddress = FirstNonEmpty(project.BillingAddressLine, project.ShippingAddressLine, FormatAddressText(ResolveBillingAddressLines(customerDetail))),
            BillingAddress = FirstNonEmpty(project.BillingAddressLine, FormatAddressText(ResolveBillingAddressLines(customerDetail))),
            BillingAddressLines = SplitAddressLines(project.BillingAddressLine, ResolveBillingAddressLines(customerDetail)),
            ShippingAddress = FirstNonEmpty(project.ShippingAddressLine, FormatAddressText(ResolveShippingAddressLines(customerDetail))),
            ShippingAddressLines = SplitAddressLines(project.ShippingAddressLine, ResolveShippingAddressLines(customerDetail)),
            ContactPerson = FirstNonEmpty(project.CustomerName, customerDetail?.Name),
            QuotationDate = quotation.CreatedAt == default ? project.CreatedAt : quotation.CreatedAt,
            ValidityStart = quotation.ValidityPeriodStart == default ? project.CreatedAt : quotation.ValidityPeriodStart,
            ValidityEnd = quotation.ValidityPeriodEnd == default ? project.ValidUntil ?? project.CreatedAt.AddDays(30) : quotation.ValidityPeriodEnd,
            SubtotalBeforeDiscount = subtotal,
            TotalDiscount = discount,
            ManualDiscountAmount = discount,
            ShippingCost = shippingCost,
            Discounts = BuildDiscounts(currentVersion?.DiscountStructure, structuredDiscount, manualDiscount),
            Subtotal = taxableSubtotal,
            TaxAmount = currentVersion?.TaxAmount ?? quotation.Tax,
            TotalAmount = total,
            Currency = FirstNonEmpty(currentVersion?.CurrencyCode, quotation.CurrencyCode, project.Currency) ?? "THB",
            DeliveryExpectations = FirstNonEmpty(currentVersion?.DeliveryExpectations, quotation.DeliveryExpectations),
            SpecialTerms = currentVersion?.SpecialTerms,
            ChangeSummary = currentVersion?.ChangeSummary,
            Items = BuildItems(project, lineItems)
        };
    }

    private static QuotationVersionDto? ResolveCurrentVersion(QuotationDetailDto quotation) =>
        quotation.Versions?
            .OrderByDescending(version => version.VersionNumber == quotation.CurrentVersionNumber)
            .ThenByDescending(version => version.VersionNumber)
            .FirstOrDefault();

    private static decimal ResolveLineSubtotal(
        ProjectDetailDto project,
        IReadOnlyList<QuotationItemDto> lineItems,
        QuotationDetailDto quotation)
    {
        var versionSubtotal = lineItems.Sum(item => item.Quantity * item.UnitPrice);
        if (versionSubtotal > 0m)
            return versionSubtotal;

        var projectSubtotal = project.Parts.Sum(GetPartLineTotal);
        return projectSubtotal > 0m ? projectSubtotal : quotation.SubTotal;
    }

    private static List<QuotationPdfItem> BuildItems(ProjectDetailDto project, IReadOnlyList<QuotationItemDto> lineItems) =>
        project.Parts.Select((part, index) =>
        {
            var lineItem = index < lineItems.Count ? lineItems[index] : null;
            var unitPrice = lineItem?.UnitPrice > 0m ? lineItem.UnitPrice : GetPartUnitPrice(part);
            var quantity = lineItem?.Quantity > 0m ? lineItem.Quantity : part.Quantity;

            return new QuotationPdfItem
            {
                Index = index + 1,
                PartName = part.FileName,
                MaterialName = FirstNonEmpty(part.MaterialName, part.MaterialCode, "-") ?? "-",
                ManufacturingProcess = FormatProcess(part.ProcessType),
                DetailLines = BuildPartDetailLines(part),
                Quantity = quantity,
                QuantityUnit = "pcs",
                UnitPrice = unitPrice,
                LineTotal = quantity * unitPrice,
                Notes = BuildPartNotes(part),
                ThumbnailUrl = part.ThumbnailUrl
            };
        }).ToList();

    private static List<string> BuildCustomerDisplayLines(ProjectDetailDto project, CustomerDetailDto? customerDetail)
    {
        var companyName = FirstNonEmpty(project.CustomerCompanyName, customerDetail?.CompanyName);
        var contactName = FirstNonEmpty(project.CustomerName, customerDetail?.Name);
        var customerEmail = FirstNonEmpty(project.CustomerCompanyEmail, project.CustomerEmail, customerDetail?.CompanyContactEmail, customerDetail?.Email);
        var customerPhone = FirstNonEmpty(project.CustomerCompanyPhone, project.CustomerPhone, customerDetail?.CompanyPhone, customerDetail?.Mobile, customerDetail?.Landline);
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            lines.Add(companyName);
            if (!string.IsNullOrWhiteSpace(contactName))
                lines.Add($"Attn: {contactName}");
            if (!string.IsNullOrWhiteSpace(customerEmail))
                lines.Add($"Email: {customerEmail}");
        }
        else
        {
            AddIfPresent(lines, contactName);
            AddIfPresent(lines, customerEmail);
        }

        AddIfPresent(lines, customerPhone);
        return lines;
    }

    private static List<string> BuildPartDetailLines(ProjectPartDto part)
    {
        var lines = new List<string>();
        if (FormatDimensions(part.Dimensions) is { Length: > 0 } dimensions)
            lines.Add($"Bounding box: {dimensions}");

        if (FormatFinish(part.Finish) is { Length: > 0 } finish)
            lines.Add($"Surface finish: {finish}");

        if (FormatTolerance(part.Tolerance, part.ProcessType) is { Length: > 0 } tolerance)
            lines.Add($"Tolerance: {tolerance}");

        if (FormatRoughness(part.RoughnessCode) is { Length: > 0 } roughness)
            lines.Add($"Surface roughness: {roughness}");

        AddIfPresent(lines, FormatColor(part));

        if (part.HasThreadedHoles)
        {
            lines.Add(part.ThreadedHoleCount > 0 && !string.IsNullOrWhiteSpace(part.ThreadedHoleSpec)
                ? $"Tapped holes: {part.ThreadedHoleCount:N0} x {part.ThreadedHoleSpec}"
                : "Tapped holes: Yes");
        }

        if (part.HasInserts)
        {
            lines.Add(part.InsertCount > 0
                ? $"Inserts: {part.InsertCount:N0} x {part.InsertType}"
                : $"Inserts: {part.InsertType}");
        }

        lines.Add($"Inspection: {part.InspectionLevel}");

        if (part.DrawingFiles.Count > 0)
        {
            var drawingNames = part.DrawingFiles
                .Select(file => file.FileName)
                .Where(name => !string.IsNullOrWhiteSpace(name));
            lines.Add($"Drawing: {string.Join(", ", drawingNames)}");
        }

        return lines;
    }

    private static string? BuildPartNotes(ProjectPartDto part)
    {
        return string.IsNullOrWhiteSpace(part.PartNotes)
            ? null
            : part.PartNotes.Trim();
    }

    private static decimal ResolveDiscountAmount(SalesDiscountStructureDto? discount, decimal lineSubtotal)
    {
        if (discount == null || discount.DiscountValue <= 0m || lineSubtotal <= 0m)
            return 0m;

        var amount = discount.DiscountType switch
        {
            SalesDiscountType.FixedAmount => discount.DiscountValue,
            SalesDiscountType.Percentage or SalesDiscountType.VolumeBased => Math.Round(lineSubtotal * (discount.DiscountValue / 100m), 2, MidpointRounding.AwayFromZero),
            _ => 0m
        };

        return Math.Min(lineSubtotal, Math.Max(0m, amount));
    }

    private static List<QuotationPdfDiscount> BuildDiscounts(
        SalesDiscountStructureDto? discount,
        decimal discountAmount,
        decimal manualDiscountAmount)
    {
        var discounts = new List<QuotationPdfDiscount>();

        if (discount != null && discountAmount > 0m)
        {
            discounts.Add(new QuotationPdfDiscount
            {
                DiscountType = discount.DiscountType.ToString(),
                DiscountValue = discountAmount,
                Conditions = discount.Conditions ?? discount.AuthorizationReason,
            });
        }

        if (manualDiscountAmount > 0m)
        {
            discounts.Add(new QuotationPdfDiscount
            {
                DiscountType = SalesDiscountType.FixedAmount.ToString(),
                DiscountValue = manualDiscountAmount,
                Conditions = "Manual discount",
            });
        }

        return discounts;
    }

    private static List<string> SplitAddressLines(string? value, IReadOnlyList<string> fallback) =>
        string.IsNullOrWhiteSpace(value)
            ? fallback.ToList()
            : value.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static List<string> ResolveBillingAddressLines(CustomerDetailDto? customer)
    {
        var address = customer?.CompanyBillingAddress
            ?? customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Billing"))
            ?? customer?.Addresses.FirstOrDefault(address => address.IsDefault);
        return FormatAddressLines(address);
    }

    private static List<string> ResolveShippingAddressLines(CustomerDetailDto? customer)
    {
        var address = customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Shipping") && address.IsDefault)
            ?? customer?.Addresses.FirstOrDefault(address => IsAddressType(address, "Shipping"));
        return FormatAddressLines(address);
    }

    private static List<string> FormatAddressLines(AddressResponse? address)
    {
        if (address is null)
            return [];

        return new[]
        {
            address.AddressLine1,
            address.AddressLine2,
            address.AddressLine3,
            address.District,
            address.City,
            address.StateProvince,
            address.PostalCode,
        }.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private static string? FormatAddressText(IReadOnlyList<string> lines) =>
        lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);

    private static bool IsAddressType(AddressResponse address, string type) =>
        address.Type.Contains(type, StringComparison.OrdinalIgnoreCase);

    private static string ResolveCustomerName(ProjectDetailDto project, QuotationDetailDto quotation, CustomerDetailDto? customerDetail) =>
        FirstNonEmpty(project.CustomerCompanyName, customerDetail?.CompanyName, quotation.CustomerName, project.CustomerName, customerDetail?.Name, "-") ?? "-";

    private static string ResolveCustomerType(ProjectDetailDto project, CustomerDetailDto? customerDetail) =>
        string.IsNullOrWhiteSpace(project.CustomerCompanyName) && string.IsNullOrWhiteSpace(customerDetail?.CompanyName)
            ? "Individual"
            : "Corporate";

    private static string? ResolveCustomerBranch(ProjectDetailDto project, CustomerDetailDto? customerDetail)
    {
        if (ResolveCustomerType(project, customerDetail) != "Corporate")
            return null;

        return ContainsThai(ResolveCustomerName(project, new QuotationDetailDto(), customerDetail)) ? "สำนักงานใหญ่" : "Head Office";
    }

    private static bool ContainsThai(string? value) =>
        value?.Any(ch => ch >= '\u0E00' && ch <= '\u0E7F') == true;

    private static decimal GetPartUnitPrice(ProjectPartDto part) =>
        part.ConfirmedPrice ?? part.ConfirmedUnitPrice ?? part.EstimatedPrice ?? part.AiSuggestedPrice ?? 0m;

    private static decimal GetPartLineTotal(ProjectPartDto part) =>
        GetPartUnitPrice(part) * Math.Max(part.Quantity, 0);

    private static string FormatProcess(string? process)
    {
        if (string.IsNullOrWhiteSpace(process))
            return "-";

        var normalized = process.Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            "FDM" => "3D Printing (FDM)",
            "SLA" => "3D Printing (SLA)",
            "SLS" => "3D Printing (SLS)",
            "CNC" or "CNC_MILLING" => "CNC Milling",
            "CNC_TURNING" => "CNC Turning",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(process.Replace("_", " ", StringComparison.Ordinal).ToLowerInvariant())
        };
    }

    private static string FormatDimensions(ModelDimensionsDto? dimensions) =>
        dimensions is null ? string.Empty : $"{FormatDimension(dimensions.X)} x {FormatDimension(dimensions.Y)} x {FormatDimension(dimensions.Z)} mm";

    private static string FormatDimension(double value) =>
        value.ToString(value >= 10 ? "0.#" : "0.##", CultureInfo.InvariantCulture);

    private static string? FormatRoughness(string? roughnessCode) =>
        roughnessCode?.ToUpperInvariant() switch
        {
            "RA_3_2" => "Ra 3.2 um",
            "RA_1_6" => "Ra 1.6 um",
            "RA_0_8" => "Ra 0.8 um",
            "RA_0_4" => "Ra 0.4 um",
            null or "" => null,
            _ => roughnessCode.Replace("_", " ", StringComparison.Ordinal)
        };

    private static string? FormatFinish(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = NormalizeCode(value);
        return normalized switch
        {
            "AS_PRNTED" or "AS_PRINTED" or "ASPRINTED" => "As-printed",
            "AS_MACHINED" or "ASMACHINED" => "As-machined",
            _ => FormatOptionName(value),
        };
    }

    private static string? FormatTolerance(string? value, string? process)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = NormalizeCode(value);
        if (process?.Contains("FDM", StringComparison.OrdinalIgnoreCase) == true)
        {
            return normalized switch
            {
                "FDM_STD" or "FDM_STANDARD" or "FDMSTANDARD" => "FDM Standard +-0.3mm",
                "FDM_FINE" or "FDMFINE" => "FDM Fine +-0.15mm",
                _ => FormatOptionName(value),
            };
        }

        return normalized switch
        {
            "ISO2768_M" or "ISO_2768_M" or "MEDIUM" => "Medium (ISO2768-m)",
            "ISO2768_F" or "ISO_2768_F" or "FINE" => "Fine (ISO2768-f)",
            _ => FormatOptionName(value),
        };
    }

    private static string? FormatColor(ProjectPartDto part)
    {
        if (!string.IsNullOrWhiteSpace(part.Color))
            return $"Color: {part.Color.Trim()}";

        foreach (var key in new[] { "paint_color_reference", "paint_color", "paint_colour", "material_color", "material_colour", "plastic_color", "plastic_colour", "anodize_color", "anodise_color" })
        {
            if (part.ProcessConfig.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return $"Color: {value.Trim()}";
        }

        if (part.ProcessConfig.TryGetValue("paint_color_hex", out var hex) && !string.IsNullOrWhiteSpace(hex))
            return $"Color: {hex.Trim()}";

        return null;
    }

    private static string NormalizeCode(string value) =>
        value.Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

    private static string FormatOptionName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return string.Join(
            " ",
            value.Replace("_", " ", StringComparison.Ordinal)
                .Replace("-", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant() switch
        {
            var text when string.IsNullOrWhiteSpace(text) => value,
            var text => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text),
        };
    }

    private static void AddIfPresent(List<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            values.Add(value.Trim());
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
