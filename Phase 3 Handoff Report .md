```markdown
# Phase 3 → Phase 4 Handoff

**Project:** CipherVault — Personal Encrypted Password & Secure Notes Manager
**Stack:** ASP.NET Core 10 MVC + EF Core 10 + SQL Server LocalDB + ASP.NET Core Identity + Data Protection API
**Tag:** `phase-3-complete`
**Date:** 2026-09-21

---

## Completed Deliverables

### Feature work

- **Server-side search + category filter**
  - Repository: `IVaultItemRepository.SearchAsync(userId, searchTerm, category)`
  - Service: `IVaultItemService.SearchItemsAsync(...)` — whitelists category via `VaultCategory.IsValid`
  - Controller: thin `Index` action reads `search`/`category` from query string
  - View: `Views/Vault/Index.cshtml` with search input + category dropdown, renders `VaultIndexViewModel`

- **Dashboard stats**
  - `IDashboardService` / `DashboardService` — injects `ApplicationDbContext` directly (trivial read-only aggregates; no repository, per architecture rule)
  - `DashboardStatsDTO` with total count, per-category counts, last-5 items
  - `Views/Dashboard/Index.cshtml` with Chart.js doughnut + recent list (masked)

- **REST API under `/api/v1/vault`**
  - `Controllers/Api/VaultApiController.cs` — GET list, GET by id, POST, PUT, DELETE
  - `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` on the class
  - `?decrypt=true` returns `400 {"error":"decryption_not_supported"}` on list and single-item endpoints
  - Reuses `IVaultItemService` — no duplicated business logic

- **Auth API**
  - `Controllers/Api/AuthApiController.cs` — `POST /api/auth/login`
  - `ITokenService` / `TokenService` issues HMAC-SHA256 JWT with claims: `nameid`, `unique_name`, `email`, `jti`
  - `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)` — enforces Phase 1 lockout policy on API path
  - Uniform 401 for unknown email and wrong password (`{"error":"invalid_credentials"}`)
  - 423 `{"error":"account_locked"}` when the account is locked out

- **Postman collection**
  - `postman/CipherVault.postman_collection.json` — Login, List, Create, Get by id, Update, Delete, unauthorized case, `?decrypt=true` case
  - Login request has a Tests script that stores `accessToken` in the collection variable `token`

### Authentication infrastructure

- **Scheme separation in `Program.cs`**
  - Identity cookie scheme remains default for MVC (`IdentityConstants.ApplicationScheme`)
  - Named `Bearer` scheme registered via `.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, ...)`
  - `OnChallenge` handler suppresses redirect-to-login; forces JSON 401 with body `{"error":"unauthorized"}`
- JWT configuration (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`) in `appsettings.json`
- `Jwt:Key` supplied via user-secrets (dev) — never committed
- `SignInManager` / `UserManager` reused from Identity — no parallel user store

### HSTS / production readiness

- `builder.Services.AddHsts()` — relying on framework defaults (loopback exclusion left in place)
- `app.UseHsts()` gated inside `if (app.Environment.IsProduction())` in the pipeline
- Verified: `Strict-Transport-Security: max-age=2592000` appears when launched with `--launch-profile Production`
- Verified: no HSTS header in Development (loopback exclusion working as designed)

### Login lockout alignment (post-test fix)

- Scaffolded `Areas/Identity/Pages/Account/Login.cshtml` (+ `.cshtml.cs`) so the browser login form participates in lockout
- Flipped `lockoutOnFailure: false` → `true` in the scaffolded page
- Result: browser login and API login now share the same lockout behavior
- Only `Account/Login` was scaffolded; all other Identity pages remain the library versions (hybrid is deliberate — see Behavior notes)

### Bug fixes applied during Phase 3 testing

- `VaultItemRepository.SearchAsync` — corrected field name `_context` → `_db`
- `VaultItemService.SearchItemsAsync` — corrected field name `_repo` → `_repository`; switched category check to `VaultCategory.IsValid`
- `VaultController.Index` — uses existing `CurrentUserId` property (was calling a non-existent `_users.GetUserId(User)`)
- `Views/Vault/Index.cshtml` — iterates `Model.Items` (was iterating the ViewModel itself)
- `VaultApiController` — uses `Async`-suffixed service method names; `Create` re-reads the created item to return the display DTO
- `Program.cs` — removed duplicate `using CipherVault.Services;` / `using CipherVault.Services.Contracts;` (CS0105); HSTS options moved from `UseHsts(...)` (invalid overload) to `AddHsts(...)`
- JWT secret regenerated with `RandomNumberGenerator.Create().GetBytes()` (Windows PowerShell 5.1 does not have `RandomNumberGenerator.Fill`)

