# Marken ShipExec Phase 1 — Biological Returns

## 1) Blueprint business requirements summary

This Phase 1 blueprint defines a returns-shipping workflow for Marken biological specimens in ShipExec. The goal is to repurpose the standard shipping screen into a controlled “Pickup From” returns experience while preserving the existing ShipExec model and enforcing the critical business rules on both client and server.

### Primary business goals
- Support biological returns labels for specimen shipments.
- Rename and reframe the standard shipping UI for a returns workflow:
  - `Consignee` becomes `Pickup From`
  - `ConsigneeReference` becomes `Temperature`
  - other reference fields are repurposed for protocol/study/site tracking
- Auto-populate shipment data from user/profile data.
- Hide irrelevant shipping controls to simplify the operator experience.
- Require dry ice entry only when temperature is `Frozen`.
- Automatically associate pickup data for non-Canada return shipments.
- Enforce shipping logic on the server, especially:
  - paperless invoice for international returns
  - medical export reason
  - service fallback logic via rate shopping
  - biological sample handling
  - dry ice weight conversion and package adjustment
- Provide a backup server-side pickup strategy if client automation fails.

### Key field mappings from the blueprint
- `ConsigneeReference` → `Temperature`
- `ShipperReference` → `Study Reference Code`
- `MiscReference1` → `Protocol Number`
- `MiscReference2` → `Site Number`
- `MiscReference3` → `Dry Ice Weight (kg)`
- `MiscReference4` → `Biological Sample`

### Default UI and shipment behaviors
- Default description: `UN3373 Category B Human Sample`
- Default terms: `Prepaid`
- Default service: `UPS Express`
- Default weight unit: `KG`
- Default return delivery: `True`
- Default Saturday delivery: `True`
- Hide standard fields that are not needed for this workflow
- Hide the Rate button in the shipping template

### Operational rules
- When `Temperature = Frozen`, show/enable dry ice entry.
- When temperature is anything else, dry ice should not be editable.
- For non-Canada pickup-from addresses, attempt to associate pickup automatically.
- For Canada pickup-from addresses, hide the Pickup button.
- Server-side logic must be authoritative even if the client-side UI automation fails.

---

## 2) Translation methodology: blueprint → code

The implementation was planned as a layered customization using three main areas:

1. **Template changes**
   - Adapt the shipping screen layout and labels
   - Hide irrelevant actions and controls
   - Expose only the fields needed for returns processing

2. **CBR hooks**
   - Handle UI behavior and user experience
   - Apply default values when the shipment is created
   - Manage page redirection, field visibility, and client-side pickup automation

3. **SBR hooks**
   - Enforce the business rules at the server layer
   - Apply shipping validation and fallback logic
   - Protect the workflow if the client-side automation does not complete successfully

### Design approach
- Use the template to shape the UI.
- Use CBR to initialize and assist the operator.
- Use SBR to guarantee correctness and compliance.

This separation keeps the UI responsive while ensuring that shipping rules cannot be bypassed.

---

## 3) Code flow: how SBR hooks, CBR hooks, and templates interact

### High-level flow
1. User logs in.
2. **CBR PageLoaded**
   - redirects the user to the shipping screen
   - initializes returns UI state
3. User starts a new shipment.
4. **CBR NewShipment**
   - maps user profile data into the shipment
   - sets reference values and shipment defaults
   - triggers pickup behavior for non-Canada shipments
5. **CBR PostLoad**
   - reacts to the loaded shipment
   - sets temperature-based weight defaults
   - toggles dry-ice editability
   - hides/shows Pickup button based on country
6. User prepares shipment.
7. **CBR PreShip**
   - makes one last attempt to associate pickup
   - ensures dry-ice data is preserved in the request
8. Shipment is submitted to server.
9. **SBR PreShip**
   - enforces the authoritative business rules
   - applies paperless invoice/export reason logic
   - validates/falls back on service selection
   - applies biological sample and dry-ice package extras
10. If pickup association still fails:
    - **SBR Ship** acts as fallback to create/attach pickup data

### Template interaction
The template is responsible for the visible form structure:
- labels
- tabs
- buttons
- field placement
- visual hiding/showing of controls

CBR then drives the dynamic behavior of those template elements:
- setting values
- toggling editability
- triggering button actions
- navigating pages

SBR remains the source of truth for shipping compliance and carrier logic.

---

## 4) Design patterns used

### Thin business rule delegates
The SBR analysis shows the generated business rules should remain thin:
- `SoxBusinessRules.PreShip(...)` delegates to `ReturnsShipmentManager.PreShip(...)`
- `SoxBusinessRules.Ship(...)` delegates to `ReturnsShipmentManager.Ship(...)`

