# Marken ShipExec Phase 1 — Biological Returns / Specimen Returns

## Overview

This implementation customizes ShipExec for Marken’s Phase 1 specimen returns workflow. The goal is to transform the standard shipping experience into a branded returns process for biological samples, with:

- Renamed UI concepts to match returns terminology
- User-profile-driven defaults and reference mapping
- Temperature-based package behavior
- Dry ice capture and conversion logic
- Pickup association automation for non-Canada returns
- Server-side enforcement of shipment rules before ship
- A fallback strategy if client-side pickup automation is unreliable

---

## 1) Business Requirements Summary

### Primary business objective
Create a specialized shipping workflow for biological/specimen returns that is easier for users to execute correctly and enforces Marken’s shipment rules consistently.

### Key requirements from the blueprint

#### UI / workflow branding
- Rename the **Consignee** tab to **Pickup From**
- Rename reference fields to business-friendly labels:
  - **Consignee Reference** → **Temperature**
  - **Shipper Reference** → **Study Reference Guide**
  - **MiscReference1** → **Protocol Number**
  - **MiscReference2** → **Site Number**
  - **MiscReference3** → **Dry Ice Weight (kg)**
  - **MiscReference4** → **Biological Sample**
- Hide the **Rate** button
- Hide the **Pickup** button when the pickup-from country is **Canada**
- Default the user into the **Shipping** page after login

#### Field defaults and visibility
- Default shipment description to:
  - `UN3373 Category B Human Sample`
- Default:
  - Service = `UPS Express`
  - Terms = `Prepaid`
  - Weight Unit = `KG`
  - Dry Ice Weight Units = `KG`
  - Return Delivery = `True`
  - Saturday Delivery = `True`
- Hide many standard shipping fields not needed for the returns workflow, including:
  - BrokerageThirdPartyBilling
  - Box Type
  - Carrier Instructions
  - Consignee PO Box
  - Consignee Third Party Billing
  - Third Party Billing
  - Declared Value Amount
  - Dimensions
  - Insurance Method
  - Origin Address
  - Packaging
  - Piece Count
  - Tracking Number
  - Residential Indicator
  - PO Box Indicator
  - Ship Date
  - Account Number
  - Tax ID

#### User/profile mapping
- User address data becomes the pickup-from address for return labels
- User custom values map to shipment references:
  - `Custom1` → Protocol Number
  - `Custom2` → Study Reference Internal Code / Study Reference Guide
  - `Custom3` → Site Number

#### Temperature and dry ice logic
- Temperature options:
  - Frozen
  - Ambient
  - Refrigerated
  - Ambient/Refrigerated Combo Box
- When **Frozen** is selected:
  - user must enter **Dry Ice Weight**
  - dry ice is stored in `MiscReference3`
  - dry ice data is used during server-side pre-ship processing
- When temperature is not Frozen:
  - dry ice should not be editable

#### Pickup association
- If pickup-from address is **not Canada**:
  - automatically associate pickup with the return shipment
- If pickup-from address **is Canada**:
  - hide the Pickup button

#### Server-side enforcement
- For international returns:
  - turn on paperless invoice
  - set export reason to **Medical**
- Adapter selection:
  - default to **CS Adapter**
  - use CS services for biological samples
- Validate service by rate shop:
  - Domestic US→US:
    - validate `NDA Early AM`
    - if invalid, downgrade to `NDA` without Saturday Delivery
  - International/cross-border:
    - validate `UPS Express` with Saturday Delivery
    - if invalid, downgrade to `UPS Saver` without Saturday Delivery
- Biological sample shipments:
  - set `RESTRICTED_ARTICLE_TYPE = 32`
- Dry ice:
  - set dry ice from `MiscReference3`
  - convert KG → LBS
  - set purpose to `Medical`
  - set regulation set based on lane
  - add dry ice weight into package weight

---

## 2) Methodology — How the Blueprint Was Translated into Code

The implementation was translated using a layered approach:

