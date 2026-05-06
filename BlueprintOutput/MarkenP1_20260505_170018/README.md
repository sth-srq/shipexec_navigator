# README.md

## Marken Phase 1 Biological Returns Workflow for ShipExec

This implementation delivers a custom ShipExec shipping experience for Phase 1 specimen returns. It is designed around UPS return labels for biological samples, with special handling for temperature-based shipping behavior, dry ice capture, pickup association, and cross-border return requirements.

---

## 1) Blueprint Summary: Business Requirements

### Business goal
Build a return-label workflow for specimen shipments that is tailored to biological returns and controlled by user profile data, shipment references, and shipping conditions.

### Core requirements from the blueprint

#### Shipping experience
- Use the standard ShipExec shipping screen, but rework it into a biological returns UI.
- Hide the Rate button.
- Rename and repurpose several fields to match the specimen return process.
- Automatically route users to the shipping page after login.

#### Reference field mapping
The blueprint maps user-visible labels to ShipExec references as follows:

- Consignee Reference → Temperature
  - Validation values:
    - Frozen
    - Ambient
    - Refrigerated
    - Ambient/Refrigerated Combo Box
- Shipper Reference → Study Reference Code
  - Source: User Custom2
- MiscReference1 → Protocol Number
  - Source: User Custom1
- MiscReference2 → Site Number
  - Source: User Custom3
- MiscReference3 → Dry Ice Weight (kg)
  - User-entered only
  - Visible/editable only when Temperature = Frozen
- MiscReference4 → Biological Sample
  - Boolean true/false
  - Default: true
  - Displayed as a checkbox near Saturday Delivery

#### Profile field options
The solution also needs:
- Label renames:
  - Consignee tab → Pickup From
  - Consignee Reference → Temperature
  - Shipper Reference → Study Reference Guide
  - MiscReference fields renamed per blueprint
- Hidden fields:
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
- Default values:
  - Description = `UN3373 Category B Human Sample`
  - Dry Ice Weight Units = `KG`
  - Return Delivery = `True`
  - Saturday Delivery = `True`
  - Service = `UPS Express`
  - Terms = `Prepaid`
  - Weight Unit = `KG` for EU→UK profile only

#### User configuration
- User spreadsheet data provides the pickup/return address.
- User Custom1 = Protocol Number
- User Custom2 = Study Reference Internal Code
- User Custom3 = Site Number

#### Client-side business rules
- On login, take the user directly to shipping.
- Temperature drives package weight defaults:
  - Ambient = 3
  - Frozen = 6
  - Refrigerated = 5
  - Ambient/Refrigerated Combo Box = 6
- If Temperature = Frozen:
  - Prompt for Dry Ice Weight
  - Save it to MiscReference3
- If Temperature is not Frozen:
  - Dry Ice Weight is not editable
- If pickup-from country is not Canada:
  - Auto-click Pickup / Pickup Request
- If pickup-from country is Canada:
  - Hide Pickup button

#### Server-side business rules
SBR is authoritative and must enforce:
- International return handling:
  - Turn on paperless invoice
  - Set export reason to `Medical`
- Adapter selection:
  - Default to CS Adapter services
  - Use CS Adapter when Biological Sample = true
- Service validation:
  - Domestic US→US:
    - Validate NDA Early AM
    - If invalid, switch to NDA without Saturday Delivery
  - International / cross-border:
    - Validate UPS Express with Saturday Delivery
    - If invalid, switch to UPS Saver without Saturday Delivery
- Biological sample handling:
  - Set `RESTRICTED_ARTICLE_TYPE = 32`
- Dry ice handling:
  - Read weight from MiscReference3
  - Convert KG to LBS
  - Set regulation set based on route
  - Set dry ice purpose = Medical
  - Add dry ice weight to package weight
- Pickup fallback:
  - If client-side pickup association fails, create/attach Pickup server-side as backup

---

## 2) Methodology: How the Blueprint Was Translated into Code

The implementation strategy was to separate concerns by responsibility:

