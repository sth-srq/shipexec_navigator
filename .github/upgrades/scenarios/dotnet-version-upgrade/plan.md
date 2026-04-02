# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade all 10 projects in ShipExecNavigator to .NET 10.0 (LTS)
**Scope**: 3 projects on .NET Framework 4.8 need migration; 7 projects already target .NET 10

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 10 projects total, only 3 require work (ClassicClassLibrary → SDK-style conversion). Remaining 7 already target net10.0. Straightforward upgrade with no deep dependency chains requiring staged validation.

---

## Tasks

### 01-prerequisites: Validate upgrade prerequisites

Verify that .NET 10 SDK is installed and that any `global.json` files in the repository are compatible with the .NET 10 SDK. If the SDK is missing, surface the download URL and block execution. If `global.json` pins an older SDK version, update the `rollForward` policy or version to allow .NET 10.

**Done when**: .NET 10 SDK confirmed present; no `global.json` is blocking the upgrade.

---

### 02-sdk-conversion: Convert legacy projects to SDK-style and net10.0

Three projects still use the legacy `.csproj` format (ClassicClassLibrary) and target .NET Framework 4.8:
- `ShipExecNavigator.Model` — 2 files, foundation library
- `ShipExecNavigator.ClientSpecificLogic` — depends on no other project in the solution
- `ShipExecNavigator.BusinessLogic` — depends on ShipExecNavigator.Model; 21 files

Convert all three to SDK-style format and change their `TargetFramework` to `net10.0`. Process in dependency order (Model first, then BusinessLogic) to ensure project references remain valid. ClientSpecificLogic has no intra-solution dependencies and can be converted in any order.

**Done when**: All three projects are SDK-style, `TargetFramework=net10.0`, and solution loads without project-level errors.

---

### 03-identity-package: Remove incompatible Microsoft.AspNet.Identity.Core

`Microsoft.AspNet.Identity.Core` 2.2.4 is incompatible with .NET 10 — no compatible version exists. It is explicitly referenced in `ShipExecNavigator.BusinessLogic` (and potentially `ShipExecNavigator.Model`). This is a .NET Framework–only package; the modern equivalent for .NET 5+ is `Microsoft.AspNetCore.Identity`.

Identify all usages of the package's types and namespaces in the affected projects. Depending on the scope of usage, either replace with `Microsoft.AspNetCore.Identity`, remove the dependency if the functionality is no longer needed, or stub the affected code. Document the approach chosen in the task progress.

**Done when**: `Microsoft.AspNet.Identity.Core` is removed from all project references; code compiles without referencing it.

---

### 04-behavioral-fixes: Review behavioral changes in BusinessLogic

The assessment identified 51 potential behavioral changes in `ShipExecNavigator.BusinessLogic`, all marked *Potential* (not blocking). Two patterns recur across many files:

- **`System.Xml.Serialization.XmlSerializer`** — behavioral differences in .NET 10 around exception handling and type resolution
- **`System.Net.Http.HttpContent`** — usage of `.ReadAsStringAsync().Result` (synchronous blocking) and `StringContent` constructor behavior changes

Review each occurrence and determine whether the current code needs adjustment. The `ReadAsStringAsync().Result` pattern is a common source of deadlocks in async contexts on modern .NET — evaluate whether async/await should be applied. Fixes that are straightforward should be applied; ambiguous cases should be documented.

**Done when**: All 51 occurrences reviewed; fixes applied where needed; remaining items documented with rationale for deferral.

---

### 05-validate: Build solution and verify upgrade

Build the full solution to confirm zero compilation errors across all 10 projects. Verify all projects now report `TargetFramework=net10.0`. Run any available automated tests to confirm no regressions were introduced.

**Done when**: Solution builds with 0 errors; all projects target net10.0; tests pass (or failures are pre-existing and documented).
