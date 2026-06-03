# DHL Express Rate Integration — Design Spec

**Date:** 2026-05-28
**Status:** Approved
**Workstream:** 2 of 2 (DHL Integration)

## Problem

Shipping costs are currently entered manually as a decimal field in `QuoteSummaryBar.razor`. There is no integration with any shipping carrier for real-time rate calculation. The business has signed up for DHL Express Commerce and needs the Maliev.Intranet to fetch live shipping rates.

## Solution

Integrate DHL Express MyDHL API (v3.2.2) Rating service via a BFF proxy pattern. Add a "Get DHL Rate" button in `QuoteSummaryBar` that fetches rates based on package weight/dimensions and destination address, auto-populating the ShippingCost field.

### Architecture: BFF Proxy

```
Blazor Client → BFF (ShippingController) → DhlExpressClient → DHL MyDHL API
```

Chosen over a new microservice for:
- Fastest implementation path
- Single-carrier MVP
- Matches existing BFF proxy pattern (`CustomerServiceClient`, `DeliveryServiceClient`, etc.)

## DHL MyDHL API Reference

| Property | Value |
|----------|-------|
| **API** | MyDHL API v3.2.2 |
| **Division** | DHL Express |
| **Base URL (prod)** | `https://express.api.dhl.com/mydhlapi` |
| **Base URL (test)** | `https://express.api.dhl.com/mydhlapi/test` |
| **Auth** | HTTP Basic Auth (credentials from DHL Express consultant) |
| **Rate quota** | 500 calls/day (test environment) |
| **Key endpoint** | `POST /rates` — returns products, rates, estimated delivery |

## Files

### New Files
| File | Description |
|------|-------------|
| `Maliev.Intranet.Bff/Clients/DhlExpressClient.cs` | Typed HttpClient calling DHL MyDHL API Rating endpoint |
| `Maliev.Intranet.Bff/Controllers/ShippingController.cs` | `GET /intranet/v1/shipping/rates` BFF endpoint |
| `Maliev.Intranet.Shared/Dtos/ShippingDtos.cs` | `ShippingRateRequestDto`, `ShippingRateResponseDto`, `ShippingRateOptionDto` |
| `Maliev.Intranet.Client/Services/ShippingService.cs` | Client-side service calling BFF rates endpoint |

### Modified Files
| File | Change |
|------|--------|
| `Maliev.Intranet.Bff/Program.cs` | Register `DhlExpressClient` with `IHttpClientFactory` |
| `Maliev.Intranet.Client/Program.cs` | Register `ShippingService` |
| `Maliev.Intranet.Client/Components/Project/QuoteSummaryBar.razor` | Add "Get DHL Rate" button + rate results display |

## API Contract

### Request
```
GET /intranet/v1/shipping/rates
  ?originCountry=TH
  &destCountry=US
  &destPostalCode=10001
  &weightKg=2.5
  &lengthCm=30
  &widthCm=20
  &heightCm=10
```

### Response
```json
{
  "success": true,
  "data": {
    "rates": [
      {
        "productName": "Express Worldwide",
        "totalPrice": 1250.00,
        "currencyCode": "THB",
        "estimatedDeliveryDate": "2026-06-03"
      }
    ]
  }
}
```

### Permission
`shipping.rates.read` — new GCP-style permission string.

## DTOs

### ShippingRateRequestDto
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `OriginCountryCode` | `string` | Yes | ISO 3166-1 alpha-2, e.g. "TH" |
| `DestinationCountryCode` | `string` | Yes | Destination country |
| `DestinationPostalCode` | `string?` | No | Postal code for accuracy |
| `WeightKg` | `decimal` | Yes | Total package weight |
| `LengthCm` | `decimal?` | No | Package length |
| `WidthCm` | `decimal?` | No | Package width |
| `HeightCm` | `decimal?` | No | Package height |

### ShippingRateResponseDto
| Field | Type | Description |
|-------|------|-------------|
| `Rates` | `List<ShippingRateOptionDto>` | Available shipping options |

### ShippingRateOptionDto
| Field | Type | Description |
|-------|------|-------------|
| `ProductName` | `string` | e.g. "Express Worldwide" |
| `TotalPrice` | `decimal` | Price in THB |
| `CurrencyCode` | `string` | "THB" |
| `EstimatedDeliveryDate` | `string?` | ISO date string |

## DhlExpressClient

- Receives `HttpClient` via primary constructor injection
- Base address configured in `Program.cs`: `https://express.api.dhl.com/mydhlapi`
- Auth header: `Basic <base64(username:password)>` — credentials from configuration
- Maps DHL JSON response to `ShippingRateResponseDto`
- Handles DHL error responses → throws typed exceptions caught by global error middleware
- Respects `CancellationToken`

## Configuration

```json
{
  "DhlExpress": {
    "BaseUrl": "https://express.api.dhl.com/mydhlapi",
    "Username": "",     // From GCP Secret Manager in production
    "Password": "",     // From GCP Secret Manager in production
    "AccountNumber": "" // DHL Express account number
  }
}
```

## UI Integration

### QuoteSummaryBar Changes
- New "Get DHL Rate" button next to Shipping field
- Button disabled when: no shipping address, no parts with weight, loading
- On click: calls `ShippingService.GetRatesAsync()` with:
  - Origin: "TH" (Thailand — configurable later)
  - Destination: resolved from customer shipping address
  - Weight: sum of part weights from project parts
  - Dimensions: from project parts (if available)
- Results shown as a dropdown/list below the button
- Selecting a rate auto-populates `ShippingCost` (in THB)
- Shows loading skeleton while fetching

### Edge Cases
- **No shipping address**: button disabled, tooltip "Add a shipping address first"
- **No weight data**: button disabled, tooltip "Parts need weight information"
- **DHL API error**: snackbar with user-friendly message
- **No rates returned**: "No rates available for this destination" message
- **Currency mismatch**: DHL returns THB — already our base currency, no conversion needed

## Phase 1 Scope (MVP)
- Rate calculation only
- Manual trigger (button click)
- Single DHL Express account
- THB pricing (DHL returns THB for TH-origin shipments)
- No label creation
- No tracking
- No pickup booking
- No landed cost / duty calculation

## Future Phases
- **Phase 2**: Shipment creation (labels), tracking, pickup booking
- **Phase 3**: Multi-carrier abstraction (UPS, FedEx), carrier comparison
- **Phase 4**: Automatic rate fetch on address/part change, landed cost
