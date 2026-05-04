# Marken ShipExec Phase 1 — Biological Returns README

## Overview

This implementation delivers Phase 1 support for Marken biological specimen return shipping in ShipExec. The blueprint’s intent is to transform a standard outbound shipping workflow into a returns-oriented workflow with:

- returns-specific field labels and defaults
- user-profile-driven pickup/return address population
- temperature-driven shipment behavior
- biological sample handling
- dry ice capture and application
- country-based pickup button behavior
- server-side enforcement for shipping rules and fallback pickup handling

The solution is split across:

- **Profile Field Options** for renaming, hiding, and defaulting fields
- **Custom shipping template** updates for the UI
- **Client-side business rules (CBR)** for page behavior and shipment defaults
- **Server-side business rules (SBR)** for authoritative shipping enforcement

---

## 1) Business Requirements Summary

### Core business goal
Enable Marken users to create **biological return labels** from the shipping screen with minimal manual input, while ensuring shipment rules remain valid for different lanes and specimen conditions.

### What the workflow must support
- Treat the shipping screen as a **“Pickup From” / return label workflow**
- Default address and reference data from the logged-in user
- Capture temperature selection:
  - Frozen
  - Ambient
  - Refrigerated
  - Ambient/Refrigerated Combo Box
- Capture biological sample status
- Capture dry ice weight when temperature is Frozen
- Hide irrelevant outbound/commercial fields
- Automatically handle pickup association for non-Canada returns
- Enforce service selection and shipping rules server-side
- Use the **CS Adapter** when biological sample is true
- Apply paperless invoice/export reason rules for international return labels
- Support a server-side backup pickup creation strategy if the client-side pickup association fails

### Key data mapping requirements
From the blueprint:

- **ConsigneeReference** → Temperature
- **ShipperReference** → Study Reference Code
- **MiscReference1** → Protocol Number
- **MiscReference2** → Site Number
- **MiscReference3** → Dry Ice Weight (kg)
- **MiscReference4** → Biological Sample

### Default values required
- Description: `UN3373 Category B Human Sample`
- Dry Ice Weight Units: `KG`
- Return Delivery: `True`
- Saturday Delivery: `True`
- Service: `UPS Express`
- Terms: `Prepaid`
- Weight Unit: `KG` for EU-to-UK scenario

---

## 2) Methodology: How the Blueprint Was Translated into Code

The implementation plan was translated into code using a layered approach:

### A. Separate concerns by execution layer
- **Template**: visual controls and button visibility
- **CBR**: UI behavior and shipment field defaults
- **SBR**: validation and carrier-facing shipment enforcement

This ensures:
- user experience is handled client-side
- business-critical logic is enforced server-side
- layout changes stay minimal and targeted

### B. Use manager classes to centralize business rules
The implementation centers around a shared manager concept, typically something like:

- `ReturnsShippingManager`
- `PickupAssociationManager`
- `ServiceSelectionManager`
- `DryIceManager`

This keeps hooks thin and easier to maintain.

### C. Preserve existing ShipExec architecture
Instead of rewriting the shipping flow, the solution:
- reuses the standard shipment object model
- repurposes existing reference fields
- uses existing hook points
- minimizes template changes

### D. Prioritize authoritative server-side checks
Client-side logic is used for:
- navigation
- defaulting
- convenience automation

Server-side logic is used for:
- service validation
- adapter selection
- dry ice application
- paperless invoice/export rules
- fallback pickup handling

---

## 3) Code Flow: How SBR Hooks, CBR Hooks, and Templates Interact

## High-level flow

1. **User logs in**
2. **CBR PageLoaded** routes the user directly to the shipping page
3. The template displays a returns-oriented shipment UI
4. **CBR NewShipment** pre-populates user address and reference fields
5. User edits shipment details, especially temperature and dry ice fields
6. **CBR PreShip** attempts to associate pickup automatically for non-Canada lanes
7. **SBR PreShip** enforces all shipment rules and final carrier-facing settings
8. If needed, **SBR Ship** acts as a fallback path for pickup creation

---

## Detailed hook interaction

### CBR PageLoaded
Responsible for:
- auto-routing users to the shipping page after login
- detecting country-based UI state
- hiding the Pickup button when the pickup-from country is Canada

