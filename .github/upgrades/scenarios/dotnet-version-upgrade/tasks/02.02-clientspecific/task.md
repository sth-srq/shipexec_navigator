# 02.02-clientspecific: Convert ShipExecNavigator.ClientSpecificLogic to SDK-style + net10.0

## Objective
Convert ShipExecNavigator.ClientSpecificLogic from legacy ClassicClassLibrary format to SDK-style and set TargetFramework to net10.0.

## Scope
- Project: ShipExecNavigator.ClientSpecificLogic\ShipExecNavigator.ClientSpecificLogic.csproj
- No intra-solution project dependencies

## Steps
1. Run convert_project_to_sdk_style tool
2. Update TargetFramework from net48 to net10.0
3. Remove packages.config if present
4. Build project individually to validate

**Done when**: Project is SDK-style, targets net10.0, builds successfully.