### UI and user experience
Handled in CBR and the shipping template:
- Page routing
- Field renaming
- Default values
- Visibility toggles
- Temperature-driven UI behavior
- Dry ice prompt capture
- Pickup button behavior

### Carrier and business enforcement
Handled in SBR:
- Service selection validation
- Paperless invoice and export reason
- Dry ice mutation
- Biological sample package extras
- Pickup fallback logic

### Maintainability approach
The generated solution uses a delegation pattern:
- Thin hooks in `SoxBusinessRules`
- Centralized business logic in manager classes
- Clear separation between:
  - Presentation concerns
  - Shipment preparation
  - Carrier-rule enforcement
  - Fallback processing

### Translation approach used
1. Extracted business rules from the blueprint.
2. Categorized each rule as:
   - Template/UI
   - CBR
   - SBR
   - Profile/field configuration
   - Optional fallback
3. Mapped each rule to the smallest responsible hook.
4. Implemented server-side safeguards for anything that could not safely rely on the browser.
5. Kept the code extensible by isolating logic into manager classes.

---

## 3) Code Flow: How SBR Hooks, CBR Hooks, and Templates Interact

### High-level flow

1. User logs in.
2. `PageLoaded` routes the user to the shipping screen.
3. The customized shipping template renders the biological returns UI.
4. `NewShipment` populates default values from the user profile.
5. `Keystroke` reacts to Temperature changes and updates weight/editability.
6. `CBR PreShip` attempts convenience actions such as pickup association.
7. `SBR PreShip` enforces authoritative server-side rules.
8. `SBR Ship` acts as backup if pickup association failed on the client.

### Interaction details

#### Template → CBR
The template provides:
- Custom field labels
- Hidden controls
- Biological Sample checkbox
- Pickup button
- Rate button hidden
- Conditional dry ice field

CBR then manipulates the rendered UI:
- Auto-redirects on load
- Defaults shipment values
- Reacts to field changes
- Attempts pickup button behavior

#### CBR → SBR
CBR prepares the shipment for shipping, but SBR is the final authority.

Example:
- CBR can prompt for dry ice weight
- SBR converts KG to LBS, sets regulation rules, and modifies package weight
- CBR can try to auto-associate pickup
- SBR provides backup pickup logic if the client workflow fails

#### SBR hook sequence
- `PreShip`
  - Main rule engine
  - Validates and mutates shipment request
- `Ship`
  - Fallback only
  - Used for pickup association if client-side workflow did not persist the Pickup object

---

## 4) Design Patterns Used

### Manager classes
Business logic is centralized into manager classes rather than being embedded directly inside hooks.

#### `ReturnShipmentManager`
- Orchestrates SBR PreShip and Ship logic
- Owns the overall biological returns workflow
- Keeps `SoxBusinessRules` thin

#### `BiologicalReturnServiceManager`
- Encapsulates service validation and switching
- Handles domestic vs international rule branching

#### `DryIceManager` concept
- Separates dry ice conversion and regulation logic
- Prevents weight/business rule code from leaking into UI code

#### `PickupAssociationManager`
- Encapsulates the fallback pickup-attachment workflow
- Keeps backup logic isolated

### Delegation
The hooks themselves delegate to managers rather than performing all logic inline.

Benefits:
- Easier testing
- Better readability
- Lower risk of hook bloat
- Easier future extension

### Defensive programming
The generated code checks for:
- Null shipment objects
- Null package defaults
- Null consignee/shipper objects
- Empty country values
- Invalid dry ice values

This is especially important because CBR and SBR execute in different contexts and cannot assume the same runtime state.

### Configuration-aware behavior
The plan supports optional `BusinessRuleSettings` for fallback pickup mapping or service mapping if the company later wants to externalize them. The blueprint itself does not require mandatory server settings.

---

## 5) File-by-File Breakdown

## `shippingTemplate.html`
Primary template customized for the biological returns experience.

