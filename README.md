# Maliev Intranet Portal

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.Intranet)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Framework](https://img.shields.io/badge/UI-Blazor%20Auto-purple)](https://learn.microsoft.com/en-us/aspnet/core/blazor/)

The unified internal operations interface for MALIEV employees. It acts as an orchestrated UI layer (BFF) over the platform's backend microservices, providing a centralized dashboard for managing customers, orders, accounting, and supply chains.

**Role in MALIEV Architecture**: The primary "Internal Operating System". It does not own core business data but aggregates state from domain services to provide a high-productivity, role-aware management experience.

---

## 🏗️ Architecture & Tech Stack

- **Frontend**: Blazor Interactive Auto (Server + WebAssembly)
- **BFF Layer**: ASP.NET Core 10.0 (C# 13) acting as a secure gateway
- **UI Library**: MudBlazor (Material Design components)
- **Real-time**: SignalR for live dashboard metrics and notifications
- **Authentication**: Google Workspace SSO + Maliev IAM (JWT)
- **Observability**: OpenTelemetry (OTLP) with server-side aggregation

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only (found in `Extensions/`).
- ❌ **FluentValidation**: Standard Data Annotations only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers**.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public members.
- ✅ **BFF Pattern**: Client-side WASM never calls domain services directly; all requests route through the Intranet BFF.
- ✅ **IAM Integration**: Permissions mapped via GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Role-Aware Dashboard**: Dynamic widgets for Sales, Accounting, and Operations based on IAM permissions.
- **Unified Management**: Single interface for Customers, Orders, Quotations, Suppliers, and Invoices.
- **Google SSO**: Secure corporate login restricted to the `@maliev.com` domain.
- **Live Notifications**: Real-time operational alerts powered by SignalR.
- **Identity Propagation**: Transparently forwards user context (JWT) to all downstream microservices.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for Testcontainers)
- Access to `Maliev.Aspire.AppHost` (for shared secrets during development)

### Local Development Setup

1. **Configure Shared Secrets**  
   The Intranet BFF relies on `sharedsecrets.json` located in the `Maliev.Aspire.AppHost` directory for Google OAuth credentials.
   
2. **Configure Environment**  
   Ensure the BFF knows where the domain services are:
   ```powershell
   $env:Services__AuthService__BaseUrl="http://localhost:5000"
   $env:Services__CustomerService__BaseUrl="http://localhost:5001"
   # ... etc
   ```

3. **Run the Application**  
   ```bash
   dotnet run --project Maliev.Intranet.Bff
   ```
   The application will be available at `https://localhost:7000`.

---

## 📡 Integrated Services

The Intranet portal orchestrates data from the following domains:

| Service | Responsibility |
|---------|----------------|
| **IAM/Auth** | Authentication & Permissions |
| **Customer** | CRM & Contact Management |
| **Order/Quotation** | Sales Pipeline & Fullfillment |
| **Accounting** | Invoices & Payments |
| **Material/Supplier** | Procurement & Inventory |
| **Employee** | Staff Directory & Roles |

---

## 🧪 Testing

```bash
# Run Blazor component tests and BFF integration tests
dotnet test
```

- **Integration Tests**: Use `Testcontainers` to verify BFF-to-Service communication.
- **UI Tests**: Verify role-based navigation and component rendering.

---

## 📄 License

Proprietary - © 2026 MALIEV Co., Ltd. All rights reserved.
