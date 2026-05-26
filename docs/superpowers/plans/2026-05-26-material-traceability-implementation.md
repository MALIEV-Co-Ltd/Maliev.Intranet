# Material Traceability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build QR-tracked physical material inventory so every block, spool, sheet, bottle, or piece can be labeled, scanned, assigned to a job, consumed, and audited.

**Architecture:** InventoryService becomes the source of truth for physical stock items while preserving the existing batch API. Intranet consumes the new item API through the BFF and renders QR labels, supplier matching, and job assignment workflows. SupplierService already owns supplier status transitions; Intranet must expose those transitions with row-version-safe calls.

**Tech Stack:** .NET 10, EF Core 10, PostgreSQL migrations, ASP.NET Core API controllers, Blazor WASM, MudBlazor, bUnit/source tests, xUnit.

---

## Files

- Modify `B:\maliev\Maliev.InventoryService\Maliev.InventoryService.Domain\Entities\InventoryBatch.cs`: add physical item fields.
- Create `B:\maliev\Maliev.InventoryService\Maliev.InventoryService.Domain\Entities\InventoryConsumptionEvent.cs`: audit exact stock consumption.
- Modify `B:\maliev\Maliev.InventoryService\Maliev.InventoryService.Infrastructure\Persistence\InventoryDbContext.cs`: map new columns and event table.
- Modify `B:\maliev\Maliev.InventoryService\Maliev.InventoryService.Application\Abstractions\IInventoryService.cs`: expose item list/detail/create/consume methods.
- Create item request/result models under `B:\maliev\Maliev.InventoryService\Maliev.InventoryService.Application\Models`.
- Modify `B:\maliev\Maliev.InventoryService\Maliev.InventoryService.Api\Controllers\InventoryController.cs`: add `/items`, `/items/{trackingCode}`, and `/items/{trackingCode}/consume`.
- Create migration under `B:\maliev\Maliev.InventoryService\Maliev.InventoryService.Infrastructure\Migrations`.
- Modify InventoryService tests for new item behavior.
- Modify `B:\maliev\Maliev.Intranet\Maliev.Intranet.Shared\Dtos\InventoryDtos.cs`: add item, QR label, supplier status, and consumption request DTOs.
- Modify `B:\maliev\Maliev.Intranet\Maliev.Intranet.Bff\Clients\InventoryServiceClient.cs` and `Controllers\InventoryController.cs`: proxy item endpoints.
- Modify `B:\maliev\Maliev.Intranet\Maliev.Intranet.Bff\Clients\SupplierServiceClient.cs` and `Controllers\SuppliersController.cs`: add supplier status update and capability filter support.
- Modify `B:\maliev\Maliev.Intranet\Maliev.Intranet.Bff\Clients\MaterialServiceClient.cs`: enrich material detail suppliers by matching SupplierService capabilities.
- Modify `B:\maliev\Maliev.Intranet\Maliev.Intranet.Client\Pages\Manufacturing\MaterialDetail.razor` and `.css`: show QR item labels, item receive form, stock items, and supplier matches.
- Modify `B:\maliev\Maliev.Intranet\Maliev.Intranet.Client\Pages\Purchasing\SupplierDetail.razor` and `.css`: add status transition controls.
- Modify `B:\maliev\Maliev.Intranet\Maliev.Intranet.Client\Pages\Manufacturing\ProductionSchedule.razor` and shared job DTOs: allow exact inventory item assignment/consumption.
- Update tests in `B:\maliev\Maliev.Intranet\Maliev.Intranet.Tests`.

## Task 1: InventoryService Physical Stock Items

- [ ] **Step 1: Write failing service tests**

Add tests asserting that `CreateInventoryItemAsync` generates a short tracking code, QR payload, dimensions, quantity unit, and audit-ready item response.

Run:

```powershell
dotnet test Maliev.InventoryService.slnx --filter "FullyQualifiedName~InventoryServiceTests" --artifacts-path B:\maliev\.codex-test-out\inventory-item-red --verbosity normal
```

