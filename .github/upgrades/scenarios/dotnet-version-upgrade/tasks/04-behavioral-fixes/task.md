# 04-behavioral-fixes: Review behavioral changes in BusinessLogic

The assessment identified 51 potential behavioral changes in `ShipExecNavigator.BusinessLogic`, all marked *Potential* (not blocking). Two patterns recur across many files:

- **`System.Xml.Serialization.XmlSerializer`** — behavioral differences in .NET 10 around exception handling and type resolution
- **`System.Net.Http.HttpContent`** — usage of `.ReadAsStringAsync().Result` (synchronous blocking) and `StringContent` constructor behavior changes

Review each occurrence and determine whether the current code needs adjustment. The `ReadAsStringAsync().Result` pattern is a common source of deadlocks in async contexts on modern .NET — evaluate whether async/await should be applied. Fixes that are straightforward should be applied; ambiguous cases should be documented.

**Done when**: All 51 occurrences reviewed; fixes applied where needed; remaining items documented with rationale for deferral.
