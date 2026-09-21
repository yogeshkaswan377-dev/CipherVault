# Master Prompt for Distributed Development of CipherVault (ASP.NET Core 10 – Final Complete Edition)

```markdown
I need to build CipherVault – a personal encrypted password & secure notes manager – across multiple AI chat sessions. Each phase must be self-contained with clear deliverables, dependencies, and testing requirements.

## PROJECT NAME: CipherVault – Personal Encrypted Password & Secure Notes Manager

## TECH STACK (ALL PHASES)
- ASP.NET Core 10 MVC (Razor Views), targeting `net10.0`
- Entity Framework Core 10 (Code First)
- SQL Server LocalDB (development) / SQL Server Express or Azure SQL (production)
- ASP.NET Core Identity for authentication (cookie-based for MVC)
- Data Protection API (v10.0.7 or later) for encryption
- Bootstrap 5 for UI
- JWT Bearer Authentication for API (Phase 3)
- Deployment: Any standard Windows ASP.NET Core + SQL Server hosting (MonsterASP, Somee recommended; Azure optional)

---

## ARCHITECTURE RULE (CRITICAL - ALL PHASES)

**Business logic NEVER in controllers or views.**
- ALL business logic (encryption, validation, ownership checks, calculations) in Service classes (`Services/`)
- ALL data transfer via DTOs (`DTOs/`)
- Controllers only: validate request → call service → return view/response

**Repository Pattern Rules:**
- **MANDATORY** for `VaultItem` (encryption + ownership checks are non-trivial).
- **Optional** for trivial read-only queries (dashboard counts, dropdown lists) — in that case inject `ApplicationDbContext` directly into the Service (never into the Controller).
- **Never** inject `DbContext` directly into a Controller.

**Do not over-engineer.** The architecture should remain clean and practical.

- Persist Data Protection keys to a persistent, application-accessible directory that is NOT publicly accessible through the web.
- The exact path must be configurable through configuration/environment settings.
- Never commit Data Protection key files to Git.
- Production hosting must provide persistent storage for the key directory.
- Password generation must use a cryptographically secure random number generator (`crypto.getRandomValues()` in JS, `RandomNumberGenerator` in .NET). Do NOT use `Math.random()`.

---

## ASP.NET CORE 10 BREAKING CHANGES & MIGRATION NOTES

The following changes in ASP.NET Core 10 must be accounted for:

1. **Cookie login redirects disabled for known API endpoints**: API endpoints will NOT redirect to login for unauthenticated requests. Return proper HTTP 401/403.
2. **`WithOpenApi` extension method deprecated**: Use built-in OpenAPI support in ASP.NET Core 10.
3. **`IActionContextAccessor` and `ActionContextAccessor` obsolete**: Use `HttpContext` directly.
4. **Razor runtime compilation obsolete**: Use pre-compiled Razor views (default).
5. **`WebHostBuilder`, `IWebHost`, `WebHost` obsolete**: Use `WebApplicationBuilder` / `WebApplication` in `Program.cs`.
6. **EF Core 10 requires .NET 10 SDK/runtime**: Target `net10.0`.
7. **Data Protection regression**: Use `Microsoft.AspNetCore.DataProtection` **v10.0.7 or later** (CVE-2026-40372 in earlier versions).

---

## SECURITY & CRYPTOGRAPHY DESIGN (MANDATORY BEFORE PHASE 2)

Before implementing encryption (Phase 2), the AI must clearly define and document:

1. **What is encrypted?**
   - Passwords
   - API keys/tokens
   - Credit card sensitive details
   - Secure note content
   
   Non-sensitive metadata (Title, Username, URL, Category, CreatedAt, UpdatedAt) remains **unencrypted** to allow search/filter.

2. **What encryption algorithm is used?**
   - ASP.NET Core Data Protection API with framework-supported mechanisms. Do not depend on a specific underlying cipher.

3. **Where is the encryption key stored?**
   - Development: default location or configured path (`App_Data/keys`).
   - Production: **configurable persistent location outside web root**.

4. **How is the key protected?**
   - Must use hosting environment's appropriate key-protection mechanism.
   - Do NOT assume Windows DPAPI is available in every production hosting environment.

5. **How is decryption performed?**
   - Only through `IDataProtector` with correct purpose string.
   - Purpose strings (`CipherVault.Secret`, `CipherVault.Notes`) are a **compatibility contract**. NEVER change them once production data exists.

6. **What happens after application restart?**
   - Data must remain decryptable as long as the same key ring is available. Persistent key storage is required.

7. **What happens after deployment?**
   - Key ring must be copied to the new environment OR configured to use a shared location (Azure Key Vault, network share).

**CRITICAL:** Never derive encryption keys from the user's Identity password hash. Use the built-in Data Protection API only.

---

## DTO DESIGN RULES (MANDATORY)

- **Separate Create and Update DTOs.** Never reuse a single DTO for both.
- `VaultItemCreateDTO`: `Secret` is **required**. `Notes` optional.
- `VaultItemUpdateDTO`: `Secret` and `Notes` are **nullable**.
  - If `Secret` is null/empty → Service MUST **preserve existing encrypted secret**.
  - If `Notes` is null → Service MUST **preserve existing notes**.
  - If `Notes` is empty string → Service sets `EncryptedNotes = null` (explicit clear).
- Display DTOs (`VaultItemDisplayDTO`) MUST NEVER contain decrypted Secret or Notes.
- Purpose strings for Data Protection (`CipherVault.Secret`, `CipherVault.Notes`) are a **compatibility contract**. Never change after production data exists.

---

## LOGGING RULES (MANDATORY)

- **NEVER log plaintext secrets, notes, or decrypted values.**
- Audit logging includes ONLY: action type (Create/Update/Delete/Reveal), userId, itemId, timestamp.
- Exception messages MUST NOT echo secret values.
- Logging must not write to URLs or query strings containing sensitive data.

---

## IDOR / OWNERSHIP RULES (MANDATORY)

- When accessing a `VaultItem` by ID, ALWAYS verify ownership in the **Service layer**.
- If not found OR not owned by current user → return **404 Not Found** (NOT 403). 403 leaks existence.

---

## COOKIE POLICY RULES

- `CookieSecurePolicy = SameAsRequest` in **Development**.
- `CookieSecurePolicy = Always` in **Production**.
- HTTP-only launch profile is NOT supported for authentication. Remove it from `launchSettings.json` or document that it cannot be used for login testing.

---

# PHASE 1: FOUNDATION & AUTHENTICATION
**Duration: 1 logical development milestone (may require multiple AI chats)**
**Dependencies: None (Starting point)**

## What MUST be built:
1. ASP.NET Core 10 MVC project targeting `net10.0` with NuGet packages:
   - Microsoft.EntityFrameworkCore.SqlServer (v10.x)
   - Microsoft.EntityFrameworkCore.Tools (v10.x)
   - Microsoft.AspNetCore.Identity.EntityFrameworkCore (v10.x)
   - Microsoft.AspNetCore.DataProtection (v10.0.7+)
2. Database setup:
   - `ApplicationDbContext` (inherits `IdentityDbContext`)
   - Connection string in `appsettings.json` (LocalDB)
   - Initial migration for Identity tables
3. Identity scaffolding:
   - Register, Login, Logout, Access Denied pages
   - Cookie authentication configured
   - `CookieSecurePolicy`: `SameAsRequest` (dev), `Always` (production)
4. Base layout (`_Layout.cshtml`):
   - Navigation: Home, Dashboard, Vault, About
   - Show logged-in user + Logout button
   - Bootstrap responsive
5. HomeController:
   - Index, About, Contact (static)
6. Placeholder DashboardController:
   - `[Authorize]` protected Index returning "Welcome to your Vault"
7. Project structure:
   - `Services/` (empty)
   - `Repositories/` (empty)
   - `DTOs/` (empty)
   - `Controllers/`
   - `Views/`
8. Anti-forgery tokens enabled globally
9. HTTPS redirection + HSTS (production only):
   ```csharp
   if (app.Environment.IsProduction()) { app.UseHsts(); }
   app.UseHttpsRedirection();
   ```

## What NOT to build:
- NO VaultItem model or CRUD
- NO encryption logic
- NO API endpoints
- NO search/filter
- NO password generator

## Deliverables:
- Working authentication
- Base layout with navigation
- Dashboard placeholder (authorized only)
- Empty Services/Repositories/DTOs directories
- HTTPS redirection (HSTS only in production)
- Project targets `net10.0`

## Testing Required:
- Register / Login / Logout work
- Dashboard requires authentication
- Unauthenticated access redirects to Login
- HTTPS redirection works in production mode

## Handoff to Phase 2:
- Identity tables created
- User ID retrievable
- Project builds and runs
- Git commit created

---

# PHASE 2: VAULT ITEM MODEL & ENCRYPTED CRUD
**Duration: 1 logical development milestone (may require multiple AI chats)**
**Dependencies: Phase 1 complete**

## What MUST be built:
1. **VaultItem Model** (`Models/VaultItem.cs`):
   - Id (int, key)
   - UserId (string, FK to Identity user)
   - Title (string, required, max 100)
   - Category (string, required — enum: Password, Note, API Key, Credit Card, Other)
   - Username (string, optional, max 100)
   - EncryptedSecret (string, required) — encrypted only
   - EncryptedNotes (string, optional) — encrypted only
   - Url (string, optional, max 500)
   - CreatedAt, UpdatedAt (DateTime)

2. **ApplicationDbContext**: Add `DbSet<VaultItem>`, configure User → VaultItems relationship. Run migration.

3. **Repository Pattern** (MANDATORY for VaultItem):
   - `Repositories/Contracts/IVaultItemRepository.cs`
   - `Repositories/VaultItemRepository.cs`
   - Methods: `GetByIdAsync`, `GetAllByUserAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`

4. **Service Layer**:
   - `Services/Contracts/IVaultItemService.cs`
   - `Services/VaultItemService.cs`
   - Inject `IVaultItemRepository` + `IDataProtectionProvider`
   - Methods:
     - `GetAllItems(string userId)`
     - `GetItem(int id, string userId)` → returns null if not owned (never 403)
     - `CreateItem(VaultItemCreateDTO dto, string userId)`
     - `UpdateItem(int id, VaultItemUpdateDTO dto, string userId)` — **blank-preserve rule applied**
     - `DeleteItem(int id, string userId)`
   - **Encryption:**
     - Purpose `"CipherVault.Secret"` for Secret
     - Purpose `"CipherVault.Notes"` for Notes
     - Encrypt before save, decrypt only when explicitly requested
     - Never decrypt in list/index view
     - Never expose decrypted content in normal responses
   - **Ownership:** Verify in service. Return `null` / throw `NotFound` for non-owned items.

5. **DTOs**:
   - `VaultItemCreateDTO` — Secret required, Notes optional
   - `VaultItemUpdateDTO` — Secret nullable, Notes nullable
   - `VaultItemDisplayDTO` — Id, Title, Category, Username, Url, CreatedAt, UpdatedAt, MaskedSecret, HasNotes (no plaintext)

6. **Controllers** (`VaultController`, thin):
   - Index — list items (masked)
   - Create (GET/POST) — uses `VaultItemCreateDTO`
   - Edit (GET/POST) — uses `VaultItemUpdateDTO` (blank = preserve)
   - Delete (GET/POST)
   - Details — masked secret + masked notes
   - RevealSecret (POST) — `[Authorize]` + `[ValidateAntiForgeryToken]`
   - RevealNotes (POST) — `[Authorize]` + `[ValidateAntiForgeryToken]`
   
   Reveal actions:
   - Verify ownership via Service
   - Return **404** if not found or not owned (never 403)
   - Decrypt only via Service
   - Never persist decrypted content
   - Never log decrypted value

7. **Views**:
   - Index — table with masked secret
   - Create/Edit — form with validation
   - Edit form: Secret/Notes fields have placeholder *"Leave blank to keep existing value"*
   - Details — metadata + separate Reveal buttons
   - Delete — confirmation page

8. **Validation**:
   - Data annotations: `[Required]`, `[StringLength]`, `[Url]`, `[RegularExpression]`
   - Custom: Category must be one of allowed values

9. **Password Generator & Strength Meter** (client-side):
   - Uses `crypto.getRandomValues()` (NOT `Math.random()`)
   - Strength meter updates live

10. **Data Protection Key Persistence & Protection Plan:**
    - Path configurable via `DataProtection:KeyDirectory`
    - Add key directory to `.gitignore` — NEVER commit keys
    - **Development:** default OS protection acceptable
    - **Production plan (documented in Phase 2, implemented in Phase 4):**
      - Key ring MUST be outside web root
      - If hosting provides DPAPI/certificate-based protection → use it
      - If shared hosting does NOT provide key protection → keys stored as plaintext files; acceptable for educational project ONLY IF:
        - File system permissions restrict access
        - Keys are backed up regularly
        - README security disclaimer explicitly mentions this limitation

## What NOT to build:
- NO search/filter
- NO dashboard stats
- NO API endpoints
- NO deployment

## Deliverables:
- Full CRUD with encryption
- Items encrypted at rest
- Secrets + Notes masked by default
- Reveal buttons work
- Password generator (crypto-secure)
- Ownership enforced (404, not 403)
- DTOs properly separated (Create vs Update)
- Edit preserves Secret/Notes when blank

## Testing Required:
- Create item with password + notes → verify DB has encrypted values only
- Verify no plaintext in DB
- Reveal returns correct plaintext only to owner
- Non-owner reveal → 404
- Restart app → data still decryptable
- Edit with blank Secret → existing Secret preserved
- Edit with blank Notes → existing Notes preserved
- Edit with empty Notes string → Notes cleared
- Delete item works
- Cross-user access → 404

## Handoff to Phase 3:
- 3+ test items created
- VaultItemService functional
- DTOs separated (Create/Update)
- Data Protection keys persisted (never in Git)
- All functionality intact

---

# PHASE 3: ADVANCED FEATURES & API
**Duration: 1 logical development milestone**
**Dependencies: Phase 2 complete**

## What MUST be built:
1. **Search & Filter**:
   - Search box + category filter
   - Server-side LINQ filtering
   - GET params (e.g., `/Vault?search=keyword&category=Password`)
   - Search Title, Username, Url (case-insensitive)

2. **Dashboard Enhancements**:
   - Total items count
   - Items per category (Chart.js pie/bar)
   - Recently added (last 5, masked)

3. **REST API**:
   - Controllers under `Controllers/Api/`
   - `[ApiController]`, `[Route("api/v1/[controller]")]`
   - `VaultApiController`:
     - GET `/api/v1/vault`
     - POST `/api/v1/vault`
     - GET `/api/v1/vault/{id}`
     - PUT `/api/v1/vault/{id}`
     - DELETE `/api/v1/vault/{id}`
   - Uses same `IVaultItemService`
   - **Security:** Never return decrypted secrets via API. Always masked. `?decrypt=true` ignored or returns 400.
   - **ASP.NET Core 10 behavior:** No login redirects for API — return proper 401/403.

4. **Authentication Scheme Separation (CRITICAL):**
   - Configure in `Program.cs`:
     ```csharp
     builder.Services.AddAuthentication(options =>
     {
         options.DefaultScheme = IdentityConstants.ApplicationScheme;
         options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
     })
     .AddIdentityCookies()
     .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
     {
         options.TokenValidationParameters = new TokenValidationParameters
         {
             ValidateIssuer = true,
             ValidateAudience = true,
             ValidateLifetime = true,
             ValidateIssuerSigningKey = true,
             ValidIssuer = builder.Configuration["Jwt:Issuer"],
             ValidAudience = builder.Configuration["Jwt:Audience"],
             IssuerSigningKey = new SymmetricSecurityKey(
                 Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
         };
     });
     ```
   - **API controllers MUST use:**
     ```csharp
     [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
     ```
   - **MVC controllers:** simple `[Authorize]` (cookie scheme).

5. **JWT Configuration:**
   - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` in configuration
   - **NEVER commit the JWT signing key to Git.**
   - Dev: `appsettings.Development.json` (gitignored)
   - Production: environment variables / hosting secrets
   - Token generation: `POST /api/auth/login`

6. **Audit Logging** (optional):
   - Log actions: Create, Update, Delete, Reveal
   - ONLY action type, userId, itemId, timestamp — NO values

7. **Postman Collection**: JSON file with sample requests

8. **Deprecated API check**: Do NOT use `WithOpenApi`.

## What NOT to build:
- NO deployment
- NO UI polish (dark theme)
- NO copy-to-clipboard

## Deliverables:
- Search + filter working
- Dashboard with chart
- REST API with JWT
- Cookie & JWT schemes separated
- API returns 401 (not redirect)
- Secrets never exposed in API
- Postman collection provided

## Testing Required:
- Search / filter work
- Dashboard accurate
- API: token → CRUD endpoints work
- API: unauthorized → 401 (not redirect)
- API: secrets always masked
- API: `?decrypt=true` ignored or 400
- MVC login still works (cookie unaffected)

## Handoff to Phase 4:
- API documented
- JWT working with explicit scheme
- Search & dashboard functional
- All prior functionality intact

---

# PHASE 4: POLISH, SECURITY HARDENING & DEPLOYMENT
**Duration: 1 logical development milestone**
**Dependencies: Phase 3 complete**

## What MUST be built:
1. **UI Polish**:
   - Dark "vault" theme (`wwwroot/css/site.css`)
   - Category icons (Bootstrap Icons / Font Awesome)
   - Copy-to-clipboard button (clears after 10s)
   - Pagination if needed
   - Delete confirmation dialogs

2. **Error Handling & Logging**:
   - Global exception middleware
   - Custom 404 / 500 pages
   - Log errors (never plaintext secrets)

3. **Data Protection Key Protection (Production):**
   - Keys outside web root
   - If hosting supports DPAPI/certificate → use it
   - If not → plaintext keys with strict file permissions + regular backups
   - Document backup/restore procedure
   - README disclaimer about shared-hosting limitation

4. **Deployment (Host-Agnostic)**:
   - Recommended hosts: MonsterASP, Somee (Azure optional)
   - Steps:
     - `dotnet publish -c Release -o published`
     - Copy via FTP / web deploy
     - Connection string via hosting panel / env vars (NEVER commit production connection strings)
     - Run migrations on production DB (or auto-migrate)
     - `ASPNETCORE_ENVIRONMENT=Production`
     - Configure Data Protection key storage to writable persistent folder
     - HTTPS binding (free SSL from host)
   - No Azure-specific features required

5. **HTTPS Enforcement**:
   - `app.UseHttpsRedirection()` always
   - `app.UseHsts()` **production only**
   - `Cookie.SecurePolicy = Always` in production
   - App must not function over plain HTTP in production

6. **Documentation**:
   - README: dev + production setup
   - API usage guide
   - Deployment guide (MonsterASP, Somee, Azure)
   - Screenshots
   - Security disclaimer
   - Data Protection key protection limitations

7. **Final Testing**:
   - Test all features on deployed app
   - Verify encryption survives redeployment
   - Mobile responsiveness

## SECURITY DISCLAIMER (README + app footer):
> CipherVault is an educational/student project and must not be marketed as a production-grade password manager unless its cryptographic design, key management, authentication, deployment configuration, and security implementation have been independently reviewed/audited. Data Protection key storage on shared hosting environments may not provide OS-level protection; keys are stored as files and must be backed up and access-restricted.

## Deliverables:
- Deployed app via HTTPS
- Source code on GitHub
- README with full docs + disclaimer
- Final project report

## Testing Required:
- Register / login / create encrypted item on production
- Restart app → data still decryptable
- CRUD / search / dashboard on production
- API via Postman against production URL (JWT)
- No sensitive data leaks (browser dev tools, logs)
- HTTPS active (no mixed content)

---

# COORDINATION PROTOCOL FOR MULTIPLE AI CHATS

## Session Handoff Template:
```
## HANDOFF FROM PHASE [X] TO PHASE [X+1]

### Completed Deliverables:
- [List]

### Database State:
- Current migrations: [list]
- Seed data: [describe]

### Known Issues:
- [list]

### Configuration:
- Connection string (dev): [default/custom]
- Data Protection keys location: [path]
- JWT settings: [if applicable]

### Next Phase Requirements:
- [specific state needed]

### Git Commit/Tag:
- [hash/tag]
```

## Cross-Phase Validation Checklist:
Before starting any phase, verify:
1. Previous phase deliverables exist
2. Database schema matches expected
3. Service classes contain business logic
4. No business logic in controllers
5. Tests from previous phase pass
6. App runs without errors
7. Security measures still effective
8. Project targets `net10.0` and uses EF Core 10
9. Separate Create/Update DTOs
10. Ownership returns 404 (not 403)

## Phase Completion Criteria (Mandatory):
A phase is **NOT complete** unless ALL are true:
- Code implemented
- Builds without errors
- Runs
- Functionality tested and works
- Security checks performed
- No blocking errors
- Git commit created
- Encryption/security design documented (if applicable)
- AI provided short explanations of important decisions

## Emergency Rollback Plan:
1. Revert to previous Git commit/tag
2. Restore DB from backup (if needed)
3. Report breaking changes
4. Re-attempt with additional test coverage

## AI Explanation Requirement:
For every major component (services, repositories, encryption, auth, API), explain:
- What it does
- Why it is implemented that way
- How it fits into the architecture
- Any security considerations
- For security components: what threat it protects against, what it does NOT protect against, hosting assumptions

Do not simply dump code without explanation.

---

# FINAL DELIVERABLES CHECKLIST

## Complete Application Features:
- [ ] User authentication (register, login, logout)
- [ ] Encrypted storage of secrets and notes
- [ ] No plaintext confidential content in DB
- [ ] Full CRUD for vault items
- [ ] Search and category filter
- [ ] Dashboard with statistics
- [ ] Password generator (crypto-secure) + strength meter
- [ ] REST API with JWT authentication
- [ ] Deployment to free/low-cost host
- [ ] Data Protection keys persisted (never in Git)
- [ ] Responsive, polished UI
- [ ] HTTPS enforced in production
- [ ] API never returns decrypted secrets
- [ ] No plaintext secrets in logs/URLs/errors
- [ ] API returns 401/403 (no login redirects)

## Security & Correctness:
- [ ] Separate Create/Update DTOs; Update preserves Secret when blank
- [ ] Ownership check returns 404 (not 403)
- [ ] API auth uses explicit JWT scheme (no cookie redirect)
- [ ] Purpose strings documented as compatibility contract
- [ ] No plaintext secrets in logs, exceptions, audit entries
- [ ] Cookie policy: `SameAsRequest` (dev), `Always` (prod)
- [ ] JWT signing key never committed to Git
- [ ] Data Protection key protection plan documented in README
- [ ] HTTPS enforced, HSTS only in production
- [ ] No deprecated APIs (WithOpenApi, WebHostBuilder, IActionContextAccessor)

## Code Quality:
- [ ] Service layer for business logic
- [ ] Repository pattern for VaultItem
- [ ] DTOs for data transfer (Create/Update separate)
- [ ] Validation via data annotations
- [ ] No business logic in controllers
- [ ] Proper error handling + logging

## Documentation:
- [ ] README with setup instructions
- [ ] API documentation (Postman collection)
- [ ] Deployment guide (MonsterASP, Somee, Azure)
- [ ] User manual
- [ ] Security disclaimer (incl. shared-hosting key limitation)
- [ ] Explanation of major components

---

# START IMMEDIATELY WITH PHASE 1

Provide the complete code for Phase 1. Ensure:
1. All file paths are correct
2. Namespaces match ASP.NET Core conventions
3. No business logic in controllers
4. Services/repositories stubbed correctly (if applicable)
5. Proper EF Core migrations — do not manually modify schema; document rollback steps
6. No real/default passwords in source. Dev test accounts only, marked clearly.
7. HTTPS redirection configured. HSTS only in production.
8. For each major file, provide brief explanation outside code block: what it does, why it exists, how it fits architecture.
9. Project targets `net10.0`, uses EF Core 10
10. Do NOT use deprecated APIs (`WithOpenApi`, `IActionContextAccessor`, `WebHostBuilder`, `IWebHost`, `WebHost`)
11. Cookie policy: `SameAsRequest` (dev), `Always` (production)
12. Explain security decisions per the AI Explanation Requirement

After Phase 1, I will provide handoff data to continue with Phase 2.
```

---

Yeh **complete final Master Prompt** hai. Ismein saare fixes integrated hain:

✅ ASP.NET Core 10 + EF Core 10
✅ Repository pattern clarification (mandatory for VaultItem, optional for trivial)
✅ Separate Create/Update DTOs with blank-preserve rule
✅ CookieSecurePolicy dev vs prod
✅ Data Protection key protection plan (dev + prod)
✅ API auth scheme separation (JWT explicit)
✅ Purpose strings as compatibility contract
✅ Logging rules (no plaintext)
✅ IDOR 404 rule
✅ Deprecated API avoidance
✅ Data Protection v10.0.7+ security fix
✅ Security disclaimer with shared-hosting key limitation

Aap ise directly copy karke `.md` file me save karo aur Phase 1 shuru karo. 🚀