# 02-sdk-conversion: Convert legacy projects to SDK-style and net10.0

Three projects still use the legacy `.csproj` format (ClassicClassLibrary) and target .NET Framework 4.8:
- `ShipExecNavigator.Model` — 2 files, foundation library
- `ShipExecNavigator.ClientSpecificLogic` — depends on no other project in the solution
- `ShipExecNavigator.BusinessLogic` — depends on ShipExecNavigator.Model; 21 files

Convert all three to SDK-style format and change their `TargetFramework` to `net10.0`. Process in dependency order (Model first, then BusinessLogic) to ensure project references remain valid. ClientSpecificLogic has no intra-solution dependencies and can be converted in any order.

**Done when**: All three projects are SDK-style, `TargetFramework=net10.0`, and solution loads without project-level errors.
