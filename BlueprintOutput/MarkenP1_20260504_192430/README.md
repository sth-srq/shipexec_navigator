# Phase 1 Biological Returns / Specimen Returns README

## Overview

This implementation customizes ShipExec into a dedicated **biological returns** workflow for Phase 1 of the Marken blueprint.

The solution converts a standard shipment entry screen into a **return-label-centric experience** where users:

- ship specimens/biological samples back to a pickup-from location,
- use standardized reference fields for protocol, study, and site tracking,
- optionally enter dry ice weight for frozen shipments,
- apply different handling rules depending on domestic vs. international lanes,
- automatically associate pickup data when possible,
- and fall back to server-side pickup creation when client-side association fails.

The blueprint is primarily a **shipping workflow transformation** rather than a new carrier integration. Most of the work is in:

- **one template**: `shippingTemplate.html`
- **three client-side hooks**: `PageLoaded`, `NewShipment`, `PreShip`
- **two server-side hooks**: `PreShip`, `Ship`

No report hooks were required.

---

## 1) Business Requirements Summary

### Primary business goal
Create a ShipExec shipping workflow for **biological returns / specimen returns** that behaves like a managed return-label process instead of a normal outbound shipment flow.

### Core requirements from the blueprint

#### User-facing screen changes
- Rename the shipping screen concept from regular shipment handling to **Pickup From**
- Rename `Consignee Reference` to **Temperature**
- Provide temperature options:
  - Frozen
  - Ambient
  - Refrigerated
  - Ambient/Refrigerated Combo Box
- Rename reference fields:
  - `Shipper Reference` → **Study Reference Code**
  - `MiscReference1` → **Protocol Number**
  - `MiscReference2` → **Site Number**
  - `MiscReference3` → **Dry Ice Weight (kg)**
  - `MiscReference4` → **Biological Sample**
- Hide the Rate button
- Hide unrelated fields that are not needed in this specialized workflow
- Default common shipment values such as:
  - description
  - service
  - terms
  - return delivery
  - Saturday delivery
  - weight units

#### Data mapping requirements
User profile and user custom fields must drive the shipment:

- User address data becomes the **pickup-from address**
- `Custom1` → Protocol Number / `MiscReference1`
- `Custom2` → Study Reference Code / `ShipperReference`
- `Custom3` → Site Number / `MiscReference2`

#### Temperature-dependent behavior
- Temperature selection changes package weight defaults:
  - Ambient → 3
  - Frozen → 6
  - Refrigerated → 5
  - Ambient/Refrigerated Combo Box → 6
- If **Frozen** is selected, the user must enter **Dry Ice Weight**
- If temperature is not Frozen, dry ice weight must not be editable

#### Pickup behavior
- If pickup-from country is **not Canada**, automatically trigger pickup association
- If pickup-from country **is Canada**, hide the Pickup button
- If client-side pickup association fails, a server-side backup must create or attach a Pickup object

#### Server-side shipping rules
- For international return labels:
  - enable paperless invoice
  - set export reason to Medical
- Use the correct service and adapter behavior based on:
  - biological sample flag
  - lane type
  - rate validation outcome
- If biological sample is true:
  - use CS Adapter services
  - set package extra `RESTRICTED_ARTICLE_TYPE = 32`
- If dry ice is present:
  - convert KG to LBS
  - set dry ice purpose to Medical
  - assign regulation set based on domestic vs. non-domestic logic
  - add dry ice weight to package weight

---

## 2) Methodology: How the Blueprint Was Translated into Code

The implementation was translated using a **requirements-to-hook mapping** approach:

### Step 1: Separate UI concerns from server enforcement
The blueprint clearly divides responsibilities:

- **Client-side business rules (CBR)** handle:
  - screen behavior
  - field defaults
  - auto-routing
  - button visibility
  - best-effort pickup association
- **Server-side business rules (SBR)** handle:
  - authoritative validation
  - carrier/service fallback logic
  - export/document settings
  - dry ice calculations
  - pickup fallback strategy

This separation ensures that the UI improves the user experience, while the server remains the final source of truth.

### Step 2: Normalize blueprint field names into ShipExec properties
The blueprint uses a mix of:
- visible labels
- profile fields
- reference fields
- package fields

The implementation translates those into code paths using the same underlying ShipExec data model:
- package-level reference fields
- shipment defaults
- profile/user data
- template bindings
- business rule hooks

### Step 3: Centralize logic in manager classes
The SBR analysis indicates the server hooks should be thin and delegate to a manager class. This is a good fit because:
- it keeps `PreShip` and `Ship` compact,
- it makes unit testing easier,
- and it isolates rule logic from framework plumbing.

### Step 4: Make pickup association resilient
The blueprint explicitly says client-side pickup association is preferred, but if it fails, a backup strategy must exist in `SBR Ship`.

So the final design uses:
1. CBR auto-click / save workflow
2. SBR fallback pickup association if needed

### Step 5: Keep the UI flexible with template bindings
The template is updated to expose only the fields the workflow needs, while hiding irrelevant standard shipping fields.

---

## 3) Code Flow: How CBR Hooks, SBR Hooks, and Templates Interact