Expected: build fails because item models and methods do not exist.

- [ ] **Step 2: Implement domain and service methods**

Add item fields to `InventoryBatch`, add `InventoryConsumptionEvent`, add application models, map with EF Core, generate tracking codes in the format `INV-YY-000001`, and generate QR payloads as `/mfg/inventory/items/{trackingCode}`.

- [ ] **Step 3: Add API contract tests and controller endpoints**

Add controller tests for:

- `POST /inventory/v1/stock/items`
- `GET /inventory/v1/stock/items?materialId=...`
- `GET /inventory/v1/stock/items/{trackingCode}`
- `POST /inventory/v1/stock/items/{trackingCode}/consume`

- [ ] **Step 4: Generate migration**

Run:

```powershell
dotnet ef migrations add AddPhysicalInventoryItems --project Maliev.InventoryService.Infrastructure --startup-project Maliev.InventoryService.Api --output-dir Migrations
```

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test Maliev.InventoryService.slnx --filter "FullyQualifiedName~InventoryServiceTests|FullyQualifiedName~InventoryControllerTests" --artifacts-path B:\maliev\.codex-test-out\inventory-item-test --verbosity normal
dotnet build Maliev.InventoryService.slnx --configuration Release --artifacts-path B:\maliev\.codex-test-out\inventory-item-build
```

Commit from `B:\maliev\Maliev.InventoryService`:

```powershell
git add Maliev.InventoryService.Domain Maliev.InventoryService.Application Maliev.InventoryService.Api Maliev.InventoryService.Infrastructure Maliev.InventoryService.Tests
git commit -m "Add physical inventory item tracking"
```

## Task 2: Intranet BFF Inventory Proxy

- [ ] **Step 1: Write failing BFF tests**

Extend `InventoryController` tests to assert item creation/list/detail/consume routes forward to InventoryService.

- [ ] **Step 2: Add shared DTOs and BFF client methods**

Add `CreateInventoryItemRequest`, `InventoryItemDto`, `InventoryItemConsumptionRequest`, and route methods.

- [ ] **Step 3: Verify and commit**

Run:

```powershell
dotnet test Maliev.Intranet.slnx --filter "FullyQualifiedName~InventoryControllerTests" --artifacts-path B:\maliev\.codex-test-out\intranet-inventory-bff-test --verbosity normal
dotnet build Maliev.Intranet.slnx --configuration Release --artifacts-path B:\maliev\.codex-test-out\intranet-inventory-bff-build
```

Commit from `B:\maliev\Maliev.Intranet`:

```powershell
git add Maliev.Intranet.Shared/Dtos/InventoryDtos.cs Maliev.Intranet.Bff/Clients/InventoryServiceClient.cs Maliev.Intranet.Bff/Controllers/InventoryController.cs Maliev.Intranet.Tests
git commit -m "Proxy physical inventory item endpoints"
```

## Task 3: Material Detail QR Workflow

- [ ] **Step 1: Write failing source tests**

Assert `MaterialDetail.razor` contains QR item labels, form-factor selectors, 50 x 30 mm print styles, and no Code39 barcode dependency.

- [ ] **Step 2: Replace barcode labels with QR labels**

Render a QR label component for material-level and item-level labels. Use short tracking code as visible backup text and `qrPayload` as the encoded value.

- [ ] **Step 3: Add item receive form**

Support form factors: block, spool, sheet, rod, bottle, bag, piece, other. Show dimension inputs only where relevant and submit to `api/v1/inventory/items`.

- [ ] **Step 4: Show inventory items**

Load `api/v1/inventory/items?materialId={id}` and show status, location, quantity, dimensions, and print-label action.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test Maliev.Intranet.slnx --filter "FullyQualifiedName~ModuleRegressionSourceTests" --artifacts-path B:\maliev\.codex-test-out\intranet-material-ui-test --verbosity normal
dotnet build Maliev.Intranet.slnx --configuration Release --artifacts-path B:\maliev\.codex-test-out\intranet-material-ui-build
```