This is a UI initialization hook.

### CBR NewShipment
Responsible for:
- setting the consignee from user address data
- mapping custom user fields into shipment references
- defaulting returns-oriented values

This makes a new shipment immediately usable as a return label.

### CBR PreShip
Responsible for:
- attempting to click **Pickup Request**
- saving the shipment so pickup association can persist
- keeping dry ice input editable only when temperature is Frozen

This is a best-effort UI automation hook before server processing.

### SBR PreShip
Responsible for:
- detecting international return movements
- enabling paperless invoice
- setting export reason to Medical
- switching/validating service behavior
- applying biological sample package extras
- applying dry ice logic and weight conversion

This is the authoritative business rule enforcement point.

### SBR Ship
Responsible for:
- fallback pickup object creation if the client-side pickup association strategy fails
- using user custom data and shipment context to populate the pickup object

This is intentionally a backup path only.

---

## 4) Design Patterns Used

## Manager class pattern
A central manager class encapsulates the business logic:

- `ReturnsShippingManager.PreShip(...)`
- `ReturnsShippingManager.Ship(...)`

Benefits:
- keeps hook code small
- allows unit testing of business rules
- isolates shipment logic from UI plumbing

## Delegation
Hooks delegate business decisions to helper methods or manager classes such as:
- service validation
- dry ice handling
- pickup population
- field value normalization

## Feature flag pattern
The backup pickup creation flow is controlled by configuration such as:

- `ReturnsShipping.EnableFallbackPickupCreation`

This prevents accidental activation of fallback behavior before operations approves it.

## Defensive programming
The implementation protects against:
- missing shipment objects
- missing package arrays
- null reference fields
- inconsistent user data
- optional pickup data not being present

## Separation of concerns
- **Template** handles presentation
- **CBR** handles client-side behavior
- **SBR** handles compliance and shipping enforcement

---

## 5) File-by-File Breakdown

## `shippingTemplate.html`
This is the only template file that needs changes for Phase 1.

### Why it was generated/modified
To expose and hide controls needed for the biological returns workflow without redesigning the whole screen.

### What changed
- Added a **Biological Sample** checkbox bound to `MiscReference4`
- Added a **Dry Ice Weight (kg)** input bound to `MiscReference3`
- Made Dry Ice Weight visible only when `ConsigneeReference == 'Frozen'`
- Hid the **Rate** button
- Kept the rest of the template intact

### Why
This supports the custom workflow while minimizing layout and binding risk.

---

## CBR `PageLoaded`
### Purpose
Initial page entry behavior.

### Responsibilities
- auto-route users to shipping
- manage Pickup button visibility based on country
- initialize return workflow page state

### Why
This provides the first user-facing behavior for the custom workflow.

---

## CBR `NewShipment`
### Purpose
Seed a new shipment with user profile data.

### Responsibilities
- set consignee/pickup-from address from user data
- map:
  - `Custom1` → Protocol Number (`MiscReference1`)
  - `Custom2` → Study Reference Code (`ShipperReference`)
  - `Custom3` → Site Number (`MiscReference2`)
- default biological sample and shipment defaults

### Why
Users should not manually recreate common return data for every label.

---

## CBR `PreShip`
### Purpose
Last client-side automation before shipment submission.

### Responsibilities
- attempt to click Pickup Request for non-Canada returns
- save the shipment afterward
- lock dry ice field unless temperature is Frozen

### Why
The blueprint explicitly wants pickup association to happen client-side first, with server-side fallback if needed.

---

## SBR `PreShip`
### Purpose
Authoritative shipment enforcement.

### Responsibilities
- detect international return labels
- enable paperless invoice for cross-border shipments
- set export reason to `Medical`
- choose/validate service behavior based on lane
- set biological sample package extra
- apply dry ice rules from `MiscReference3`

### Why
This is the critical compliance and carrier-facing rule gate.

---

## SBR `Ship`
### Purpose
Backup pickup strategy.

### Responsibilities
- create or populate a Pickup object when the client-side association fails
- use user custom data or shipment context
- remain optional behind a feature flag

### Why
The blueprint explicitly identifies this as a fallback only if the UI pickup automation does not persist correctly.

