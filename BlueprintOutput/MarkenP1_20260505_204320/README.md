# Marken ShipExec Phase 1 — Biological Returns README

## Overview

This Phase 1 blueprint implements a Marken biological returns shipping workflow in ShipExec, centered on the existing Shipping screen and the server-side `PreShip` / `Ship` hooks.

The solution is designed to support:

- Return-label creation for specimen shipments
- Profile-driven defaults and field renaming on the Shipping screen
- Conditional client-side behavior based on Temperature and country
- Authoritative server-side shipping logic for:
  - paperless international invoices
  - biological sample handling
  - dry ice processing
  - service validation and substitution
  - pickup fallback logic if UI automation fails

The implementation intentionally keeps the client side focused on UX and data capture, while the server side remains the source of truth for shipping rules.

---

## 1) Business Requirements Summary

### Business goal
Support Marken’s Phase 1 biological specimen return-label workflow in ShipExec with minimal disruption to the standard Shipping experience.

### Core requirements from the blueprint

#### Company / adapter configuration
- UPS adapter credentials must be configured for Marken:
  - User ID: `MarkenShipExec`
  - Password: `APIapi2023!!`
  - Access key: `ED6F5B10202ADED2`
- CS adapter configuration is reserved for later phases, but the SBR logic already anticipates CS adapter usage for biological sample scenarios.

#### Field mapping and naming
The following business fields are repurposed for the returns workflow:

- `ConsigneeReference` → `Temperature`
  - Valid values:
    - Frozen
    - Ambient
    - Refrigerated
    - Ambient/Refrigerated Combo Box
- `ShipperReference` → `Study Reference Code`
- `MiscReference1` → `Protocol Number`
- `MiscReference2` → `Site Number`
- `MiscReference3` → `Dry Ice Weight (kg)`
- `MiscReference4` → `Biological Sample`

#### UI defaults and hidden fields
The Shipping screen must:
- Rename the `Consignee` tab to `Pickup From`
- Default:
  - Description = `UN3373 Category B Human Sample`
  - Return Delivery = `True`
  - Saturday Delivery = `True`
  - Service = `UPS Express`
  - Terms = `Prepaid`
  - Dry Ice Weight Units = `KG`
  - Weight Unit = `KG` for EU-to-UK use case
- Hide many irrelevant shipping fields, such as:
  - Brokerage billing fields
  - Dimensions / packaging fields
  - Tracking-related fields
  - Ship Date / account / tax ID / residential indicators, etc.
- Hide the Rate button in the custom template
- Add a new checkbox for `Biological Sample`

#### User data mapping
User profile data is used as the source for return-label information:
- User address → pickup-from / consignee address
- `Custom1` → `Protocol Number`
- `Custom2` → `Study Reference Code`
- `Custom3` → `Site Number`

#### Client-side behavior
- Auto-navigate users to the Shipping page after login
- Set package weight based on selected Temperature:
  - Ambient → 3
  - Frozen → 6
  - Refrigerated → 5
  - Ambient/Refrigerated Combo Box → 6
- If Temperature is `Frozen`:
  - Prompt for dry ice weight
  - Store it in `MiscReference3`
- If Temperature is not `Frozen`:
  - Dry ice weight must not be editable
- For non-Canada pickup-from locations:
  - Attempt to auto-associate Pickup
- For Canada:
  - Hide Pickup button

#### Server-side rules
`SBR PreShip` must:
- Apply paperless invoice for international return shipments
- Set export reason to `Medical`
- Apply biological sample packaging flags
- Convert dry ice from KG to LB before adding to shipment weight
- Set dry ice regulations and purpose
- Validate or swap services using rate shopping rules

`SBR Ship` is only a fallback:
- If the UI-based Pickup association does not work, create/associate Pickup server-side

---

## 2) Methodology: How the Blueprint Was Translated into Code

### Translation approach
The implementation was derived by splitting the blueprint into three layers:

1. **Template layer**
   - Handles naming, visibility, layout, and default UI controls
   - Implemented in `shippingTemplate.html`

