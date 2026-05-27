# Quick Scan Mobile Scanner Design

Date: 2026-05-27
Repo: `B:\maliev\Maliev.Intranet`

## Goal

MALIEV Intranet should make QR/barcode scanning always reachable on tablet and mobile layouts. Shop-floor users should be able to scan printed labels for job tickets, delivery notes, materials, inventory items, and similar operational identifiers without hunting for the page-specific form.

The first version is intentionally conservative: scanning resolves the payload, shows the target, and navigates or prepares the next action. It must not silently update production, inventory, delivery, or material state. Every mutation needs an explicit user confirmation after the scan is resolved.

## User Experience

The existing responsive shell already moves `TopBar` to a fixed bottom bar at tablet/mobile widths. Quick Scan belongs there as a persistent action using the `QrCodeScanner` icon and a short label when space allows. It should be visible at the same `max-width: 1280px` breakpoint as the bottom navigation shell and hidden from the desktop primary navigation.

When tapped, Quick Scan opens a focused MudBlazor dialog:

- Camera preview starts only after the user taps the action.
- The tap unlocks Web Audio so success/error feedback can play without autoplay issues.
- Successful decode triggers a short generated beep and optional vibration.
- Duplicate reads are debounced so one printed code does not fire repeatedly while the camera stays pointed at it.
- Users can close the scanner without losing the current page.
- If camera permission is denied, unsupported, or unavailable, the dialog shows a manual paste field with the same resolver behavior.

The result view should show the resolved type, primary identifier, and the safest next action. Examples:

- Inventory item: show tracking code, material, location, remaining quantity, and actions such as open material or select item in production workflow.
- Job ticket: show job number, project/order context, current status, and action to open the production/job context.
- Delivery note: show delivery note number, current status, customer, and action to open delivery detail.
- Unknown payload: show that the code was not recognized and offer manual search or copy.

## Architecture

### UI components

- `QuickScanButton` lives in `Maliev.Intranet.Client/Layout` because it is shell-specific and is rendered from `TopBar`.
- `QuickScanDialog` lives in `Maliev.Intranet.Client/Components/Shared` and owns the scanner state, manual fallback, result state, and confirmation prompts.
- The dialog should use MudBlazor primitives for buttons, alerts, skeleton/error states, and responsive layout.

### Browser interop

Add a scoped JavaScript module under `Maliev.Intranet.Client/wwwroot/js`, for example `quick-scan.js`.

Responsibilities:

- Start and stop camera streams with `navigator.mediaDevices.getUserMedia`.
- Prefer the browser `BarcodeDetector` API when available.
- Support QR payloads first; linear barcode formats can be added behind the same interop surface.
- Initialize or resume a reusable `AudioContext` on the user tap.
- Play generated success/error tones through Web Audio.
- Trigger `navigator.vibrate(...)` when supported.
- Clean up video tracks when the scanner closes or the component disposes.

If `BarcodeDetector` support is not broad enough for the required devices, add a proven scanner library behind this module rather than spreading scanner-specific code into Razor components.

### Payload resolver

The client should not infer business meaning from URLs and tracking codes beyond basic normalization. Add a BFF resolver endpoint that accepts one scanned payload and returns a typed result:

`POST /api/v1/quick-scan/resolve`

Request:

- `payload`: the raw scanned string.

Response shape:

- `kind`: `InventoryItem`, `Material`, `JobTicket`, `DeliveryNote`, `Project`, `Unknown`.
- `displayTitle`: primary text shown to the user.
- `displaySubtitle`: supporting context.
- `navigateTo`: app-relative URL when the safest next step is navigation.
- `actions`: optional explicit follow-up actions that require confirmation before calling any mutation endpoint.
- `error`: friendly message when unresolved or unauthorized.

The BFF resolver must verify downstream DTOs/contracts first for each supported domain:

- Inventory/material item codes through `InventoryController` and `InventoryServiceClient`.
- Job ticket QR URLs through `JobsController` and `JobServiceClient`.
- Delivery note identifiers through `DeliveryNotesController` and `DeliveryServiceClient`.

The resolver should accept full MALIEV URLs, app-relative URLs, and short printed codes when the downstream service already supports them. It should normalize but not trust unvalidated identifiers.

### State-changing actions

Version 1 does not auto-mutate on scan. State changes are explicit, separate buttons after a resolved result:

- `Mark delivered`
- `Record material use`
- `Update job status`
- `Receive/consume stock`

Each action must show the target entity and intended change before submitting. The scanner result may prefill existing page workflows, but the user must still confirm.

## Permissions And Security

The resolver endpoint must require authentication and appropriate read permissions. Action endpoints must use the existing domain-specific `[RequirePermission]` rules and should not introduce service-account routes for user-driven scan actions.

The scanner should never send camera frames to the server. Only decoded text is sent to the BFF. Unknown payloads should be treated as untrusted input and displayed safely without raw HTML rendering.

## Error Handling

Camera failures should be actionable and non-blocking:

- Permission denied: explain that camera access is needed and keep manual paste available.
- Unsupported scanner API: keep manual paste available and optionally guide the user to a supported browser/device.
- No match: show a friendly unresolved state and avoid error snackbars for normal misreads.
- Unauthorized match: show a permission-aware message without leaking hidden entity details.

Beep/vibration should only fire for successful decode/resolution states. Error tones can be added once users have a clear need for them.

## Testing

Add focused tests before implementation is committed:

- Source/component test that `TopBar` renders Quick Scan for the mobile/tablet shell and keeps it out of desktop primary navigation.
- Component test for scanner dialog states: ready, camera unavailable, manual payload, resolved result, and unknown result.
- BFF controller tests for resolver payloads and permission behavior.
- Existing production material scan tests should continue to pass; the new flow can reuse its lookup behavior but must not break manual scan/paste.
- Build verification with `dotnet build Maliev.Intranet.slnx`.

Browser-visible verification is required for UI completion:

- Tablet/mobile viewport shows Quick Scan persistently in the bottom bar.
- Scanner dialog fits without overlapping the bottom shell or safe-area inset.
- Manual fallback works on desktop and when camera support is unavailable.

## Initial Implementation Scope

Implement the persistent entry point, scanner dialog, JS lifecycle wrapper, and resolver contract first. Wire these resolver targets in the first pass:

1. Inventory item/material item QR payloads already used by production material traceability.
2. Job ticket QR URLs exposed by existing job QR DTOs.
3. Delivery note identifiers/URLs.

Defer bulk scanning, offline queues, immediate mutation shortcuts, and advanced action automation until v1 has been validated with real printed labels.
