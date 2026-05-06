# Marken ShipExec Phase 1 Biological Returns

## Overview

This implementation translates the Marken Phase 1 blueprint into a returns-focused ShipExec workflow for biological specimen shipments. The core goal is to convert the standard shipping experience into a guided return-label process with:

- renamed shipping fields and tabs,
- automatic population from user profile data,
- temperature-driven shipment behavior,
- dry ice handling,
- biological sample marking,
- pickup association rules, and
- server-side enforcement for anything the UI could bypass.

The resulting solution is centered on:

- `shippingTemplate.html` for the UI,
- CBR hooks for page setup and client convenience actions,
- SBR hooks for authoritative shipping enforcement,
- supporting manager classes to keep business logic modular and testable.

---

## 1. Business Requirements Summary

### Primary business objective

Create a ShipExec shipping experience for Marken biological returns where users can quickly create compliant specimen return labels using preconfigured profile data and controlled shipment options.

### Key blueprint requirements

#### UI / workflow changes
- Rename the `Consignee` tab to `Pickup From`.
- Rename `ConsigneeReference` to `Temperature`.
- Rename `ShipperReference` to `Study Reference Guide`.
- Rename misc references to:
  - `MiscReference1` → `Protocol Number`
  - `MiscReference2` → `Site Number`
  - `MiscReference3` → `Dry Ice Weight (kg)`
  - `MiscReference4` → `Biological Sample`
- Hide many standard shipping fields that are not part of the returns workflow.
- Default shipment description to `UN3373 Category B Human Sample`.
- Hide the Rate button.
- Show/hide the Pickup button based on country.
- Make dry ice entry conditional on `Temperature = Frozen`.

#### Data mapping requirements
- User address data becomes the return pickup-from address.
- `Custom1` becomes Protocol Number.
- `Custom2` becomes Study Reference Code / Shipper Reference.
- `Custom3` becomes Site Number.

#### Shipping behavior rules
- Temperature drives package weight defaults.
- Frozen shipments require dry ice weight input.
- Non-Frozen shipments must not allow dry ice editing.
- Non-Canada pickup-from addresses should trigger pickup association.
- Canada pickup-from addresses should suppress pickup controls.
- International returns require paperless invoice and export reason `Medical`.

#### Carrier/service enforcement
- Default to CS adapter services.
- Biological sample shipments should use CS adapter services for rate shop and final shipping/processing.
- Lane validation should enforce appropriate service fallbacks:
  - domestic US-to-US: `NDA Early AM`, fallback to `NDA` without Saturday Delivery
  - international/cross-border: `UPS Express` with Saturday Delivery, fallback to `UPS Saver` without Saturday Delivery

#### Dry ice requirements
- Dry ice weight is entered in kilograms.
- Convert kilograms to pounds before adding it to package weight.
- Set dry ice purpose to `Medical`.
- Apply different regulation sets depending on lane.

#### Pickup fallback
- If client-side pickup association fails, SBR Ship must provide a server-side fallback path.

---

## 2. Methodology: How the Blueprint Was Translated Into Code

The implementation plan was built around a simple principle:

- **UI handles convenience**
- **SBR handles enforcement**
- **Templates define presentation**
- **Managers encapsulate business logic**

### Translation strategy

#### Step 1: Identify what belongs in the template
The blueprint required visible UI changes such as:
- renamed tabs/labels,
- hidden fields,
- default display behavior,
- conditional button visibility.

These are best represented in `shippingTemplate.html`.

#### Step 2: Identify what belongs in CBR
The blueprint asked for:
- redirecting to shipping on login,
- pre-populating shipment defaults,
- toggling pickup and dry ice UI behavior,
- attempting pickup association before ship.

These were mapped to:
- `CBR PageLoaded`
- `CBR NewShipment`
- `CBR PreShip`

#### Step 3: Identify authoritative server-side rules
Anything that can be bypassed through saved data, API calls, or UI manipulation was moved to:
- `SBR PreShip`
- `SBR Ship`

This includes:
- customs/paperless invoice logic,
- service validation and fallback,
- biological sample package extras,
- dry ice conversion and package weight mutation,
- pickup fallback behavior.

#### Step 4: Delegate business logic into managers
Instead of putting everything in the hook bodies, the implementation delegates to manager classes such as:
- `BiologicalReturnsRulesManager`
- `PickupAssociationManager`
- `CarrierSelectionManager`
- `ReferenceMappingManager`
- `ReturnsTemplateStateManager`

This keeps `SoxBusinessRules` thin and easier to maintain.

---

## 3. Code Flow: How SBR Hooks, CBR Hooks, and Templates Interact

### End-to-end flow

#### 1. User loads the application
- `CBR PageLoaded` runs first.
- If the user is on a login/home route, they are redirected to the shipping page.
- The UI state is initialized:
  - Pickup button visibility
  - Dry ice editability
  - Rate button visibility

