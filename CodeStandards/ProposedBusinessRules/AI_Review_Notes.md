# AI Review Pass 1 - Structural Analysis

## Reviewer Focus: Error Handling, Logging, Data Flow, and Silent Failures

### SBR Issues Found

| # | Severity | Location | Issue | Recommendation |
|---|----------|----------|-------|----------------|
| 1 | HIGH | `Load()` | No error handling - exceptions propagate unlogged | Wrap in try/catch, log before rethrow |
| 2 | HIGH | `PreShip()` | No null check on shipmentRequest | Add null guard with clear error message |
| 3 | HIGH | `PostShip()` | No null check on shipmentResponse before accessing `.PackageDefaults` | Add null guard, return early |
| 4 | MEDIUM | `PostShip()` | Exception in PostShip could lose the shipment result | Catch and log but don't rethrow (ship already happened) |
| 5 | MEDIUM | All hooks | Zero logging - impossible to debug in production | Add entry/exit logging to every hook |
| 6 | LOW | `Ship()` override | No documentation about what returning null means | Add XML comment explaining null = use default |
| 7 | LOW | `ParseBatchFile()` | Commented example code is functional but no guidance on when to use | Add decision guidance comments |

### CBR Issues Found

| # | Severity | Location | Issue | Recommendation |
|---|----------|----------|-------|----------------|
| 1 | HIGH | All hooks | Completely empty - provides zero guidance to developers | Add commented examples and documentation |
| 2 | HIGH | `Keystroke()` | No null checks - if shipmentRequest is null, any code added here crashes silently | Add defensive null check |
| 3 | MEDIUM | `PreShip()` | No example of cancellation pattern despite it being critical | Add throw/userParams examples |
| 4 | MEDIUM | `PostShip()` | No ErrorCode check example - developers won't know to check for failures | Document the success/failure pattern |
| 5 | LOW | `PageLoaded()` | No route-based dispatch example | Show the if/else pattern from SampleCBRCode |

### Data Flow Concerns

1. **userParams bridge**: The template doesn't demonstrate how CBR can set values in userParams 
   that the SBR reads. This is a critical pattern that most implementations need.

2. **Execution order**: No documentation about which fires first (CBR → SBR → CBR). Developers 
   often assume SBR fires first because it's "the server" and write broken code.

3. **Override hooks**: The `Ship()`, `Print()`, `Rate()` etc. override hooks return null but 
   don't explain that null means "proceed with default behavior". A developer might think 
   they need to return something.

---

# AI Review Pass 2 - Best Practices & Maintainability

## Reviewer Focus: Patterns, Separation of Concerns, Documentation Quality

### Architecture Recommendations

1. **Logging Strategy**: Every hook should log at entry minimum. Use structured logging with 
   the hook name and key parameter values. This costs nothing in performance but saves hours 
   in debugging.

2. **Error Handling Strategy**:
   - Pre-hooks (PreShip, PreLoad, etc.): Log and rethrow - the exception message IS the UI message
   - Post-hooks (PostShip, PostVoid, etc.): Log and swallow - the operation already happened
   - Override hooks (Ship, Print, Rate): Log and return null on failure - let the default run

3. **The Tools Anti-Pattern**: Creating `new Tools(Logger)` in every hook wastes memory. Consider 
   making it a class-level field initialized in the first hook call (lazy initialization).
   However, this is minor given the typical hook execution frequency.

4. **BusinessRuleSettings Access**: The template should show the safe pattern:
   ```csharp
   string value = new Tools(Logger).GetStringValueFromBusinessRuleSettings("key", BusinessRuleSettings);
   if (string.IsNullOrEmpty(value)) { /* handle missing config */ }
   ```

5. **CBR Console Logging**: Use `console.log('[CBR] HookName')` prefix pattern in every hook.
   This makes it trivial to filter browser console output to just CBR activity.

### Documentation Quality

The original template has good XML docs on parameters but ZERO guidance on:
- When each hook fires in the overall flow
- What returning null vs a value means in override hooks  
- How to cancel an operation (throw vs userParams)
- The CBR → SBR → CBR execution order
- Common real-world use cases for each hook

### Proposed Changes Applied

All recommendations from both review passes have been incorporated into the proposed 
`SoxBusinessRules.cs` and `ClientBusinessRules.js` files in this folder.

Key improvements:
- Every hook has entry logging
- Critical hooks have try/catch with appropriate error handling strategy
- Comments explain WHY (not just what) for every section
- Examples show common patterns commented out (ready to uncomment)
- Execution order is documented at class level and per-hook
- Override hooks clearly document the null = default behavior
- CBR includes defensive null checks and the userParams bridge pattern
