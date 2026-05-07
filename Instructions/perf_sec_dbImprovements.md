# ShipExec Navigator — Security, Database & Performance Improvement Proposals

## Instructions for AI Chatbot

> You are reviewing the ShipExec Navigator codebase — a Blazor Server application (.NET 10) that communicates with a ShipExec Management Studio REST API. The DAL uses Dapper with raw SQL against SQL Server. The app includes an AI chat feature (OpenAI/Azure OpenAI) that can execute destructive actions on entities.
>
> Below is a comprehensive list of identified issues across Security, Database/ORM, and Performance. For each item, provide:
> 1. A brief description of the problem
> 2. The affected file(s) or layer
> 3. The risk/impact
> 4. A concrete fix or code approach
>
> Organize by category and priority (P0 = critical/immediate, P1 = high, P2 = medium, P3 = low).

---

## Category 1: Security

### P0 — Critical

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| S1 | API keys & credentials committed to Git in plaintext | `appsettings.json` | OpenAI key (`sk-proj-...`), Azure OpenAI key, SQL credentials (`User Id=progistics;Password=progistics`) are in a public GitHub repo. Keys are compromised. |
| S2 | No authentication or authorization on the application | `Program.cs` | No `AddAuthentication()`, `AddAuthorization()`, `UseAuthentication()`, or `UseAuthorization()`. The `/api/show-alert` endpoint and all Blazor circuits are publicly accessible. |
| S3 | AI prompt injection with destructive action execution | `AiChatService.cs` | User input is passed directly to GPT which returns executable actions (`user-delete`, `entity-delete`, `cbr-edit`, `javascript`). No confirmation gate between LLM response and execution for destructive actions. |

### P1 — High

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| S4 | Server-Side Request Forgery (SSRF) via user-controlled `adminUrl` | `ShipExecService.cs`, `AppManager.cs` | User provides `adminUrl` from the browser; server makes HTTP requests to that URL with Bearer tokens. Can target internal network, cloud metadata endpoints, or exfiltrate tokens. |
| S5 | JWT used without signature validation | `JWTManager.cs`, `AppManager.cs` | JWT is deserialized and `access_token` extracted without any cryptographic verification — no signature, expiration, audience, or issuer checks. |
| S6 | CSP allows `unsafe-inline` and `unsafe-eval` | `Program.cs` (line ~93) | Combined with the AI `javascript` action type, this creates a direct XSS execution path. |
| S7 | No rate limiting on any endpoint | `Program.cs` | No `Microsoft.AspNetCore.RateLimiting`. Attackers can spam AI endpoint (incurring costs), flood ApiLogs, or DoS via SignalR. |
| S8 | Circuit-level authorization not enforced | `ShipExecService.cs` | After auth is added, no validation that authenticated user owns `_currentCompanyId`. SignalR message manipulation could access other circuits' state. |

### P2 — Medium

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| S9 | File upload validates only extension | `UploadDialog.razor` | Only checks `.csv` extension — no MIME validation, no CSV injection protection, trivially bypassed. |
| S10 | `TrustServerCertificate=True` in connection string | `appsettings.json` | Disables SSL certificate validation for SQL connection — MITM vulnerability. |
| S11 | AI action TOCTOU gap | Blazor component layer | Confirmation dialog shows one action but payload could be tampered before execution. |
| S12 | PII stored unmasked in ApiLogs | `ApiLogManager.cs` | `RequestData`/`ResponseData` may contain user emails, addresses, phone numbers, shipper account credentials. |
| S13 | No audit trail enforcement (UserId) | `VarianceManager.cs` | `UserId` field exists but nothing enforces it's populated with the authenticated user. |
| S14 | Anti-forgery not enforced on minimal API | `Program.cs` | `/api/show-alert` endpoint doesn't validate antiforgery token despite `UseAntiforgery()` being called. |

