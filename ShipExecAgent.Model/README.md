# ShipExecAgent.Model

Minimal domain model library containing the JWT payload type used during authentication.

---

## Classes

### `JWT`

Represents the JSON payload of a ShipExec Management Studio authentication token.

```csharp
public class JWT
{
    public string access_token  { get; set; }
    public string refresh_token { get; set; }
    // ... additional token fields
}
```

This object is deserialised from the raw JSON string the user pastes into the
Connect dialog (or stored in `DebugConfig.JwtToken` during development).

The `access_token` field is extracted by `JWTManager.GetAccessToken` and attached
as a `Bearer` header on every outbound HTTP request to the Management Studio API.

The `refresh_token` field is used by `AppManager.RefreshJwt` (legacy file-based
flow) to request a fresh access token before the current one expires.

---

## Usage

```csharp
// Deserialise
var jwt = JsonHelper.Deserialize<JWT>(rawJson);

// Extract Bearer token
var accessToken = jwt.access_token;

// Serialise for file persistence (legacy flow)
File.WriteAllText(path, JsonHelper.Serialize(jwt));
```

---

## Notes

- This project has no dependencies on other solution projects.
- It is referenced by `ShipExecAgent.BusinessLogic` only.
- The small scope is intentional — keeping authentication models isolated
  prevents accidental tight coupling between the model and business logic layers.
