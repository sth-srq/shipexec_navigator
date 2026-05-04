# Marken ShipExec Phase 1 Biological Returns

## Overview

This implementation supports Marken’s Phase 1 ShipExec biological returns workflow. The blueprint focuses on adapting the standard shipping experience so users can create compliant return labels with:

- Return-oriented field labels and defaults
- Pickup-from address behavior driven by the user profile
- Temperature-based handling, including dry ice capture for frozen shipments
- Conditional pickup association for non-Canada return lanes
- Server-side enforcement of carrier/service and export rules
- Backup pickup creation if the client-side flow fails

The solution is split across:

- **CBR hooks** for UI setup and user interaction
- **SBR hooks** for authoritative shipment validation and fallback logic
- **Shipping template changes** for field labels, visibility, and workflow controls

---

## 1) Summary of Business Requirements

### Core business goal
Create a shipping experience for **biological specimen returns** that behaves like a dedicated returns application, while still using ShipExec’s standard shipping engine.

### Required business behaviors

#### Return-label UI customization
- Rename standard shipping labels to returns terminology:
  - **Consignee** → **Pickup From**
  - **Consignee Reference** → **Temperature**
  - **Shipper Reference** → **Study Reference Guide/Code**
  - **MiscReference1** → **Protocol Number**
  - **MiscReference2** → **Site Number**
  - **MiscReference3** → **Dry Ice Weight (kg)**
  - **MiscReference4** → **Biological Sample**
- Add a **Biological Sample** checkbox near Saturday Delivery
- Hide irrelevant shipping fields and controls
- Hide the **Rate** button in the shipping template

#### Profile and defaulting requirements
- Use the user’s spreadsheet/profile address data as the **Pickup From** address
- Populate:
  - `Custom1` → Protocol Number
  - `Custom2` → Study Reference Guide/Code
  - `Custom3` → Site Number
- Default shipment settings:
  - Description = `UN3373 Category B Human Sample`
  - Service = `UPS Express`
  - Terms = `Prepaid`
  - Weight Unit = `KG`
  - Return Delivery = `True`
  - Saturday Delivery = `True`
  - Biological Sample = `True`

#### Temperature-based logic
- Temperature options drive package weight defaults:
  - Ambient → 3
  - Frozen → 6
  - Refrigerated → 5
  - Ambient/Refrigerated Combo Box → 6
- If Temperature = **Frozen**:
  - User must enter dry ice weight
  - Dry ice field becomes editable/visible
- If Temperature is anything else:
  - Dry ice field should not be editable

#### Pickup logic
- If Pickup From / Consignee country is **not Canada**:
  - Automatically associate a pickup with the return label
- If Pickup From / Consignee country **is Canada**:
  - Hide the Pickup button

#### Server-side shipping rules
- For international return lanes:
  - Turn on **paperless invoice**
  - Set export reason to **Medical**
- Use **CS Adapter services** when Biological Sample is true
- Rate-shop and validate service selection:
  - US to US:
    - Prefer `NDA Early AM`
    - If not valid, fall back to `NDA without Saturday`
  - Cross-border / international:
    - Prefer `UPS Express with Saturday`
    - If not valid, fall back to `UPS Saver without Saturday`
- For biological shipments:
  - Set package extra `RESTRICTED_ARTICLE_TYPE = 32`
- For dry ice:
  - Populate dry ice weight from `MiscReference3`
  - Convert KG to LBS
  - Add dry ice weight to package weight
  - Set dry ice purpose to `Medical`
  - Apply the correct dry ice regulation set based on lane type

#### Backup pickup strategy
- If the client-side pickup association flow fails, the server must be able to:
  - Create or attach a `Pickup` object
  - Populate it from configurable user/custom mappings
  - Continue shipping without blocking the workflow

---

## 2) How the Blueprint Was Translated Into Code

### Implementation methodology

The blueprint was translated using a layered approach:

1. **Identify the minimum required hooks**
   - CBR: `PageLoaded`, `NewShipment`, `PreShip`
   - SBR: `PreShip`, `Ship`
   - Template: `shippingTemplate.html`

2. **Separate UI behavior from business enforcement**
   - CBR manages screen flow, defaults, and user interaction
   - SBR enforces final shipping logic and carrier/service validation

3. **Centralize business logic in helper/manager classes**
   - Avoids duplicating logic between hooks
   - Makes the SBR methods thin delegation layers

4. **Map blueprint fields to ShipExec objects**
   - Profile fields and custom values are translated into `PackageDefaults`, `MiscReference` fields, and `PackageExtras`

5. **Implement fallback behavior for risk points**
   - Client-side pickup association can fail, so server-side fallback is preserved
   - Rate validation can reject the preferred service, so fallback services are selected

### Resulting implementation structure
- **CBR** prepares the shipment and the UI
- **Template** visually reflects the returns workflow
- **SBR PreShip** authoritatively modifies shipment request values
- **SBR Ship** only intervenes if pickup association fallback is required

---

## 3) Code Flow: How SBR Hooks, CBR Hooks, and Templates Interact

## End-to-end flow