#### 2. User starts a new shipment
- `CBR NewShipment` seeds defaults from the logged-in user's profile and address.
- This populates:
  - Pickup From / Consignee address
  - Shipper Reference
  - Protocol Number
  - Site Number
  - Biological Sample flag
  - Default description

#### 3. User edits shipment details
- The `shippingTemplate.html` exposes the required controls:
  - Temperature
  - Biological Sample
  - Dry Ice Weight
- Field visibility and labels reflect the Marken terminology.
- UI behavior is controlled by the CBR state flags.

#### 4. Client-side pre-ship step
- `CBR PreShip` attempts to trigger pickup association for non-Canada shipments.
- This is a convenience step only.
- If it fails, shipping is still allowed to proceed.

#### 5. Server-side enforcement
- `SBR PreShip` is the final authoritative rule gate.
- It:
  - detects domestic vs international lane,
  - applies paperless invoice and export reason,
  - sets package extras for biological samples,
  - converts and applies dry ice weight,
  - validates or adjusts service selection.

#### 6. Server-side pickup fallback
- `SBR Ship` provides a fallback path if client pickup association did not attach correctly.
- It can build a pickup object from shipment/profile data or prepare one for use by standard shipping flow.

---

## 4. Design Patterns Used

### 1. Manager / service class pattern
Business logic is delegated to dedicated classes rather than being embedded in hooks.

Examples:
- `BiologicalReturnsRulesManager`
- `PickupAssociationManager`

Benefits:
- easier unit testing,
- better readability,
- reduced hook complexity,
- easier future expansion.

### 2. Thin hook / delegation pattern
The hooks act as entry points and delegate work to managers.

Example flow:
- `SBR PreShip` creates manager
- manager performs rule enforcement
- hook stays short and maintainable

### 3. Configuration-driven behavior
Several behaviors are driven by `BusinessRuleSettings`, including:
- service symbols,
- pickup fallback enablement,
- package-extra keys,
- dry ice defaults.

This reduces hardcoding and allows deployment-time tuning.

### 4. Defensive programming
The generated logic assumes fields may be missing and initializes them safely:
- shipment object null checks,
- package defaults creation,
- consignee creation,
- dry ice parsing validation,
- pickup fallback safety.

### 5. State-driven UI behavior
The client-side solution uses UI state flags instead of scattered DOM manipulation:
- `HidePickupButton`
- `AllowDryIceEdit`
- `HideRateButton`

This makes the template easier to reason about.

---

## 5. File-by-File Breakdown

## `shippingTemplate.html`

This is the only template explicitly required by the blueprint.

### What changed and why

#### Toolbar changes
- Added/retained `New`, `Repeat`, `Build`, `DistList`, `Pickup`, `Pending`, `MailRoom`
- Hidden the `Rate` button
- Pickup visibility is controlled by UI state

#### Address tab changes
- `Consignee` tab renamed to `Pickup From`
- Address components kept aligned with return-label semantics

#### Reference section changes
- `ShipperReference` caption changed to `Study Reference Guide`
- `ConsigneeReference` converted to `Temperature`
- `MiscReference1` renamed to `Protocol Number`
- `MiscReference2` renamed to `Site Number`
- `MiscReference3` renamed to `Dry Ice Weight (kg)`
- `MiscReference4` renamed to `Biological Sample`

#### Biological sample controls
- A checkbox was added near `Saturday Delivery`
- This maps to `MiscReference4`
- It is the visible control for the biological sample flag

#### Field hiding
The template hides a large list of fields not needed for this workflow, including:
- Brokerage and third-party billing controls
- box type
- carrier instructions
- dimensions
- declared value
- insurance
- origin address
- packaging
- piece count
- tracking number
- residential/PO box flags
- ship date
- account/tax ID

#### Dry ice UI behavior
- `MiscReference3` is hidden unless dry ice editing is allowed
- CBR controls that visibility based on `Temperature = Frozen`

### Why only one template changed
The blueprint only required shipping-screen changes. Other templates were not part of the stated workflow and were left unchanged.

---

## 6. Generated Hooks and Their Responsibility

### CBR `PageLoaded`
Purpose:
- redirect to shipping page on login
- initialize UI state
- hide/show pickup
- hide rate button
- control dry ice editability

### CBR `NewShipment`
Purpose:
- create a fresh returns shipment shell
- populate defaults from user address/custom fields
- set the biological sample flag
- default the description
- prepare UI state for the new shipment

### CBR `PreShip`
Purpose:
- try the client-side pickup action for non-Canada shipments
- save the shipment if possible
- do not block shipping if the pickup action fails

### SBR `PreShip`
Purpose:
- enforce all critical shipment rules on the server
- apply customs and export behavior
- set biological sample package extras
- convert dry ice and add it to package weight
- validate/swap carrier service as needed

### SBR `Ship`
Purpose:
- provide backup pickup association logic
- avoid shipment failure when client-side pickup association was unsuccessful

---

## 7. Manager Classes and Why They Exist