### A. UI behavior was assigned to CBR
Client-side business rules handle:
- page routing after login
- pre-population of shipment values
- field show/hide behavior
- user interaction for pickup association
- temperature-specific UI reactions

This matches the blueprint’s expectation that the shipping screen should feel like a specialized returns workflow immediately on load.

### B. Authoritative shipment logic was assigned to SBR
Server-side business rules handle:
- shipment transformations that must be trusted
- service validation and fallback
- dry ice weight conversion and package manipulation
- paperless invoice/export reason rules
- biological sample package extras
- fallback pickup handling if the client-side method fails

This is important because any business-critical or carrier-sensitive rule should be enforced server-side, not just in the browser.

### C. Template changes were used for branding and field layout
The shipping template was adjusted to:
- rename labels
- hide irrelevant controls
- show only the fields that support the returns workflow
- expose temperature and dry ice behavior clearly
- integrate CBR state flags for conditional visibility

### D. Manager classes were introduced to centralize business logic
Rather than spreading logic across hooks, helper managers were designed to keep behavior maintainable:
- `ReturnsShipmentManager`
- `ReturnsPickupManager`
- `DryIceManager`
- `BiologicalSampleManager`
- `ServiceSelectionManager`
- `PickupAssociationManager`
- `ReturnsUiManager`

---

## 3) Code Flow: How SBR Hooks, CBR Hooks, and Templates Interact

### High-level flow

1. **User logs in**
2. **CBR PageLoaded**
   - redirects user to Shipping
   - sets UI flags
   - hides Pickup button for Canada
3. **CBR NewShipment**
   - seeds return shipment defaults
   - maps profile data into fields
   - sets initial temperature / biological sample defaults
4. **User interacts with the shipping template**
   - sees renamed fields and hidden controls
   - chooses Temperature
   - if Frozen, enters Dry Ice Weight
5. **CBR PreShip**
   - performs last client-side validation
   - may trigger pickup association flow
6. **SBR PreShip**
   - authoritative server-side rule processing
   - adjusts service, dry ice, paperless invoice, adapter preference, and package extras
7. **SBR Ship**
   - fallback only, if pickup association needs server-side correction
8. **Shipment is completed**

### Hook responsibilities

#### CBR PageLoaded
Used for:
- redirecting to Shipping after login
- initializing the returns UI state
- hiding Pickup if the return country is Canada

#### CBR NewShipment
Used for:
- setting consignee/pickup-from fields from user profile
- mapping user custom fields to references
- defaulting service, terms, weight unit, and return flags

#### CBR PreShip
Used for:
- final client-side validation
- pickup association coordination
- prompting user for dry ice when Frozen is selected

#### SBR PreShip
Used for:
- enforcing international return rules
- selecting adapter/service logic
- applying biological sample package settings
- converting and applying dry ice weight
- setting paperless invoice/export reason rules

#### SBR Ship
Used only as a fallback:
- if the Pickup Request/save UI flow does not successfully attach the pickup
- may create or populate a Pickup object from user data

#### Template
Used for:
- renamed labels
- field visibility
- pickup button visibility
- temperature dropdown
- dry ice input
- biological sample checkbox
- rate button suppression

---

## 4) Design Patterns Used

### Manager pattern
Business logic is concentrated in dedicated manager classes rather than being scattered across hooks.

Examples:
- `ReturnsShipmentManager`
- `ReturnsPickupManager`

This improves:
- readability
- testability
- maintainability

### Delegation
The hooks delegate to helper methods and managers instead of implementing all logic inline.

Example:
- `SBR PreShip` delegates to `ReturnsShipmentManager.PreShip(...)`
- `SBR Ship` delegates to `ReturnsPickupManager.Ship(...)`

### Separation of concerns
Each layer has a distinct responsibility:
- **Template**: layout and visibility
- **CBR**: user experience and lightweight validation
- **SBR**: authoritative shipment rules
- **Managers**: reusable domain logic

### Fallback strategy
Pickup association is attempted on the client side first, but a server-side fallback is preserved in `SBR Ship`.

