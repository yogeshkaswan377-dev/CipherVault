## HANDOFF FROM PHASE 1 TO PHASE 2

### Completed Deliverables:
- CipherVault.csproj targeting net10.0 with EF Core 10, Identity 10.0.7, Data Protection 10.0.7
- Program.cs (WebApplication pattern; no WithOpenApi / WebHostBuilder / IWebHost / IActionContextAccessor)
- ApplicationDbContext : IdentityDbContext (ready for DbSet<VaultItem> in Phase 2)
- InitialIdentitySchema migration applied to LocalDB (CipherVaultDb)
- Identity auth (Register/Login/Logout/AccessDenied) via Default Identity UI
- _Layout.cshtml with responsive Bootstrap 5 navbar + _LoginPartial (shows user name + Logout)
- HomeController (Index/About/Contact/Privacy/Error)
- DashboardController [Authorize] returning placeholder view
- Empty Services/, Services/Contracts/, Repositories/, Repositories/Contracts/, DTOs/
- Data Protection keys persisted to App_Data/keys (git-ignored)
- HTTPS redirection always on; HSTS in production only

### Database State:
- Migrations: InitialIdentitySchema
- Seed data: none (test users created manually during Phase 1 testing)
- Connection: (localdb)\mssqllocaldb / CipherVaultDb
- ASP.NET Identity schema version: default (Version3)

### Known Issues (all non-blocking):
- RequireConfirmedAccount = false — acceptable for Phase 2; email sender is out of scope
- NuGet warnings NU1901 for transitive NuGet.Packaging / NuGet.Protocol 6.12.1 (low severity, tooling-only)
- The auth cookie no longer uses a __Host- prefix. Do NOT re-add it: the prefix is rejected
  by browsers when the cookie is issued over plain HTTP, which breaks local dev login.
  Any future cookie rename must be re-tested with the dev-login flow.

### Configuration:
- DataProtection:KeyDirectory = "<ContentRoot>/App_Data/keys"
  (override via env var DataProtection__KeyDirectory). SetApplicationName("CipherVault").
- Auth cookie:
    Name        = "CipherVault.Auth"       (NOT __Host- prefixed)
    Path        = "/"
    HttpOnly    = true
    SameSite    = Lax
    SecurePolicy= Always                   (unconditional, dev + prod)
    Expire      = 2h sliding
- HTTPS (dev):
    launchSettings.json has profiles: https (default) | http | Production
    URLs: https://localhost:7123 ; http://localhost:5123
    Dev cert trusted via `dotnet dev-certs https --trust`
    Run prod-like locally: `dotnet run --launch-profile Production`
    (Note: `$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet run` does NOT work —
     launchSettings.json overrides the env var. Use --launch-profile or --no-launch-profile.)
- JWT settings: N/A (Phase 3)

### Next Phase Requirements (Phase 2):
- Extend ApplicationDbContext with DbSet<VaultItem> and configure the
  VaultItem -> IdentityUser relationship (UserId string FK).
- Inject IDataProtectionProvider into VaultItemService and create two protectors:
    _secretProtector = provider.CreateProtector("CipherVault.Secret");
    _notesProtector  = provider.CreateProtector("CipherVault.Notes");
  The purpose strings are a compatibility contract — once data is written with them,
  they must never change or the existing ciphertext becomes undecryptable.
- Controllers obtain current user id via UserManager.GetUserId(User) (already available).
- Do NOT re-introduce a __Host- cookie prefix. Do NOT change SecurePolicy to a
  non-Always value. Do NOT modify DataProtection:KeyDirectory mid-development without
  backing up the existing App_Data/keys folder.

### Git Commit/Tag:
- Tag: phase-1-complete