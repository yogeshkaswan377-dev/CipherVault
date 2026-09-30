# Phase 4 — Handoff / Completion Report

**Project:** CipherVault — Personal Encrypted Password & Secure Notes Manager  
**Stack:** ASP.NET Core 10 MVC + EF Core 10 + SQL Server + Identity + Data Protection API  
**Tag:** `phase-4-deployed`  
**Live URL:** https://ciphervault.tryasp.net  
**Date:** 2026-09-30

---

## Table of Contents

- [Completed Deliverables](#completed-deliverables)
- [Database State (Production)](#database-state-production)
- [Encryption Contract (DO NOT CHANGE)](#encryption-contract-do-not-change)
- [Production Configuration](#production-configuration)
- [Verification Performed](#verification-performed)
- [Security Audits](#security-audits)
- [Known Issues / Limitations](#known-issues--limitations)
- [Behavior Notes](#behavior-notes)
- [Git Tags](#git-tags)
- [Project Complete — Deliverables Checklist](#project-complete--deliverables-checklist)
- [Post-Deployment Recommendations](#post-deployment-recommendations)
- [Contact / Continuation](#contact--continuation)

---

## Completed Deliverables

### Phase 4.1 — Pagination + UI Foundation

- `VaultIndexViewModel` extended with `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasPrevious`, `HasNext`, `FirstItemOnPage`, `LastItemOnPage`
- `IVaultItemRepository.SearchPagedAsync` — server-side `Skip`/`Take` + `CountAsync` with stable `OrderByDescending(UpdatedAt).ThenByDescending(Id)`
- `IVaultItemService.SearchItemsPagedAsync` — `page`/`pageSize` clamping in service layer (1–100)
- `VaultController.Index` accepts `page` and `pageSize` query params
- Filter preservation across page navigation (search + category preserved in URLs)
- Windowed pagination (max 5 page links + first/last)

### Phase 4.2 — Global Exception Handling

- `Middleware/ExceptionHandlingMiddleware.cs`:
  - `/api/*` paths → `500 {"error":"internal_server_error"}` (no stack trace)
  - MVC paths → re-execute to `/Home/HttpError?code=500`
  - Logs method + path only, never query string or body
  - `Response.HasStarted` guard prevents double-write
- `HomeController.HttpError(int code)` action (renamed from `StatusCode` — avoids `ControllerBase.StatusCode` conflict)
- `Views/Home/NotFound.cshtml` + `ServerError.cshtml` — Linear-styled custom pages
- `UseStatusCodePagesWithReExecute("/Home/HttpError", "?code={0}")` — runs in both Development and Production (only exception middleware is Dev-excluded)

### Phase 4.3 — Copy-to-Clipboard with 10s Auto-Clear

- `wwwroot/js/clipboard.js`:
  - `crypto.getRandomValues()` — no `Math.random()`
  - 10-second auto-clear via `navigator.clipboard.writeText('')`
  - Focus-retry mechanism for Chrome/Firefox silent failures (`NotAllowedError` when tab unfocused)
  - `is-revealed` / `data-revealed` gate prevents copying masked placeholders
- Copy buttons on Details page (Secret + Notes) — disabled until reveal
- Toast notification on copy

### Phase 4.4A — Configuration Hardening

- `appsettings.Production.json` — empty placeholder secrets, committed (contains no secrets)
- `Program.cs` — `DataProtection:KeyDirectory` fails fast at startup in Production if empty
- Data Protection key ring untracked from git (`git rm --cached`)
- JwtBearer package upgraded `10.0.0` → `10.0.7` (CVE-2026-40372 fix)
- `appsettings.Development.json` untracked (contains only logging config, precautionary)

### Phase 4.4B — Documentation Set

| File | Purpose |
|------|---------|
| `README.md` | Project overview, features, architecture, quick start, security disclaimer |
| `docs/SECURITY.md` | Encryption design, key management, threat model, non-goals, hosting assumptions |
| `docs/DEPLOYMENT.md` | MonsterASP + Somee + Azure walkthroughs, post-deploy verification, troubleshooting |
| `docs/API.md` | Full REST API reference, error codes, Postman companion |
| `docs/USER_MANUAL.md` | End-user guide (features, reveal, blank-preserve, FAQ) |

### Phase 4.6 — Linear UI Theme

**Design system:** CSS variable-based tokens, Inter font, indigo accent `#5E6AD2`

**Pages completed (15/15):**

- `_Layout.cshtml` — sidebar layout, topbar with breadcrumb, footer
- `_LoginPartial.cshtml` — sidebar user block with avatar + logout
- `Dashboard/Index.cshtml` — KPI cards + Chart.js doughnut (Linear palette) + recent list
- `Vault/Index.cshtml` — card grid layout (replaced table)
- `Vault/Details.cshtml` — 2-column layout, secret/notes sections, sidebar metadata
- `Vault/Create.cshtml` + `Edit.cshtml` — sectioned forms, password generator, strength meter, character counters, sticky footer
- `Vault/Delete.cshtml` — red warning banner, checkbox confirmation, disabled-till-tick button
- `Home/Index.cshtml` — hero + 6 feature cards
- `Home/About.cshtml` — hero, features, tech stack pills, security callout
- `Home/Contact.cshtml` — 3 contact cards, vulnerability reporting
- `Home/Privacy.cshtml` — numbered sections, data storage table, callouts
- `Home/Error.cshtml` + `NotFound.cshtml` + `ServerError.cshtml` — Linear-styled
- `Identity/Account/Login.cshtml` + `Register.cshtml` — split-screen layout with brand panel

**Assets:**

- Bootstrap Icons self-hosted at `wwwroot/lib/bootstrap-icons/` (CDN replaced)
- `Helpers/VaultCategoryIcons.cs` — Razor CS1513 workaround for switch expressions
- `wwwroot/js/password-generator.js` — exposes `window.CipherVaultGenerator.generate(opts)`

### Phase 4 — Deployment

- **Host:** MonsterASP free tier
- **URL:** https://ciphervault.tryasp.net
- **SSL:** Let's Encrypt (90-day manual renewal on free plan)
- **SQL Server:** MonsterASP free database
- **Deployment method:** ZIP upload + extract via File Manager
- `SatelliteResourceLanguages=en` added to csproj (ZIP size reduction)
- `postman/**` excluded from publish via csproj

---

## Database State (Production)

- **Server:** MonsterASP MSSQL (`dbXXXX.public.databaseasp.net`)
- **Database:** `CipherVaultDb`
- **Migrations applied:**
  - `20260917143425_InitialIdentitySchema`
  - `20260921080100_AddVaultItems`

**Tables:**
`AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`, `AspNetRoleClaims`, `VaultItems`, `__EFMigrationsHistory`

**Indexes:**

- `IX_VaultItems_UserId`
- `IX_VaultItems_UserId_Category`
- `EmailIndex`, `UserNameIndex` (Identity)

**FK:** `VaultItems.UserId` → `AspNetUsers.Id` (Cascade)

**Seed data:** none (users register via production site)

---

## Encryption Contract (DO NOT CHANGE)

| Purpose string | Used for |
|----------------|----------|
| `CipherVault.Secret` | Vault item `Secret` field |
| `CipherVault.Notes` | Vault item `Notes` field |

- Both declared as `public const` on `VaultItemService`
- **Compatibility contract** — changing them makes existing ciphertext permanently undecryptable
- Data Protection key ring at `DataProtection:KeyDirectory` (production: env var, dev: `App_Data/keys`)
- `SetApplicationName`: `"CipherVault"` — must remain consistent across deployments sharing the key ring

---

## Production Configuration

### Environment Variables (MonsterASP)

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | MonsterASP SQL connection string (no `Encrypt=True`) |
| `Jwt__Key` | Production-only 88-char base64 (separate from dev user-secrets) |
| `DataProtection__KeyDirectory` | Writable path outside web root |

### Cookie Policy (Locked)

| Setting | Value |
|---------|-------|
| Name | `CipherVault.Auth` (no `__Host-` prefix) |
| HttpOnly | `true` |
| SameSite | `Lax` |
| SecurePolicy | `Always` (dev + prod) |
| Expiration | 2 hours sliding |

### HTTPS

- `UseHttpsRedirection()` — unconditional
- `UseHsts()` — Production only
- Let's Encrypt SSL — manual renewal every 90 days on free plan
- HSTS `max-age`: `2592000` (30 days)

---

## Verification Performed

### Development (local `--launch-profile Production`)

- Register / login / logout / lockout (5 attempts → 15 min)
- Create / Edit (blank-preserve) / Delete / Reveal / Copy
- Search + category filter + pagination
- Dashboard chart + counts
- API: JWT login, CRUD, 401 without token, `?decrypt=true` → 400, IDOR → 404
- Custom 404 + JSON 500 (API)
- HSTS header present
- Restart → data still decryptable (key ring persistence)

### Production (live deployment)

- HTTPS padlock active, no mixed content
- Register + login works
- Vault CRUD, reveal, copy-to-clipboard
- Dashboard chart renders
- Search + filter functional
- API 401 returns JSON (not redirect)
- Custom 404 page renders
- **App pool restart → data remains decryptable** ✅
- No plaintext in browser DevTools Network/Console
- No plaintext in server logs

---

## Security Audits

- `git ls-files` — no `App_Data/keys/*`, no `appsettings.Development.json`, no `deploy.zip`, no `published/`
- `appsettings.Production.json` — all secret placeholders empty
- `.gitignore` covers: `App_Data/`, `appsettings.Development.json`, `published/`, `deploy.zip`, `bin/`, `obj/`
- JWT signing key present only in user-secrets (dev) and env vars (prod)
- CORS not configured (default deny cross-origin) — API consumed same-origin only

---

## Known Issues / Limitations

| Issue | Impact | Notes |
|-------|--------|-------|
| MonsterASP app sleep (free tier) | ~5–10s cold start after 20 min idle | Paid plan removes; UptimeRobot ping mitigates |
| SSL manual renewal (free tier) | Site down if missed at day 90 | Calendar reminder recommended; paid plan auto-renews |
| NU1901 warnings (NuGet.Packaging/Protocol 6.12.1) | Build warnings only | Tooling-only, not loaded at runtime |
| Data Protection keys plaintext on disk | Filesystem compromise = key compromise | No DPAPI on shared hosting; documented in `SECURITY.md` |
| No 2FA / MFA | Password-only auth | Non-goal for educational scope |
| No password reset flow | Forgotten password = locked out | No email sender configured |
| Chart.js loaded from CDN | Runtime CDN dependency | Self-host possible if offline required |
| No automated tests | All testing manual | Documented in `README` |
| HSTS loopback exclusion default | `localhost` excluded from HSTS | Intentional — prevents dev browser pinning |

---

## Behavior Notes

- **HSTS loopback exclusion is intentional** — `ExcludedHosts` retains defaults (`localhost`, `127.0.0.1`, `[::1]`). Production requests come from real domains, so exclusion does not apply.
- **API responses use `VaultItemDisplayDTO`** — `MaskedSecret` is a fixed placeholder, `HasNotes` is a boolean. No endpoint can return plaintext by construction.
- **Lockout enforced on both login paths** — MVC login page (`Areas/Identity/Pages/Account/Login.cshtml.cs`) and API (`AuthApiController`) both call `PasswordSignInAsync`/`CheckPasswordSignInAsync` with `lockoutOnFailure: true`.
- **Hybrid Identity UI** — only `Account/Login` and `Account/Register` are scaffolded. Other Identity pages (Manage, Forgot Password, etc.) remain library versions.
- **Data Protection key path resolution** — `..\CipherVault-keys` (relative to content root) chosen on MonsterASP. `App_Data/keys` fallback disabled in Production (hard fail on empty config).
- **`VaultIndexViewModel` wraps items** — future pagination extensions must extend this class, not replace it.

---

## Git Tags

```text
phase-1-complete
phase-2-complete
phase-3-complete
phase-4-complete
phase-4-deployed   ← Final production deployment
```

---

## Project Complete — Deliverables Checklist

### Application Features

- ✅ User authentication (register, login, logout, lockout)
- ✅ Encrypted storage (Secret + Notes via Data Protection)
- ✅ No plaintext confidential content in DB
- ✅ Full CRUD with ownership scoping (404, not 403)
- ✅ Search + category filter + pagination
- ✅ Dashboard with Chart.js statistics
- ✅ Crypto-secure password generator + strength meter
- ✅ REST API with JWT bearer auth
- ✅ Live deployment on MonsterASP
- ✅ Data Protection keys persisted (never committed)
- ✅ Responsive Linear-themed UI
- ✅ HTTPS enforced in production
- ✅ API never returns decrypted secrets
- ✅ No plaintext in logs/URLs/errors
- ✅ API 401/403 (no login redirects)

### Security & Correctness

- ✅ Separate Create/Update DTOs (blank-preserve rule)
- ✅ Ownership check returns 404 (not 403)
- ✅ Explicit JWT scheme (no cookie redirect on `/api/*`)
- ✅ Purpose strings documented as compatibility contract
- ✅ No plaintext in logs, exceptions, audit entries
- ✅ Cookie `SecurePolicy.Always`
- ✅ JWT signing key never committed
- ✅ Data Protection key protection plan documented
- ✅ HTTPS + HSTS (production only)
- ✅ No deprecated APIs (`WithOpenApi`, `WebHostBuilder`, `IActionContextAccessor`)

### Code Quality

- ✅ Service layer for business logic
- ✅ Repository pattern for `VaultItem`
- ✅ Distinct Create / Update / Display DTOs
- ✅ Data-annotation validation
- ✅ No business logic in controllers
- ✅ Global exception handling
- ✅ Structured logging (no sensitive data)

### Documentation

- ✅ README with setup + live URL
- ✅ SECURITY.md with encryption design + threat model
- ✅ DEPLOYMENT.md with MonsterASP + Somee + Azure guides
- ✅ API.md with full endpoint reference
- ✅ USER_MANUAL.md with end-user guide
- ✅ Security disclaimer in README + app footer
- ✅ Postman collection

### Deployment

- ✅ Live HTTPS site: https://ciphervault.tryasp.net
- ✅ Production SQL Server database with migrations applied
- ✅ Environment variables configured (all 4 required)
- ✅ Data Protection keys persist across restart
- ✅ SSL certificate active (Let's Encrypt, 90-day manual renewal)
- ✅ Secrets audit clean (no leaks in git)

---

## Post-Deployment Recommendations

### Immediate

- Set calendar reminder for SSL renewal at **day 83** (not day 90)
- Enable **UptimeRobot** (free) — ping every 5 min to prevent app sleep
- Verify key directory backup — copy `CipherVault-keys/*.xml` off-server regularly
- Monitor logs weekly for `ExceptionHandlingMiddleware` errors

### Short-term

- Add screenshots to README (`docs/screenshots/`)
- Custom domain — if purchased, add CNAME + re-issue SSL
- GitHub Actions deploy automation (optional)
- Backup automation — daily DB + key ring

### Long-term

- Automated tests — xUnit + `WebApplicationFactory`
- Paid hosting plan — removes sleep + auto-renews SSL
- 2FA / WebAuthn — if scope expands
- Import/export — CSV/JSON for interoperability
- Security audit — before removing educational disclaimer

---

## Contact / Continuation

- **Repository:** GitHub (public)
- **Live URL:** https://ciphervault.tryasp.net
- **Handoff status:** ✅ Complete — project shipped
- **Next phase:** N/A — Phase 4 was the final phase.
- **Future work:** Deferred items in `docs/SECURITY.md` §16 (Known Limitations) and `README.md` §Testing.

---

> **Report prepared:** 2026-09-30  
> **Phase status:** ✅ Complete  
> **Deployment status:** ✅ Live