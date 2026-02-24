# Quickstart: Maliev.Intranet

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop (or equivalent) for Testcontainers
- Access to Maliev NuGet feed (configured in `nuget.config`)

## Running Locally

1. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

2. **Run the Solution**
   The solution includes the BFF (Server) and Client (WASM).
   ```bash
   dotnet run --project Maliev.Intranet.Bff
   ```
   This will start the BFF at `https://localhost:7xxx` and serve the Blazor client.

3. **Running Tests**
   ```bash
   dotnet test
   ```
   Integration tests use Testcontainers to spin up Redis and WireMock for downstream services.

## Project Structure

- **Maliev.Intranet.Bff**: Server-side ASP.NET Core project. Hosts the API and the WASM client.
- **Maliev.Intranet.Client**: Blazor WebAssembly project. Contains UI components.
- **Maliev.Intranet.Shared**: DTOs shared between Server and Client.

## Key Commands

- **Add Migration** (if local DB state used, currently none):
  `dotnet ef migrations add <Name> --project ...`
- **Update Contracts**:
  Edit `contracts/openapi.yaml` then update the C# models in `Shared`.
