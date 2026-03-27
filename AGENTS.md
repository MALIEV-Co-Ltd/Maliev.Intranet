# Maliev.Intranet Agent Guidelines

This document provides essential information for agentic coding assistants operating in the Maliev.Intranet repository. It captures architecture, standards, and operational workflows.

## 🏗️ Architecture & Tech Stack

- **Framework**: .NET 10.0 (C# 13)
- **Frontend**: Blazor Interactive Auto (Server + WebAssembly)
- **BFF Layer**: ASP.NET Core acting as a secure gateway for domain microservices.
- **UI Components**: MudBlazor (Material Design components).
- **Observability**: OpenTelemetry (OTLP) with business metrics (meters) and distributed tracing.
- **Communication**: SignalR for real-time dashboard updates and alerts.
- **Identity**: Google OAuth2 + JWT (User identity propagated to downstream services via the JWT Bearer token — the `sub` claim carries the user's GUID).
- **Rendering Modes**: Always consider server-side vs client-side rendering trade-offs:
  - `InteractiveServer`: Fast initial load, requires connection to server, good for data-heavy forms.
  - `InteractiveWebAssembly`: True offline capability, faster subsequent loads, better for static content.
  - `InteractiveAuto`: Best of both worlds but with added complexity. Default choice when uncertain.

## 🛠️ Build, Lint, and Test Commands

| Action | Command |
|--------|---------|
| **Restore** | `dotnet restore` |
| **Build** | `dotnet build Maliev.Intranet.slnx` |
| **Run BFF** | `dotnet run --project Maliev.Intranet.Bff` |
| **Lint/Format** | `dotnet format Maliev.Intranet.slnx` |
| **Test All** | `dotnet test` |
| **Single Test** | `dotnet test --filter Name={TestName}` |
| **Specific Project** | `dotnet test Maliev.Intranet.Tests` |

> **Note**: Integration tests require Docker for **Testcontainers**.

## ⚖️ Development Mandates (Constitution)

- ✅ **TreatWarningsAsErrors**: Enabled in all projects. Do not introduce warnings.
- ✅ **XML Documentation**: Required on all public members (classes, methods, properties).
- ✅ **BFF Pattern**: Blazor WASM MUST NOT call domain services directly. Route all requests through the Bff.
- ✅ **Explicit Mapping**: NO AutoMapper. Use manual mapping extensions in `Extensions/` folders.
- ✅ **Data Annotations**: NO FluentValidation. Use standard `System.ComponentModel.DataAnnotations`.
- ✅ **xUnit Assertions**: NO FluentAssertions. Use standard `Xunit.Assert`.
- ✅ **Async Suffix**: All asynchronous methods must end with the `Async` suffix.
- ✅ **Identity Propagation**: Ensure user context (JWT) is forwarded using `UserContextHandler`.
- ✅ **Input Validation**: All input must have validation. Use immediate (client-side) validation when appropriate for responsiveness; always enforce validation server-side as the source of truth.
- ✅ **Zero Dead BFF Endpoints**: Every BFF endpoint must have actual usage. Before committing, verify the endpoint is wired to a client component, handler, or integration test. Orphaned endpoints are not allowed.

## 🎨 Code Style & Conventions

### 1. Naming Conventions
- **Classes/Interfaces/Methods**: `PascalCase` (e.g., `UserContextHandler`, `IReferenceDataService`).
- **Interfaces**: Prefix with `I` (e.g., `ICustomerService`).
- **Properties**: `PascalCase`.
- **Parameters/Local Variables**: `camelCase`.
- **Fields**: `_camelCase` with underscore prefix (e.g., `_httpClient`).
- **Namespaces**: Match folder structure, starting with `Maliev.Intranet`.

### 2. Formatting & Structure
- **C# 13 Features**: Use primary constructors where appropriate (e.g., `public class MyService(IHttpClientFactory factory) { ... }`).
- **Namespaces**: Use file-scoped namespaces (e.g., `namespace Maliev.Intranet.Shared;`).
- **Imports**: Organize alphabetically; remove unused `using` statements.
- **Collection Initialization**: Prefer modern `[]` syntax for empty or small collections (e.g., `List<string> list = [];`).
- **Strings**: Use raw string literals (`"""`) for multi-line strings or JSON.

### 3. Error Handling & Responses
- **API Responses**: Wrap BFF responses in `MalievResponse<T>` or `PagedResponse<T>`.
- **Downstream Calls**: Use `DownstreamResponse<T>` to handle responses from domain services.
- **Global Error Handling**: Use `ErrorBoundary` in Blazor and Exception Middleware in BFF.
- **Logging**: Use `ILogger<T>` for structured logging. Do not use `Console.WriteLine`.
- **User-Friendly Errors**: Never expose raw exceptions to the frontend. Always map exceptions to user-friendly messages. Use a standard `ErrorResponse` DTO structure that frontend components can parse and display meaningfully.

### 4. Blazor & MudBlazor Best Practices
- **Component Parameters**: Use `[Parameter]` for public properties that are meant to be passed from parents.
- **EventCallback**: Use `EventCallback` or `EventCallback<T>` for child-to-parent communication.
- **RenderMode**: Most pages should use `@rendermode InteractiveAuto`. Always consider the trade-offs between `InteractiveServer`, `InteractiveWebAssembly`, and `InteractiveAuto` based on the page's requirements (offline support, initial load speed, data complexity).
- **MudBlazor**: 
    - Use `MudTable` with `ServerData` for large datasets.
    - Use `MudDialog` for modals and `ISnackbar` for transient notifications.
    - Prefer `MudStack` and `MudItem` for layouts over raw CSS/HTML where possible.
- **Text Input with Live Counter**: For all `longtext` fields or any input that could potentially exceed the max length limit, display a live character counter (e.g., `50/2000` or `13/100`). Use immediate client-side validation to enforce the limit.
- **Skeleton Loaders**: Always use skeleton components with animation when waiting for data to load. Never show blank spaces or spinner-only states.

### 5. Mapping Extensions (Manual)
Since AutoMapper is banned, use static extension classes:
```csharp
public static class CustomerExtensions
{
    public static CustomerDto ToDto(this Customer entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name
    };
}
```

## 📂 Project Structure

- `Maliev.Intranet.Bff`: ASP.NET Core host. Contains Controllers, Hubs, and Handlers.
- `Maliev.Intranet.Client`: Blazor WASM client. Contains Pages and Shared Components.
- `Maliev.Intranet.Shared`: DTOs, Interfaces, Enums, and common extension methods.
- `Maliev.Intranet.Tests`: xUnit tests (Unit and Integration).
- `specs/`: Markdown specifications and feature plans. Use these to understand requirements.
- `.specify/`: Automation scripts and templates for feature development.

## 🧪 Testing Guidelines

- **Unit Tests**: Place in `Maliev.Intranet.Tests`. Mock external dependencies using `Moq` or similar.
- **Integration Tests**: Use **Testcontainers** for database or external service dependencies. Use `WebApplicationFactory` for BFF tests.
- **Naming**: `[MethodName]_[Scenario]_[ExpectedResult]` (e.g., `GetCustomerAsync_WhenNotFound_ReturnsNull`).
- **Assertions**: Stick to `Assert.Equal`, `Assert.NotNull`, etc.
- **UX/UI Validation**: Always validate UI changes with tests. Component tests should verify rendered output, user interactions, and loading states. Never commit UI changes without corresponding test coverage.

## 📡 Communication & Identity

- **SignalR**: Hubs should be located in `Maliev.Intranet.Bff/Hubs`. Use typed hubs if possible.
- **Identity**: Identity is propagated to downstream services via `UserContextHandler`, which forwards the user's platform JWT as a Bearer token. The JWT `sub` claim contains the user's GUID. No separate `X-User-Id` header is forwarded.
- **Permissions**: Follow GCP-style naming: `{service}.{resource}.{action}` (e.g., `orders.shipments.create`).

## 🔄 Frontend-to-Backend Wiring

When connecting the Blazor Frontend (Client) or the BFF to downstream microservices, agents must follow this verification protocol to ensure data integrity:

1.  **Downstream First**: Before modifying any UI component or BFF controller, always inspect the source microservice (e.g., `Maliev.CustomerService`) for the latest DTO definitions and business logic.
2.  **DTO Synchronization**: Ensure that the DTOs in `Maliev.Intranet.Shared` perfectly match the request/response models of the downstream service. Missing properties in the Shared layer lead to data loss during create/update operations.
3.  **Feature Parity**: Check for hidden features or validation rules in the microservice layer (e.g., specific regex for VAT, default values for Timezones, or mandatory Boolean flags like `IsDefault`).
4.  **Implicit Requirements**: If a downstream service expects multiple related records (like both Billing and Shipping addresses), the BFF orchestration must ensure all are created, even if the UI simplifies the input.

Failure to check the downstream "source of truth" first is the primary cause of platform-level data inconsistencies.

## 🤖 Interaction Policy

- **Verification**: Always verify changes with `dotnet build` before finishing.
- **New Features**: Check `specs/` for requirements before implementing.
- **Refactoring**: Maintain consistency with `Maliev.Intranet.Shared/Dtos/CommonDtos.cs`.
- **UI**: Use MudBlazor components (`MudTable`, `MudButton`, `MudCard`) for consistent look and feel.


## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
