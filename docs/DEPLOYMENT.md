# CipherVault — Deployment Guide

> **Audience:** deployers hosting CipherVault on Windows shared hosting
> (MonsterASP, Somee) or Azure App Service. Read
> [SECURITY.md](SECURITY.md) first — it explains what the deployment must
> protect, and why some steps below are non-negotiable.

CipherVault is **host-agnostic**. It runs on any ASP.NET Core 10 host with
SQL Server. This guide covers the three most common targets:

| Host                  | Cost                      | SQL Server   | Best for                        |
| --------------------- | ------------------------- | ------------ | ------------------------------- |
| **MonsterASP**        | Free tier                 | ✅ Included  | First-time deployment, learning |
| **Somee**             | Free tier                 | ✅ Included  | Backup option; alternative      |
| **Azure App Service** | Paid (student credits OK) | ✅ Azure SQL | Production-grade deployment     |

All three follow the same pattern. Read
[Step 1 — Publish](#step-1--publish-locally) once, then jump to your host.

---

## Table of Contents

- [Pre-deployment checklist](#pre-deployment-checklist)
- [Step 1 — Publish locally](#step-1--publish-locally)
- [Step 2 — Generate production secrets](#step-2--generate-production-secrets)
- [Step 3 — Prepare the key ring](#step-3--prepare-the-key-ring)
- [MonsterASP deployment](#monsterasp-deployment)
- [Somee deployment](#somee-deployment)
- [Azure App Service deployment](#azure-app-service-deployment)
- [Post-deployment verification](#post-deployment-verification)
- [Troubleshooting](#troubleshooting)
- [Updating an existing deployment](#updating-an-existing-deployment)

---

## Pre-deployment checklist

Run through this **before** touching your hosting panel. Every item is
required — a skipped step typically means "app starts, login fails".

### Code

- [ ] `dotnet build` succeeds with 0 errors
- [ ] `CipherVault.csproj` targets `net10.0`
- [ ] `Microsoft.AspNetCore.DataProtection` ≥ **10.0.7** (CVE-2026-40372)
- [ ] All ASP.NET Core packages aligned to the same 10.0.7 version
- [ ] No `Console.WriteLine` debug logging left in code
- [ ] `git status` is clean (all changes committed)

### Secrets hygiene

- [ ] `git ls-files` shows **no** `App_Data/keys/*.xml`
- [ ] `git ls-files` shows **no** `appsettings.Development.json`
- [ ] `Jwt:Key` is in user-secrets only (dev), not in any tracked file
- [ ] Production connection string is **not** in `appsettings.Production.json`
- [ ] `.gitignore` includes `App_Data/`, `published/`, `*.user`

### Hosting account ready

- [ ] Hosting account created
- [ ] SQL Server database provisioned (see host-specific section)
- [ ] FTP / Web Deploy credentials in hand
- [ ] Free SSL certificate issued (or planned)

### Local test passed

- [ ] `dotnet run --launch-profile Production` starts without errors
- [ ] Login → create item → restart → reveal works locally
- [ ] Custom 404 page renders locally
- [ ] API returns `401 {"error":"unauthorized"}` without a token

---

## Step 1 — Publish locally

Publishing produces a folder of self-contained files ready to upload.

```bash
dotnet publish -c Release -o published
```

This creates a `published/` directory containing:

```text
published/
├── CipherVault.dll
├── CipherVault.exe              (Windows launcher)
├── appsettings.json
├── appsettings.Production.json
├── web.config                    (IIS reverse-proxy config)
├── wwwroot/                      (CSS, JS, images)
├── CipherVault.deps.json
├── CipherVault.runtimeconfig.json
└── ... (dependency DLLs)
```

> ⚠️ **Do NOT upload the following**
>
> | Path                           | Why                                                                 |
> | ------------------------------ | ------------------------------------------------------------------- |
> | `App_Data/keys/`               | The production key ring is created on the host, not copied from dev |
> | `appsettings.Development.json` | Dev-only settings — never published                                 |
> | `bin/`, `obj/`                 | Build intermediates — leave them out                                |
> | `.git/`                        | Source-control metadata                                             |

The published folder should not contain any keys. If it does, you copied
your dev `App_Data/keys` in by mistake — delete and re-publish.

**Sanity check**

```powershell
# Should be empty
Get-ChildItem .\published -Recurse -Filter "*.xml" |
    Where-Object { $_.FullName -match "keys" }
```

If that returns nothing, you're clean.

---

## Step 2 — Generate production secrets

You need three values for production. Generate all three now, before
touching the hosting panel.

### 2.1 JWT signing key

A 32-byte random value, base64-encoded. This is **not** the same as the dev
`Jwt:Key` — generate a fresh one for production.

**PowerShell:**

```powershell
$key = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
Write-Host $key
```

**Bash / Linux / macOS:**

```bash
openssl rand -base64 32
```

Expected output: an 88-character base64 string ending in `=`.

Example (**do not use this — generate your own**):

```text
7u2UPWmP8jyi5GG8dZ/d+y8ZOjHorEoOmgx7CKizoDeMoeL3nSbGBiyZNHdo6mgPo9FDnVNZGLyzn1MtfINPYg==
```

Save this value. You'll paste it into the hosting panel as `Jwt__Key`.

### 2.2 Production connection string

Your host provides this. Format:

```text
Server=<host>,<port>;Database=<db-name>;User Id=<user>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Paste it into a safe place (password manager). You'll paste it into the
hosting panel as `ConnectionStrings__DefaultConnection`.

### 2.3 Data Protection key directory

A path on the host where the key ring will be persisted. Requirements:

- Absolute path (not relative)
- Outside the web root (not under `wwwroot` or the `published` folder)
- Persistent (not a temp folder, not a container's ephemeral FS)
- Writable by the app's service account

Common patterns by host:

| Host              | Recommended path                                                                                                                   |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| MonsterASP        | `D:\home\<account>\CipherVault-keys` or `C:\HostingSpaces\<account>\CipherVault-keys` (check panel for your account's disk layout) |
| Somee             | `D:\Hosting\<account>\keys` or as documented in Somee panel                                                                        |
| Azure App Service | `D:\home\CipherVault-keys` (persistent) or Azure Blob + Key Vault                                                                  |

Save the path. You'll paste it into the hosting panel as
`DataProtection__KeyDirectory`.

---

## Step 3 — Prepare the key ring

**First deployment:** do nothing. The app creates the key ring on first run.

**Redeployment:** the key ring must **not** be overwritten. If you already
have a working deployment with real data, the existing key ring is required
to decrypt it. See [Updating an existing deployment](#updating-an-existing-deployment).

**First-time verification**

After the app starts for the first time, the key directory will contain one
or more `key-<guid>.xml` files. Verify by:

1. Logging into your hosting panel's file manager
2. Navigating to your `DataProtection__KeyDirectory`
3. Confirming at least one `key-*.xml` file exists

Back this directory up immediately. Losing it makes all ciphertext
permanently undecryptable.

---

## MonsterASP deployment

**Website:** [monsterasp.net](https://monsterasp.net)  
**Cost:** Free tier available

### 1. Create an account and a website

1. Sign up at [monsterasp.net](https://monsterasp.net)
2. From the control panel, create a new website
3. Choose:
   - **Runtime:** ASP.NET Core 10
   - **Plan:** Free (or paid if you need custom domain SSL)
4. Note the assigned **FTP host**, **FTP username**, **FTP password**

### 2. Create a SQL Server database

1. In the control panel, go to **Databases → SQL Server**
2. Create a new database, name it e.g. `CipherVaultDb`
3. Note:
   - **Server** (e.g. `sqlXXX.monsterasp.net`)
   - **Database name**
   - **Username**
   - **Password**
4. Copy the full connection string — you'll need it in step 5

If the panel offers a "Connection string" field with copy button, use it
directly. Otherwise compose:

```text
Server=<server>,1433;Database=<db>;User Id=<user>;Password=<pass>;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### 3. Upload published files

1. Open an FTP client (FileZilla recommended)
2. Connect with the FTP credentials from step 1
3. Navigate to the site's root directory — usually `/wwwroot` or `/httpdocs`
4. Upload everything from your local `published/` folder
5. **Do not upload `App_Data/keys/`** even if it exists

First upload may take a few minutes depending on file count.

### 4. Configure environment variables

MonsterASP exposes env vars via a `web.config` file in the site root, or
via the panel's **Environment Variables** section.

**Option A — Control panel (preferred):**

1. Go to **Website → Configuration → Environment Variables**
2. Add:

| Key                                    | Value                                                             |
| -------------------------------------- | ----------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`               | `Production`                                                      |
| `ConnectionStrings__DefaultConnection` | (paste from step 2)                                               |
| `DataProtection__KeyDirectory`         | (paste from step 3.3 — e.g. `D:\home\<account>\CipherVault-keys`) |
| `Jwt__Key`                             | (paste from step 2.1)                                             |

**Option B — `web.config`:**

Edit the uploaded `web.config` and add inside `<aspNetCore>`:

```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="ConnectionStrings__DefaultConnection" value="..." />
  <environmentVariable name="DataProtection__KeyDirectory" value="D:\home\<account>\CipherVault-keys" />
  <environmentVariable name="Jwt__Key" value="..." />
</environmentVariables>
```

> ⚠️ **Security note:** if you use Option B, `web.config` will contain secrets
> in plaintext on the server's filesystem. Ensure the file's permissions
> restrict read access. Option A is preferred when available.

### 5. Run migrations on the production database

The app runs migrations automatically in **development only**. In production,
apply them manually once.

**Option A — From your local machine (recommended):**

Temporarily point your local app at the production DB and run migrations:

```powershell
# In a fresh PowerShell window
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=...;..."
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet ef database update
```

Then close the window (env vars are session-scoped).

**Option B — Let the app do it on first run (temporary code change):**

Modify `DbInitializer.SeedAsync` to call `context.Database.MigrateAsync()`
unconditionally, deploy, run once, then revert. **Not recommended** —
production auto-migration is a footgun.

**Option C — SQL script:**

```bash
dotnet ef migrations script -o migrations.sql --idempotent
```

Upload and run `migrations.sql` via the hosting panel's SQL query tool.

### 6. Enable HTTPS and test

1. In the MonsterASP panel, enable a free SSL certificate for your
   assigned subdomain
2. Wait for the certificate to provision (usually 5–15 minutes)
3. Visit `https://<yoursubdomain>.monsterasp.net/`
4. Verify [Post-deployment verification](#post-deployment-verification)

---

## Somee deployment

**Website:** [somee.com](https://somee.com)  
**Cost:** Free tier available

Somee is similar to MonsterASP but has slightly different UI conventions.
The overall flow is the same — the differences are called out below.

### 1. Create an account and a website

1. Sign up at [somee.com](https://somee.com)
2. Create a new website with:
   - **Runtime:** ASP.NET Core 10 (if not listed, choose the latest available)
   - **Plan:** Free
3. Note the FTP credentials and the assigned website URL

### 2. Create a SQL Server database

1. In the panel, go to **Databases → Add**
2. Create a database named `CipherVaultDb`
3. Somee's connection string format is:

```text
workstation id=CipherVaultDb.mssql.somee.com;packet size=4096;user id=<user>;pwd=<pass>;data source=CipherVaultDb.mssql.somee.com;persist security info=False;initial catalog=CipherVaultDb;TrustServerCertificate=True
```

Copy the exact string Somee provides — do not retype it manually.

### 3. Upload published files

1. FTP client → connect with Somee credentials
2. Somee's site root is usually `/wwwroot` or the folder named after your site
3. Upload everything from `published/`
4. Do not upload `App_Data/keys/`

### 4. Configure environment variables

Somee does not have a first-class environment variables UI on the free tier.
Use `web.config` for all four variables (see MonsterASP step 4, Option B).

Ensure `<aspNetCore>` in `web.config` has:

```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="ConnectionStrings__DefaultConnection" value="..." />
  <environmentVariable name="DataProtection__KeyDirectory" value="D:\Hosting\<account>\keys" />
  <environmentVariable name="Jwt__Key" value="..." />
</environmentVariables>
```

Verify the key directory path exists and is writable by Somee's app pool.
Somee docs recommend `D:\Hosting\<youraccount>\` for persistent storage.
If that fails, contact Somee support — the free tier may restrict file
system writes outside the site root.

### 5. Run migrations

Same as MonsterASP — use `dotnet ef database update` from your local machine
with the production connection string, or run `migrations.sql` via Somee's
SQL query panel.

### 6. Enable HTTPS and test

Somee free tier includes a shared SSL certificate on `*.somee.com`. Visit
`https://<yoursite>.somee.com/` and run the post-deployment verification.

---

## Azure App Service deployment

**Website:** [azure.microsoft.com](https://azure.microsoft.com)  
**Cost:** Free student credits available; App Service B1 ~$13/month  
**Recommended for:** portfolio projects, real production use

Azure has the best story for key management because you can use Azure Key
Vault instead of relying on the file system.

### 1. Create resources

1. **App Service** — Runtime: .NET 10, OS: Windows or Linux
2. **Azure SQL Database** — Basic tier is fine for a portfolio project
3. (Optional but recommended) **Azure Key Vault** — for the Data Protection key ring

### 2. Configure App Service environment variables

In the Azure portal, **App Service → Configuration → Application settings**:

| Name                                   | Value                                                                    |
| -------------------------------------- | ------------------------------------------------------------------------ |
| `ASPNETCORE_ENVIRONMENT`               | `Production`                                                             |
| `ConnectionStrings__DefaultConnection` | (Azure SQL connection string)                                            |
| `DataProtection__KeyDirectory`         | `/home/CipherVault-keys` (Linux) or `D:\home\CipherVault-keys` (Windows) |
| `Jwt__Key`                             | (paste from step 2.1)                                                    |

Azure App Service provides persistent storage at `/home` (Linux) or
`D:\home` (Windows). Using that path is safe across redeploys.

### 3. (Recommended) Use Azure Key Vault for the key ring

Instead of persisting keys to the file system, use Azure Blob Storage +
Key Vault for wrapping. Add to `Program.cs` (not in current codebase):

```csharp
builder.Services
    .AddDataProtection()
    .PersistKeysToAzureBlobStorage(new Uri("<blob-sas-or-managed-identity-uri>"))
    .ProtectKeysWithAzureKeyVault(new Uri("<key-vault-key-uri>"), new DefaultAzureCredential())
    .SetApplicationName("CipherVault");
```

This requires:

- A blob container in Azure Storage
- A key in Azure Key Vault
- Managed Identity assigned to the App Service, granted access to both

This project does not ship this configuration. Add it if you deploy to
Azure and care about key protection at rest.

### 4. Deploy

Three options:

- **Option A — GitHub Actions:** Azure portal → **Deployment Center** →
  GitHub → select your repo. Azure generates a workflow file.
- **Option B — Zip Deploy via CLI:**

```bash
dotnet publish -c Release -o published
cd published
zip -r ../publish.zip .
cd ..
az webapp deployment source config-zip --resource-group <rg> \
    --name <app-name> --src publish.zip
```

- **Option C — VS Code Azure extension:** right-click the project → **Deploy
  to Web App**.

### 5. Run migrations

```powershell
$env:ConnectionStrings__DefaultConnection = "<azure-sql-conn-string>"
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet ef database update
```

Or use the SQL script approach (`dotnet ef migrations script`).

### 6. Verify

See [Post-deployment verification](#post-deployment-verification).

---

## Post-deployment verification

Run these checks against the **live deployed URL** — not localhost.

### Basic

| #   | Check                   | How                         | Expected                                         |
| --- | ----------------------- | --------------------------- | ------------------------------------------------ |
| 1   | Site loads over HTTPS   | Visit the URL               | HTTPS padlock, no cert warnings                  |
| 2   | HTTP redirects to HTTPS | Visit `http://...`          | Redirect to `https://...`                        |
| 3   | HSTS active             | `curl -I https://<domain>/` | `Strict-Transport-Security: max-age=...` present |
| 4   | Custom 404              | Visit `/nonexistent-xyz`    | Dark-themed 404 page (not default IIS page)      |

### Functional

| #   | Check             | How                | Expected                            |
| --- | ----------------- | ------------------ | ----------------------------------- |
| 5   | Register works    | Fill register form | Account created, redirected         |
| 6   | Login works       | Log in             | Dashboard renders                   |
| 7   | Create item works | Add a vault item   | Item appears in list, secret masked |
| 8   | Reveal works      | Click Reveal       | Plaintext shown                     |
| 9   | Copy works        | Click Copy → paste | Works, clears after 10s             |
| 10  | Search works      | Filter by category | Results filtered                    |
| 11  | Pagination works  | Navigate pages     | Filter preserved in URL             |

### 🔥 Critical — encryption + key ring

| #   | Check                   | How                                                                                              | Expected                         |
| --- | ----------------------- | ------------------------------------------------------------------------------------------------ | -------------------------------- |
| 12  | Restart persistence     | Log in → create item → wait for app to recycle (or restart via panel) → log in → reveal the item | Plaintext decrypts correctly     |
| 13  | Redeploy persistence    | After step 12, deploy a code change (e.g. bump footer version) → reveal the item again           | Still decrypts                   |
| 14  | Key ring exists on host | File manager → `DataProtection__KeyDirectory`                                                    | At least one `key-*.xml` present |

If step 12 or 13 fails — the key ring isn't being persisted correctly.
Verify:

- `DataProtection__KeyDirectory` is set (check app logs for the startup throw if missing)
- The directory is writable by the app
- The directory is **not** inside the published folder (which gets overwritten on redeploy)

### Security

| #   | Check                      | How                                                               | Expected                                             |
| --- | -------------------------- | ----------------------------------------------------------------- | ---------------------------------------------------- |
| 15  | API 401 without token      | `curl https://<domain>/api/v1/vault`                              | `{"error":"unauthorized"}` HTTP 401 — not a redirect |
| 16  | API 401 with invalid token | `curl -H "Authorization: Bearer bad" ...`                         | Same as above                                        |
| 17  | `?decrypt=true` rejected   | curl the list endpoint with a valid token and `?decrypt=true`     | HTTP 400 `{"error":"decryption_not_supported"}`      |
| 18  | Cookie is Secure           | DevTools → Application → Cookies → `CipherVault.Auth`             | `Secure` ✓, `HttpOnly` ✓, `SameSite=Lax`             |
| 19  | No plaintext in logs       | Log into hosting panel, view app logs, search for a test password | No matches                                           |
| 20  | No stack traces            | Force a 500 (visit a broken URL, or send malformed API JSON)      | Generic error, no stack trace                        |

### API (with Postman)

Import `postman/CipherVault.postman_collection.json` and set the
`baseUrl` variable to your production URL. Run:

1. `POST /api/auth/login` → token returned
2. `GET /api/v1/vault` with token → list returned
3. `POST /api/v1/vault` → item created
4. `GET /api/v1/vault/{id}` → item returned (secret masked)
5. `PUT /api/v1/vault/{id}` → item updated
6. `DELETE /api/v1/vault/{id}` → item deleted
7. `GET /api/v1/vault` without token → 401 JSON

---

## Troubleshooting

### "The ConnectionString property has not been initialized"

**Cause:** `ConnectionStrings__DefaultConnection` env var not set, or set
to empty.

**Fix:**

- Confirm the env var is configured in the hosting panel
- Restart the app pool (recycle) so it picks up the new value
- Check for typos in the variable name — it's **two underscores**:
  `ConnectionStrings__DefaultConnection` (not `ConnectionStrings_DefaultConnection`)

### "Failed to bind to address http://...: address already in use"

**Cause:** another process on the host is using the same port.

**Fix:** hosting providers usually manage the port binding via `web.config`
and the `<aspNetCore>` `processPath`. Do not override `applicationUrl` in
production. If the panel exposes a port setting, use its default.

### "Unhandled exception. DataProtection:KeyDirectory must be set in Production"

**Cause:** `DataProtection__KeyDirectory` env var not set.

**Fix:** set it in the hosting panel. Restart the app.

### Login page loads but login always fails

**Symptoms:** form submits, returns to login page with no error.

**Possible causes:**

1. **JWT key wrong shape.** `Jwt:Key` must be base64 of exactly 32 bytes
   (44 characters including `==`). Verify with `$key.Length -eq 44`.
2. **Cookie not being set.** Check DevTools → Application → Cookies. If
   `CipherVault.Auth` is missing, the cookie was rejected — likely because
   HTTPS is misconfigured.
3. **Data Protection key ring changed between requests** (e.g. writing to a
   non-persistent directory). Check if a new `key-*.xml` appears on every
   app restart in the panel's file manager.

### "Key {guid} may be persisted to storage in unencrypted form"

This is a **warning**, not an error. See [SECURITY.md §5](SECURITY.md#5-key-protection-in-production).
On shared hosting, keys are plaintext XML files. Acceptable for education;
add DPAPI or certificate protection for production-grade deployments.

### Data "disappears" after redeploy

**Cause:** the key ring was overwritten during redeploy, or the app is
writing to a different key directory than before.

**Diagnosis:**

1. Log into the host, navigate to `DataProtection__KeyDirectory`
2. Are the same `key-*.xml` files still there? If they changed, the key ring
   was regenerated.
3. Check the deployment script — did it delete the key directory, or upload
   a fresh empty one?

**Prevention:** put the key directory outside the published folder, and
make sure your deploy script does not touch it.

**Recovery:** if the old key files were deleted, ciphertext is unrecoverable.
Restore from backup if available.

### Custom 404 page does not appear

**Cause:** hosting provider's IIS intercepts 404 before the app sees it.

**Fix:** the `web.config` needs:

```xml
<system.webServer>
  <httpErrors existingResponse="PassThrough" />
</system.webServer>
```

Without `PassThrough`, IIS serves its own 404 page instead of the app's.

### API returns redirect instead of 401

**Cause:** authentication scheme separation misconfigured, or `web.config`
rewriting the response.

**Fix:** verify `Program.cs` has:

```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
```

on API controllers. And confirm the `JwtBearerEvents.OnChallenge` handler is
registered (it writes the JSON 401 and calls `ctx.HandleResponse()`).

---

## Updating an existing deployment

Once you have production data (real users, real vault items), the deployment
procedure changes. You **must** protect the key ring.

### Rule 1 — Never delete or overwrite the key directory

Put it outside the published folder. Verify your deploy tool doesn't include
it.

### Rule 2 — Back up the key directory before every update

Download the current `key-*.xml` files locally (or to a separate location on
the host). If anything goes wrong, you can restore.

```powershell
# FTP client: download all files from DataProtection__KeyDirectory
# Store them in a dated folder, e.g. backups/2026-09-24/
```

### Rule 3 — Apply migrations before uploading code

If the new version includes EF migrations:

```powershell
dotnet ef database update --connection "<prod-conn-string>"
```

Or generate an idempotent SQL script and run it via the panel.

**Reason:** running new code against an old schema typically throws. Migrate
first, deploy second.

### Rule 4 — Deploy with a rolling restart if possible

MonsterASP, Somee, and Azure App Service support app pool recycle or
deployment slots. Recycle after deploying so the new code picks up.

### Update procedure

1. **Back up the key ring** (download from host)
2. **Back up the database** (panel's backup tool, or `sqlcmd` if available)
3. **Apply migrations** (`dotnet ef database update` from local)
4. `dotnet publish -c Release -o published`
5. **Upload only the changed files** if your FTP client supports sync;
   otherwise upload everything **except** `App_Data/keys/`
6. **Recycle the app pool** (panel button, or by touching `web.config`)
7. **Run the post-deployment verification** — at minimum, checks **1, 2, 5,
   6, 7, 8, and 12**

### Rollback

If something breaks:

1. Stop the app (panel → Stop website)
2. Restore the key ring from backup (if changed)
3. Restore the database from backup (if migrations caused the break)
4. Re-upload the previous `published/` folder
5. Restart the app
6. Verify

Rollback from a bad migration may require a database restore — EF does not
auto-revert. Plan migrations carefully.

---

## Quick reference — environment variables

| Variable                               | Required? | Example value                                                                  |
| -------------------------------------- | --------- | ------------------------------------------------------------------------------ |
| `ASPNETCORE_ENVIRONMENT`               | ✅        | `Production`                                                                   |
| `ConnectionStrings__DefaultConnection` | ✅        | `Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True` |
| `DataProtection__KeyDirectory`         | ✅        | `D:\home\<account>\CipherVault-keys`                                           |
| `Jwt__Key`                             | ✅        | base64 of 32 random bytes (88 chars)                                           |
| `Jwt__Issuer`                          | optional  | `CipherVault` (defaults from `appsettings`)                                    |
| `Jwt__Audience`                        | optional  | `CipherVault`                                                                  |
| `Jwt__ExpiryMinutes`                   | optional  | `60`                                                                           |

All four required variables use double underscores (`__`) as the
.NET configuration hierarchy separator.

---

## Related documents

- [SECURITY.md](SECURITY.md) — encryption design and threat model
- [API.md](API.md) — REST API reference
- [USER_MANUAL.md](USER_MANUAL.md) — end-user guide
- [README.md](../README.md) — project overview

---

> **Reminder:** CipherVault is an educational project. Follow the
> [security disclaimer](../README.md#security-disclaimer) before storing
> anything of real value.