## End-to-end workflow

### A. Template loads
`shippingTemplate.html` renders the custom biological returns screen.

It:
- renames fields
- hides unnecessary controls
- exposes Temperature and Biological Sample inputs
- adds Dry Ice Weight entry
- hides the Rate button
- hides the Pickup button when pickup-from country is Canada

### B. CBR `PageLoaded`
Runs when the shipping page loads.

Responsibilities:
- detect the shipping page
- enforce button visibility
- respond to Temperature changes
- disable Dry Ice Weight unless Temperature = Frozen
- help set package weight defaults
- auto-click Pickup if pickup-from country is not Canada

### C. CBR `NewShipment`
Runs when a new shipment is created.

Responsibilities:
- populate pickup-from address using user profile data
- map `Custom1/2/3` into reference fields
- apply default shipment settings
- preselect biological sample state
- seed shipment for return-label processing

### D. CBR `PreShip`
Runs before the shipment is sent to the server.

Responsibilities:
- make one last attempt to associate Pickup for non-Canada pickup-from shipments
- preserve Dry Ice Weight in `MiscReference3`
- preserve Biological Sample in `MiscReference4`

### E. SBR `PreShip`
This is the authoritative enforcement hook.

Responsibilities:
- determine lane type
- enable paperless invoice for international shipments
- set export reason to Medical
- select service/adapter behavior
- enforce biological sample package extras
- calculate dry ice weight in pounds
- set dry ice purpose and regulation set
- add dry ice to package weight
- apply service fallback rules

### F. SBR `Ship`
Backup path only.

Responsibilities:
- create or attach a Pickup object if client-side pickup association failed
- use business rule settings and user/profile data as needed
- allow the default ShipExec flow to continue if no fallback action is needed

---

## 4) Design Patterns Used

### 1. Manager class pattern
The SBR implementation is centered around a manager class such as:

- `BiologicalReturnsShipmentManager`

Why:
- isolates business logic from ShipExec hook signatures
- keeps hook methods thin
- makes rule behavior easier to test and maintain

### 2. Delegation pattern
The hook entry points delegate to helper methods and manager methods instead of embedding all logic inline.

Examples:
- `PreShip(...)` delegates to `BiologicalReturnsShipmentManager.PreShip(...)`
- `Ship(...)` delegates to `BiologicalReturnsShipmentManager.Ship(...)`

### 3. Rule-based branching
The implementation uses condition-driven rules for:
- domestic US-to-US
- Canada-to-Canada
- cross-border / international
- biological sample true/false
- Frozen vs. non-Frozen

This is a straightforward business-rules pattern that matches the blueprint well.

### 4. Defensive programming
Because ShipExec object models can vary by deployment/version, the code uses:
- null checks
- reflection-based property access in the SBR helper
- safe fallback defaults
- conditional UI access

This reduces runtime risk when a field or control name differs slightly from the blueprint assumption.

### 5. Best-effort + fallback architecture
Client-side behavior attempts to satisfy the workflow first. Server-side logic then guarantees correctness if the UI path fails.

This pattern is especially important for:
- Pickup association
- dry ice persistence
- service fallback
- shipping compliance settings

---

## 5) File-by-File Breakdown

## `shippingTemplate.html`

### Why it was generated/changed
This template is the main user interface transformation for the workflow.

### What it does
- Renames the **Consignee** tab to **Pickup From**
- Renames `Consignee Reference` to **Temperature**
- Adds validation options for temperature values
- Renames reference fields to:
  - Study Reference Code
  - Protocol Number
  - Site Number
  - Dry Ice Weight (kg)
  - Biological Sample
- Hides the **Rate** button
- Hides the **Pickup** button when pickup-from country is Canada
- Hides unrelated shipping fields called out in the blueprint
- Keeps the screen focused on the return-label use case

### Why it matters
The workflow must feel like a specimen return form rather than a generic outbound shipment screen. The template enforces that experience visually.

---

## CBR: `PageLoaded`

### Why it was generated
To apply immediate UI behavior the moment the shipping page opens.

### Key behavior
- auto-route user to shipping page after login
- enable/disable Dry Ice Weight depending on Temperature
- hide/show Pickup button based on country
- auto-click Pickup for non-Canada pickup-from addresses

### Why it matters
This is the best place for page-level interaction logic because it runs early and can react to the current UI state.

---

## CBR: `NewShipment`

### Why it was generated
To initialize the shipment using the user’s stored return-label data.

### Key behavior
- copy user address data into pickup-from fields
- set `ShipperReference` from `Custom2`
- set `MiscReference1` from `Custom1`
- set `MiscReference2` from `Custom3`
- apply default shipment values
- default Biological Sample state

### Why it matters
This reduces manual input and ensures every new return shipment begins in a valid, standardized state.

---

## CBR: `PreShip`

### Why it was generated
To provide a final client-side check before submission.

### Key behavior
- attempt to persist Pickup association
- preserve Dry Ice Weight in `MiscReference3`
- preserve Biological Sample in `MiscReference4`

### Why it matters
This hook bridges the UI and the server. It ensures the server receives the data the template collected.