---

## Database State

- Migrations: `InitialIdentitySchema`, `AddVaultItems` (no new migration in Phase 3 — search/filter is LINQ-only, no schema change)
- Tables: `AspNetUsers...`, `VaultItems`
- Indexes: `IX_VaultItems_UserId`, `IX_VaultItems_UserId_Category` (the composite index covers the filtered search path)
- FK: `VaultItems.UserId → AspNetUsers.Id` (Cascade)
- Seed data: none (test accounts created manually during Phase 3 verification)

### Test accounts (local dev only)

- `alice@test.local` — primary CRUD test account
- `bob@test.local` — cross-user IDOR test account
- `testing3@gmail.com` — lockout test account

Unlock any account with:

```sql
UPDATE AspNetUsers
SET AccessFailedCount = 0, LockoutEnd = NULL
WHERE Email = '<email>';
```

Run via PowerShell `System.Data.SqlClient.SqlConnection`:

```powershell
$conn = New-Object System.Data.SqlClient.SqlConnection
$conn.ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=CipherVaultDb;Trusted_Connection=True;"
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "UPDATE AspNetUsers SET AccessFailedCount = 0, LockoutEnd = NULL WHERE Email = 'testing3@gmail.com';"
$cmd.ExecuteNonQuery()
$conn.Close()
```

---

## Configuration

- `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryMinutes` — `appsettings.json`
- `Jwt:Key` — user-secrets (dev). **Never in git.** Production: `Jwt__Key` env var.
- Cookie auth unchanged from Phase 1:
  - Name: `CipherVault.Auth` (no `__Host-` prefix — dev login requires this)
  - `HttpOnly = true`, `SameSite = Lax`, `Path = /`
  - `SecurePolicy = Always` (unconditional, dev + prod)
  - 2h sliding expiration
- Data Protection keys: `App_Data/keys` (gitignored, override via `DataProtection:KeyDirectory` or `DataProtection__KeyDirectory`)
- `SetApplicationName("CipherVault")` — active
- HTTPS redirection always on; HSTS only when `IsProduction()`

---

## Verification Performed

### Test 1 — Lockout ✅

- 5 wrong passwords via `/api/auth/login` → 5× 401
- 6th attempt with correct password → 423 `{"error":"account_locked"}`
- Same policy confirmed on the browser form after scaffolding `Account/Login`
- Account unlock via SQL confirms restoration of normal login

### Test 2 — No-token 401 without redirect ✅

- `GET /api/v1/vault` with `Authorization` header disabled → `401 Unauthorized`
- Response body: `{"error":"unauthorized"}` (produced by the `OnChallenge` handler — proves JWT scheme handled the challenge, not the cookie scheme)
- `Content-Type: application/json` confirms JSON response, not an HTML login page
- **No `Location:` header** in the response — proves no redirect is emitted

### Test 3 — HSTS in Production ✅

- `dotnet run --launch-profile Production` → console reports `Hosting environment: Production`
- `Invoke-WebRequest` against `https://localhost:7123/` → response includes `Strict-Transport-Security: max-age=2592000`
- Development profile: header absent (loopback exclusion working)
- Temporary `ExcludedHosts.Clear()` used only for local verification, removed before tag

### Other test groups

- Search/filter ✅ — no params, `?search=`, `?category=`, combined, cross-user scoping
- Dashboard ✅ — counts, chart, recent-5, empty-account view
- API contract ✅ — CRUD with Bearer token, no plaintext in responses, `?decrypt=true` → 400, IDOR → 404
- Regression ✅ — MVC cookie login/logout, Phase 2 RevealSecret/RevealNotes intact

---

## Behavior Notes

- **Lockout is enforced on both login paths.**
  - API: `AuthApiController` → `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`
  - MVC: scaffolded `Areas/Identity/Pages/Account/Login.cshtml.cs` → `PasswordSignInAsync(..., lockoutOnFailure: true)`