### What was generated/changed
- Renamed UI labels:
  - Consignee → Pickup From
  - Consignee Reference → Temperature
  - Shipper Reference → Study Reference Guide
  - MiscReference1 → Protocol Number
  - MiscReference2 → Site Number
  - MiscReference3 → Dry Ice Weight (kg)
  - MiscReference4 → Biological Sample
- Hid the Rate button.
- Added the Biological Sample checkbox near Saturday Delivery.
- Added conditional behavior for Dry Ice Weight.
- Added Pickup button visibility support for Canada vs non-Canada.
- Hid the listed irrelevant or unsupported fields.

### Why
This is the main user-facing surface for the new workflow. The template must reflect the specimen return process rather than a generic shipping process.

---

## CBR hooks

### `PageLoaded`
Responsibilities:
- Route user to shipping page on login
- Hide Rate button
- Initialize page-level UI behavior

Why:
- Ensures the workflow starts in the correct place
- Removes rate-based UI from the user experience

### `NewShipment`
Responsibilities:
- Load address defaults from user profile
- Set pickup-from/consignee from user data
- Copy user Custom1/2/3 into the correct fields
- Apply default shipment values

Why:
- Users should begin with a prepopulated return-label setup
- Reduces manual entry and ensures data consistency

### `Keystroke`
Responsibilities:
- Detect Temperature changes
- Apply package weight defaults
- Enable/disable Dry Ice Weight field
- Prompt for dry ice entry when Frozen is selected

Why:
- Immediate UI feedback is required when Temperature changes
- Keeps the field state synchronized with the selected return type

### `PreShip`
Responsibilities:
- Attempt client-side Pickup association for non-Canada shipments
- Prompt for Dry Ice Weight if needed
- Validate required client-side state before submission

Why:
- Provides a convenient user workflow before submission
- Reduces but does not replace server-side enforcement

---

## SBR hooks

### `PreShip`
Responsibilities:
- Determine domestic vs international route
- Apply paperless invoice and export reason for cross-border shipments
- Choose service behavior based on Biological Sample
- Validate service eligibility
- Apply dry ice rules and package extras

Why:
- This is the authoritative shipping-rule checkpoint
- Any rule that affects carrier behavior must be enforced here

### `Ship`
Responsibilities:
- Fallback pickup association
- Create or attach Pickup when client-side pickup workflow fails

Why:
- The blueprint explicitly calls for a server fallback if the browser-side click/save flow does not work reliably

---

## Manager/support classes

### `ReturnShipmentManager`
Central coordinator for SBR behavior.

### `BiologicalReturnServiceManager`
Applies service selection logic:
- Domestic US→US
- International/cross-border
- Biological Sample compatibility

### `PickupAssociationManager`
Handles pickup fallback evaluation.

### `DryIceManager`
Referenced in the implementation plan as the right abstraction for:
- KG to LBS conversion
- Dry ice regulation set
- Dry ice purpose assignment
- Weight adjustment

Even if implemented as part of `ReturnShipmentManager` initially, this is a strong candidate for extraction.

---

## 6) Testing Recommendations

### Functional test scenarios

#### UI
- Login redirects to shipping page
- Rate button is hidden
- Field labels display as expected
- Biological Sample checkbox appears near Saturday Delivery
- Pickup From tab appears instead of Consignee
- Dry Ice Weight is editable only when Temperature = Frozen

#### CBR behavior
- Temperature = Ambient sets weight to 3
- Temperature = Frozen sets weight to 6
- Temperature = Refrigerated sets weight to 5
- Temperature = Ambient/Refrigerated Combo Box sets weight to 6
- Frozen temperature prompts for dry ice
- Non-Frozen temperature disables dry ice field
- Non-Canada pickup address triggers pickup flow
- Canada pickup address hides pickup button

#### SBR behavior
- US→US shipment validates NDA Early AM and falls back if invalid
- International shipment validates UPS Express Saturday and falls back if invalid
- Biological Sample = true sets `RESTRICTED_ARTICLE_TYPE = 32`
- Dry Ice Weight is converted KG → LBS
- Dry Ice Purpose = Medical
- Dry Ice Regulation Set changes based on route
- Cross-border shipments set paperless invoice and export reason correctly

