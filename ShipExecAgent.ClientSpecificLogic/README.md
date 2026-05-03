# ShipExecAgent.ClientSpecificLogic

Plug-in library for per-client behavioural overrides.  When a company's name matches
a known client pattern, the resolver returns a specialised `IClientSpecificLogic`
implementation instead of the generic default.

---

## Contents

- [Purpose](#purpose)
- [Classes](#classes)
- [How to Add a New Client Override](#how-to-add-a-new-client-override)
- [Logging](#logging)

---

## Purpose

Different ShipExec customer deployments may require small behavioural differences in
how the Navigator processes or displays data (e.g. custom field mapping, suppression
of certain entity types, special import rules).

Rather than scattering `if (companyName == "Wesbanco")` checks throughout the main
codebase, all per-client logic is isolated here and accessed through a single resolver.

---

## Classes

### `IClientSpecificLogic`

The contract every client-specific implementation must fulfil.

```csharp
public interface IClientSpecificLogic
{
    // Override points defined here (e.g. transform methods, feature flags)
}
```

### `DefaultCompanyLogic`

The no-op default returned when no specific override is found.
Implements `IClientSpecificLogic` with standard behaviour for all methods.

### `WesbancoClientSpecificLogic`

Overrides for companies whose name contains `"wesbanco"` (case-insensitive).
Inherits from or wraps `DefaultCompanyLogic` and overrides only the methods
that differ for this client.

### `Wesbanco`

Supporting data or helper type for Wesbanco-specific processing.

### `ClientLogicResolver`

Static factory.  Call `Resolve(companyName)` to get the correct implementation.

```csharp
var logic = ClientLogicResolver.Resolve(companyName);
```

**Matching rules (evaluated in order):**

| Pattern | Returns |
|---|---|
| name contains `"wesbanco"` (case-insensitive) | `WesbancoClientSpecificLogic` |
| *(no match)* | `DefaultCompanyLogic` |

---

## How to Add a New Client Override

1. Create a new class implementing `IClientSpecificLogic`:
   ```csharp
   public class AcmeClientSpecificLogic : IClientSpecificLogic
   {
       // override methods that differ for Acme
   }
   ```

2. Add a matching branch in `ClientLogicResolver.Resolve`:
   ```csharp
   if (companyName.IndexOf("acme", StringComparison.OrdinalIgnoreCase) >= 0)
       return new AcmeClientSpecificLogic();
   ```

3. Add a supporting data class if needed (see `Wesbanco.cs` as reference).

---

## Logging

Non-DI logging via the static `LoggerProvider` gateway in
`ShipExecAgent.ClientSpecificLogic.Logging`:

```csharp
// ClientSpecificLogic\Logging\LoggerProvider.cs
internal static class LoggerProvider
{
    internal static void Initialize(ILoggerFactory factory) => ...
    internal static ILogger<T> CreateLogger<T>() => ...
}
```

Initialised in `Program.cs`:
```csharp
ShipExecAgent.ClientSpecificLogic.Logging.LoggerProvider.Initialize(loggerFactory);
```