---

## SBR: `PreShip`

### Why it was generated
This is the main enforcement point for shipping rules.

### Key behavior
- detect shipment lane
- enable paperless invoice for international returns
- set export reason to Medical
- apply adapter/service selection logic
- enforce biological sample package extras
- process dry ice values
- adjust package weight

### Why it matters
Anything related to carrier compliance, shipment validity, or regulatory handling must be enforced server-side.

---

## SBR: `Ship`

### Why it was generated
To serve as a backup pickup association path.

### Key behavior
- create or attach a Pickup if client-side association failed
- use configurable business rule settings
- preserve default ship flow when possible

### Why it matters
This prevents the workflow from failing when the UI cannot reliably attach the Pickup object.

---

## 6) Testing, Deployment, and Future Enhancements

## Testing recommendations

### Functional test cases
Test each of these scenarios end-to-end:

- Temperature = Frozen
  - Dry Ice Weight becomes editable
  - package weight defaults to 6
  - dry ice values are sent to SBR
- Temperature = Ambient
  - Dry Ice Weight is disabled
  - package weight defaults to 3
- Temperature = Refrigerated
  - package weight defaults to 5
- Temperature = Ambient/Refrigerated Combo Box
  - package weight defaults to 6
- Pickup-from country = Canada
  - Pickup button is hidden
- Pickup-from country != Canada
  - Pickup button is shown and auto-click behavior is triggered
- Biological Sample = true
  - CS Adapter path and package extra logic are activated
- International return
  - paperless invoice enabled
  - export reason = Medical
- Dry ice entered
  - KG converted to LBS
  - added to package weight
  - regulation set assigned correctly

### Regression test areas
- normal outbound shipments
- non-biological return shipments
- shipments without dry ice
- shipments with missing user custom values
- template rendering when fields are hidden
- fallback pickup creation in SBR Ship

### Validation checks
- ensure reference fields save in the expected location
- ensure service fallback rules do not override valid selections unnecessarily
- confirm weight conversion is accurate and not double-applied
- verify button click automation does not loop or fire twice

---

## Deployment recommendations

- Deploy template, CBR, and SBR together as a coordinated release
- Validate the ShipExec object model in the target environment before enabling the SBR fallback logic
- Confirm that business rule settings are populated if pickup fallback is enabled
- Use a lower environment to confirm:
  - field names
  - profile binding
  - button selectors
  - user custom field mappings

---

## Future enhancements

### Possible Phase 2 improvements
- WorldEase / POE support
- richer pickup address validation
- more explicit adapter service configuration
- better lane classification helper methods
- improved modal UX for entering dry ice
- admin-configurable temperature-to-weight mappings
- a dedicated return-label manager for all biological workflows

### Technical enhancements
- replace any reflection-based access with strongly typed object access if the ShipExec SDK supports it cleanly
- centralize field names and profile keys into constants
- add structured logging around pickup fallback and dry ice conversion
- add unit tests around the manager class rule logic

---

## 7) Caveats and Manual Steps

### Important caveats

#### 1. Country logic is not completely uniform in the blueprint
The blueprint references:
- Canada-specific hide/show logic
- US-to-US rules
- CA-to-CA rules
- cross-border movement

These terms should be verified against the actual business meaning in your deployment, especially around:
- what counts as “international”
- how Canada-specific shipments are classified
- whether the return-from address or consignee address is the authoritative country field

#### 2. Client-side pickup association may be unreliable
The blueprint itself warns that the pickup click/save method may fail.

That means:
- the client workflow should be treated as best effort
- `SBR Ship` backup logic should remain available

#### 3. Dry ice conversion requires precision
Dry ice is entered in KG but added to package weight in LBS.

Be careful with:
- rounding
- unit conversion consistency
- whether package weight is stored as decimal, double, or string in your ShipExec model

#### 4. Hidden field behavior may vary by profile/template
Some fields are hidden via:
- profile field options
- template markup
- or both

Those settings should be tested together to ensure there is no conflict.

#### 5. Biological Sample and Temperature dependencies must stay synchronized
The UI must keep:
- Temperature selection
- dry ice editability
- Biological Sample value
- package weight default

in sync to avoid user confusion.

---

## Manual setup steps

Before enabling in production, confirm:

- user profile address data is complete
- `Custom1`, `Custom2`, and `Custom3` are populated
- the temperature validation list exists exactly as expected
- `MiscReference3` and `MiscReference4` are available on the shipment/package object model
- the Pickup button selector matches the deployed template
- business rule settings for fallback pickup are configured if needed
- carrier/service names match the actual ShipExec service catalog in your environment

---

## Implementation Summary

This blueprint was implemented as a **return-label workflow overlay** on ShipExec:

- the **template** reshapes the UI,
- the **CBR hooks** provide client-side defaults and interaction,
- the **SBR hooks** enforce compliance and shipping rules,
- and the **manager-based design** keeps the business logic maintainable.

The result is a specialized biological returns shipping experience with:
- standardized references,
- temperature-based behavior,
- dry ice support,
- pickup automation,
- and server-side fallback protection.