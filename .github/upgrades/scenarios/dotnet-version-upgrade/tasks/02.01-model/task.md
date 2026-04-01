# 02.01-model: Convert ShipExecNavigator.Model to SDK-style + net10.0

## Objective
Convert ShipExecNavigator.Model from legacy ClassicClassLibrary format to SDK-style and set TargetFramework to net10.0.

## Scope
- Project: ShipExecNavigator.Model\ShipExecNavigator.Model.csproj
- 2 source files
- Foundation library (no project dependencies)

## Steps
1. Run convert_project_to_sdk_style tool
2. Update TargetFramework from net48 to net10.0
3. Remove packages.config if present
4. Build project individually to validate

**Done when**: Project is SDK-style, targets net10.0, builds successfully.
