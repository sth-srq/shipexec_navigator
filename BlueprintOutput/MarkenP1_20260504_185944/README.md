# Marken Phase 1 — Biological Returns ShipExec README

## Overview

This implementation supports Marken’s Phase 1 specimen return workflow in ShipExec. The goal is to make the shipping UI behave like a specialized return-label experience while enforcing critical carrier and shipment rules on the server.

In short, the blueprint requires:

- A simplified shipping screen for biological returns
- User/profile-driven defaults for reference fields and return address data
- Temperature-based shipping logic
- Dry ice capture and conversion rules
- Biological sample handling
- Pickup association for return shipments
- Server-side enforcement for service selection and international return requirements

---

## 1) Business Requirement Summary

### Primary business objective
Support **biological specimen returns** in a way that is:

- Easy for users to complete
- Consistent with Marken return-label operations
- Safe from UI-only failures by enforcing key rules on the server

### Key functional requirements from the blueprint

#### UI and workflow simplification
- Redirect users to the shipping page on login
- Hide the **Rate** button
- Rename the shipping experience to better reflect returns:
  - **Consignee** tab → **Pickup From**
  - **Consignee Reference** → **Temperature**
  - **Shipper Reference** → **Study Reference Guide**
  - Custom reference captions renamed to:
    - Protocol Number
    - Site Number
    - Dry Ice Weight (kg)
    - Biological Sample

#### Defaulting and profile mapping
- User address data becomes the return pickup-from address
- User custom fields are mapped to return reference fields:
  - `Custom1` → Protocol Number
  - `Custom2` → Study Reference Code / Shipper Reference
  - `Custom3` → Site Number
- Several standard ShipExec fields must be hidden
- Standard defaults must be applied:
  - Description = `UN3373 Category B Human Sample`
  - Service = `UPS Express`
  - Terms = `Prepaid`
  - Weight unit = `KG`
  - Return Delivery = `True`
  - Saturday Delivery = `True`

#### Temperature and dry ice handling
- Temperature drives default package weight:
  - Ambient → 3
  - Frozen → 6
  - Refrigerated → 5
  - Ambient/Refrigerated Combo Box → 6
- If Temperature = Frozen:
  - Dry Ice Weight becomes editable
  - Dry Ice Weight is stored in `MiscReference3`
- If not Frozen:
  - Dry Ice Weight is hidden or read-only

#### Biological sample handling
- `MiscReference4` indicates Biological Sample
- If enabled:
  - package extra `RESTRICTED_ARTICLE_TYPE = 32` must be set
  - biological sample processing must be enforced server-side

#### Return pickup association
- For non-Canadian pickup-from addresses:
  - automatically associate a Pickup with the shipment
- For Canadian pickup-from addresses:
  - hide the Pickup button

#### Server-enforced shipping rules
On the server, the implementation must:
- Apply paperless invoice/export reason for international returns
- Select correct services based on shipment type and rate-shopping validation
- Apply dry ice settings and convert KG to LBS
- Add dry ice weight into shipment package weight
- Use CS adapter behavior for biological sample shipments
- Provide a fallback Pickup association strategy if the client-side flow fails

---

## 2) Methodology: How the Blueprint Was Translated into Code

The implementation was designed using a **layered responsibility model**:

### Client-side business rules (CBR)
Used for:
- UI defaults
- Field visibility
- Auto-routing
- User interaction flow
- Capturing input before submission

### Server-side business rules (SBR)
Used for:
- Authoritative shipping validation
- Carrier/service fallback logic
- Dry ice normalization
- Paperless invoice/export reason enforcement
- Pickup fallback when client-side logic is unreliable

### Template changes
Used for:
- Renaming labels/tabs
- Adding visible controls
- Hiding the Rate button
- Exposing the right fields for the Marken workflow

### Support classes / managers
Used to keep logic maintainable and testable:
- `ReturnShipmentManager`
- `PickupAssociationManager`
- `ServiceSelectionManager`

This separation ensures:
- UI behavior is responsive
- server rules remain authoritative
- business logic is centralized rather than scattered across hooks

---

## 3) Code Flow: How SBR Hooks, CBR Hooks, and Templates Interact

## End-to-end flow

### 1. Template loads
The shipping template is customized to present the Marken return workflow:

- “Pickup From” replaces the standard consignee presentation
- Temperature and Biological Sample fields appear in the Reference section
- Rate button is hidden
- Pickup button visibility is controlled dynamically

### 2. CBR `PageLoaded`
Runs when the page first loads.

Responsibilities:
- Redirect user to shipping page after login
- Initialize page-level UI behavior
- Prepare pickup-related display logic

### 3. CBR `NewShipment`
Runs when a new shipment is created.

Responsibilities:
- Default consignee/pickup-from address from user profile
- Copy user custom fields into shipment references
- Prepopulate the shipment with Marken defaults