This is a practical resilience pattern:
- primary path = UI automation
- fallback path = server creation/population of pickup context

### Defensive programming
The implementation checks for null or missing structures before applying logic. This reduces runtime errors in partially populated shipment states.

---

## 5) File-by-File Breakdown

## `shippingTemplate.html`
Primary UI file for the returns workflow.

### Why it was generated/updated
To rebrand the standard shipping screen for Marken biological returns and expose the correct controls.

### Main changes
- Renamed **Consignee** area to **Pickup From**
- Renamed field labels:
  - Temperature
  - Study Reference Guide
  - Protocol Number
  - Site Number
  - Dry Ice Weight (kg)
  - Biological Sample
- Hid the **Rate** button
- Hid the **Pickup** button when `hidePickupButton` is true
- Added a Temperature dropdown bound to `ConsigneeReference`
- Added conditional Dry Ice Weight input, shown only when Frozen is selected
- Added Biological Sample checkbox near Saturday Delivery
- Applied numerous field hides through `ng-hide` / FieldOptions
- Bound UI behavior to ViewModel flags set by CBR

### Why this matters
This is what turns ShipExec into a branded returns tool instead of a generic shipping form.

---

## CBR hook methods

### `PageLoaded(location)`
#### Purpose
- route the user to Shipping after login
- initialize return workflow state
- determine whether Pickup should be hidden

#### Why it exists
To immediately place the user in the proper workflow and initialize UI flags.

---

### `NewShipment(shipmentRequest)`
#### Purpose
- populate the shipment from user profile data
- map custom fields into return references
- set defaults required by the blueprint

#### Why it exists
To eliminate manual data entry and ensure every new return shipment starts with the correct baseline values.

---

### `PreShip(shipmentRequest, userParams)`
#### Purpose
- validate that Frozen shipments include dry ice
- mark pickup association as needed for non-Canada returns
- provide a last client-side guard before server processing

#### Why it exists
To catch missing required fields before the shipment is sent to the server.

---

### `PostLoad(loadValue, shipmentRequest)`
#### Purpose
- resync UI flags after a loaded shipment/order
- restore dry ice editability if Frozen is present

#### Why it exists
Useful when loading saved or preexisting shipment data into the returns UI.

---

### `PostSelectAddressBook(shipmentRequest, nameaddress)`
#### Purpose
- copy selected address values into the pickup-from fields
- update country-based UI flags

#### Why it exists
To keep address book selections aligned with the return workflow and preserve the Pickup From semantics.

---

## SBR hook methods

### `PreShip(shipmentRequest, userParams)`
#### Purpose
- enforce international return rules
- set paperless invoice/export reason
- select adapter preference for biological sample shipments
- validate and downgrade service if necessary
- apply biological sample package extras
- convert and apply dry ice weight

#### Why it exists
This is the core server-side business rule enforcement point.

---

### `Ship(shipmentRequest, pickup, shipWithoutTransaction, print, userParams)`
#### Purpose
- provide fallback pickup association behavior
- create/populate pickup data if the client-side flow failed

#### Why it exists
The blueprint explicitly calls for a backup strategy if the Pickup Request/save UI flow proves unreliable.

---

## Helper classes / managers

### `ReturnsShipmentManager`
Central server-side business rule coordinator for returns shipments.

### `ReturnsPickupManager`
Fallback pickup association strategy.

### `DryIceManager`
Intended to own dry ice conversion, regulation, and package weight logic.

### `BiologicalSampleManager`
Intended to own biological sample-specific flags and package extras.

### `ServiceSelectionManager`
Intended to centralize service validation and downgrade logic.

### `PickupAssociationManager`
Intended to manage pickup association either from UI state or server-side fallback.

### `ReturnsUiManager`
Intended to centralize UI flags and page-level returns workflow behavior.

---

## 6) Testing, Deployment, and Future Enhancements

## Testing suggestions

### Functional test cases
1. **US domestic return**
   - pickup-from country = US
   - verify temperature options
   - verify default service behavior
   - verify dry ice logic when Frozen is selected