### 1. Page load
- `CBR PageLoaded`
  - Routes the user to the shipping page after login
  - Hides the Pickup button when the pickup-from country is Canada
  - Ensures the user sees the Marken returns workflow immediately

### 2. New shipment creation
- `CBR NewShipment`
  - Copies the user address into the consignee/pickup-from section
  - Populates:
    - Study Reference Guide/Code from Custom2
    - Protocol Number from Custom1
    - Site Number from Custom3
  - Applies default shipment values
  - Sets Biological Sample to true

### 3. User interaction in the template
- `shippingTemplate.html`
  - Shows renamed fields and returns-oriented labels
  - Shows the Biological Sample checkbox
  - Hides the Rate button
  - Shows Dry Ice Weight only when Temperature = Frozen
  - Hides Pickup for Canada lanes

### 4. Pre-ship client-side validation
- `CBR PreShip`
  - If pickup-from is non-Canada, attempts to trigger Pickup Request + Save
  - If Temperature is Frozen, requires Dry Ice Weight
  - Normalizes the shipment object before submission

### 5. Server-side authoritative validation
- `SBR PreShip`
  - Determines domestic vs international lane
  - Enables paperless invoice for international lanes
  - Selects correct service path and fallback
  - Handles biological sample package extras
  - Converts and applies dry ice values

### 6. Server-side backup
- `SBR Ship`
  - Only used if pickup association failed on the client side
  - Creates or attaches a Pickup object from configurable user data
  - Lets the normal ship flow continue

---

## 4) Design Patterns Used

### 1. Manager class pattern
The implementation uses manager classes to isolate major business areas:

- `ReturnsShipmentManager`
  - Owns returns shipping logic
  - Handles dry ice, international rules, and service selection
- `PickupAssociationManager`
  - Encapsulates fallback pickup creation/attachment
- `ReturnsFieldMappingHelper`
  - Centralizes custom field mapping and renamed field semantics
- `DryIceManager`
  - Encapsulates dry ice conversion and regulatory settings
- `ReturnsServiceSelectionManager`
  - Encapsulates service validation and fallback logic

### 2. Delegation from hooks to managers
The SBR hook code is intentionally thin:

- `SBR PreShip` delegates to `ReturnsShipmentManager.PreShip(...)`
- `SBR Ship` delegates to `PickupAssociationManager.Ship(...)`

This improves:
- readability
- testability
- reuse across hooks

### 3. Defensive programming
The code uses:
- null checks
- safe parsing
- reflection-based property reads
- fallback defaults

This is important because the blueprint depends on several user/profile/config values that may not always be populated.

### 4. Conditional workflow pattern
Behavior changes based on:
- country/lane
- temperature
- biological sample flag
- pickup availability

This is a classic rules-driven shipping workflow.

### 5. Configuration-driven fallback
Only pickup fallback is promoted to configurable `BusinessRuleSettings`.  
This matches the analysis result that server-side settings should remain minimal and only cover the values needed for Ship fallback.

---

## 5) File-by-File Breakdown

## Generated or modified files

### `shippingTemplate.html`
Primary UI implementation for the returns workflow.

#### Why it was generated
The blueprint explicitly requires UI changes:
- field renaming
- pickup visibility rules
- dry ice conditional display
- rate button hiding
- biological sample checkbox

#### Key changes
- Renamed tabs/labels to returns terminology
- Added `Biological Sample` checkbox near Saturday Delivery
- Conditionally show `Dry Ice Weight (kg)` when Temperature = Frozen
- Hide Pickup button when pickup-from country is Canada
- Hide Rate button

#### Why it matters
This is the user-facing entry point for the business process. Without these changes, the workflow would still look like standard parcel shipping instead of biological returns.

---

### `ReturnsShipmentManager.cs`
Central server-side business rule engine for `SBR PreShip`.

#### Why it was generated
The blueprint requires multiple server-side decisions that must be authoritative:
- lane detection
- paperless invoice
- export reason
- biological sample handling
- dry ice handling
- service fallback logic

#### Responsibilities
- Validate shipment request structure
- Detect domestic vs international returns
- Set paperless invoice and export reason
- Read `MiscReference4` biological sample flag
- Add `RESTRICTED_ARTICLE_TYPE`
- Read and convert dry ice from KG to LBS
- Apply dry ice purpose/regulation values
- Rate-shop service selection and fallback

---

### `PickupAssociationManager.cs`
Server-side fallback handler for pickup association in `SBR Ship`.

#### Why it was generated
The blueprint says the client-side pickup association is preferred, but may fail.  
This class provides the backup strategy.

#### Responsibilities
- Check whether fallback pickup creation is enabled
- Avoid interfering when pickup already exists
- Build a `Pickup` object from user/custom data mappings
- Attach pickup to the shipment request
- Let default ship flow continue

---

### `Tools.cs`
Lightweight helper for reading `BusinessRuleSettings`.

#### Why it was generated
To simplify and standardize reading configuration values.

#### Responsibilities
- Get string setting values
- Get boolean setting values
- Keep SBR manager code cleaner

---

### CBR hook script file
This may be implemented in a dedicated CBR JavaScript/TypeScript hook module, depending on the ShipExec deployment structure.