2. **Client-side business rules (CBR)**
   - Handles user experience, field seeding, and UI interaction
   - Implemented in:
     - `PageLoaded`
     - `NewShipment`
     - `PreShip`

3. **Server-side business rules (SBR)**
   - Enforces shipping integrity and carrier-facing logic
   - Implemented in:
     - `PreShip`
     - `Ship` fallback only

### Design philosophy
The code structure follows a “thin rule hook, thick manager” pattern:
- Hooks remain small and delegate work
- Business logic is centralized in manager/helper classes
- Configuration values are externalized through `BusinessRuleSettings`

This makes the Phase 1 behavior:
- easier to test
- easier to extend
- less fragile when business rules change

---

## 3) Code Flow: How CBR, SBR, and Templates Interact

### End-to-end flow

#### 1. User opens ShipExec
`CBR PageLoaded`
- Automatically routes the user to the Shipping page
- Establishes page-level pickup visibility behavior
- Prepares shared UI logic for later hooks

#### 2. User starts a new return shipment
`CBR NewShipment`
- Copies the logged-in user’s address into the shipment consignee/pickup-from fields
- Populates reference fields from `Custom1`, `Custom2`, and `Custom3`
- Applies return-label defaults

#### 3. User edits the shipment
Template + CBR work together:
- Template renames fields and hides irrelevant controls
- CBR reacts to Temperature and country changes
- When `Frozen` is selected, dry ice input becomes available

#### 4. User prepares to ship
`CBR PreShip`
- Tries to auto-click the Pickup button for non-Canada shipments
- Saves/associates pickup if the UI supports it
- Enforces dry ice field behavior client-side

#### 5. Server-side validation runs
`SBR PreShip`
- Determines domestic vs international status
- Applies paperless invoice rules
- Applies biological sample and dry ice rules
- Performs service validation/swap based on shipping route and configured service symbols

#### 6. Final shipping transaction
`SBR Ship`
- Only used if client-side pickup association fails
- Attempts a fallback pickup association strategy

### Interaction summary
- **Templates** define what the user sees
- **CBR** prepares and guides the user
- **SBR** validates and finalizes business logic

---

## 4) Design Patterns Used

### 1. Manager / delegation pattern
The generated SBR hook code does not embed all logic directly in the hook method. Instead, it delegates to manager classes such as:
- `ReturnShipmentRulesManager`
- `PickupAssociationManager`

This keeps the actual business rules organized and testable.

### 2. Thin-hook architecture
Hooks remain small wrappers around business logic:
- `SoxBusinessRules.PreShip` simply constructs a manager and calls `PreShip(...)`
- `SoxBusinessRules.Ship` simply constructs a manager and calls `Ship(...)`

This is useful because:
- hook entry points stay readable
- business rules can be modified without rewriting hook plumbing

### 3. Configuration-driven behavior
Several rules are not hardcoded; they are read from settings:
- default CS adapter carrier symbol
- domestic service symbols
- international service symbols
- pickup fallback defaults

This makes the implementation environment-friendly and easier to update.

### 4. Defensive null handling
Generated code checks for:
- missing shipment requests
- missing packages
- missing consignee/return address objects
- missing weights
- missing settings

This reduces the risk of runtime failures in live shipping flows.

### 5. Shared client helper reuse
`PageLoaded` exposes shared pickup-button logic through a window-level helper:
- `window.MarkenUpdatePickupButtonVisibility`

That helper can be reused by:
- `NewShipment`
- `PreShip`
- any future UI interactions

---

## 5) File-by-File Breakdown

## `shippingTemplate.html`

### Why it was generated/updated
This is the main UI template for Phase 1. The blueprint requires extensive UI renaming, hiding, and control changes on the Shipping screen.

### What it does
- Renames the `Consignee` tab to `Pickup From`
- Renames `ConsigneeReference` to `Temperature`
- Renames field captions for:
  - `ShipperReference`
  - `MiscReference1`
  - `MiscReference2`
  - `MiscReference3`
  - `MiscReference4`