This is a classic **delegation pattern**:
- the hook method is small
- the manager class owns the implementation
- runtime dependencies are injected into the manager

### Manager classes
The implementation plan intentionally splits responsibilities across helper classes:

- **ReturnsShipmentManager**
  - central orchestration for server-side return logic
- **TemperatureHandlingManager**
  - temperature-to-weight behavior
- **DryIceManager**
  - dry ice conversion and package extra population
- **PickupAssociationManager**
  - pickup creation/attachment logic and fallback handling
- **ReturnsServiceSelectionManager**
  - route/service validation and fallback decisions

This is a **single responsibility** style design that makes the logic easier to test and maintain.

### Defensive programming
The helper code uses:
- null checks
- safe parsing
- fallback defaults
- no-op behavior when optional data is missing

That matters because the blueprint leaves some data shapes partially unspecified, especially around user custom fields and pickup object details.

### Template-driven UI customization
The solution uses the existing shipping template and field options rather than introducing a new page type. That is a practical **configuration-over-code** approach:
- captions change without changing bindings
- visibility can be controlled from the UI layer
- business rules still remain consistent at the server layer

---

## 5) File-by-file breakdown

## Generated / modified files

### `shippingTemplate.html`
Purpose:
- Repurpose the standard shipping UI into a biological returns screen.

Why it was generated:
- The blueprint explicitly requires a custom shipping template for “Biological Returns Shipping Template.”

Key changes:
- Adds/retains the shipping page structure
- Renames the shipping context to `Pickup From`
- Displays reference fields relevant to the returns workflow
- Exposes:
  - Temperature
  - Study Reference Guide
  - Protocol Number
  - Site Number
  - Dry Ice Weight (kg)
  - Biological Sample
- Hides the Rate button
- Keeps Pickup available for client-side visibility/triggering
- Disables Dry Ice Weight unless Temperature is `Frozen`

Notes:
- The template uses Angular-style bindings and ShipExec field components.
- The `Pickup` action remains in the template so CBR can hide or trigger it depending on the country.

---

### `ReturnsShipmentManager.cs`
Purpose:
- Encapsulate all server-side logic for Phase 1 Marken returns.

Why it was generated:
- The SBR analysis explicitly requires all business logic to be encapsulated in helper classes with `SoxBusinessRules` acting only as a delegator.

Responsibilities:
- PreShip enforcement:
  - detect international returns
  - enable paperless invoice
  - set export reason to `Medical`
  - apply service fallback logic
  - process biological sample package extras
  - convert and apply dry ice weight
- Ship fallback:
  - create or attach a Pickup object if client-side pickup association failed

Design notes:
- This class is the main orchestration layer.
- It keeps the hook methods short and readable.
- It supports future extension without changing the business-rule entry points.

---

### `Tools.cs`
Purpose:
- Provide lightweight utility support for settings retrieval and helper functionality.

Why it was generated:
- The manager class references a helper toolset, and Phase 1 may later need configurable settings even though the blueprint says the SBR settings are currently `NA`.

Responsibilities:
- Retrieve string values from business rule settings
- Provide a place for future utility methods

Notes:
- This file is intentionally small.
- It is mostly scaffolding for future configuration growth.

---

## CBR hook implementation sections

These hooks are implemented in the client business rules file, typically the Thin Client/ShipExec CBR script module.

### `PageLoaded`
Purpose:
- Redirect the user to the shipping page after login.
- Initialize returns-specific UI state.

What it does:
- auto-navigates to shipping
- marks the UI as initialized
- optionally applies visibility/editability state if the shipment is already loaded

Why it exists:
- The blueprint explicitly says the user should be taken directly to the shipping page after login.

---

### `NewShipment`
Purpose:
- Seed a new return shipment from user profile and custom data.

What it does:
- maps the user’s address into the Consignee/Pickup From fields
- copies:
  - `Custom2` → Shipper Reference
  - `Custom1` → MiscReference1
  - `Custom3` → MiscReference2
- applies default values:
  - description
  - terms
  - service
  - weight unit
  - return delivery
  - Saturday delivery
- triggers pickup automation for non-Canada shipments

Why it exists:
- This minimizes operator work and ensures the shipment starts in the correct state.

---

### `PostLoad`
Purpose:
- React to loaded shipment data and adjust the UI accordingly.

What it does:
- reads `Temperature`
- sets package weight based on temperature:
  - Ambient → 3
  - Frozen → 6
  - Refrigerated → 5
  - Ambient/Refrigerated Combo Box → 6
- enables/disables `MiscReference3`
- prompts for dry ice when Frozen
- hides/shows Pickup based on country
- triggers Pickup if needed

Why it exists:
- This is the best hook for dynamic field control after data is visible.

---

### `PreShip`
Purpose:
- Final client-side safeguard before the shipment is sent.