### 4. CBR `PostLoad`
Runs after shipment data is loaded into the UI.

Responsibilities:
- Set package weight according to Temperature
- Show/hide or enable/disable Dry Ice Weight
- Hide the Pickup button for Canada
- Auto-click Pickup for non-Canada shipments when supported

### 5. CBR `PreShip`
Runs just before shipment submission from the client.

Responsibilities:
- Copy client-entered Dry Ice Weight into `MiscReference3`
- Try to run Pickup Request / Save for international returns
- Ensure the shipment payload is ready for server validation

### 6. SBR `PreShip`
Runs on the server before shipping is finalized.

Responsibilities:
- Enforce international return logic
- Apply paperless invoice and export reason
- Validate service and fallback service selection
- Apply biological sample package extras
- Convert and apply dry ice values
- Add dry ice weight into the package amount

### 7. SBR `Ship`
Backup only.

Responsibilities:
- Create or associate Pickup server-side if the CBR pickup flow fails
- Use user custom/profile data as fallback input

---

## 4) Design Patterns Used

### Manager class pattern
Business rules are encapsulated in dedicated helper classes rather than placed directly in hook bodies.

Examples:
- `ReturnShipmentManager`
  - Owns Marken return shipping server rules
- `PickupAssociationManager`
  - Owns pickup fallback logic
- `ServiceSelectionManager`
  - Owns service validation and fallback selection

Benefits:
- Cleaner hook code
- Easier testing
- Reusable logic
- Better separation of concerns

### Delegation pattern
The SBR hooks delegate to helper classes:

- `SBR PreShip` delegates to `ReturnShipmentManager.PreShip(...)`
- `SBR Ship` delegates to `PickupAssociationManager.Ship(...)`

This keeps hook code small and readable while isolating logic in reusable helpers.

### Guard clause pattern
The implementation uses early exits when:
- shipment objects are missing
- no packages exist
- required values are invalid

This prevents null reference failures and makes control flow easier to follow.

### Fallback pattern
The pickup association logic is designed as a fallback:
- first try client-side pickup creation/save
- if unreliable, use server-side pickup attachment/creation

This is explicitly aligned with the blueprint’s “backup” requirement for `SBR Ship`.

---

## 5) File-by-File Breakdown

## `shippingTemplate.html`

### Why it was generated/updated
This is the main UI surface for the Marken biological return workflow.

### What it changes
- Renames the consignee tab to **Pickup From**
- Adds a **Temperature** selector
- Adds **Dry Ice Weight (kg)** input, conditionally shown for Frozen
- Adds **Biological Sample** checkbox
- Hides the **Rate** button
- Keeps the Pickup button available only when appropriate
- Simplifies the user experience to match return-label operations

### Important code references
- `vm.openPickup()`
- `vm.rate()` hidden
- `field-select` for Temperature
- `field-input` for Dry Ice Weight
- `field-checkbox` for Biological Sample

---

## CBR hook: `PageLoaded`

### Why it was generated
To route users directly into the shipping experience after login.

### Responsibilities
- Auto-navigate to shipping page
- Initialize page state
- Support pickup-button state preparation

### Why it matters
The blueprint explicitly requires a streamlined workflow where users land directly on shipping after login.

---

## CBR hook: `NewShipment`

### Why it was generated
To initialize a return shipment with the correct user-derived data.

### Responsibilities
- Copy user address into consignee/pickup-from
- Copy `Custom2` into Shipper Reference / Study Reference Code
- Copy `Custom1` into MiscReference1 / Protocol Number
- Apply safe default values

### Why it matters
This is the primary place where user profile data is transformed into a return shipment.

---

## CBR hook: `PostLoad`

### Why it was generated
To react to shipment data after the screen renders.

### Responsibilities
- Set weight based on Temperature
- Control Dry Ice Weight editability
- Hide Pickup button for Canada
- Auto-click Pickup for non-Canadian shipment contexts where possible

### Why it matters
This hook handles dynamic UI behavior that depends on loaded shipment state.

---

## CBR hook: `PreShip`

### Why it was generated
To prepare the shipment payload before it is sent to the server.

### Responsibilities
- Persist Dry Ice Weight into `MiscReference3`
- Trigger Pickup Request / Save for non-Canadian pickups
- Provide a clean handoff to SBR

### Why it matters
This is the last client-side chance to prepare data before authoritative server processing.

---

## Optional CBR hook: `PostSelectAddressBook`

### Why it was included in the analysis
Because selecting an address book entry can change the country and affect pickup logic.

### Responsibilities
- Re-evaluate country rules after address selection
- Keep Pickup button visibility in sync
- Auto-click Pickup for non-Canada when appropriate

### Why it matters
Without this hook, the UI could become inconsistent after the user changes the pickup-from address.

---

## SBR hook: `PreShip`