- Adds a `Biological Sample` checkbox
- Hides the Rate button
- Preserves the existing Angular bindings and shipping layout

### Why it matters
This template makes the standard ShipExec screen match the Marken return-label workflow without requiring a separate application screen.

---

## `SoxBusinessRules` / SBR hook implementation

### `PreShip`
### Why it was generated
This is the authoritative server-side rule hook for the workflow.

### What it does
- Determines whether the shipment is domestic or international
- Applies paperless invoice and export reason rules for international shipments
- Detects the biological sample flag from `MiscReference4`
- Applies `RESTRICTED_ARTICLE_TYPE = 32`
- Reads dry ice weight from `MiscReference3`
- Converts dry ice from KG to LB
- Sets dry ice purpose/regulation
- Adjusts shipment/package weight
- Validates or swaps service based on domestic/international routing

### Important implementation note
The blueprint expects rate shopping / service validation. The generated code is structured to use configurable service symbols and can be extended to perform real carrier rate-shop calls if needed in the runtime environment.

---

## `SoxBusinessRules` / `Ship` fallback

### Why it was generated
The blueprint calls for a fallback path if client-side pickup automation is unreliable.

### What it does
- Provides a backup pickup-association strategy
- Uses configurable pickup defaults
- Preserves normal ship behavior if pickup data is already present or configuration is incomplete

### Important implementation note
This hook is intentionally conservative. It currently prepares for pickup creation/association rather than forcing a replacement of default shipping behavior.

---

## `CBR PageLoaded`

### Why it was generated
This hook ensures the user lands on Shipping immediately and sees the correct return-label UI.

### What it does
- Redirects the browser to the Shipping page if needed
- Sets up shared pickup-button visibility behavior
- Hides the Pickup button when the country is Canada
- Exposes a reusable UI helper for other hooks

---

## `CBR NewShipment`

### Why it was generated
This hook seeds a new return shipment with user profile data.

### What it does
- Copies the logged-in user’s address into the shipment consignee/pickup-from fields
- Maps:
  - `Custom2` → `ShipperReference`
  - `Custom1` → `MiscReference1`
  - `Custom3` → `MiscReference2`
- Applies base defaults such as description and delivery flags
- Refreshes pickup button visibility

---

## `CBR PreShip`

### Why it was generated
This hook prepares the shipment for final submission from the UI side.

### What it does
- Attempts to auto-click Pickup for non-Canada shipments
- Clicks Save if available so pickup association can persist
- Enforces dry ice input rules:
  - editable and prompted when `Frozen`
  - disabled when not `Frozen`
- Clears dry ice value for non-Frozen shipments
- Reuses the shared pickup visibility helper

---

## `ReturnShipmentRulesManager`

### Why it was generated
This class centralizes the server-side business rules so the hook remains thin.

### Responsibilities
- Domestic vs international detection
- Paperless invoice/export reason logic
- Biological sample handling
- Dry ice conversion and packaging updates
- Service validation/swap behavior
- Config lookup for service symbols

### Why it’s useful
This structure makes the logic easier to:
- test independently
- maintain
- extend for Phase 2 and beyond

---

## `PickupAssociationManager`

### Why it was generated
The blueprint requires a fallback pickup association strategy if the UI automation fails.

### Responsibilities
- Encapsulate pickup fallback behavior
- Read pickup defaults from settings
- Preserve default shipping behavior when no intervention is needed

---

## `BusinessRuleSettings` entries

### Why they were generated
The blueprint references configurable service/adapter behavior, and the analysis confirms that these should not be hardcoded.

### Config keys introduced
- `ReturnShipment.CsAdapterCarrierSymbol`
- `ReturnShipment.DomesticNdaEarlyAmServiceSymbol`
- `ReturnShipment.DomesticNdaFallbackServiceSymbol`
- `ReturnShipment.InternationalUpsExpressServiceSymbol`
- `ReturnShipment.InternationalUpsSaverFallbackServiceSymbol`
- `ReturnShipment.PickupDefaultCarrierSymbol`
- `ReturnShipment.PickupDefaultType`

