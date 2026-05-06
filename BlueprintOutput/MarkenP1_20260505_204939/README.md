# Marken ShipExec Phase 1 — Specimen Returns README

## Overview

This implementation packages the Phase 1 Marken specimen-returns blueprint into a ShipExec customization focused on one primary workflow:

- Create return labels from a user’s saved pickup-from address
- Capture specimen metadata through renamed reference fields
- Support temperature-based package defaults
- Enforce dry ice requirements for frozen samples
- Apply server-side shipping rules for domestic and international returns
- Provide a fallback pickup association path if the browser-side flow fails

The solution is intentionally split between:

- **CBR (Client Side Business Rules)** for UI behavior, defaulting, and user guidance
- **SBR (Server Side Business Rules)** for authoritative validation and shipping enforcement
- **Template changes** for screen layout and field presentation

---

## 1) Business Requirements Summary

The blueprint describes a specialized shipping workflow for biological/specimen return labels.

### Core business goals

- Rename the shipping experience to match return-label language:
  - “Consignee” becomes **Pickup From**
  - reference fields become specimen-related fields such as:
    - **Temperature**
    - **Protocol Number**
    - **Site Number**
    - **Dry Ice Weight (kg)**
    - **Biological Sample**
- Prepopulate the shipment from the user profile and user custom fields
- Hide shipping fields that are not relevant to this workflow
- Default common shipping values:
  - Description
  - Service
  - Terms
  - Return Delivery
  - Saturday Delivery
  - Ship Date
  - Units
- Enforce temperature-based behavior:
  - Temperature drives the default package weight
  - Frozen shipments require dry ice entry
- Handle biological sample shipments with special carrier metadata
- Apply international return paperwork rules:
  - Paperless invoice
  - Export reason = Medical
- Support service fallback logic based on rate-shopping validation
- Provide a backup pickup association strategy if the browser-side pickup flow fails

### Important user-driven mappings

- User address → Pickup From / Consignee
- User `Custom1` → Protocol Number
- User `Custom2` → Study Reference Code
- User `Custom3` → Site Number
- Temperature selection → package weight and dry ice behavior
- Biological Sample checkbox → shipment-level carrier extras

---

## 2) Methodology: How the Blueprint Was Translated into Code

The implementation was derived by classifying each requirement into one of three categories:

### A. Template responsibilities
Used for changes that affect layout and visible field labels only.

Examples:
- Rename tab captions
- Hide the Rate button
- Add the Biological Sample checkbox
- Rename reference fields
- Add dry ice field UI affordances

### B. CBR responsibilities
Used for behavior that should feel immediate to the user.

Examples:
- Redirect user to Shipping page on login
- Copy user profile data into a new shipment
- Auto-fill temperature-based weight
- Toggle dry ice editability
- Store entered values into shipment fields before submit

### C. SBR responsibilities
Used for authoritative enforcement and any logic that must survive client-side failure.

Examples:
- Detect domestic vs. international return shipment
- Apply paperless invoice/export reason
- Convert dry ice from KG to LBS
- Set package extras for biological samples
- Provide backup pickup association logic

This separation prevents the UI from being the single source of truth for carrier-compliance rules.

---

## 3) Code Flow: How CBR Hooks, SBR Hooks, and Templates Interact

### High-level flow

1. **Template loads**
   - The shipping UI renders the custom fields and buttons
   - The Rate button is hidden
   - Biological Sample and Temperature fields appear in the right places

2. **CBR `PageLoaded` runs**
   - Redirects the user to the shipping page if needed
   - Hides the Pickup button when the pickup-from country is Canada

3. **CBR `NewShipment` runs**
   - Seeds the shipment from the logged-in user profile
   - Copies user address into Pickup From
   - Maps user custom values into reference fields
   - Applies default values like description, terms, service, and units

4. **CBR `PostLoad` / `Keystroke` run**
   - Updates package weight based on Temperature
   - Enables/disables Dry Ice Weight entry depending on Frozen selection
   - Reacts immediately when the user changes the temperature field

5. **CBR `PreShip` runs**
   - Ensures Frozen shipments have a dry ice value
   - Stores it in `MiscReference3`
   - Blocks ship if dry ice is missing

6. **SBR `PreShip` runs**
   - Enforces the final business rules on the server
   - Determines domestic vs. international
   - Sets paperless invoice and export reason
   - Converts dry ice and updates package totals
   - Adds `PackageExtras.RESTRICTED_ARTICLE_TYPE` for biological samples
   - Applies service defaults/fallbacks