### Why it was generated
This is the authoritative server-side enforcement point.

### Responsibilities
- Detect domestic vs international return shipments
- Set commercial invoice method and export reason for international shipments
- Apply biological sample package extras
- Convert dry ice from KG to LBS
- Apply dry ice regulation sets
- Add dry ice weight to package weight
- Validate and fallback service selection

### Key helper reference
- `ReturnShipmentManager.PreShip(...)`

### Why it matters
Even if the browser logic fails, shipping rules still must be enforced on the server.

---

## SBR hook: `Ship`

### Why it was generated
Backup strategy for pickup association.

### Responsibilities
- Create or attach Pickup server-side if client-side pickup creation fails
- Use user/profile data as a fallback source

### Key helper reference
- `PickupAssociationManager.Ship(...)`

### Why it matters
The blueprint explicitly identifies this as a fallback path only.

---

## Helper / manager classes

### `ReturnShipmentManager`
Owns the main server-side Marken return shipping behavior.

Responsibilities:
- Determine international return status
- Apply paperless invoice and export reason
- Apply biological sample package extras
- Apply dry ice rules
- Validate service choices

### `PickupAssociationManager`
Owns the pickup fallback logic.

Responsibilities:
- Infer pickup data from user parameters
- Attach or reconstruct pickup information when needed

### `ServiceSelectionManager`
Owns service-validation abstraction.

Responsibilities:
- Abstract rate-shopping-based service checks
- Enable fallback logic for service selection

---

## 6) Testing, Deployment, and Future Enhancements

## Testing recommendations

### UI / CBR tests
Test the following scenarios in the browser:
- Login routes directly to shipping
- New shipment defaults are populated correctly
- Temperature selections update package weight correctly
- Frozen enables Dry Ice Weight
- Non-Frozen disables/hides Dry Ice Weight
- Canada hides Pickup button
- Non-Canada shows or auto-clicks Pickup

### Server / SBR tests
Validate:
- Domestic US-to-US
  - service fallback behavior
  - Saturday delivery handling
- Cross-border and international returns
  - paperless invoice set
  - export reason set to Medical
- Biological Sample = true
  - package extra is applied
- Dry ice present
  - KG to LBS conversion
  - package weight adjustment

### Regression tests
- Standard shipments should not be broken
- Hidden fields remain hidden
- No accidental overwrite of unrelated shipment values
- Pickup fallback only runs when needed

---

## Deployment notes

- Deploy template changes together with CBR and SBR updates
- Ensure the configured profile field mappings match the blueprint
- Verify carrier credentials and adapter behavior before production rollout
- Confirm that service names like `NDA Early AM`, `NDA`, `UPS Express`, and `UPS Saver` are valid in the environment

---

## Future enhancements

Potential Phase 2 improvements:
- WorldEase / POE configuration support
- Better pickup creation UX
- Stronger validation of temperature and dry ice combinations
- More explicit audit logging for biological sample shipments
- Improved service lookup abstraction for carrier-specific rate shopping
- Additional automation around user profile provisioning

---

## 7) Caveats and Manual Steps

## Manual configuration still required

### Profile/field options
The blueprint requires profile-level configuration such as:
- renaming fields
- hiding standard fields
- setting default values
- defining validation lists

These are not purely code changes and may require manual admin setup.

### Carrier/service validation
The implementation depends on valid carrier/service naming and availability.

### Business rule settings
The blueprint states:
- **Configurable ShipExec Server Business Rules Values: NA**

So no custom SBR settings are required from the blueprint itself, but the environment must still support the selected carrier services and adapter behavior.

---

## Caveats

### Pickup flow reliability
The blueprint itself notes that client-side pickup association may fail.  
If so:
- use the SBR `Ship` fallback path
- do not rely solely on the client click/save flow

### Dry ice unit conversion
Dry Ice Weight is entered in kilograms but applied to package weight in pounds.  
This conversion must remain correct and consistent.

### Biological sample logic
The `MiscReference4` boolean must be treated carefully:
- only `true` should trigger restricted article behavior
- missing or malformed values should default safely

### International return determination
Country comparison must be accurate:
- pickup-from vs return-to
- domestic vs cross-border logic
- Canada-specific UI rules

### UI template coupling
The template and CBR logic are tightly coupled by field names and button names.
If template IDs or bindings change, the CBR selectors may need updates.

---

## Implementation Summary

This blueprint was translated into a clean layered implementation:

- **Template** handles labels, visibility, and field layout
- **CBR** handles UI defaults and interactive behavior
- **SBR** enforces shipment correctness and carrier rules
- **Managers** keep complex business logic isolated and testable

That structure aligns well with the blueprint’s intent:
- fast user experience on the front end
- authoritative enforcement on the server
- fallback paths for unreliable UI pickup handling

If you want, I can also produce a shorter “developer handoff” version of this README or a more formal “release notes” version.