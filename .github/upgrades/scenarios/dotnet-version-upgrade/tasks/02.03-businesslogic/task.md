# 02.03-businesslogic: Convert ShipExecNavigator.BusinessLogic to SDK-style + net10.0

## Objective
Convert ShipExecNavigator.BusinessLogic from legacy ClassicClassLibrary format to SDK-style and set TargetFramework to net10.0.

## Scope
- Project: ShipExecNavigator.BusinessLogic\ShipExecNavigator.BusinessLogic.csproj
- 21 source files
- Depends on ShipExecNavigator.Model (must be completed first)

## Steps
1. Confirm 02.01-model is complete
2. Run convert_project_to_sdk_style tool
3. Update TargetFramework from net48 to net10.0
4. Remove packages.config if present
5. Note: Microsoft.AspNet.Identity.Core is incompatible — it will be handled in task 03-identity-package. Do not remove it here; just ensure the project converts cleanly.
6. Build project individually to validate (ignore incompatible package errors — addressed in task 03)

**Done when**: Project is SDK-style, targets net10.0, format conversion is clean.