7. **SBR `Ship` runs**
   - Acts as a backup path for pickup association
   - If client-side pickup saving fails, the server-side strategy can create or attach a pickup object

---

## 4) Design Patterns Used

### Manager class delegation
The SBR implementation is intentionally thin and delegates most logic to a manager class:

- `SpecimenReturnRulesManager.PreShip(...)`
- `SpecimenReturnRulesManager.Ship(...)`

This keeps the hook entrypoints readable and easier to maintain.

### Single-responsibility separation
Each layer owns a distinct concern:

- **Template**: presentation
- **CBR**: interactive UX and defaulting
- **SBR**: final rule enforcement

### Defensive normalization
The code checks for missing objects before using them:

- Create `PackageDefaults` if absent
- Create `Weight` objects if needed
- Create `PackageExtras` when biological flags are enabled
- Guard against null shipment and null pickup data

### Delegation for reusable rule logic
The implementation plan introduces helper/manager concepts such as:

- `SpecimenReturnRulesManager`
- `DryIceManager`
- `PickupAssociationManager`
- `ReferenceFieldMapper`
- `ServiceSelectionManager`

Even where not fully implemented, the architecture is clearly designed to centralize business rules instead of scattering them across hooks.

### Fallback-first design
The blueprint explicitly anticipates that the client-side pickup-save flow may fail, so the server side includes a fallback strategy rather than relying on UI success.

---

## 5) File-by-File Breakdown

## `shippingTemplate.html`

### Purpose
This is the only template explicitly required by the blueprint.

### What it generated and why

- **Rename “Consignee” to “Pickup From”**
  - Matches the return-label workflow language
- **Rename reference fields**
  - `ConsigneeReference` → Temperature
  - `ShipperReference` → Study Reference Guide
  - `MiscReference1` → Protocol Number
  - `MiscReference2` → Site Number
  - `MiscReference3` → Dry Ice Weight (kg)
  - `MiscReference4` → Biological Sample
- **Hide the Rate button**
  - The workflow is meant to be guided, not user-rated
- **Add Biological Sample checkbox**
  - The blueprint calls for a checkbox near Saturday Delivery
- **Add dry ice field behavior**
  - Disabled unless Temperature = Frozen
- **Provide pickup button hook**
  - Actual visibility logic is handled in CBR `PageLoaded`

### Notes
The template is the best place to present the workflow correctly, but it should not contain authoritative business logic.

---

## CBR hook file

The implementation plan defines these hooks as required:

### `PageLoaded(string location)`
Why it exists:
- Redirect users to the shipping page on login
- Hide Pickup button for Canada pickup-from addresses

### `NewShipment(ShipmentRequest shipmentRequest)`
Why it exists:
- Copy the user’s address into the shipment as Pickup From
- Map user custom values into reference fields
- Apply default specimen-return settings

### `PostLoad(string loadValue, ShipmentRequest shipmentRequest)`
Why it exists:
- Set initial package weight from Temperature
- Enable/disable dry ice editing based on Frozen

### `Keystroke(ShipmentRequest shipmentRequest, object vm, object event)`
Why it exists:
- React immediately to temperature changes
- Keep the temperature-driven weight logic responsive

### `PreShip(ShipmentRequest shipmentRequest)`
Why it exists:
- Require Dry Ice Weight for Frozen samples
- Copy the dry ice value into `MiscReference3`

### `PostBuildShipment(ShipmentRequest shipmentRequest)`
Why it exists:
- Normalize the final shipment object before sending it to the server
- Preserve Biological Sample and dry ice values

### Why this file matters
This is where the UI becomes a workflow instead of a generic shipping screen.

---

## SBR hook file

### `PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)`
Why it exists:
- Final server-side enforcement of specimen-return rules
- Apply international return logic
- Convert and apply dry ice weight
- Set package extras for biological samples
- Normalize service and default settings

### `Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)`
Why it exists:
- Backup strategy when pickup association fails client-side
- Allows the server to recover from UI flow issues

### Analysis outcome
The blueprint does **not** require additional SBR hooks such as rate, void, manifest, or post-ship behavior, so those were intentionally omitted.

---

## Helper / manager class

### `SpecimenReturnRulesManager`
Generated as the central rule coordinator for server-side logic.

