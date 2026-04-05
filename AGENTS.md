# Maliev.Intranet Agent Guidelines

This document provides essential information for agentic coding assistants operating in the Maliev.Intranet repository. It captures architecture, standards, and operational workflows.

---

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

---

## 🛠️ Build, Test & Lint Commands

All commands run from within this service directory (`B:\maliev\Maliev.Intranet`).

```powershell
# Build (treats warnings as errors — all must be fixed)
dotnet build Maliev.Intranet.slnx

# Run all tests
dotnet test Maliev.Intranet.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~CustomerTests.GetCustomerAsync_WhenNotFound_ReturnsNull"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~CustomerTests"

# Run with code coverage
dotnet test Maliev.Intranet.slnx --collect:"XPlat Code Coverage"

# Format check
dotnet format Maliev.Intranet.slnx

# Run BFF
dotnet run --project Maliev.Intranet.Bff

# EF Core migrations (Infrastructure project only)
dotnet ef migrations add <Name> --project Maliev.Intranet.Infrastructure --startup-project Maliev.Intranet.Infrastructure
```

> **Note**: Integration tests require Docker for **Testcontainers**.

---

## Code Style & Conventions

### C# Naming & Formatting

- **Namespaces**: File-scoped (`namespace Maliev.Intranet.Shared;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `GetCustomerAsync`)
- **Interfaces**: Prefix with `I` (e.g., `ICustomerService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `orders.shipments.create`, `customer.customers.create`
  - Invalid: `customer.customer.create` (singular), `orders.ship` (missing action)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace
- **C# 13 Features**: Use primary constructors where appropriate (e.g., `public class MyService(IHttpClientFactory factory) { ... }`)
- **Collection Initialization**: Prefer modern `[]` syntax for empty or small collections (e.g., `List<string> list = [];`)
- **Strings**: Use raw string literals (`"""`) for multi-line strings or JSON

### C# Patterns

- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("intranet/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {FileId}", fileId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: Check existing conventions in this service
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

### Blazor & MudBlazor Best Practices

- **Component Parameters**: Use `[Parameter]` for public properties that are meant to be passed from parents.
- **EventCallback**: Use `EventCallback` or `EventCallback<T>` for child-to-parent communication.
- **RenderMode**: Most pages should use `@rendermode InteractiveAuto`. Always consider the trade-offs between `InteractiveServer`, `InteractiveWebAssembly`, and `InteractiveAuto` based on the page's requirements (offline support, initial load speed, data complexity).
- **MudBlazor**:
    - Use `MudTable` with `ServerData` for large datasets.
    - Use `MudDialog` for modals and `ISnackbar` for transient notifications.
    - Prefer `MudStack` and `MudItem` for layouts over raw CSS/HTML where possible.
    - **Typography**: Never hard-code `font-size` values in CSS (e.g. `12px`, `14px`). Always use MudBlazor CSS typography variables such as `var(--mud-typography-body1-size)`, `var(--mud-typography-body2-size)`, `var(--mud-typography-caption-size)`, `var(--mud-typography-overline-size)`, `var(--mud-typography-default-size)`, etc. This ensures sizes stay consistent with the application's theme.
- **Text Input with Live Counter**: For all `longtext` fields or any input that could potentially exceed the max length limit, display a live character counter (e.g., `50/2000` or `13/100`). Use immediate client-side validation to enforce the limit.
- **Skeleton Loaders**: Always use skeleton components with animation when waiting for data to load. Never show blank spaces or spinner-only states.

### 3D Viewer URL Resolution (Option B)

The recommended pattern uses pre-signed URLs from the BFF cache, eliminating the need for the `viewer-url` API endpoint:

```csharp
// ✅ OPTION B (preferred): Use pre-signed URL from cache - no API call needed
if (!string.IsNullOrEmpty(part.GlbSignedUrl))
{
    part.ViewerUrl = part.GlbSignedUrl;
    return;
}

// Fallback: Call viewer-url API for backward compatibility (drafts created before this fix)
var storagePath = part.StoragePath;
```

**Why this works**: 
1. `FileAnalyzedConsumer` generates a signed URL from `GlbStoragePath` and stores it in the BFF cache (`GlbSignedUrl`)
2. The cache is returned in the `analysis-status` API response
3. SignalR also pushes `GlbSignedUrl` via the `GlbReady` event
4. Client checks `GlbSignedUrl` first - if available, uses it directly without any API call
5. Only falls back to `viewer-url` API for older drafts that don't have the cached URL

**Important**: Never pass `GlbStoragePath` (which ends in `_viewer.glb`) to the `viewer-url` endpoint - this causes a double-suffix bug producing paths like `file.stl_viewer.glb_viewer.glb`.

### Mapping Extensions (Manual)

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

### API Responses & Error Handling

- **API Responses**: Wrap BFF responses in `MalievResponse<T>` or `PagedResponse<T>`.
- **Downstream Calls**: Use `DownstreamResponse<T>` to handle responses from domain services.
- **Global Error Handling**: Use `ErrorBoundary` in Blazor and Exception Middleware in BFF.
- **User-Friendly Errors**: Never expose raw exceptions to the frontend. Always map exceptions to user-friendly messages. Use a standard `ErrorResponse` DTO structure that frontend components can parse and display meaningfully.

---

## Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/{service}/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## 📂 Project Structure

- `Maliev.Intranet.Bff`: ASP.NET Core host. Contains Controllers, Hubs, and Handlers.
- `Maliev.Intranet.Client`: Blazor WASM client. Contains Pages and Shared Components.
- `Maliev.Intranet.Shared`: DTOs, Interfaces, Enums, and common extension methods.
- `Maliev.Intranet.Tests`: xUnit tests (Unit and Integration).
- `specs/`: Markdown specifications and feature plans. Use these to understand requirements.
- `.specify/`: Automation scripts and templates for feature development.

---

## 🧪 Testing Rules

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`
- **UX/UI Validation**: Always validate UI changes with tests. Component tests should verify rendered output, user interactions, and loading states. Never commit UI changes without corresponding test coverage.

---

## 📡 Communication & Identity

- **SignalR**: Hubs should be located in `Maliev.Intranet.Bff/Hubs`. Use typed hubs if possible.
- **Identity**: Identity is propagated to downstream services via `UserContextHandler`, which forwards the user's platform JWT as a Bearer token. The JWT `sub` claim contains the user's GUID. No separate `X-User-Id` header is forwarded.
- **Permissions**: Follow GCP-style naming: `{service}.{resource}.{action}` (e.g., `orders.shipments.create`).

---

## 🔄 Frontend-to-Backend Wiring

When connecting the Blazor Frontend (Client) or the BFF to downstream microservices, agents must follow this verification protocol to ensure data integrity:

1.  **Downstream First**: Before modifying any UI component or BFF controller, always inspect the source microservice (e.g., `Maliev.CustomerService`) for the latest DTO definitions and business logic.
2.  **DTO Synchronization**: Ensure that the DTOs in `Maliev.Intranet.Shared` perfectly match the request/response models of the downstream service. Missing properties in the Shared layer lead to data loss during create/update operations.
3.  **Feature Parity**: Check for hidden features or validation rules in the microservice layer (e.g., specific regex for VAT, default values for Timezones, or mandatory Boolean flags like `IsDefault`).
4.  **Implicit Requirements**: If a downstream service expects multiple related records (like both Billing and Shipping addresses), the BFF orchestration must ensure all are created, even if the UI simplifies the input.

Failure to check the downstream "source of truth" first is the primary cause of platform-level data inconsistencies.

---

## Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("domain.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with service domain (e.g., `/intranet`)
- **Scalar docs**: Configured at `/{service}/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards
- **BFF Pattern**: Blazor WASM MUST NOT call domain services directly. Route all requests through the Bff.
- **Identity Propagation**: Ensure user context (JWT) is forwarded using `UserContextHandler`.
- **Input Validation**: All input must have validation. Use immediate (client-side) validation when appropriate for responsiveness; always enforce validation server-side as the source of truth.
- **Zero Dead BFF Endpoints**: Every BFF endpoint must have actual usage. Before committing, verify the endpoint is wired to a client component, handler, or integration test. Orphaned endpoints are not allowed.

### Database & EF Core — Mandatory Rules

**EF Core Design Package**:
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project:
  ```
  dotnet ef migrations add <Name> --project Maliev.Intranet.Infrastructure --startup-project Maliev.Intranet.Infrastructure
  ```

**PostgreSQL xmin Concurrency** — Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead

---

## Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

---

## 🤖 Interaction Policy

- **Verification**: Always verify changes with `dotnet build` before finishing.
- **New Features**: Check `specs/` for requirements before implementing.
- **Refactoring**: Maintain consistency with `Maliev.Intranet.Shared/Dtos/CommonDtos.cs`.
- **UI**: Use MudBlazor components (`MudTable`, `MudButton`, `MudCard`) for consistent look and feel.
