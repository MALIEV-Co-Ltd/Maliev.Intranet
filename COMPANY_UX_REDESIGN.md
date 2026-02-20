# Company Information UX Redesign

## Overview
Complete redesign of company creation/linking flow with BDEX integration and enhanced data storage.

## Key Changes

### 1. Remove "Has Company" Toggle
- Company section **always visible** on customer creation page
- Determined by Tax ID field state:
  - **Empty** → No company
  - **Filled** → Company creation/linking triggered

### 2. Visual States

#### State 1: Empty (Initial)
```
┌─────────────────────────────────────────────────────┐
│ 📋 COMPANY INFORMATION (Optional)                   │
├─────────────────────────────────────────────────────┤
│ Tax ID / Juristic ID                                │
│ ┌─────────────────────────────────┬──────────────┐ │
│ │ 0105500002375                   │ [🔍 Search]  │ │
│ └─────────────────────────────────┴──────────────┘ │
│                                                     │
│ ℹ️  Enter 13-digit Thai Tax ID to verify company   │
│    data from Department of Business Development    │
└─────────────────────────────────────────────────────┘
```

#### State 2: Verified (BDEX Lookup Success)
```
┌─────────────────────────────────────────────────────┐
│ 📋 COMPANY INFORMATION                               │
│ ✅ Verified from DBD Registry on Dec 15, 2025      │
├─────────────────────────────────────────────────────┤
│ Tax ID: 0105500002375                               │
│                                                     │
│ Company Name (Thai) 🔒                              │
│ บริษัท มาลีฟ จำกัด                                 │
│                                                     │
│ Full Name (Thai) 🔒                                 │
│ บริษัท มาลีฟ จำกัด (มหาชน)                        │
│                                                     │
│ Registration Date: Jan 15, 2020                     │
│ Status: Active (ดำเนินกิจการปกติ)                  │
│ Business Type: Limited Company                      │
│                                                     │
│ Business Objectives (First 200 chars):              │
│ ให้บริการพัฒนาซอฟต์แวร์; ให้บริการที่ปรึกษา...    │
│                                                     │
│ [Show Full Details] [🔄 Re-verify]                 │
└─────────────────────────────────────────────────────┘
```

#### State 3: Manual Entry (User Skips Verification)
```
┌─────────────────────────────────────────────────────┐
│ 📋 COMPANY INFORMATION                               │
│ ⚠️  Manual Entry - Data Not Verified               │
├─────────────────────────────────────────────────────┤
│ Tax ID: 0105500002375    [🔍 Verify Now]           │
│                                                     │
│ Company Name *                                      │
│ [Acme Corp Thailand                            ]    │
│                                                     │
│ ⚠️  We recommend verifying company data from DBD    │
│    registry to ensure accuracy                      │
│                                                     │
│ Company Phone                                       │
│ [+66 2 123-4567                                ]    │
│                                                     │
│ ... other fields ...                                │
└─────────────────────────────────────────────────────┘
```

### 3. Field Behavior

| Field | Empty State | Verified State | Manual State |
|-------|-------------|----------------|--------------|
| Tax ID | Editable | Read-only | Read-only |
| Company Name | Hidden | Read-only | Editable* |
| Full Name (Thai) | Hidden | Read-only | Hidden |
| Registration Date | Hidden | Read-only | Hidden |
| Status | Hidden | Read-only | Hidden |
| Business Type | Hidden | Read-only | Hidden |
| Contact Phone | Hidden | Editable | Editable |
| Contact Email | Hidden | Editable | Editable |
| Segment | Hidden | Editable | Editable |
| Tier | Hidden | Editable | Editable |

*Required field

### 4. User Flow

#### Scenario A: BDEX Verification (Recommended)
1. User enters 13-digit Tax ID
2. User clicks "Search" button
3. System queries BDEX API
4. **Success**: Auto-fill all fields, lock core fields, show ✅ badge
5. User can edit non-core fields (phone, email, segment, tier)
6. Submit creates company with `isVerifiedFromBdex = true`

#### Scenario B: Manual Entry (Fallback)
1. User enters Tax ID
2. User fills company name manually (bypasses search)
3. System shows ⚠️ warning badge
4. User can fill all fields manually
5. Submit creates company with `isVerifiedFromBdex = false`

#### Scenario C: No Company
1. User leaves Tax ID empty
2. Company section collapses or becomes minimal
3. Customer created without company linkage

### 5. Data Enrichment from BDEX

Store these additional fields when BDEX lookup succeeds:

```json
{
  "name": "มาลีฟ",
  "vatNumber": "0105500002375",
  "fullNameTh": "บริษัท มาลีฟ จำกัด (มหาชน)",
  "registrationDate": "2020-01-15T00:00:00Z",
  "companyStatus": "1",
  "companyStatusNameTh": "ดำเนินกิจการปกติ",
  "companyTypeCode": "5",
  "businessObjectives": "ให้บริการพัฒนาซอฟต์แวร์; ให้บริการที่ปรึกษา; ...",
  "isVerifiedFromBdex": true,
  "bdexVerificationDate": "2025-12-15T10:30:00Z",
  "stockSymbol": "MLEV",
  "segment": "Enterprise",
  "tier": "Gold"
}
```

### 6. Customer Details Page Enhancement

Add "Company" tab/section with ability to:

#### When Customer Has No Company:
```
┌─────────────────────────────────────────────────────┐
│ This customer is not linked to any company          │
│                                                     │
│ [+ Link Existing Company] [+ Create & Link Company]│
└─────────────────────────────────────────────────────┘
```

#### When Customer Has Company:
```
┌─────────────────────────────────────────────────────┐
│ 📋 COMPANY DETAILS                                   │
│ ✅ Verified from DBD Registry                       │
├─────────────────────────────────────────────────────┤
│ Company Name: บริษัท มาลีฟ จำกัด                   │
│ Tax ID: 0105500002375                               │
│ Status: Active                                      │
│ Registration Date: Jan 15, 2020                     │
│                                                     │
│ [View Full Details] [🔄 Re-verify] [Unlink]        │
└─────────────────────────────────────────────────────┘
```

### 7. Benefits

✅ **Data Accuracy**: Direct verification from official government registry
✅ **User Efficiency**: Auto-fill eliminates manual data entry
✅ **Data Richness**: Store 10+ additional fields for analytics
✅ **Flexibility**: Manual entry still possible as fallback
✅ **Transparency**: Clear visual indicators of data source
✅ **Audit Trail**: Track when/if company was verified

## Implementation Priority

1. ✅ **Phase 1**: Database schema updates (Done)
2. ✅ **Phase 2**: DTO/Model updates (Done)
3. 🔄 **Phase 3**: CustomerNew.razor UX redesign (In Progress)
4. ⏳ **Phase 4**: CustomerDetails.razor company linking
5. ⏳ **Phase 5**: Company management APIs
6. ⏳ **Phase 6**: Re-verification functionality