---

## 6) Testing Recommendations

## Functional test cases

### Domestic US-to-US
- Confirm:
  - default service validation behavior
  - dry ice rules only apply when dry ice is entered
  - pickup button behavior is correct based on country logic
- Verify service fallback if the requested service is invalid

### Canada-to-Canada
- Confirm:
  - Pickup button is hidden
  - no inappropriate cross-border invoice behavior is applied
  - temperature and dry ice logic still work as expected

### Cross-border return label
- Confirm:
  - paperless invoice is enabled
  - export reason is set to `Medical`
  - appropriate service validation occurs
  - pickup association behavior is correct

### Biological sample shipment
- Confirm:
  - `MiscReference4` drives biological sample behavior
  - CS Adapter is used when required
  - package extra `RESTRICTED_ARTICLE_TYPE = 32` is set

### Frozen shipment
- Confirm:
  - dry ice field becomes editable
  - `MiscReference3` is captured
  - KG-to-LBS conversion is correct
  - dry ice is added to package weight

---

## Technical test suggestions
- Verify null handling for all reference fields
- Validate behavior when the user profile has missing custom fields
- Validate behavior when pickup association click/save fails
- Confirm rate shopping and service fallback do not break label generation
- Test template rendering with hidden fields and conditional elements

---

## 7) Deployment Notes

## Configuration steps
Before enabling in production, confirm:

- profile field options are configured correctly
- user custom fields are populated from the spreadsheet/source data
- carrier credentials are valid for UPS and CS Adapter usage
- feature flag for fallback pickup creation is set appropriately
- service codes and adapter mappings match the environment

## Recommended rollout order
1. Deploy profile and template changes
2. Enable CBR logic
3. Enable SBR PreShip
4. Keep SBR Ship fallback disabled initially
5. Validate in lower environment
6. Enable fallback pickup creation only if needed

---

## 8) Manual Steps and Caveats

## Manual steps required
- Populate user-level custom data:
  - `Custom1` = Protocol Number
  - `Custom2` = Study Reference Code
  - `Custom3` = Site Number
- Confirm pickup/return address source data is available for each user
- Verify site profile defaults:
  - shipper
  - return address
  - service
  - units
- Confirm country values are normalized consistently

## Caveats
- The client-side pickup click/save strategy may be unreliable depending on timing and DOM behavior
- Hiding the Rate button may reduce visibility into service selection issues
- Dry ice conversion must be correct to avoid carrier acceptance problems
- Adapter selection based on biological sample status creates a hidden dependency on carrier configuration
- Paperless invoice and export reason logic must only trigger on true cross-border shipments

---

## 9) Future Enhancements

### Recommended next-phase improvements
- Add a robust server-side pickup association implementation if client automation is unstable
- Expand temperature-to-package logic into a reusable service
- Add validation messaging for dry ice entry and missing user custom data
- Improve logging around service fallback decisions
- Add explicit lane classification helpers for US/CA/EU logic
- Create a richer modal for dry ice capture if needed
- Add automated tests for each lane and temperature combination

### Optional architectural improvements
- Extract:
  - address mapping
  - service selection
  - dry ice logic
  - pickup creation
  into dedicated services
- Add stronger feature-flag governance for backup behaviors
- Add audit logging for fallback pickup creation and service overrides

---

## 10) Summary of Generated Pieces

### Generated/updated artifacts
- `shippingTemplate.html`
- CBR hook logic:
  - `PageLoaded`
  - `NewShipment`
  - `PreShip`
- SBR hook logic:
  - `PreShip`
  - `Ship`
- Supporting manager class:
  - `ReturnsShippingManager`
- Optional configuration keys:
  - `ReturnsShipping.EnableFallbackPickupCreation`
  - `ReturnsShipping.DefaultPickupCountryField`
  - `ReturnsShipping.DefaultPickupAddressSource`

### Why this structure works
It keeps the workflow:
- configurable
- testable
- minimal in the UI layer
- enforceable at the server layer

---

If you want, I can also turn this into a more formal `README.md` with:
- an **Installation** section
- a **Configuration** table
- a **Hook sequence diagram**
- and a **mapping matrix** for every field in the blueprint.