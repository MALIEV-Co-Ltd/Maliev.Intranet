# Data Model: Maliev.Intranet

**Feature**: `001-create-intranet-portal`
**Scope**: Shared DTOs and BFF Aggregation Models (No persistent domain entities in this service)

## Shared Models (`Maliev.Intranet.Shared`)

These models are used by both the BFF controller/hubs and the Blazor Client.

### Dashboard

**DashboardViewModel**
- `List<WidgetData> Widgets`
- `List<SystemAlert> Alerts`
- `List<PendingTask> MyTasks`

**WidgetData**
- `string Title`
- `string Type` (e.g., "Chart", "Stat", "List")
- `object Data` (JSON payload specific to type)
- `string SourceService` (e.g., "OrderService")

**SystemAlert**
- `Guid Id`
- `string Message`
- `string Severity` (Info, Warning, Critical)
- `DateTime Timestamp`
- `string? ActionLink`

### Operational Domain Models

**CustomerSummaryDto**
- `Guid Id`
- `string Name`
- `string Email`
- `string Status`
- `decimal OutstandingBalance`

**OrderSummaryDto**
- `Guid Id`
- `string OrderNumber`
- `string CustomerName`
- `decimal TotalAmount`
- `string Status` (Pending, Approved, Shipped, etc.)
- `DateTime CreatedAt`

**UserContextDto**
- `Guid UserId`
- `string DisplayName`
- `List<string> Roles`
- `List<string> Permissions`

## BFF Internal Models (`Maliev.Intranet.Bff`)

These models are used internally by the BFF to deserialize downstream responses.

**DownstreamResponse<T>**
- `T Data`
- `bool Success`
- `string ErrorMessage`

## Validation Rules

Since we use DataAnnotations, validation attributes will be applied to the Shared DTOs (e.g., InputModels for forms).

**CreateCustomerInputModel**
- `[Required] Name`: Max 100 chars
- `[Required][EmailAddress] Email`

**UpdateOrderStatusInputModel**
- `[Required] OrderId`
- `[Required] NewStatus`
- `string? Comment`: Max 500 chars
