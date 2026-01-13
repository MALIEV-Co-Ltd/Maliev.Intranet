# Research & Decisions: Create Intranet Portal

**Feature**: `001-create-intranet-portal`
**Date**: 2025-01-07

## 1. Frontend Framework

- **Decision**: **Blazor (Interactive Auto)**
- **Rationale**:
  - **Single Language**: Allows the team to use C# across the entire stack (Backend + Frontend), sharing DTOs and validation logic via `Maliev.Intranet.Shared`.
  - **Performance**: Interactive Auto renders initially on the server (fast First Paint) and seamlessly transitions to WebAssembly (low latency interactivity) in the background.
  - **Productivity**: Strong typing and compile-time checks reduce bugs compared to JS frameworks.
- **Alternatives Considered**:
  - *React/Angular*: Rejected to minimize context switching between languages and leverage existing .NET expertise.
  - *Blazor Server*: Rejected due to latency sensitivity and server resource cost for high user counts.
  - *Blazor WASM (Standalone)*: Rejected due to slower initial load times.

## 2. Architecture Pattern

- **Decision**: **Backend-for-Frontend (BFF)**
- **Rationale**:
  - **Security**: Tokens and secrets are kept server-side in the BFF; the browser only holds a session cookie or a strictly scoped BFF token.
  - **Performance**: The BFF aggregates multiple downstream calls (Customer, Order, etc.) into coarse-grained responses, reducing browser network chatter.
  - **Protocol Hiding**: The BFF can translate gRPC or AMQP from internal services to simple JSON for the client if needed (though REST is primary here).
- **Alternatives Considered**:
  - *Direct Client Aggregation*: Rejected due to CORS complexity, "chattiness", and exposure of internal microservice topology to the browser.

## 3. Real-time Updates

- **Decision**: **SignalR**
- **Rationale**:
  - **Native Support**: First-class citizen in ASP.NET Core and Blazor.
  - **Operational Requirement**: The "operational dashboard" needs to show alerts and tasks immediately without polling.
- **Alternatives Considered**:
  - *Polling*: Rejected as it adds unnecessary load and latency.
  - *SSE*: SignalR handles fallback and connection management better.

## 4. Observability

- **Decision**: **OpenTelemetry (OTLP)**
- **Rationale**:
  - **Standardization**: Aligns with `Maliev.Aspire.ServiceDefaults` used by all backend services.
  - **End-to-End Tracing**: Allows tracing a click in the Blazor button all the way through the BFF to the database of a downstream microservice.
- **Implementation**: The Blazor client will use the OpenTelemetry JS interop or .NET library to export traces to the BFF or collector.

## 5. Deployment

- **Decision**: **Single Container (BFF serves Client)**
- **Rationale**:
  - **Simplicity**: The BFF project will host the WASM assets. This simplifies deployment to a single Kubernetes service/ingress.
  - **Version Alignment**: Ensures the client and BFF API are always deployed in sync.