What it does:
- preserves temperature and dry-ice values
- retries Pickup association
- attempts Save
- blocks shipment only if Frozen is selected and dry ice is missing

Why it exists:
- Client-side automation is helpful, but it should not be the only enforcement layer.

---

## SBR hook implementation sections

### `PreShip`
Purpose:
- Authoritatively enforce all returns shipping rules before shipment processing.

What it does:
- validates shipment request shape
- reads:
  - temperature
  - biological sample flag
  - dry ice weight
- detects international return shipments
- turns on paperless invoice
- sets export reason to `Medical`
- applies biological sample package extras
- converts dry ice from KG to LBS
- adds dry ice to package weight
- validates/falls back on service selection
- prepares CS-adapter-compatible behavior

Why it exists:
- The blueprint explicitly places the critical business logic here.

---

### `Ship`
Purpose:
- Provide a backup pickup-association strategy.

What it does:
- checks whether pickup already exists
- if not, and the shipment is cross-border/return-related:
  - creates a fallback pickup object
  - attaches it to the shipment
- otherwise returns control to normal ShipExec flow

Why it exists:
- The blueprint says the client-side pickup automation may fail, so SBR needs a fallback.

---

## 6) Suggestions for testing, deployment, and future enhancements

## Testing recommendations

### Functional test cases
1. **Canada to Canada return**
   - Pickup button should be hidden
   - no cross-border paperless invoice logic should fire
   - shipment should use normal returns defaults

2. **US to US return**
   - temperature defaults should apply
   - service validation should favor NDA Early AM
   - dry ice should be handled if Frozen is selected

3. **Cross-border return**
   - Pickup button should be available/triggered
   - paperless invoice should be enabled
   - export reason should be `Medical`
   - service fallback should be evaluated

4. **Frozen biological sample**
   - dry ice field becomes editable
   - dry ice must be captured
   - dry ice weight should be converted KG → LBS in SBR
   - package extras should include restricted article type

5. **Non-frozen temperature**
   - dry ice field should not be editable
   - weight defaults should still apply based on temperature

### Negative tests
- missing shipment packages
- missing or non-numeric dry ice weight
- missing pickup country
- client-side pickup click fails
- rate shopping unavailable or carrier response invalid

### Regression checks
- standard shipping flow still works for non-returns users
- hidden fields do not break the UI binding
- changing captions does not break data storage
- the shipping screen loads without template errors

---

## Deployment recommendations
- Deploy template and CBR changes together so the UI and behavior remain consistent.
- Deploy SBR changes with the manager class at the same time; the SBR hooks depend on it.
- Validate in a non-production environment first with:
  - a Canada profile
  - a US profile
  - a cross-border profile
  - a biological sample with dry ice
- Confirm that the adapter configuration values are correct before enabling production use.

---

## Future enhancements
- Add a dedicated modal for dry ice entry instead of using a prompt.
- Add stronger pickup-object mapping from the user spreadsheet/profile data.
- Externalize service-fallback thresholds into configurable settings.
- Add explicit logging around carrier/service fallback decisions.
- Add post-ship writeback if Marken needs downstream tracking integration.
- Add more robust country-pair classification for international return logic.
- Expand support for the CS/WorldEase/POE phase referenced in the blueprint.

---

## 7) Caveats and manual steps

### Important caveats
- The blueprint says **SBR configurable values are `NA`**, so no business rule settings keys are required for Phase 1.
- The CS Adapter / WorldEase / POE work is explicitly deferred to a separate phase.
- The implementation plan assumes `Custom1`, `Custom2`, and `Custom3` exist on the user profile and contain the expected data.
- The exact shape of the Pickup object may need adjustment once the ShipExec runtime object model is validated.
- The dry-ice conversion must be carefully rounded and tested to avoid package-weight drift.
- Client-side auto-click behavior may be unreliable in Thin Client timing scenarios, which is why the SBR fallback exists.

### Manual steps likely required
- Confirm field-option captions and visibility settings in the ShipExec profile configuration.
- Ensure the UPS adapter credentials/configuration are correctly installed:
  - UserID
  - Password
  - Access Key
- Confirm that the user spreadsheet/profile data is loaded into the expected custom fields.
- Verify that the shipping template is the active template for this workflow.
- Validate whether the Pickup button ID/click target matches the actual UI component name in the deployed environment.

---

## Summary

This Phase 1 implementation is a returns-focused ShipExec customization that combines:
- UI repurposing via the shipping template,
- operator assistance via CBR,
- and authoritative shipping enforcement via SBR.

The overall architecture is intentionally layered so that:
- the user gets a simple, guided returns experience,
- the shipping request stays compliant,
- and the server can recover when client automation is incomplete.