### Regression tests
- Ensure default ShipExec behavior still works for unaffected fields
- Confirm hidden fields do not break validation
- Confirm shipment still ships if pickup object is already present

### Negative tests
- Missing or invalid Dry Ice Weight
- Null consignee/shipper data
- Missing custom user fields
- Unsupported country values
- Rate-shop failure or carrier service unavailability

---

## 7) Deployment Recommendations

### Pre-deployment checklist
- Verify field option mappings in the target environment
- Confirm user spreadsheet columns match:
  - Custom1
  - Custom2
  - Custom3
  - Address fields
- Confirm UPS adapter credentials are correct
- Confirm CS Adapter is configured if required for biological sample processing
- Validate that the shipping template is the active template for the profile/site

### Environment validation
- Test in non-production before enabling for live specimen shipments
- Confirm template rendering across browsers used by operators
- Verify server hook deployment order:
  - Template
  - CBR
  - SBR

### Operational note
Because the Rate button is hidden, any rate validation needed for SBR must not depend on the user clicking the UI rate action. It must be performed programmatically on the server.

---

## 8) Future Enhancements

### Strong candidates for phase 2
- Externalize pickup fallback mapping into `BusinessRuleSettings`
- Extract dry ice handling into a dedicated `DryIceManager`
- Improve pickup association with a robust server-created Pickup object
- Add richer validation messages for missing temperature or dry ice values
- Add audit logging for:
  - Temperature selection
  - Biological Sample flag
  - Service fallback decisions
  - Dry ice conversion results
- Add a clearer modal dialog for dry ice entry instead of using a browser prompt
- Add stronger country-routing utilities for international logic

### Usability improvements
- Replace manual DOM button clicking with explicit app methods if available
- Add a clearer visual state indicator for when pickup association is pending
- Provide inline validation for protocol/site/study fields

---

## 9) Caveats and Manual Steps

### Caveat: pickup association from the client may be unreliable
The blueprint explicitly warns that clicking Pickup Request and Save may not always work from the browser side. For that reason:
- CBR attempts the flow
- SBR `Ship` provides a fallback

### Caveat: country comparisons must be exact
Domestic vs international logic depends on accurate country values. The implementation must normalize and compare country codes consistently.

### Caveat: hidden fields may still be required by downstream validation
Some hidden fields may be required internally by the ShipExec workflow or carrier adapter. They should be hidden only if the runtime confirms they are not mandatory.

### Caveat: rate validation still occurs even though Rate UI is hidden
Hiding the Rate button does not remove the need for service validation. SBR must handle rate-shopping and fallback logic programmatically.

### Manual step: verify field mappings
Before deployment, confirm that:
- User Custom1/2/3 values are populated correctly
- Address data exists and matches the expected schema
- Temperature values match the allowed validation list exactly

### Manual step: confirm template behavior in the deployed environment
Because the template uses UI selectors and field names, final testing should verify:
- Pickup button selector compatibility
- Dry Ice field selector compatibility
- Whether the active UI binds to `ConsigneeReference`, `MiscReference3`, and related fields as expected

---

## 10) Implementation Notes

### What was intentionally kept thin
The generated hooks are designed to orchestrate, not own all logic:
- CBR handles user experience and data staging
- SBR handles rule enforcement and carrier mutation
- Manager classes keep logic modular

### What remains configurable
If the company later wants to expose more behavior through Commander, the best candidates are:
- Pickup fallback mapping
- Service fallback mapping
- Site-specific service overrides
- Country handling rules
- Dry ice prompt behavior

---

## 11) Summary

This blueprint was translated into a ShipExec implementation that:
- Presents a specimen-return-oriented shipping UI
- Prepopulates shipment data from user profile fields
- Responds to Temperature with weight and dry ice logic
- Attempts pickup association client-side when needed
- Enforces all carrier/business rules server-side
- Falls back safely when client-side pickup association fails

The result is a maintainable, layered design that matches the blueprint’s requirements while keeping the final shipping decision logic authoritative on the server.