### P3 — Low

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| S15 | Missing security headers | `Program.cs` | No `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`. |
| S16 | `AllowedHosts: *` — no host header filtering | `appsettings.json` | Allows host header injection attacks. |
| S17 | Sensitive data in structured logs | Serilog configuration, `AppManager.cs` | JWT lengths, admin URLs, full API payloads logged — risk if logs are accessed by unauthorized parties. |
| S18 | SignalR hub not hardened | `Program.cs` | `MaximumReceiveMessageSize` not capped, no circuit timeout for idle connections, `DetailedErrors` may leak in production. |

---

## Category 2: Database / ORM (Dapper)

### P0 — Critical

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| D1 | No indexes on `dbo.Variances` | `CreateShipExecNavigatorDatabase.sql` | Only PK index on `Id`. Queries filter by `CompanyId`, `IsActive`, and sort by `CreatedOn DESC` — full table scans on NVARCHAR(MAX) rows. |
| D2 | Unbounded `GetAllAsync()` on Variances | `VarianceManager.cs` | `SELECT * FROM dbo.Variances ORDER BY CreatedOn DESC` returns every row ever created with all MAX columns. Will OOM or timeout at scale. |
| D3 | MERGE without HOLDLOCK (race condition) | `TemplateManager.cs` | Two concurrent upserts for same `(CompanyId, TemplateId)` can both evaluate "WHEN NOT MATCHED" causing unique constraint violation. |

### P1 — High

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| D4 | N+1 batch upsert in transaction | `TemplateManager.UpsertBatchAsync()` | Executes N individual MERGE statements inside a transaction — N network round-trips while holding locks. |
| D5 | `SELECT *` everywhere with NVARCHAR(MAX) columns | All managers | Fetches multi-MB payload columns (`VarianceData`, `RequestData`, `ResponseData`, `TemplateData`) for list/summary views. |
| D6 | Suboptimal composite index on ApiLogs | `CreateShipExecNavigatorDatabase.sql` | Index is `(CompanyId)` alone but query is `WHERE CompanyId = @x ORDER BY OccurredOn DESC` — requires separate sort. |

### P2 — Medium

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| D7 | No data retention / archival strategy | All tables | `ApiLogs` and `Variances` are append-only with no TTL, partitioning, or archive mechanism. Unbounded growth. |
| D8 | Dual timestamp ownership (app vs DB default) | All managers | Code sets `DateTime.UtcNow` but DDL also has `DEFAULT SYSUTCDATETIME()`. Inconsistent ownership; clock drift risk. |
| D9 | No connection resiliency / retry logic | `SqlConnectionFactory.cs` | Raw `SqlConnection` with no retry policy. Transient failures immediately propagate. |
| D10 | No `CommandTimeout` override | All Dapper calls | Default 30s for unbounded queries on growing tables. |
| D11 | No optimistic concurrency | `TemplateManager`, `VarianceManager` | No `RowVersion` column — last write wins silently on concurrent edits. |
| D12 | Transaction isolation level unspecified | `TemplateManager.UpsertBatchAsync()` | Defaults to READ COMMITTED — partial state visible during batch. |
| D13 | `NVARCHAR(MAX)` for ASCII JSON data | All tables | 2 bytes/char for data that's always UTF-8 ASCII JSON. Double storage cost. |

