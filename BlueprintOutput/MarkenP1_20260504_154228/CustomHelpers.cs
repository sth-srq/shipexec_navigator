using System;
using System.Collections.Generic;
using System.Globalization;

public bool GetBoolSetting(string key, bool defaultValue)
{
    if (BusinessRuleSettings == null) return defaultValue;
    var setting = BusinessRuleSettings.Find(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
    if (setting == null || string.IsNullOrWhiteSpace(setting.Value)) return defaultValue;
    bool parsed;
    return bool.TryParse(setting.Value, out parsed) ? parsed : defaultValue;
}

public void EnsurePickupFromCustomData(ShipmentRequest request)
{
    // Optional fallback hook for associating a Pickup object from user custom data.
    // Left intentionally minimal because the blueprint marks this as backup-only.
}