- **Lockout affects sign-in only, not existing sessions.** A user who was already authenticated before lockout remains authenticated until the cookie/JWT expires or they log out. This is standard ASP.NET Core Identity behavior and matches how most production systems work. No session-eviction mechanism is implemented, by design.

- **Hybrid Identity UI.** Only `Account/Login` is scaffolded; all other Identity pages (Register, Manage, etc.) remain library versions from `Microsoft.AspNetCore.Identity.UI`. Any future change to Identity behavior must account for which pages are scaffolded vs library-provided.

- **HSTS loopback exclusion is intentional.** `HstsOptions.ExcludedHosts` retains its defaults (`localhost`, `127.0.0.1`, `[::1]`). This prevents HSTS from poisoning dev browsers on localhost. In production, the request host is a real domain, so the exclusion does not apply.

- **API response shape.** All API responses return `VaultItemDisplayDTO` — the `MaskedSecret` field is a fixed mask string, and `HasNotes` is a boolean. No response can carry plaintext by construction.

---

## Known Issues

- **No pagination** on `/Vault` or `/api/v1/vault` — Phase 4 polish item.
- **No global exception middleware** — an unhandled exception in the API returns the default developer exception page (dev) or the `/Home/Error` page (prod). Phase 4 will add a JSON-returning exception handler for API paths.
- **Chart.js loaded from CDN** (`cdn.jsdelivr.net`) — Phase 4 may self-host for offline operation and to remove an external dependency at runtime.
- **`RequireConfirmedAccount = false`** — carried over from Phase 1, still correct because no email sender is wired up. If Phase 4 adds one, flip this and re-test.
- **NU1901 warnings** on transitive `NuGet.Packaging` / `NuGet.Protocol` 6.12.1 — low-severity, tooling-only, not loaded at runtime.
- **`VaultIndexViewModel` wraps items** — Phase 4 pagination must extend this class (add `Page`, `PageSize`, `TotalCount`), not replace it.

---

## Next Phase Requirements (Phase 4)

### UI polish

- Dark "vault" theme via `wwwroot/css/site.css`
- Category icons (Bootstrap Icons or Font Awesome)
- Copy-to-clipboard button on Details view, cleared after 10 seconds
- Pagination on `/Vault` (extend `VaultIndexViewModel`)
- Delete confirmation dialog

### Error handling

- Global exception middleware that returns JSON for `/api/*` and the error page for MVC
- Custom 404 / 500 views
- Ensure exception messages never echo secrets (logging rule)

### Data Protection key protection (production)

- Key ring outside web root
- Document key backup/restore procedure
- README disclaimer about shared-hosting key protection limitations

### Deployment (host-agnostic)

- `dotnet publish -c Release -o published`
- Recommend MonsterASP or Somee
- Connection string via hosting panel/env var — never committed
- `ASPNETCORE_ENVIRONMENT=Production`
- Writable persistent folder for Data Protection keys
- Free SSL from host, HTTPS binding

### Documentation

- README: dev + production setup, API usage, deployment guide, security disclaimer
- Screenshots
- User manual

### Final testing

- Register / login / CRUD on the deployed app
- Restart app → data still decryptable (key ring persists)
- API via Postman against the production URL
- Verify no sensitive data in browser dev tools or server logs

---

## Handoff Checklist

- [x] Phase 3 code builds with 0 errors (2 NU1901 warnings only)
- [x] All six test groups pass
- [x] Search/filter/dashboard functional
- [x] REST API works with JWT
- [x] Cookie + JWT schemes separated (verified via Test 2)
- [x] API never returns decrypted values
- [x] Lockout enforced on both login paths
- [x] HSTS active in Production, off in Development
- [x] Scoped `Areas/Identity/Pages/Account/Login` (only this page)
- [x] `Jwt:Key` in user-secrets only — not in any tracked file
- [x] Data Protection keys persist at `App_Data/keys` (gitignored)
- [x] No `AddHsts` block left in `Program.cs` (only `app.UseHsts()` in Production branch)
- [x] No `_diag/env` endpoint left in `Program.cs`
- [x] Postman collection committed
- [x] Handoff report committed

---

## Git Commit / Tag

- Phase 3 main commit: `Phase 3: search/filter, dashboard, REST API with JWT; scaffolded login for lockout`
- Handoff doc commit: `docs: phase 3 → phase 4 handoff`
- Tag: `phase-3-complete`
```