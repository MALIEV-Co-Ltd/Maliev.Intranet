# Implementation Plan: Create Intranet Portal

**Branch**: `001-create-intranet-portal` | **Date**: 2025-01-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-create-intranet-portal/spec.md`

## Summary

The goal is to create a "Maliev.Intranet" web application to serve as a unified operational dashboard and management portal for employees. It will be built using **Blazor (Interactive Auto)** with a **Backend-for-Frontend (BFF)** architecture in .NET 10. The system will aggregate data from multiple microservices (Customer, Order, etc.) via REST APIs, provide real-time updates via **SignalR**, and enforce role-based access control via the IAM service. Observability will be handled by OpenTelemetry.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: Blazor (Interactive Auto), SignalR, Microsoft.Extensions.ServiceDiscovery (Aspire), OpenTelemetry, Microsoft.AspNetCore.Authentication.Google
**Storage**: Redis (for BFF caching, session state) - MUST use `redis:8.4-alpine`, No direct DB access
**Testing**: xUnit, Testcontainers (for simulating downstream services), Playwright (for E2E)
**Target Platform**: Kubernetes (GKE), Docker containers (Linux) - Expose port 8080
**Project Type**: Web Application (BFF + Blazor Client)
**Performance Goals**: Dashboard load < 3s, List rendering < 2s for 100 items. MUST emit business metrics (e.g., `active_sessions`).
**Constraints**: No direct DB access to other services, must use Maliev.Aspire.ServiceDefaults, strict IAM integration
**Observability**: Standard .NET `ILogger` with mandatory `appsettings.json` LogLevel config. OpenTelemetry for tracing/metrics.
**Scale/Scope**: Internal enterprise usage, supporting all operational domains (Sales, Accounting, HR, etc.)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Service Autonomy** | ✅ PASS | Intranet has no own DB (except cache), strictly uses APIs. |
| **II. Explicit Contracts** | ✅ PASS | Will define BFF contracts via Scalar/OpenAPI. |
| **III. Test-First** | ✅ PASS | Plan includes xUnit/Testcontainers setup. |
| **IV. Real Infrastructure** | ✅ PASS | Integration tests will use Testcontainers for Redis/Downstream mocks. |
| **V. Observability** | ✅ PASS | OpenTelemetry mandated by specs. |
| **VI. Security** | ✅ PASS | IAM integration mandated. |
| **VII. Secrets Management** | ✅ PASS | Google Secret Manager pattern will be used. |
| **X. Docker Best Practices** | ✅ PASS | Dockerfile will follow standard pattern (non-root user). |
| **XIII. Aspire Integration** | ✅ PASS | ServiceDefaults NuGet usage planned. |
| **XIV. Code Quality** | ✅ PASS | No AutoMapper/FluentValidation/FluentAssertions. |
| **XV. Project Structure** | ✅ PASS | Flat structure (Maliev.Intranet.Bff, Maliev.Intranet.Client). |

## Project Structure

### Documentation (this feature)

```text
specs/001-create-intranet-portal/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
Maliev.Intranet/
├── Maliev.Intranet.Bff/           # Backend-for-Frontend (ASP.NET Core)
│   ├── Controllers/               # Aggregation endpoints
│   ├── Clients/                   # Downstream service clients (typed HttpClient)
│   ├── Hubs/                      # SignalR hubs
│   ├── Program.cs
│   └── Dockerfile                 # Multi-stage build for BFF+Client
├── Maliev.Intranet.Client/        # Blazor WebAssembly Client
│   ├── Pages/                     # Razor components
│   ├── Layout/                    # Main layout/nav
│   └── Services/                  # API proxies
├── Maliev.Intranet.Shared/        # Shared DTOs between BFF and Client
├── Maliev.Intranet.Tests/         # Integration & E2E Tests
│   ├── Integration/
│   └── E2E/
├── Maliev.Intranet.sln
└── nuget.config
```

**Structure Decision**: A standard Blazor Interactive Auto solution structure, adapted to the flat repository convention. The `Bff` project serves the `Client` (WASM) and handles API aggregation. `Shared` contains the DTOs used by both. `Tests` covers the solution.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | | |