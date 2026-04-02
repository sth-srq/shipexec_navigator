# 01-prerequisites: Validate upgrade prerequisites

Verify that .NET 10 SDK is installed and that any `global.json` files in the repository are compatible with the .NET 10 SDK. If the SDK is missing, surface the download URL and block execution. If `global.json` pins an older SDK version, update the `rollForward` policy or version to allow .NET 10.

**Done when**: .NET 10 SDK confirmed present; no `global.json` is blocking the upgrade.