### `BiologicalReturnsRulesManager`
Responsible for:
- lane detection
- temperature-based behavior
- dry ice conversion
- biological sample package extras
- service selection enforcement

### `PickupAssociationManager`
Responsible for:
- pickup fallback strategy
- constructing pickup data from shipment defaults
- avoiding failure when UI pickup flow does not persist correctly

### `CarrierSelectionManager`
Recommended responsibility:
- determine adapter/service family
- apply domestic vs international service rules
- support biological-sample routing differences

### `ReferenceMappingManager`
Recommended responsibility:
- map Custom1/2/3 to reference fields
- normalize profile-driven defaults
- keep UI and server mapping aligned

### `ReturnsTemplateStateManager`
Recommended responsibility:
- manage template visibility state
- control pickup button visibility
- control dry ice editability
- keep `PageLoaded` logic compact

---

## 8. Testing Recommendations

## Functional test scenarios

### UI and navigation
- Verify login redirects to shipping page.
- Verify Pickup From tab is renamed correctly.
- Verify Rate button is hidden.
- Verify dry ice field is hidden unless `Temperature = Frozen`.

### Shipment initialization
- Create a new shipment and verify:
  - user address is copied into Pickup From,
  - `Custom1/2/3` map into references correctly,
  - description defaults to `UN3373 Category B Human Sample`,
  - biological sample defaults to true.

### Temperature behavior
- Set Temperature to each option:
  - Frozen
  - Ambient
  - Refrigerated
  - Ambient/Refrigerated Combo Box
- Confirm package weight updates correctly.

### Dry ice behavior
- For Frozen shipments:
  - enter dry ice weight in KG
  - verify conversion to LB
  - verify package weight increases
- For non-Frozen shipments:
  - verify dry ice cannot be edited

### Pickup behavior
- For non-Canada pickup-from addresses:
  - verify Pickup button appears
  - verify client-side pickup is attempted
- For Canada:
  - verify Pickup button is hidden

### Server-side behavior
- Verify international shipments set:
  - paperless invoice
  - export reason `Medical`
- Verify biological sample shipments set restricted article package extras.
- Verify lane-based service fallback behavior.
- Verify pickup fallback path does not duplicate pickup creation.

---

## 9. Deployment Notes

### Configuration required
Before deployment, confirm:
- profile field options are updated in ShipExec Commander,
- business rule settings exist for service and pickup defaults,
- UPS and CS adapter credentials are valid,
- any environment-specific service symbols are correct.

### Suggested deployment order
1. Update profile field options and defaults.
2. Deploy `shippingTemplate.html`.
3. Deploy CBR hooks.
4. Deploy SBR hooks.
5. Deploy manager/helper classes.
6. Validate in a test environment.
7. Promote to production after shipment scenarios pass.

### Coordination point
The blueprint references both UPS and CS adapters. Confirm exact service-symbol mappings before final go-live, especially for:
- domestic fallback services,
- international fallback services,
- biological sample adapter routing.

---

## 10. Future Enhancements

### Recommended next phase items
- Add full server-side pickup creation in `SBR Ship` if client pickup actions remain unreliable.
- Implement stronger idempotency checks for pickup creation/association.
- Expand service routing into a dedicated `CarrierSelectionManager`.
- Add automated validation for temperature-to-weight mapping.
- Add reporting support for biological returns batch review.
- Add label/report templates for:
  - biological returns shipping label,
  - packing slip,
  - customs/commercial invoice,
  - manifest/batch summary.

### Reporting opportunities
The report analysis suggests these output documents may be useful:
- Biological Returns Shipping Label
- Biological Returns Packing Slip
- International Returns Customs/Commercial Invoice Document
- Biological Returns Manifest/Batch Report

---

## 11. Caveats and Manual Steps

### Important caveats
- Hiding a field in the UI does not prevent data submission; SBR must enforce rules server-side.
- The blueprint repurposes `ConsigneeReference` as `Temperature`, which may affect downstream expectations.
- Dry ice weight is entered in kilograms but must be converted to pounds before weight calculations.
- Service mappings are not fully specified in the blueprint and may need business confirmation.
- Pickup logic has both client-side and server-side paths; idempotency is essential to prevent duplicates.
- Auto-redirect on login may affect existing user navigation behavior.

### Manual steps likely needed
- Update profile field captions and hidden-field settings in Commander.
- Confirm adapter credentials and access keys.
- Verify business rule settings values for service symbols and pickup defaults.
- Validate the user address spreadsheet/data format used for returns.
- Confirm `Custom1`, `Custom2`, and `Custom3` source fields are populated correctly.

---

## 12. Summary

This implementation converts a standard ShipExec shipping workflow into a Marken-specific biological returns process by combining:

- template-driven UI changes,
- client-side shipment initialization,
- server-side rule enforcement,
- and modular manager-based business logic.

The result is a maintainable Phase 1 foundation that supports return-label creation, specimen-specific requirements, and safe fallback behavior when the client workflow cannot be trusted to complete critical shipping steps.