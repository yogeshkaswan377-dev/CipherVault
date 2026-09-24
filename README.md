# CipherVault

> A personal encrypted password & secure notes manager built on ASP.NET Core 10 MVC.

CipherVault is an educational/student project that demonstrates how to build a
password manager–style application with real encryption at rest, ownership-scoped
data access, a REST API with JWT authentication, and production-oriented security
configuration.

**⚠️ This is NOT a production-grade password manager.** See the
[Security Disclaimer](#security-disclaimer) below.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Quick Start (Development)](#quick-start-development)
- [Configuration](#configuration)
- [REST API](#rest-api)
- [Security Design](#security-design)
- [Project Structure](#project-structure)
- [Documentation](#documentation)
- [Testing](#testing)
- [Deployment](#deployment)
- [Security Disclaimer](#security-disclaimer)
- [License](#license)

---

## Features

### User-facing

- **Authentication** — Register, login, logout, password policy, account lockout
  (5 failed attempts → 15-minute lockout) via ASP.NET Core Identity.
- **Encrypted vault items** — Store passwords, notes, API keys, credit card
  details, and other secrets.
- **Secret + Notes encryption** — Sensitive fields are encrypted at rest with the
  ASP.NET Core Data Protection API. Metadata (Title, Username, URL, Category,
  timestamps) remains plaintext to support search and filtering.
- **Reveal on demand** — Secrets and notes are masked by default. A per-item
  "Reveal" action decrypts and displays the value for the owning user only.
- **Copy-to-clipboard with auto-clear** — Copy a revealed value; the OS
  clipboard is overwritten with an empty string after 10 seconds.
- **Password generator** — Cryptographically secure generator
  (`crypto.getRandomValues()`), with a live strength meter.
- **Search & filter** — Case-insensitive search across Title / Username / URL,
  plus category filter. Server-side LINQ, always scoped to the current user.
- **Pagination** — Filter state is preserved across page navigation.
- **Dashboard** — Total item count, per-category breakdown (Chart.js), and the
  five most recently updated items (masked).
- **Dark "vault" theme** — High-contrast, responsive UI built on Bootstrap 5.

### Engineering

- **Clean architecture** — Controllers are thin; business logic lives in
  services; data access lives in repositories.
- **DTO separation** — Distinct Create / Update / Display DTOs. Display DTOs
  never carry decrypted values.
- **Ownership-scoped access (IDOR-safe)** — Non-owned items return **404**,
  never 403.
- **Global exception handling** — JSON error responses for `/api/*`, custom
  HTML error pages for MVC paths. No stack traces ever reach the client.
- **Explicit auth scheme separation** — MVC uses cookies; the REST API uses
  JWT Bearer. API endpoints return 401 (never a login redirect).
- **Audit logging** — Create / Update / Delete / Reveal events are logged with
  action type, user ID, item ID, and timestamp. **Plaintext values are never
  logged.**
- **Data Protection key persistence** — Keys survive application restarts and
  redeployments (with proper key-ring handling).

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 (`net10.0`) |
| Web framework | ASP.NET Core 10 MVC (Razor Views) |
| ORM | Entity Framework Core 10 (Code First) |
| Database | SQL Server LocalDB (dev), SQL Server Express / Azure SQL (prod) |
| Authentication | ASP.NET Core Identity (cookie) + JWT Bearer (API) |
| Encryption | ASP.NET Core Data Protection API v10.0.7+ |
| UI | Bootstrap 5 + Bootstrap Icons |
| Charts | Chart.js |
| Client crypto | Web Crypto API (`crypto.getRandomValues`) |

---

## Architecture

```text
┌─────────────────┐     ┌──────────────────┐     ┌────────────────────┐
│   Controllers   │────▶│     Services     │────▶│    Repositories    │
│     (thin)      │     │ (business logic) │     │   (data access)    │
└─────────────────┘     └──────────────────┘     └────────────────────┘
          │                       │                         │
          │                       ▼                         ▼
          │             ┌──────────────────┐     ┌────────────────────┐
          │             │       DTOs       │     │ ApplicationDbContext│
          │             │ (data transfer)  │     └────────────────────┘
          │             └──────────────────┘
          ▼
   Razor Views / JSON API
```

### Rules that the codebase follows

- **Controllers** never contain business logic. They validate the request,
  call a service, and return a view or response.
- **All business logic** (encryption, validation, ownership checks,
  pagination math) lives in `Services/`.
- **All data transfer** between layers uses DTOs from `DTOs/`. Create and
  Update DTOs are strictly separated.
- **Repositories** are mandatory for `VaultItem` (encryption + ownership is
  non-trivial). Trivial read-only queries (dashboard stats, dropdown lists)
  may inject `ApplicationDbContext` directly into a service — but **never**
  into a controller.
- **`DbContext` is never injected into a controller.**

---

## Quick Start (Development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server LocalDB (installed with Visual Studio, or the standalone
  [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb))
- Git
- A trusted dev HTTPS certificate:
  ```bash
  dotnet dev-certs https --trust
  ```

### 1. Clone and restore

```bash
git clone <your-repo-url> CipherVault
cd CipherVault
dotnet restore
```

### 2. Configure JWT signing key (dev)

The JWT signing key is never committed to source control. Use
`user-secrets`:

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 32)"
```

On Windows PowerShell:

```powershell
$key = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "Jwt:Key" $key
```

### 3. Apply migrations

```bash
dotnet ef database update
```

This creates the `CipherVaultDb` database on LocalDB, including the ASP.NET
Core Identity schema and the `VaultItems` table.

### 4. Run

```bash
dotnet run --launch-profile https
```

Open `https://localhost:7123`.

### 5. Create your first account

Click **Register** in the navbar.

Choose a password that meets the policy:

- At least 8 characters
- At least one uppercase, one lowercase, one digit, one non-alphanumeric

Log in. You'll land on the Dashboard.

> **Note:** `RequireConfirmedAccount = false` in development — no email
> sender is wired up. If you add one, flip the flag in `Program.cs`.

---

## Configuration

### Development (`appsettings.json`)

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | LocalDB connection string |
| `DataProtection:KeyDirectory` | Where the Data Protection key ring is persisted (default: `App_Data/keys`) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | JWT token settings |
| `Jwt:Key` | Not in `appsettings.json` — set via `user-secrets` |

### Production (`appsettings.Production.json` + env vars)

Secrets are provided via environment variables at runtime. The committed
`appsettings.Production.json` contains empty placeholder values only.

| Environment Variable | Purpose |
|----------------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Must be `Production` |
| `ConnectionStrings__DefaultConnection` | Production SQL Server connection string |
| `DataProtection__KeyDirectory` | Absolute path to a persistent, writable directory outside the web root |
| `Jwt__Key` | Base64 of 32 random bytes (regenerate for production) |

> **Critical:** `DataProtection:KeyDirectory` must be set in production.
> If it is missing, the application fails fast at startup with a clear error
> message. Silent fallback to a relative path is intentionally disabled to
> prevent keys from landing in a non-persistent or web-exposed folder.

### Cookie settings (locked)

| Setting | Value | Rationale |
|---------|-------|-----------|
| Cookie name | `CipherVault.Auth` | No `__Host-` prefix — that prefix requires `Secure` + `Path=/` and is silently rejected by browsers when the cookie is issued over plain HTTP, which breaks local dev login |
| `HttpOnly` | `true` | Prevents JavaScript access (XSS mitigation) |
| `SameSite` | `Lax` | CSRF mitigation for top-level navigations |
| `SecurePolicy` | `Always` | Cookie is only ever sent over TLS — in both dev and production |
| Expiration | 2 hours sliding | Reduces session-hijack window |

---

## REST API

Base path: `/api/v1/vault`. All endpoints require a JWT Bearer token obtained
from `POST /api/auth/login`.

The REST API never returns decrypted secrets or notes. All responses use
the masked display DTO. The `?decrypt=true` query parameter is explicitly
rejected with `400 {"error":"decryption_not_supported"}`.

See `docs/API.md` for the full reference and
`postman/CipherVault.postman_collection.json`
for importable requests.

Quick example:

```bash
# 1. Get a token
curl -X POST https://localhost:7123/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"alice@test.local","password":"<password>"}'

# 2. Call the API
curl https://localhost:7123/api/v1/vault \
     -H "Authorization: Bearer <token>"
```

---

## Security Design

CipherVault uses the ASP.NET Core Data Protection API for encryption at rest.
Two purpose strings define the compatibility contract:

| Purpose string | Used for |
|----------------|----------|
| `CipherVault.Secret` | The `Secret` field of a vault item |
| `CipherVault.Notes` | The `Notes` field of a vault item |

These purpose strings **must never change** once production data exists.
Changing them makes existing ciphertext permanently undecryptable.

For a full description of the encryption model, key management, threat model,
and hosting assumptions, see `docs/SECURITY.md`.

Key points:

- **What is encrypted:** `Secret`, `Notes`.
- **What is NOT encrypted:** `Title`, `Username`, `URL`, `Category`, `CreatedAt`,
  `UpdatedAt`. These are needed for search and filtering.
- **How keys are stored:** Data Protection key ring persisted as XML files in
  the directory configured via `DataProtection:KeyDirectory`.
- **After application restart:** data remains decryptable as long as the key
  ring is intact and available.
- **After redeployment:** the key ring must be copied to the new environment,
  or configured to use a shared persistent location.
- **Hosting assumption:** on shared hosting without DPAPI or certificate-based
  key protection, keys are stored as plaintext XML files. This is acceptable
  for an educational project only if file system permissions restrict
  access and keys are backed up regularly.

---

## Project Structure

```text
CipherVault/
├── Areas/Identity/           # Scaffolded Login page (lockout participation)
├── Controllers/
│   ├── HomeController.cs
│   ├── DashboardController.cs
│   ├── VaultController.cs
│   └── Api/
│       ├── AuthApiController.cs
│       └── VaultApiController.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs
├── DTOs/                     # Create / Update / Display DTOs
├── Helpers/                  # View-agnostic helpers (e.g. category icons)
├── Middleware/               # ExceptionHandlingMiddleware
├── Migrations/               # EF Core migrations
├── Models/                   # Domain entities (VaultItem, VaultCategory)
├── Repositories/
│   ├── Contracts/
│   └── VaultItemRepository.cs
├── Services/
│   ├── Contracts/
│   ├── VaultItemService.cs
│   ├── DashboardService.cs
│   └── TokenService.cs
├── ViewModels/
├── Views/
│   ├── Vault/                # Index, Create, Edit, Details, Delete
│   ├── Dashboard/
│   ├── Home/
│   └── Shared/
├── wwwroot/
│   ├── css/site.css
│   └── js/
│       ├── password-generator.js
│       └── clipboard.js
├── docs/
│   ├── DEPLOYMENT.md
│   ├── API.md
│   ├── SECURITY.md
│   ├── USER_MANUAL.md
│   └── screenshots/
├── postman/
│   └── CipherVault.postman_collection.json
├── App_Data/                 # Data Protection keys (git-ignored)
├── appsettings.json
├── appsettings.Production.json
└── CipherVault.csproj
```

---

## Documentation

| Document | Description |
|----------|-------------|
| `docs/DEPLOYMENT.md` | Step-by-step deployment to MonsterASP, Somee, and Azure |
| `docs/API.md` | REST API reference |
| `docs/SECURITY.md` | Encryption design, key management, threat model |
| `docs/USER_MANUAL.md` | End-user guide |

---

## Testing

### Automated

There is no automated test suite yet. The project was verified manually against
the following suites. Contributions welcome.

### Manual test groups

| Group | Coverage |
|-------|----------|
| Auth | Register, login, logout, lockout (5 attempts → 15 min), weak password rejection |
| Encryption | Plaintext absent from DB, ciphertext present, restart → decryptable |
| CRUD | Create / Edit (blank-preserve) / Delete / Reveal / non-owner 404 |
| Search & Pagination | Filter preservation across pages, category filter, empty state |
| Dashboard | Counts, chart, recent-5, empty-account view |
| API | Token issue, CRUD via Bearer, 401 without token, `?decrypt=true` → 400, IDOR → 404 |
| Error handling | Custom 404 page, JSON 500 for `/api/*`, no stack traces, no secret in logs |
| Production profile | HSTS active, HTTPS-only, no redirect for API, cookie flags |
| Restart persistence | Data still decryptable after restart with same key ring |

### Where secrets are checked

- No plaintext in DB — inspect `VaultItems` directly.
- No plaintext in logs — search log files for a distinctive test password.
- No plaintext in browser dev tools — Network tab during reveal.

---

## Deployment

CipherVault is designed to be host-agnostic and runs on any Windows
ASP.NET Core host with SQL Server. Recommended free/low-cost hosts:

- **MonsterASP** — Windows hosting with SQL Server, free tier available.
- **Somee** — free ASP.NET Core + SQL Server hosting.
- **Azure App Service** — optional; not required.

At a glance:

```bash
dotnet publish -c Release -o published
```

Then configure the following environment variables on the host:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<prod-conn-string>
DataProtection__KeyDirectory=<absolute-path-outside-web-root>
Jwt__Key=<base64-of-32-random-bytes>
```

Full step-by-step instructions, screenshots, and troubleshooting:
`docs/DEPLOYMENT.md`.

---

## Security Disclaimer

CipherVault is an **educational/student project** and must not be marketed as
a production-grade password manager unless its cryptographic design, key
management, authentication, deployment configuration, and security
implementation have been independently reviewed and audited.

Data Protection key storage on shared hosting environments may not provide
OS-level protection. In such deployments, keys are stored as files and must
be backed up regularly and access-restricted at the file system level.

Do not use CipherVault to store high-value secrets (banking credentials,
recovery phrases, government IDs) without an independent security review.

---

## License

This project is provided for educational use. Replace this section with your
chosen license (e.g. MIT, Apache 2.0, or a custom educational license) before
publishing.