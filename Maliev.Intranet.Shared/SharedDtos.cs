using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents the aggregated data for the operational dashboard.
/// </summary>
public class DashboardViewModel
{
    /// <summary>
    /// Gets or sets the list of dashboard widgets.
    /// </summary>
    public List<WidgetData> Widgets { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of active system alerts.
    /// </summary>
    public List<SystemAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Represents data for a single dashboard widget.
/// </summary>
public class WidgetData
{
    /// <summary>
    /// Gets or sets the widget title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the widget type (e.g., "Stat", "Chart").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data payload for the widget.
    /// </summary>
    public System.Text.Json.JsonElement Data { get; set; }

    /// <summary>
    /// Gets or sets the name of the source microservice.
    /// </summary>
    public string SourceService { get; set; } = string.Empty;
}

/// <summary>
/// Represents a system-wide alert or notification.
/// </summary>
public class SystemAlert
{
    /// <summary>
    /// Gets or sets the unique identifier for the alert.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level (e.g., "Info", "Warning", "Critical").
    /// </summary>
    public string Severity { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the timestamp when the alert was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets an optional link for user action.
    /// </summary>
    public string? ActionLink { get; set; }
}

/// <summary>
/// Represents a summary of a customer record.
/// </summary>
public class CustomerSummaryDto
{
    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outstanding balance for the customer.
    /// </summary>
    public decimal OutstandingBalance { get; set; }
}

/// <summary>
/// Represents a summary of an order record.
/// </summary>
public class OrderSummaryDto
{
    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer name associated with the order.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount of the order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents the context and permissions of the currently authenticated user.
/// </summary>
public class UserContextDto
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the user's profile picture.
    /// </summary>
    public string? PictureUrl { get; set; }

    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of specific permissions granted to the user.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Defines the standard permission policies for the Maliev Intranet.
/// Uses a 3-part GCS-style naming convention (domain.resource.action).
/// </summary>
public static class MalievPermissions
{
    public static class Customer
    {
        public const string Read = "customer.profile.read";

        public static class Profile
        {
            public const string Read = "customer.profile.read";
            public const string Write = "customer.profile.write";
        }
    }

    public static class Order
    {
        public const string Read = "order.management.read";
        public const string Approve = "order.management.approve";

        public static class Management
        {
            public const string Read = "order.management.read";
            public const string Write = "order.management.write";
            public const string Approve = "order.management.approve";
        }
    }

    public static class Iam
    {
        public const string Manage = "iam.access.manage";

        public static class Access
        {
            public const string Manage = "iam.access.manage";
        }
    }

    public static class Accounting
    {
        public const string View = "accounting.financials.view";

        public static class Financials
        {
            public const string View = "accounting.financials.view";
        }
    }
}


/// <summary>
/// Generic wrapper for a standard Maliev service response.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class MalievResponse<T>
{
    /// <summary>
    /// Gets or sets the data payload.
    /// </summary>
    public T? Data { get; set; }
}

/// <summary>
/// Generic wrapper for a paged response.
/// </summary>
/// <typeparam name="T">The type of the items in the list.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the list of data items.
    /// </summary>
    public IEnumerable<T> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    public PaginationMeta Meta { get; set; } = new();
}

/// <summary>
/// Metadata for paged responses.
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Represents a permission defined in the IAM service.
/// </summary>
public class PermissionDto
{
    /// <summary>
    /// Gets or sets the unique name of the permission (e.g., "customer.customers.read").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of what the permission allows.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a role defined in the IAM service.
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Gets or sets the unique name of the role (e.g., "admin").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the role.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of permissions associated with this role.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Request model for assigning roles or permissions to a user.
/// </summary>
public class UserAssignmentRequest
{
    /// <summary>
    /// Gets or sets the target user's ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the list of role names to assign.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of permission names to assign.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Represents a summary of a principal (user or service account).
/// </summary>
public class PrincipalSummaryDto
{
    public Guid PrincipalId { get; set; }
    public string PrincipalType { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? LinkedService { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for granting a role to a principal.
/// </summary>
public class GrantRoleRequestDto
{
    [Required]
    public string RoleId { get; set; } = string.Empty;
    public string? ResourcePath { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Represents a role binding for a principal.
/// </summary>
public class RoleBindingDto
{
    public Guid BindingId { get; set; }
    public Guid PrincipalId { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string? ResourcePath { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Generic wrapper for a response from a downstream microservice.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class DownstreamResponse<T>
{
    /// <summary>
    /// Gets or sets the data payload.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents detailed information for a single customer.
/// </summary>
public class CustomerDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public decimal OutstandingBalance { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }

    // Aggregate Stats
    public decimal TotalSpent { get; set; }
    public int ActiveOrdersCount { get; set; }
    public int OpenQuotationsCount { get; set; }
}

/// <summary>
/// Represents a summary of a quotation record.
/// </summary>
public class QuotationSummaryDto
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
}

/// <summary>
/// Represents detailed information for a single quotation.
/// </summary>
public class QuotationDetailDto
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Represents a summary of a material record.
/// </summary>
public class MaterialSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public decimal StockQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents detailed information for a single material.
/// </summary>
public class MaterialDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal ReorderLevel { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
}

/// <summary>
/// Represents a summary of an employee record.
/// </summary>
public class EmployeeSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
}

/// <summary>
/// Represents detailed information for a single employee.
/// </summary>
public class EmployeeDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? ManagerName { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Represents a summary of an invoice record.
/// </summary>
public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
}

/// <summary>
/// Represents detailed information for a single invoice.
/// </summary>
public class InvoiceDetailDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Represents a summary of a payment record.
/// </summary>
public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Represents detailed information for a single payment.
/// </summary>
public class PaymentDetailDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Represents a summary of a supplier record.
/// </summary>
public class SupplierSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Rating { get; set; }
}

/// <summary>
/// Represents detailed information for a single supplier.
/// </summary>
public class SupplierDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string? ContactPerson { get; set; }
    public string? Website { get; set; }
}

