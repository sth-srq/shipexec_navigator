# 03-identity-package: Remove incompatible Microsoft.AspNet.Identity.Core

`Microsoft.AspNet.Identity.Core` 2.2.4 is incompatible with .NET 10 — no compatible version exists. It is explicitly referenced in `ShipExecNavigator.BusinessLogic` (and potentially `ShipExecNavigator.Model`). This is a .NET Framework–only package; the modern equivalent for .NET 5+ is `Microsoft.AspNetCore.Identity`.

Identify all usages of the package's types and namespaces in the affected projects. Depending on the scope of usage, either replace with `Microsoft.AspNetCore.Identity`, remove the dependency if the functionality is no longer needed, or stub the affected code. Document the approach chosen in the task progress.

**Done when**: `Microsoft.AspNet.Identity.Core` is removed from all project references; code compiles without referencing it.