#### Methods included
- `PageLoaded(location)`
- `NewShipment(shipmentRequest)`
- `PreShip(shipmentRequest, userParams)`

#### Why it was generated
The blueprint requires client-side workflow control and defaulting before shipment submission.

---

## 6) Testing, Deployment, and Future Enhancements

## Suggested testing

### Functional tests
Validate at least these core scenarios:

#### 1. US to US, non-frozen
- Default temperature selection
- Pickup button behavior
- No dry ice prompt
- Service fallback logic for domestic lane

#### 2. US to US, frozen
- Dry ice field appears
- Dry ice is required
- Dry ice weight converts from KG to LBS
- Package weight is adjusted

#### 3. Canada to Canada
- Pickup button hidden
- No pickup association attempt
- Correct domestic/international rule behavior

#### 4. Cross-border/international
- Paperless invoice enabled
- Export reason = Medical
- Service fallback logic applied
- Dry ice regulation set correctly

#### 5. Biological Sample = true
- `RESTRICTED_ARTICLE_TYPE = 32`
- CS adapter path behavior is respected

### Regression tests
- Hidden fields do not break bindings
- Renamed labels still map to the right properties
- Rate button remains hidden
- No duplicate pickup objects are created
- Existing shipping flows outside the Marken profile are unaffected

### Logging/observability tests
- Confirm log messages for:
  - pickup fallback
  - rate fallback
  - dry ice conversion
  - international rule application

---

## Deployment suggestions

### Configuration
Ensure these profile/business rule values are reviewed before production:
- Pickup fallback enable flag
- Pickup fallback custom field keys
- User profile custom mappings
- Temperature validation options
- Default country mapping for user addresses

### Environment validation
Before release:
- Verify the shipping template changes in a test profile
- Confirm CBR/SBR hook registration order
- Validate adapter availability for UPS and CS workflows
- Confirm rate-shopping behavior in the target ShipExec environment

### Rollout strategy
- Deploy to a staging or QA profile first
- Test all shipping lanes and temperature combinations
- Enable pickup fallback only after client-side pickup association is confirmed reliable
- Monitor logs during early production use

---

## Future enhancements

### Potential improvements
- Extract lane detection into a reusable country/lane service
- Add stronger validation messages for missing dry ice and temperature combinations
- Replace DOM-based button clicking with a more explicit UI event hook if supported
- Add audit logging for pickup fallback creation
- Add a dedicated config screen for pickup fallback mappings
- Improve service selection by integrating a real rate response parser instead of conservative service-symbol checks
- Expand support for additional biological sample packaging rules if Phase 2 adds them

---

## 7) Caveats and Manual Steps

### Caveats

#### 1. Pickup association may still be fragile on the client side
The blueprint explicitly warns that the CBR click/save approach may not always work.  
That is why the SBR Ship fallback exists.

#### 2. Dry ice unit assumptions must be verified
The blueprint says `MiscReference3` is entered in KG, and the server converts it to LBS.  
Confirm the downstream package weight unit expected by ShipExec before final deployment.

#### 3. Country normalization must be carefully handled
Rules differ for:
- US to US
- CA to CA
- cross-border
- international

Country strings like `USA`, `United States`, `Canada`, or `UK` must be normalized consistently.

#### 4. Field hiding and renaming can conflict
If a field is hidden both in the template and profile field options, ensure one source of truth is used consistently to avoid UI desynchronization.

#### 5. Adapter/service behavior may vary by environment
The blueprint references UPS and CS adapters, but actual carrier symbols and service names can vary by ShipExec profile and deployment.

---

### Manual steps likely needed

- Confirm the exact mapping of user spreadsheet columns to ShipExec user custom fields
- Verify profile-level hidden/default field settings
- Confirm BusinessRuleSettings keys for pickup fallback
- Validate that the target profile includes the expected UPS/CS adapter configuration
- Ensure the shipping template is deployed to the correct Marken profile only
- Re-test the pickup association flow in the real UI environment, since it depends on runtime DOM behavior

---

## File and hook summary

### CBR
- `PageLoaded`
  - Route user to shipping
  - Hide Pickup for Canada
- `NewShipment`
  - Set defaults from user profile
  - Map custom values to references
- `PreShip`
  - Attempt pickup association
  - Validate dry ice entry

### SBR
- `PreShip`
  - Enforce business rules
  - Apply paperless invoice/export reason
  - Apply biological sample and dry ice logic
  - Select or fallback services
- `Ship`
  - Fallback pickup creation/attachment

### Template
- `shippingTemplate.html`
  - Renamed fields
  - Added Biological Sample checkbox
  - Hidden Rate button
  - Conditional dry ice and pickup behavior

---

## Conclusion

This implementation turns ShipExec’s standard shipping UI into a Marken-specific biological returns workflow while keeping the business rules maintainable and testable. The approach is intentionally layered:

- **CBR** for user experience and initial defaulting
- **Template** for visual workflow changes
- **SBR** for authoritative validation and backup behavior

The largest operational risks are pickup association reliability and correct lane/country normalization, so those should receive the most attention during QA and rollout.