### Why these matter
They allow system administrators to adjust:
- carrier/service mapping
- fallback behaviors
- pickup defaults

without changing code.

---

## 6) Testing Recommendations

## Functional test scenarios

### Domestic return shipment
Validate:
- user lands on Shipping page
- pickup-from address defaults from user profile
- Temperature field renaming works
- Pickup button is hidden for Canada
- service validation behaves correctly for US-to-US
- no dry ice is applied unless Frozen is selected

### International return shipment
Validate:
- pickup button appears for non-Canada addresses
- Pickup auto-association is attempted
- paperless invoice is enabled
- export reason is set to `Medical`
- service changes to fallback when needed

### Frozen biological shipment
Validate:
- Temperature = Frozen reveals dry ice input
- dry ice value is captured in `MiscReference3`
- dry ice is converted from KG to LB on the server
- package weight increases by dry ice amount
- `RESTRICTED_ARTICLE_TYPE = 32` is set

### Non-Frozen shipment
Validate:
- dry ice field is disabled/non-editable
- `MiscReference3` is cleared
- no dry ice weight is sent to SBR

### Pickup fallback scenario
Validate:
- if CBR pickup automation fails, SBR Ship fallback can still associate or create pickup data
- shipment completes without manual intervention when fallback config is available

---

## Recommended test types
- Unit tests for manager classes
- Hook execution tests for `PreShip` and `Ship`
- UI smoke tests for template bindings
- End-to-end tests for the full return-label workflow

---

## 7) Deployment Guidance

### Recommended deployment order
1. Deploy `shippingTemplate.html`
2. Deploy CBR hooks
3. Deploy SBR hooks and helper classes
4. Configure BusinessRuleSettings
5. Run end-to-end validation in a non-production environment

### Environment setup checklist
- UPS adapter credentials loaded correctly
- Carrier/service symbol settings populated
- Profile field options aligned with template field names
- User custom fields mapped correctly
- Pickup fallback settings only enabled if needed

### Production readiness checks
- Verify that international return labels generate correctly
- Confirm dry ice weight conversions are accurate
- Confirm no hidden field is unexpectedly required by the UI
- Confirm service substitution works across real carrier responses

---

## 8) Future Enhancements

### Phase 2 / follow-up items
- CS adapter configuration and WorldEase / POE integration
- More explicit adapter selection logic for biological sample shipments
- Stronger pickup object creation support in SBR Ship
- Expanded validation for profile-specific use cases:
  - US-to-US
  - EU-to-UK
- Better UI feedback for dry ice entry and pickup status
- More granular service fallback diagnostics

### Nice-to-have improvements
- Centralized constants for field captions and settings keys
- Better error messaging for invalid dry ice input
- Automated logging for pickup-association failures
- Dedicated helper for country / route classification

---

## 9) Caveats and Manual Steps

### Caveats
- The blueprint refers to both Canada/US and EU/UK profile behavior; these rules should be verified against actual profile data to avoid unintended overlap.
- Dry ice weight is entered in KG in the UI, but must be converted to LB before adding to weight in SBR.
- Client-side pickup automation may not be reliable in all environments; the SBR Ship fallback should be treated as an insurance policy.
- Rate shopping/service validation depends on carrier service availability and correct configuration.

### Manual steps likely needed
- Load real profile field options for renamed/hidden/defaulted fields
- Confirm the `Temperature` validation list values in the profile
- Populate BusinessRuleSettings with the correct service symbols
- Verify user spreadsheet address fields are mapped correctly
- Confirm which profile is active for each phase 1 use case

---

## Summary

This implementation turns the Blueprint Phase 1 requirements into a layered ShipExec solution:

- **Templates** shape the Shipping screen for Marken returns
- **CBR hooks** prefill and guide the user through the return-label workflow
- **SBR hooks** enforce the business rules that matter for carrier compliance and shipping integrity
- **Managers and settings** keep the code maintainable and configurable

If you want, I can also generate:
- a compact architecture diagram in markdown
- a deployment checklist
- or a developer-focused “how to modify this solution” section