### P3 — Low

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| D14 | Inconsistent soft/hard delete | Across tables | `Variances` has `IsActive` soft-delete; `CompanyTemplates` and `ApiLogs` use hard DELETE. |
| D15 | `IDbConnectionFactory` returns sync `IDbConnection` | `IDbConnectionFactory.cs` | Can't cleanly add `OpenAsync()` with retry through the interface. |
| D16 | No pagination support (only TOP) | `ApiLogManager.cs` | No OFFSET/FETCH — adding pagination later is a breaking change. |
| D17 | No connection pool monitoring | Infrastructure | Default 100 pool size with no health check or metrics. |
| D18 | No database migration framework | `Scripts\` folder | Manual SQL scripts with no tracking of applied migrations. |

---

## Category 3: Performance

### P0 — Critical

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| P1 | Sync-over-async `.Result` on 20+ HTTP calls | `AppManager.cs`, `RequestGenerationBase.cs` | Every outbound HTTP call blocks a thread pool thread. Under concurrency, causes thread starvation and potential deadlocks in Blazor Server. |
| P2 | N+1 user detail fetching (100+ sequential HTTP calls) | `ShipExecService.cs` (lines ~300, ~430) | Fetches user list then calls `GetUserDetail()` per user synchronously. 100 users = ~20s blocked. |
| P3 | Sequential 15+ API calls in `BuildEntityIndexAsync` | `ShipExecService.cs` | 15 independent entity fetches executed sequentially. Total wall time 30-60+ seconds. |

### P1 — High

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| P4 | `new HttpClient()` per request — socket exhaustion | `AppManager.cs`, `RequestGenerationBase.cs` | Creates/disposes HttpClient per call. Causes `TIME_WAIT` socket buildup and eventual `SocketException`. |
| P5 | O(n²) variance comparison | `RequestGenerationBase.GetVariances()` | `Find()` does linear scan per item. 500 entities = 250,000 comparisons. |
| P6 | Duplicate fetches (index build + tree expansion) | `ShipExecService.cs` | Same entities fetched twice — once for index, again when user expands category. No caching. |
| P7 | Blazor tree re-render cascading | `XmlNodeTree.razor` | Recursive tree with no virtualization. 500+ expanded nodes = hundreds of DOM diffs per interaction. |
| P8 | No outbound API throttle or circuit breaker | `AppManager.cs` | 20 parallel users × 15 parallel fetches = 300 concurrent outbound connections. Target API likely rate-limited. |

### P2 — Medium

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| P9 | Reflection per entity in `ExtractEntityFields()` | `ShipExecService.cs` | `GetProperties()` called for every entity — 10-100x slower than compiled access. |
| P10 | Full XML string round-trips for diff | `ShipExecService.GetDiffAsync()` | Serializes tree to XML, parses back, deserializes to objects, compares. Multi-MB strings copied multiple times. |
| P11 | `Task.Run()` wrapping synchronous code | `ShipExecService.cs` (all methods) | Doesn't make code async — just moves blocking to thread pool thread. Still consumes one thread per operation. |
| P12 | Dual JSON frameworks (Newtonsoft + System.Text.Json) | Multiple files | Double JIT cost, double allocations, potential behavior differences. |
| P13 | Unbounded in-memory entity index per circuit | `ShipExecService.cs` | `CategoryDetailsJson` dictionary holds 5-10MB of strings per browser tab for circuit lifetime. |
| P14 | SignalR payload size for large trees | Blazor rendering | 1000+ expanded nodes = large render batches over SignalR. |
| P15 | `ms.ToArray()` allocation in XmlRepository | `XmlRepository.cs` | Creates full copy of MemoryStream buffer (5MB+ for large XML). |

### P3 — Low

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| P16 | Repeated `string.Replace("AdministrationService", "UserManagerService")` | `AppManager.cs` | Same URL transformation computed on every call instead of cached at construction. |
| P17 | Shipper sort parses every element's ID XML | `XmlRepository.cs` | `int.TryParse(e.Element("Id")?.Value)` for 500 elements on every load. |
| P18 | RAG cosine similarity — brute-force linear scan | `InMemoryRagService.cs` | O(n) scan with full sort per query. Fine for ~2000 chunks but doesn't scale. |
| P19 | `JsonConvert.SerializeObject` per variance in hot path | `ShipExecService.BuildDiffResult()` | JSON serialization for every variance item during diff display. |
| P20 | No response caching for company data | `AppManager.cs` | No ETag, If-Modified-Since, or in-memory TTL cache. Every operation hits the API fresh. |

---

## Cross-Cutting / Architectural

| # | Issue | Affected Area | Description |
|---|-------|---------------|-------------|
| X1 | No health check endpoint | `Program.cs` | No `/health` endpoint to monitor SQL pool, API reachability, memory pressure, circuit count. |
| X2 | No structured error boundary | `AppManager.cs`, `ShipExecService.cs` | Raw exceptions propagate up through `Task.Run`. No categorization, no retry, no user-friendly messages. Unhandled exceptions kill Blazor circuits. |
| X3 | No correlation ID for distributed tracing | All layers | Cannot trace a user action from Blazor → ShipExecService → AppManager → external API → DAL. |

---

## Total Issue Count

| Category | P0 | P1 | P2 | P3 | Total |
|----------|----|----|----|----|-------|
| Security | 3 | 5 | 6 | 4 | **18** |
| Database | 3 | 3 | 7 | 5 | **18** |
| Performance | 3 | 5 | 7 | 5 | **20** |
| Cross-cutting | — | — | — | — | **3** |
| **Total** | **9** | **13** | **20** | **14** | **59** |

---

## Suggested Implementation Phases

### Phase 1 — Emergency (Week 1)
- **S1**: Rotate all keys, move to user-secrets/Key Vault, purge Git history
- **S2**: Add authentication (cookie or Windows auth for internal tool)
- **D1**: Add indexes on Variances (`CompanyId, IsActive, CreatedOn DESC`)
- **D6**: Replace ApiLogs index with composite `(CompanyId, OccurredOn DESC)`

### Phase 2 — Foundation (Weeks 2-3)
- **P4**: Inject `IHttpClientFactory` into `AppManager` / `RequestGenerationBase`
- **P5**: Dictionary-based variance lookup (O(n) → O(1))
- **D2**: Add mandatory pagination / TOP limit to `GetAllAsync()`
- **D3**: Add `WITH (HOLDLOCK)` to MERGE statements
- **S4**: Validate `adminUrl` against allowlist
- **S7**: Add rate limiting middleware
- **P16**: Cache computed URLs at construction

### Phase 3 — Async Conversion (Weeks 3-5)
- **P1/P11**: Convert `AppManager` methods to `async Task<T>` with `await`
- **P2**: Parallelize user detail fetching with `Task.WhenAll`
- **P3**: Parallelize independent entity fetches in `BuildEntityIndexAsync`
- **D9**: Add Polly retry policies on SQL connections

### Phase 4 — Optimization (Weeks 5-7)
- **P6**: Add in-memory cache for entity collections (serve tree from cache)
- **P7**: Implement virtualization on tree components
- **P8**: Add `SemaphoreSlim` throttle + Polly circuit breaker
- **P9**: Cache `PropertyInfo[]` by type
- **D5**: Create summary projection queries (no `SELECT *`)
- **D4**: Replace N+1 MERGE with TVP or bulk pattern

### Phase 5 — Hardening (Weeks 7-9)
- **S3**: Add confirmation gate + payload signing for AI actions
- **S5**: Validate JWT signature with `Microsoft.IdentityModel.Tokens`
- **S6**: Remove `unsafe-eval` from CSP; eliminate `javascript` action type
- **S8**: Per-operation company authorization check
- **D7**: Implement retention policy (archive rows > 90 days)
- **D11**: Add `RowVersion` for optimistic concurrency
- **X1**: Add health check endpoints
- **X2**: Implement structured error boundary with Polly

### Phase 6 — Polish (Ongoing)
- **P12**: Standardize on System.Text.Json with source generators
- **P13**: Lazy-load category details for AI context
- **D13**: Evaluate VARCHAR(MAX) migration for JSON columns
- **D18**: Adopt migration framework (DbUp or FluentMigrator)
- **X3**: Add correlation IDs across all layers
- **S12**: Implement PII masking before ApiLog persistence
