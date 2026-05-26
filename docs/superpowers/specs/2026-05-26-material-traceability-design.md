# Material Traceability Design

Date: 2026-05-26

## Goal

Make material inventory traceable at the physical item level. Each block, spool, sheet, resin bottle, or other stock item must have its own scannable QR label so employees can identify it, assign it to a manufacturing job, consume quantity from it, and audit who used what.

## Current Gaps

- Inventory is displayed as long identifiers that are hard to scan or use on the shop floor.
- Material inventory is batch/weight oriented, but machining stock needs physical piece identity and dimensions.
- Filament spools need per-spool visibility, location, remaining weight, and scan-to-use workflow.
- Material detail can show "no suppliers linked" even when a supplier can supply the material.
- Supplier onboarding can remain pending approval without an obvious Intranet status transition.
- Job detail needs material assignment that links to a managed inventory item, not a free-text material id.

## Domain Split

### MaterialService

Owns the material catalog and manufacturing-facing definitions:

- Material name, category, process compatibility, and technical properties.
- Form-factor templates such as block, spool, sheet, rod, bottle, bag, or generic item.
- Material-specific property definitions used by Intranet receive-stock forms.
- Material catalog links to supplier capabilities by material id, but does not own supplier approval state or physical stock.

### InventoryService

Owns physical stock items and shop-floor traceability:

- One inventory item per physical stock unit.
- QR payload and short human-readable tracking code.
- Physical attributes such as dimensions, weight, count, lot, storage location, and status.
- Consumption events that link inventory item, job, operator, quantity, timestamp, and remaining stock.
- Label data for 50 x 30 mm material/equipment stickers.

### SupplierService

Owns supplier lifecycle and material supply capability:

- Supplier approval/status transitions.
- Supplier material capabilities by material id.
- Supplier SKU, lead time, MOQ, last price, currency, approval state, and preferred flag.
- Intranet status controls for moving suppliers out of pending approval.

### JobService

Owns job assignment details:

- Editable job operator, priority, customer, queue status, and selected material inventory item.
- References the chosen InventoryService stock item and exposes expected finish time.
- Does not duplicate inventory quantities or supplier metadata.

### Intranet

Owns employee workflow:

- Material detail shows stock items as QR-tracked physical inventory.
- Supplier section lists active and pending suppliers that can supply the material.
- Receive-stock form adapts to material type and creates one item per block/spool/piece.
- Label preview and print action generate 50 x 30 mm QR labels.
- Job detail scan flow resolves QR code, validates material compatibility, assigns the exact stock item, and records consumption.

## Inventory Item Model

Add an inventory item concept without breaking existing batch endpoints immediately.

Core fields:

- `id`
- `trackingCode`, for example `INV-26-000184`
- `qrPayload`, for example `/mfg/inventory/items/INV-26-000184`
- `materialId`
- `formFactor`: `Block`, `Spool`, `Sheet`, `Rod`, `Bottle`, `Bag`, `Piece`, `Other`
- `status`: `Available`, `Reserved`, `InUse`, `PartiallyConsumed`, `Depleted`, `Quarantined`, `Lost`
- `location`
- `supplierId`
- `purchaseOrderId`
- `lotNumber`
- `receivedAt`
- `receivedBy`

Quantity fields:

- `initialQuantity`
- `remainingQuantity`
- `quantityUnit`: `g`, `kg`, `mm3`, `pcs`, `ml`, `m`, or service-supported units

Physical fields:

- `lengthMm`
- `widthMm`
- `heightMm`
- `diameterMm`
- `thicknessMm`
- `color`
- `materialGrade`
- `manufacturerSku`

Consumption event fields:

- `inventoryItemId`
- `jobId`
- `orderItemId`
- `operatorId`
- `machineId`
- `quantityConsumed`
- `consumedAt`
- `remainingQuantityAfter`
- `notes`

## QR Label

Use QR as the primary scan target. Text is backup for humans.

Label size: 50 x 30 mm.

Required label content:

- QR code.
- Short tracking code.
- Material name or short material code.
- Key physical attributes, for example `100 x 100 x 50 mm block`.
- Storage location and status.

The QR payload should route to an Intranet scan endpoint or item detail page, not expose an unreadable UUID as the primary visible identifier.

## Material Examples

### Delrin POM Block

Receiving two Delrin blocks creates two separate inventory items:

- `INV-26-000184`: 100 x 100 x 50 mm block.
- `INV-26-000185`: 50 x 50 x 50 mm block.

Each item has its own QR label and can be assigned/consumed independently.

### 3D Printing Spool

Each spool is its own item:

- Material: PLA black.
- Quantity: 1000 g initial, remaining grams updated through job consumption.
- Location: printer rack, cabinet, or assigned printer.
- QR scan shows spool status without walking to the printer.

### Sheet or Plate

Each sheet/plate item stores thickness and dimensions, supports partial consumption, and remains visible until depleted or retired.

## Supplier Workflow

Material detail should show a supplier capability list:

- Supplier name and approval status.
- Supplier SKU.
- Lead time.
- MOQ.
- Last quoted price and currency.
- Preferred supplier marker.
- Link to supplier detail.

Supplier detail and supplier list must expose status transitions for authorized employees:

- Pending approval to active.
- Active to inactive, blocked, or suspended.
- Rejected or blocked states where supported by SupplierService.

Status change actions should disable during save and show the new state after the request completes.

## Job Scan Workflow

1. Employee opens job detail or production schedule detail.
2. Employee clicks scan/assign material.
3. QR code resolves to an InventoryService item by tracking code.
4. Intranet validates that the stock material matches the job material requirement.
5. Employee confirms quantity consumed or reserved.
6. InventoryService records a consumption event and updates remaining quantity/status.
7. Job detail shows the linked material item with navigation to the inventory item page.

## Implementation Order

1. Add the InventoryService physical inventory item contract and tests.
2. Add supplier material capability/status UI and BFF coverage.
3. Update Intranet material detail with QR labels, receive-stock item forms, and stock item list.
4. Update production job detail material assignment to scan/select inventory items.
5. Add label print CSS and browser verification for 50 x 30 mm output.

## Open Decision

Confirm that InventoryService should be the source of truth for each physical stock item, including QR payload, location, status, and consumption history. MaterialService remains the catalog, and SupplierService owns suppliers and supply capabilities.