2. **International return**
   - pickup-from country ≠ return-to country
   - verify paperless invoice is enabled
   - verify export reason = Medical
   - verify service downgrade behavior if needed

3. **Canada pickup-from**
   - verify Pickup button is hidden
   - verify no pickup association prompt is shown

4. **Frozen shipment**
   - verify dry ice field becomes editable
   - verify dry ice is required
   - verify KG→LBS conversion is applied in SBR PreShip

5. **Biological Sample = true**
   - verify `RESTRICTED_ARTICLE_TYPE = 32`
   - verify CS adapter preference is used

6. **Address book selection**
   - confirm selected address updates Pickup From fields
   - confirm country-dependent UI state updates correctly

### Regression tests
- Verify hidden fields do not break existing bindings
- Verify default fields still save correctly
- Verify shipment can still be built, saved, and shipped end-to-end
- Verify the Rate button is not accessible via template or keyboard shortcut

### Automation ideas
- UI test for login → Shipping redirect
- UI test for Frozen temperature behavior
- Integration test for SBR PreShip on domestic and cross-border shipments
- Test dry ice conversion precision

---

## Deployment suggestions

### Recommended deployment sequence
1. Deploy template changes first
2. Deploy CBR logic next
3. Deploy SBR logic after CBR validation
4. Validate end-to-end with a test profile
5. Roll out to production with a small user group first

### Environment checks
- Confirm profile field names match the live ShipExec environment
- Confirm User Custom fields are populated as expected
- Confirm the Pickup button selector or action exists in the current template/controller
- Confirm service names used in rate-shopping match actual carrier/service identifiers

---

## Future enhancements

- Add explicit `Temperature` domain validation in shared manager logic
- Expand dry ice modal/UX if field entry should be more guided
- Improve adapter selection logic with explicit service family configuration
- Add PostShip logging or downstream return-system integration
- Add better pickup association telemetry or retry logic
- Add a formal unit-test suite for manager classes
- Support additional return lanes with profile-driven configuration

---

## 7) Caveats and Manual Steps

### Caveat: Pickup button implementation may vary
The blueprint references a Pickup button and pickup association flow, but the exact DOM control or API method may differ in the actual ShipExec build.

**Manual step:** verify the real button name, action method, and selector before finalizing the auto-click behavior.

### Caveat: Client-side pickup automation is fragile
Automatically clicking Pickup and Save may fail if:
- the UI timing changes
- the modal or page layout changes
- the action requires async readiness

**Mitigation:** keep the SBR Ship fallback strategy available.

### Caveat: Field naming must match actual profile schema
The mapping between:
- `Custom1`
- `Custom2`
- `Custom3`
and the returns fields must be verified against real profile data.

**Manual step:** validate the user spreadsheet/profile import format.

### Caveat: Service validation depends on actual carrier/rate behavior
The service downgrade logic assumes rate shopping can determine whether the desired service is valid.

**Manual step:** verify actual service names and carrier constraints for:
- `NDA Early AM`
- `NDA`
- `UPS Express`
- `UPS Saver`

### Caveat: Dry ice weight conversion must be applied once
Because package weight is modified when dry ice is added, double-applying the conversion would inflate weight and distort rating.

**Manual step:** ensure the conversion path is executed only once per shipment lifecycle.

### Caveat: Hidden fields may still be referenced elsewhere
Some hidden fields could still be read by existing validations or bindings.

**Manual step:** regression-test the shipping page after hiding all Blueprint-listed fields.

---

## Implementation Summary

This Phase 1 solution introduces a specialized Marken biological returns workflow by combining:

- **Template updates** for branding and field layout
- **CBR hooks** for client-side initialization and UX behavior
- **SBR hooks** for authoritative shipment enforcement
- **Manager classes** for cleaner business logic delegation

The result is a ShipExec workflow tailored for specimen returns with controlled defaults, dry ice support, pickup association handling, and carrier-rule enforcement aligned to the blueprint.