Responsibilities:
- Determine whether shipment is domestic or international
- Apply paperless invoice and export reason rules
- Process biological sample settings
- Convert dry ice KG → LBS
- Support pickup fallback behavior

### Why this pattern was used
It keeps SBR hooks concise and easier to audit.

---

## 6) Testing Suggestions

## Functional test scenarios

### Domestic US-to-US
- Temperature = Ambient
- Confirm package weight defaults to 3
- Verify normal shipping service behavior
- Confirm no international paperwork is added

### Frozen specimen
- Temperature = Frozen
- Confirm Dry Ice Weight becomes required
- Verify `MiscReference3` is populated
- Confirm server converts dry ice KG to LBS
- Confirm weight is increased by dry ice amount

### International return
- Pickup From country differs from return-to country
- Confirm:
  - Commercial invoice method = 1
  - Export reason = Medical

### Biological sample
- Biological Sample = true
- Confirm `PackageExtras["RESTRICTED_ARTICLE_TYPE"] = "32"`

### Canada pickup-from address
- Confirm Pickup button is hidden
- Confirm pickup flow is not offered in the UI

### Non-Canada pickup-from address
- Confirm pickup action is available
- Validate pickup association behavior

## Regression checks
- Existing non-specimen shipments should not be affected
- Fields hidden in the template should still be safely handled server-side
- Shipping should still work if the client-side pickup association fails

---

## 7) Deployment Suggestions

### Recommended rollout order
1. Deploy the template changes
2. Deploy CBR logic
3. Deploy SBR logic
4. Validate in staging with real profile data
5. Promote to production after full scenario testing

### Environment checks
- Confirm the UPS adapter credentials are configured correctly
- Confirm any CS adapter dependencies are available when Biological Sample is true
- Confirm profile-level defaults exist for shipper and return address
- Confirm the temperature validation list values are available per user profile

### Deployment caveat
Because the blueprint says the CS adapter and WorldEase data are handled in a separate phase, Phase 1 should not depend on that functionality.

---

## 8) Future Enhancements

The blueprint and implementation plan suggest several natural next steps:

### Service-selection refinement
- Add more precise rate-shopping fallback logic
- Centralize service symbol comparison in a helper class

### Pickup creation robustness
- Implement full server-side pickup creation if client-side association remains unreliable
- Persist pickup linkage back to shipment records

### Better dry ice handling
- Add stronger validation for dry ice values
- Support unit conversion or normalization in a dedicated helper

### Expanded audit logging
- Log country comparisons, service changes, and biological sample decisions
- Improve troubleshooting for operational teams

### Additional adapter support
- Add CS adapter-specific logic once WorldEase and POE data are ready

---

## 9) Caveats and Manual Steps

### Caveats

- The blueprint mixes UI behavior and authoritative business rules, so some logic must remain split between CBR and SBR.
- Pickup association on the client may be brittle; the server-side fallback should be implemented and tested carefully.
- Dry ice conversion must happen exactly once.
- The Temperature validation list is profile-dependent and may differ by user profile.
- Hiding a field in the template does not remove the need for backend enforcement.

### Manual steps likely required

- Configure profile field options for:
  - renamed labels
  - hidden fields
  - default values
- Confirm validation list values for Temperature by profile
- Load user custom values into the source user profile data
- Confirm adapter credentials and service mappings
- Verify the return address and shipper-level defaults are populated at the site/profile level

---

## 10) Summary of What Was Generated

### Generated artifacts
- `shippingTemplate.html`
- CBR hook logic:
  - `PageLoaded`
  - `NewShipment`
  - `PostLoad`
  - `Keystroke`
  - `PreShip`
  - `PostBuildShipment`
- SBR hook logic:
  - `PreShip`
  - `Ship`
- `SpecimenReturnRulesManager` helper class

### Why this structure was chosen
It matches the blueprint’s division of responsibilities:
- UI presentation in the template
- Workflow convenience in CBR
- Enforcement and recovery in SBR

---

## 11) Final Implementation Notes

This Phase 1 solution is designed to be safe, maintainable, and traceable:

- **Safe** because server-side hooks enforce the rules even if the client fails
- **Maintainable** because business logic is centralized in manager classes
- **Traceable** because the implementation plan includes logging and defensive checks

The main manual integration point is ensuring the field names, profile options, and shipment object model used in ShipExec match the blueprint mappings exactly.