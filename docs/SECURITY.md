# CipherVault — Security Design

> **Audience:** developers, reviewers, and anyone evaluating whether CipherVault
> can be trusted with real secrets. Read this before storing anything sensitive.
>
> CipherVault is an **educational/student project**. This document describes the
> security model honestly — what it protects against, what it does not protect
> against, and the assumptions it makes about its hosting environment.

---

## Table of Contents

- [1. What is encrypted](#1-what-is-encrypted)
- [2. Encryption algorithm and API](#2-encryption-algorithm-and-api)
- [3. Purpose strings (compatibility contract)](#3-purpose-strings-compatibility-contract)
- [4. Key management](#4-key-management)
- [5. Key protection in production](#5-key-protection-in-production)
- [6. What happens after restart / redeploy](#6-what-happens-after-restart--redeploy)
- [7. Threat model](#7-threat-model)
- [8. What CipherVault does NOT protect against](#8-what-ciphervault-does-not-protect-against)
- [9. Hosting assumptions](#9-hosting-assumptions)
- [10. Authentication and session security](#10-authentication-and-session-security)
- [11. Ownership and access control (IDOR)](#11-ownership-and-access-control-idor)
- [12. Logging rules](#12-logging-rules)
- [13. Error handling](#13-error-handling)
- [14. API authentication separation](#14-api-authentication-separation)
- [15. Data Protection library version requirement](#15-data-protection-library-version-requirement)
- [16. Known limitations and non-goals](#16-known-limitations-and-non-goals)
- [17. Operational recommendations](#17-operational-recommendations)
- [18. Security disclaimer](#18-security-disclaimer)

---

## 1. What is encrypted

CipherVault encrypts exactly two fields on each vault item:

| Field | Encrypted? | Reason |
|-------|------------|--------|
| `Secret` | ✅ **Yes** | The sensitive value itself (password, API key, card number, etc.) |
| `Notes` | ✅ **Yes** | Free-form sensitive content |
| `Title` | ❌ No | Needed for search / list display |
| `Username` | ❌ No | Needed for search |
| `Url` | ❌ No | Needed for search |
| `Category` | ❌ No | Needed for filtering |
| `CreatedAt`, `UpdatedAt` | ❌ No | Needed for sorting and pagination |
| `UserId` (owner) | ❌ No | Needed for ownership checks and DB-level scoping |

**Trade-off:** Keeping metadata plaintext enables server-side search, filtering,
and pagination without decrypting every row. The trade-off is that an attacker
with database read access learns *that* a user has items in certain categories,
with certain titles, and when they were last updated. This is a deliberate
design decision documented as a **known limitation** (see §16).

**Never encrypted, by design:** the encryption keys themselves. CipherVault
does **not** derive keys from user passwords. See §4 and §4.3.

---

## 2. Encryption algorithm and API

CipherVault uses the **ASP.NET Core Data Protection API**
(`Microsoft.AspNetCore.DataProtection`).

- **Framework-provided primitives.** The Data Protection API chooses its own
  underlying algorithms (currently AES-CBC + HMAC-SHA256 for
  confidentiality + integrity). CipherVault does **not** hard-code a cipher.
- **Authenticated encryption.** Data Protection produces authenticated
  ciphertext — tampering is detected on decryption and results in an
  exception rather than silent corruption.
- **Purpose-scoped keys.** Each purpose string derives its own sub-key inside
  the shared key ring.
- **No custom cryptography.** CipherVault does not implement its own block
  cipher mode, padding, IV handling, or MAC. This is intentional — custom
  crypto is one of the most common ways password managers get broken.

**Version requirement:** `Microsoft.AspNetCore.DataProtection` **v10.0.7 or
later**. Earlier versions are affected by CVE-2026-40372 (see §15).

---

## 3. Purpose strings (compatibility contract)

Two purpose strings are used:

```csharp
public const string SecretPurpose = "CipherVault.Secret";
public const string NotesPurpose  = "CipherVault.Notes";
```

These are declared as `public const` on `VaultItemService`, and the two
protectors are created once in the constructor:

```csharp
_secretProtector = provider.CreateProtector(SecretPurpose);
_notesProtector  = provider.CreateProtector(NotesPurpose);
```

> 🚨 **Compatibility contract**
>
> These strings must **NEVER** change once production data exists.
>
> The purpose string is part of the key derivation path inside Data Protection.
>
> Changing `"CipherVault.Secret"` to `"CipherVault.Secret.v2"` will produce a
> different sub-key and make every existing ciphertext permanently
> undecryptable.
>
> If a future version needs to change the format (e.g. add a version prefix),
> it must be done via a migration: read with the old purpose, write with
> the new one, and keep the old purpose string in the codebase as long as any
> ciphertext encrypted under it still exists.
>
> Any PR that touches these strings must be treated as a breaking change and
> must include a data-migration plan.

---

## 4. Key management

### 4.1 Where the key ring lives

The Data Protection key ring is persisted to the file system via:

```csharp
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .SetApplicationName("CipherVault");
```

The directory is configurable:

| Environment | Source | Default |
|-------------|--------|---------|
| Development | `DataProtection:KeyDirectory` in `appsettings.json` | `App_Data/keys` (relative) |
| Production | `DataProtection__KeyDirectory` environment variable | Required — startup fails if missing |

The production check is intentional: silent fallback to a relative path could
place keys inside the web root (where they may be served as static files) or
in a non-persistent folder (where they would be lost on redeploy, making all
ciphertext unrecoverable).

### 4.2 Key ring contents

The key ring is a set of XML files, one per key. Each file contains:

- The key's GUID and creation/activation dates
- The raw key material (base64)
- Optionally, a symmetric-wrapped or DPAPI-wrapped version of the material

The files live in the directory you configured. They are secrets. Anyone
with read access to the key ring plus read access to the database can decrypt
every vault item in the system.

### 4.3 What CipherVault does NOT do

- ❌ Does not derive encryption keys from the user's password.
- ❌ Does not derive keys from the user's password hash.
- ❌ Does not store encryption keys in the database.
- ❌ Does not require the user to remember a second "master password" for
  the key ring.

CipherVault's model is: the server holds the keys, the server decrypts on
request for the authenticated owner. This is fundamentally different from
client-side-zero-knowledge managers like Bitwarden or 1Password. See §16 for
the implications.

### 4.4 Key ring configuration — `.gitignore`

`App_Data/` (which contains the default key directory) is git-ignored:

```gitignore
App_Data/keys/
App_Data/
```

Never commit key files to source control. If a key file is committed by
mistake, treat it as a breach of that key ring and rotate immediately (see
§4.6).

### 4.5 Application name

`SetApplicationName("CipherVault")` is set on the Data Protection builder.
This must be consistent across all deployments that share the same key ring —
changing it derives a different key hierarchy and makes old ciphertext
undecryptable.

### 4.6 Key rotation

If a key ring must be rotated (e.g. because it was leaked), the safe procedure is:

1. Generate a new key ring in a new directory.
2. Configure the app to use the new directory.
3. Write a migration that decrypts every existing value with the old key
   ring, then re-encrypts with the new one.
4. Retire the old key ring only after confirming every row has been re-encrypted.

This project does not ship such a migration — it is left as an exercise for
a production deployer. In the current codebase, keys are effectively
long-lived and should be treated as long-term secrets.

---

## 5. Key protection in production

The Data Protection API can protect key files at rest using various mechanisms.
CipherVault relies on framework defaults and the hosting environment:

| Environment | Key protection mechanism | Notes |
|-------------|--------------------------|-------|
| Windows (IIS, Windows Service) | DPAPI (per-machine or per-user, chosen by DP) | Keys are encrypted with the OS account's DPAPI key |
| Linux (systemd, Docker) | None by default — keys are plaintext XML files | DP can also use X.509 certificates or Azure Key Vault if configured |
| Shared hosting (MonsterASP, Somee) | Usually none — plaintext XML files | File system permissions are the only protection |

> ⚠️ **Shared-hosting reality:** on shared Windows hosts that do not expose
> DPAPI or a certificate store, Data Protection keys land on disk as
> plaintext XML. Anyone with file system access to that directory can
> read them. See §9 for the assumptions this project makes.

### How to add OS-level protection (production hardening)

If your hosting environment supports it, add one of the following after
`AddDataProtection()`:

```csharp
// Windows DPAPI (best on Windows hosts running under the app's service account)
.ProtectKeysWithDpapi()

// Windows DPAPI, machine-scoped (survives app pool recycle, needs admin to install)
.ProtectKeysWithDpapi(protectToLocalMachine: true)

// X.509 certificate (portable across Windows/Linux; requires a cert in the store)
.ProtectKeysWithCertificate(certificate)

// Azure Key Vault (best for Azure-hosted deployments)
.PersistKeysToAzureBlobStorage(...)
.ProtectKeysWithAzureKeyVault(...)
```

The current codebase does not enable any of these. This is a documented
limitation of the educational build. A production deployment should add the
mechanism that matches its hosting.

---

## 6. What happens after restart / redeploy

**After application restart**

- Data remains decryptable as long as the same key ring is available at
  the configured `DataProtection:KeyDirectory`.
- The Data Protection API picks the newest active key and continues. Old keys
  remain in the ring and continue to decrypt data encrypted with them.

**After redeployment to a new machine**

- The key ring must be copied to the new environment, or
  `DataProtection:KeyDirectory` must point to a shared persistent location
  (network share, mounted volume, Azure Key Vault).
- If the key ring is lost, all ciphertext becomes **permanently
  undecryptable**. There is no recovery.
- Redeploying without the key ring is equivalent to factory-resetting the app.

**Restart safety checklist**

- [ ] `DataProtection:KeyDirectory` set to an absolute path outside the web root
- [ ] Directory is writable by the app's service account
- [ ] Directory is on persistent storage (not a container's ephemeral FS)
- [ ] Key files are backed up regularly (see §17)
- [ ] Backup restoration procedure tested at least once

---

## 7. Threat model

This section enumerates the threats CipherVault does mitigate, and the
defenses in place for each.

### 7.1 Database-only compromise (attacker reads DB, not key ring)

**Defense:** `Secret` and `Notes` columns contain authenticated ciphertext, not
plaintext. Without the key ring, the attacker cannot decrypt them.

**Residual risk:** Metadata (`Title`, `Username`, `URL`, `Category`, timestamps) is
plaintext. An attacker learns the shape of the user's vault — e.g. "this user
has 12 password entries, 3 credit cards, and the titles mention 'bank' and
'gmail'". This is a deliberate trade-off (§1, §16).

### 7.2 Key ring compromise (attacker reads keys, not DB)

**Defense:** None. If keys are compromised, the attacker can decrypt anything
they also have ciphertext for.

**Mitigation:** Key files must be access-restricted at the file system level,
backed up securely, and never committed to source control (§4.4).

### 7.3 Cross-user data access (IDOR)

**Defense:** Every service method that touches a `VaultItem` scopes its query
by `UserId`. If the item is not found or not owned by the current user, the
service returns `null` and the controller returns `404 Not Found` — never
`403`. Returning `403` would leak the existence of other users' items.

See §11 for details.

### 7.4 Session hijacking / CSRF / XSS

**Defenses:**

- **Cookie:** `HttpOnly`, `SameSite=Lax`, `SecurePolicy=Always`, 2-hour sliding
  expiration. See §10.
- **CSRF:** Anti-forgery tokens validated globally for MVC form posts; explicit
  `[ValidateAntiForgeryToken]` on reveal actions. API uses JWT, which is not
  susceptible to classic CSRF (no ambient cookie auth on `/api/*`).
- **XSS:** Razor views HTML-encode all interpolated values by default. User
  input that reaches the DOM via JS is inserted via `textContent` (not
  `innerHTML`) — e.g. the clipboard helper, the search filter.
- **CSP / security headers:** Not configured. See §16 — this is a gap.

### 7.5 Password brute-force / credential stuffing

**Defenses:**

- **Lockout:** 5 failed attempts → 15-minute lockout, enabled on both the MVC
  login path and the JWT login path (`lockoutOnFailure: true`).
- **Password policy:** minimum 8 characters, requires upper, lower, digit, and
  non-alphanumeric.
- **Unique email:** enforced at the Identity user store.

**Not implemented:** rate limiting at the edge, IP throttling, CAPTCHA,
breach-password checks (HaveIBeenPwned integration). These are left as future
work.

### 7.6 Insider access (server operator)

**Defense:** None. The server can decrypt any value at any time because it
holds the keys. This is a fundamental property of the architecture, not a bug.

**Implication:** CipherVault should not be used for secrets that must remain
secret from the server operator. If that is a requirement, a
client-side-zero-knowledge design is necessary (out of scope for this project).

### 7.7 Clipboard exposure

**Defense:** After a copy, the OS clipboard is overwritten with an empty
string after 10 seconds. See `wwwroot/js/clipboard.js`.

**Residual risk:** During the 10-second window the secret is in the OS
clipboard. OS-level clipboard history (e.g. Windows Win+V) may capture it
before the clear runs. There is no way to prevent this from a web app.
Browser tab must be focused for the clear to succeed — if the user navigates
away, the clear is retried on focus.

### 7.8 Log leakage

**Defense:** Logging rules (§12) prohibit logging plaintext secrets, notes,
or user-supplied values. Audit logs contain only action type, user ID, item
ID, and timestamp.

### 7.9 Stack-trace leakage

**Defense:** Global exception middleware (§13) returns a JSON error body for
API paths and a generic custom error page for MVC paths. No stack traces,
exception messages, or request IDs ever reach the client.

---

## 8. What CipherVault does NOT protect against

This is the honest list. None of these are bugs — they are non-goals for an
educational project, and each one is a hard requirement for a real password
manager.

| Threat | Why it's not mitigated |
|--------|------------------------|
| Malicious server operator | The server holds the keys; a rogue admin can decrypt everything. |
| Client-side malware / keylogger | Anything a user types can be captured before it reaches CipherVault. |
| Compromised browser extension | Extensions can read the DOM after reveal. |
| OS-level clipboard sniffers | Clipboard history tools may capture values before the 10s clear. |
| Memory dumps / cold boot attacks | Decrypted values live in server process memory during reveal. |
| Side-channel attacks (timing, cache) | Not analysed. The Data Protection API's primitives are assumed sound. |
| Supply-chain attacks on NuGet packages | No SBOM, no signed-package policy, no regular dependency audit. |
| Key ring leakage via filesystem | Only the filesystem's own permissions protect the keys. |
| Malicious hosting provider | The host can read the key ring and the database. |
| Phishing | Users can be tricked into entering their credentials on a look-alike site. |
| 2FA / MFA bypass | MFA is not implemented. |
| Account recovery | There is no password-reset flow, so there is no recovery attack surface and no way to recover a forgotten password. |
| Email confirmation | `RequireConfirmedAccount = false`. Anyone can register with any email. |
| Brute-force of the Data Protection key | Out of scope — depends on the DP library's primitives and key length. |

---

## 9. Hosting assumptions

CipherVault assumes the following about its production host:

### 9.1 File system

- A persistent, writable directory exists outside the web root.
- The directory is not reachable via HTTP.
- The directory's permissions restrict access to the app's service account.
- The directory is included in the host's backup schedule.

### 9.2 Database

- A SQL Server instance reachable from the app.
- The connection string is supplied via environment variable, not committed
  source.
- The database is backed up independently of the key ring.

### 9.3 HTTPS

- TLS terminates at the app (Kestrel) or at a reverse proxy.
- The app is reached via HTTPS only; HTTP requests are redirected.
- HSTS is enabled in production (§10).

### 9.4 Hosting options

The project is host-agnostic. The following are known to work:

| Host | Key protection | Notes |
|------|----------------|-------|
| MonsterASP | None (plaintext XML on disk) | File permissions are the only protection. Documented limitation. |
| Somee | None (plaintext XML on disk) | Same as above. |
| Azure App Service | Recommended: Azure Key Vault, or App Service certificate | Requires additional config; see `docs/DEPLOYMENT.md` |
| Self-hosted Windows (IIS) | DPAPI available | `.ProtectKeysWithDpapi()` can be added |
| Self-hosted Linux + systemd | None by default | Configure certificate-based protection if possible |

**Shared-hosting reality:** on hosts that do not provide DPAPI or a
certificate store, keys are plaintext XML files. This is acceptable for an
educational project only if the deployer:

- Restricts file system access to the app's user.
- Backs up the key directory regularly.
- Documents the limitation to end users.
- Understands that a filesystem compromise of that directory is equivalent
  to a full vault compromise.

---

## 10. Authentication and session security

### 10.1 Password policy

Configured in `Program.cs`:

| Setting | Value |
|---------|-------|
| Minimum length | 8 |
| Require digit | Yes |
| Require lowercase | Yes |
| Require uppercase | Yes |
| Require non-alphanumeric | Yes |
| Require unique email | Yes |
| Require confirmed account | No — no email sender configured |

### 10.2 Lockout policy

| Setting | Value |
|---------|-------|
| Max failed attempts | 5 |
| Lockout duration | 15 minutes |
| Allowed for new users | Yes |
| Applies to | MVC login form and `POST /api/auth/login` |

Both login paths use `lockoutOnFailure: true`. A locked user cannot log in
until the lockout window expires, or an administrator resets
`AccessFailedCount` and `LockoutEnd` in the database.

### 10.3 Cookie configuration

```csharp
options.Cookie.Name         = "CipherVault.Auth";     // no __Host- prefix
options.Cookie.HttpOnly     = true;
options.Cookie.SameSite     = SameSiteMode.Lax;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.Path         = "/";
options.ExpireTimeSpan      = TimeSpan.FromHours(2);
options.SlidingExpiration   = true;
```

**Why no `__Host-` prefix:** the prefix requires the cookie to be issued over
HTTPS with `Path=/` and no `Domain`. Browsers silently reject cookies that
violate this. During local development over HTTPS this works, but any misstep
(e.g. running on HTTP locally, or behind a proxy that strips the
`Secure` flag) causes the cookie to be dropped and login to silently fail.
The project deliberately avoids `__Host-` to keep local dev friction-free.
The `SecurePolicy=Always` setting already guarantees the cookie is sent only
over TLS.

### 10.4 Session fixation / rotation

ASP.NET Core Identity rotates the auth cookie on sign-in by default. On
sign-out, the cookie is invalidated server-side via `SignInManager`.

### 10.5 What is not implemented

- ❌ Multi-factor authentication (TOTP, WebAuthn)
- ❌ Password reset / account recovery (no email sender)
- ❌ "Sign out all devices" / session management UI
- ❌ Login attempt logging beyond Identity's built-in counters
- ❌ Session revocation on password change (Identity's default behavior,
  documented as a known limitation)

---

## 11. Ownership and access control (IDOR)

Every code path that resolves a `VaultItem` by ID follows this rule:

> If the item is not found **OR** is not owned by the current user, return
> `404 Not Found`. Never return `403`.

**Why 404, not 403**

A `403` reveals that the requested resource exists and is owned by someone
else. An attacker can enumerate IDs and learn:

- How many items exist system-wide
- That a specific ID belongs to a valid vault entry (even if not theirs)
- Roughly when entries were created (by ID ordering)

A `404` leaks none of this. The response is indistinguishable from "no such
item". This is a standard technique in applications with per-user resources.

**Where it's enforced**

- `VaultItemService.GetItemAsync(id, userId)` — the service fetches by
  ID, then checks `item.UserId == userId`. On mismatch, returns `null`.
- `VaultItemService.UpdateItemAsync` / `DeleteItemAsync` — same pattern.
- `VaultController.Details` / `Edit` / `Delete` / `RevealSecret` /
  `RevealNotes` — when the service returns `null`, the controller returns
  `NotFound()`.
- `VaultApiController` — same behavior, returns `404` with no body.

The check is always in the service, never in the controller. This
prevents accidental bypass if a new controller action forgets to check.

---

## 12. Logging rules

These rules are mandatory and enforced by code review:

- Never log plaintext secrets or notes. Not in audit logs, not in
  exception logs, not in debug logs.
- Never log decrypted values. Reveal operations log the action, not
  the value.
- Never log user input that might be sensitive. Search terms, filter
  values, and URL query strings may contain sensitive data and are not
  logged verbatim.
- Audit log contents: action type (`Create` / `Update` / `Delete` /
  `Reveal`), user ID, item ID, timestamp. Nothing else.
- Exception messages must not include user input values that could be
  secrets.
- HTTP request logging must not include the request body or query
  string if those could contain secrets.

**What gets logged in practice**

- Startup: environment name, listening URLs (safe).
- Authentication: Identity logs sign-in success/failure without
  passwords.
- Audit events: structured events with `UserId`, `ItemId`, `Action`,
  `Timestamp`.
- Exceptions: method + path only. The exception object is logged, but
  its `Message` is treated as potentially tainted.

**Verifying no plaintext leaks**

A simple smoke test:

```bash
# 1. Create an item with a distinctive secret, e.g. "UniqueTestValue987!"
# 2. Grep all logs for that value:
grep -r "UniqueTestValue987" logs/
# Expected: no matches
```

---

## 13. Error handling

The `ExceptionHandlingMiddleware` runs first in the pipeline:

- For `/api/*` paths: any unhandled exception produces
  `HTTP 500 {"error":"internal_server_error"}` with no stack trace.
- For MVC paths: any unhandled exception is re-executed to
  `/Home/HttpError?code=500`, which renders the custom `ServerError` page.
- `404` for MVC paths: `UseStatusCodePagesWithReExecute` routes to the
  custom `NotFound` page. API paths keep the bare `404` so JSON clients can
  parse it.

**What is never sent to the client**

- Stack traces
- Exception messages
- Inner exceptions
- Connection strings
- File paths
- Request IDs derived from sensitive data
- The exact HTTP status of a non-existent resource (`404` both for "not
  found" and "not yours")

**What is logged server-side**

- Method + path (no query string)
- Full exception object
- Timestamp
- Environment name

The log is for operators. The client sees a generic message.

---

## 14. API authentication separation

CipherVault runs two authentication schemes side-by-side:

| Path | Scheme | Behavior on missing/invalid auth |
|------|--------|----------------------------------|
| MVC pages (`/Vault`, `/Dashboard`, etc.) | Cookie (`IdentityConstants.ApplicationScheme`) | Redirect to `/Identity/Account/Login` |
| REST API (`/api/*`) | JWT Bearer (`JwtBearerDefaults.AuthenticationScheme`) | `401 JSON {"error":"unauthorized"}` — no redirect |

The API controllers are explicitly decorated:

```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
```

This bypasses the cookie challenge entirely for API paths. If the JWT is
missing or invalid, the JWT handler's `OnChallenge` fires and writes a JSON
`401` — never a redirect.

**Why this matters**

Without explicit scheme separation, an unauthenticated API request would be
challenged by the default (cookie) scheme, which would issue a `302` redirect
to the login page. An API client expecting JSON would receive HTML and fail
to parse it, or worse, follow the redirect and leak information.

**JWT configuration**

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer           = true,
    ValidateAudience         = true,
    ValidateLifetime         = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer              = builder.Configuration["Jwt:Issuer"],
    ValidAudience            = builder.Configuration["Jwt:Audience"],
    IssuerSigningKey         = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
    ClockSkew                = TimeSpan.FromMinutes(1)
};
```

JWT signing key (`Jwt:Key`) is never committed to source control. In
development it lives in user-secrets; in production it is provided via
`Jwt__Key`. See `docs/DEPLOYMENT.md` for details.

---

## 15. Data Protection library version requirement

**Minimum required version:** `Microsoft.AspNetCore.DataProtection` v10.0.7.

Earlier versions are affected by CVE-2026-40372, which allowed certain
attacker-controlled inputs to influence key selection. The fix landed in
v10.0.7.

**Verifying the version**

```bash
dotnet list package --include-transitive | grep DataProtection
```

Expected: `Microsoft.AspNetCore.DataProtection >= 10.0.7`.

In an ASP.NET Core 10 project targeting `net10.0`, the Data Protection
assembly is provided by the shared framework (`Microsoft.AspNetCore.App`),
so the version is tied to the SDK. Confirm the SDK is `>= 10.0.7`.

**What happens if you downgrade**

Downgrading to a version below 10.0.7 re-introduces the CVE. Do not do this
to resolve a version conflict. Instead, align all ASP.NET Core packages to
the patched version.

---

## 16. Known limitations and non-goals

This list is intentionally blunt. If any item here is a hard requirement
for your use case, CipherVault is not the right tool.

### Cryptographic

- **No client-side encryption.** Plaintext reaches the server, is encrypted
  server-side, and is decrypted server-side on reveal. The server operator
  can decrypt anything.
- **No zero-knowledge model.** CipherVault does not derive keys from the
  user's master password, and users do not manage separate vault keys.
- **Metadata is plaintext.** Titles, usernames, URLs, categories, and
  timestamps are not encrypted. An attacker with DB access learns the shape
  of every user's vault.
- **No per-item key derivation.** All items of the same purpose share the
  same sub-key from the same key ring.
- **No key rotation UI.** Rotating the key ring requires a code change and a
  manual migration.

### Application

- **No 2FA / MFA.**
- **No password reset flow.** A forgotten password means a permanently
  inaccessible account (but the vault items remain in the DB and can be
  recovered by an admin with DB + key ring access).
- **No email confirmation.** `RequireConfirmedAccount = false`.
- **No sharing / teams.** Every item belongs to exactly one user.
- **No import / export.** No CSV, no JSON, no 1Password/Bitwarden import.
- **No attachments.** Text fields only.
- **No version history.** Edits overwrite.
- **No soft delete.** Deleting an item removes the row (via cascade from
  `AspNetUsers`, or directly).

### Operational

- **No CSP or security headers.** `Content-Security-Policy`, `X-Frame-Options`,
  `Referrer-Policy`, `Permissions-Policy` are not set.
- **No rate limiting.** Login, register, and API endpoints have no
  throttling beyond Identity's lockout policy.
- **No audit log persistence.** Audit events go to the app log, which may
  be rotated. There is no audit log table.
- **No SBOM, no signed-package policy.** Dependencies are restored from
  NuGet without signature verification or vulnerability scanning in CI.
- **No automated tests.** All testing documented in this repo is manual.
- **NU1901 warnings** on transitive `NuGet.Packaging` / `NuGet.Protocol`
  6.12.1 are acknowledged but not remediated (build tooling only, not
  loaded at runtime).

### Design

- **No service-level authorization policies** beyond ownership checks.
- **No explicit transaction management.** EF Core's implicit transaction per
  `SaveChangesAsync` is relied upon.
- **No caching layer.** Every list request hits the DB.

---

## 17. Operational recommendations

If you intend to run CipherVault for any purpose beyond a local demo,
implement the following before storing anything real.

### Before going live

- Set `DataProtection:KeyDirectory` to an absolute path outside the web
  root. Verify with `Test-Path` that it resolves and is writable.
- Set `Jwt:Key` to a freshly generated 32-byte base64 string. Do not
  reuse the dev key.
- Set a strong production connection string via
  `ConnectionStrings__DefaultConnection`. Use an account with the least
  privilege necessary (no `db_owner` unless migrations require it).
- Verify HSTS is active by curling the production URL — check for
  `Strict-Transport-Security` in the response.
- Verify the custom `404` page renders for both logged-in and logged-out
  users.
- Verify the API returns `401` JSON, not a redirect, when called without
  a token.
- Back up the key directory before the first real user registers.

### Ongoing operations

- Back up the key ring on the same schedule as the database. Both are
  required to recover data.
- Test key ring restoration at least once. Do not assume the backup
  works.
- Monitor the app log for repeated exceptions from
  `ExceptionHandlingMiddleware`. Investigate before they become routine.
- Review audit log volume. Sudden spikes in `Reveal` events may indicate
  compromise.
- Rotate the JWT signing key on a schedule (e.g. quarterly) and on
  suspected compromise. Rotating the JWT key invalidates all active tokens
  and forces re-login.
- Rotate user passwords on suspected compromise. Note that this does
  not invalidate active sessions until they expire or the user logs out
  (see §10.5).

### Incident response

**If the key ring is compromised:**

1. Assume all ciphertext is compromised.
2. Rotate the key ring to a new directory (§4.6).
3. Force-password-reset every user.
4. Re-encrypt every item with the new key ring (requires a migration that
   does not currently exist).
5. Rotate `Jwt:Key` to invalidate all active tokens.
6. Notify affected users.

**If the database is compromised but the key ring is not:**

1. `Secret` and `Notes` columns remain confidential.
2. Metadata (titles, usernames, URLs) is compromised.
3. Force-password-reset every user (password hashes were in the DB).
4. Rotate `Jwt:Key`.
5. Review audit logs for unauthorized access.

---

## 18. Security disclaimer

CipherVault is an **educational/student project** and must not be marketed
as a production-grade password manager unless its cryptographic design,
key management, authentication, deployment configuration, and security
implementation have been independently reviewed and audited.

Data Protection key storage on shared hosting environments may not provide
OS-level protection. In such deployments, keys are stored as files and
must be backed up regularly and access-restricted at the file system
level.

Do not use CipherVault to store high-value secrets (banking credentials,
recovery phrases, government IDs) without an independent security review.

**Reporting a vulnerability**

If you find a security issue, please do not open a public issue. Contact
the maintainer directly. Include:

- A description of the issue
- Steps to reproduce
- The potential impact
- Any suggested mitigation

You will receive a response within a reasonable timeframe. Coordinated
disclosure is preferred.