Commit:

```powershell
git add Maliev.Intranet.Client/Pages/Manufacturing/MaterialDetail.razor Maliev.Intranet.Client/Pages/Manufacturing/MaterialDetail.razor.css Maliev.Intranet.Tests
git commit -m "Add QR material stock item workflow"
```

## Task 4: Supplier Matching and Status Transitions

- [ ] **Step 1: Write failing tests**

Assert supplier capability filters are forwarded and SupplierDetail has status controls for pending approval to active.

- [ ] **Step 2: Add BFF status transition route**

`PATCH api/v1/suppliers/{id}/status` forwards status, reason, and row version to SupplierService.

- [ ] **Step 3: Add SupplierDetail controls**

Render a status dropdown/action row, disable while saving, refresh supplier detail after a successful transition, and show errors from downstream body.

- [ ] **Step 4: Enrich material supplier section**

Material detail should list explicit material supplier if available plus capability-matched suppliers from SupplierService.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test Maliev.Intranet.slnx --filter "FullyQualifiedName~SupplierServiceClientTests|FullyQualifiedName~ModuleRegressionSourceTests" --artifacts-path B:\maliev\.codex-test-out\intranet-supplier-workflow-test --verbosity normal
dotnet build Maliev.Intranet.slnx --configuration Release --artifacts-path B:\maliev\.codex-test-out\intranet-supplier-workflow-build
```

Commit:

```powershell
git add Maliev.Intranet.Bff/Clients/SupplierServiceClient.cs Maliev.Intranet.Bff/Controllers/SuppliersController.cs Maliev.Intranet.Bff/Clients/MaterialServiceClient.cs Maliev.Intranet.Client/Pages/Purchasing/SupplierDetail.razor Maliev.Intranet.Client/Pages/Purchasing/SupplierDetail.razor.css Maliev.Intranet.Tests
git commit -m "Add supplier material readiness controls"
```

## Task 5: Job Material Assignment

- [ ] **Step 1: Write failing job detail tests**

Assert the production job detail supports exact inventory item lookup by tracking code and expected finish time remains visible.

- [ ] **Step 2: Add scan/select flow**

In the production job detail drawer, allow an employee to paste/scan a QR payload or tracking code, resolve it through `api/v1/inventory/items/{trackingCode}`, validate material compatibility, and submit consumption/reservation.

- [ ] **Step 3: Verify and commit**

Run:

```powershell
dotnet test Maliev.Intranet.slnx --filter "FullyQualifiedName~ProductionSchedulePageTests|FullyQualifiedName~JobsControllerTests" --artifacts-path B:\maliev\.codex-test-out\intranet-job-material-test --verbosity normal
dotnet build Maliev.Intranet.slnx --configuration Release --artifacts-path B:\maliev\.codex-test-out\intranet-job-material-build
```

Commit:

```powershell
git add Maliev.Intranet.Client/Pages/Manufacturing/ProductionSchedule.razor Maliev.Intranet.Client/Pages/Manufacturing/ProductionSchedule.razor.css Maliev.Intranet.Shared/Dtos/JobDtos.cs Maliev.Intranet.Tests
git commit -m "Assign physical inventory items to jobs"
```

## Task 6: Browser Verification

- [ ] **Step 1: Restart affected Aspire resources**

Use Aspire resource restart for InventoryService, SupplierService, JobService, and Intranet BFF if running.

- [ ] **Step 2: Verify pages**

Open and check:

- `/mfg/materials/{id}` for QR labels, stock item list, supplier section.
- `/purchasing/suppliers/{id}` for status transition controls.
- `/sales/projects/...` production schedule job detail for material assignment.

- [ ] **Step 3: Final status**

Report commits, verification commands, and any remaining cross-service work.
