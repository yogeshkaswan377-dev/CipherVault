## HANDOFF FROM PHASE 2 TO PHASE 3

### Completed Deliverables:
- VaultItem model + VaultCategory constants
- ApplicationDbContext extended with DbSet<VaultItem>, FK, indexes
- Migration: AddVaultItems
- IVaultItemRepository / VaultItemRepository (data access only)
- IVaultItemService / VaultItemService (encryption + ownership + audit logging)
- DTOs: VaultItemCreateDTO / VaultItemUpdateDTO / VaultItemDisplayDTO
- Validation: ValidCategoryAttribute, OptionalUrlAttribute
- VaultController (thin): Index / Create / Edit / Delete / Details / RevealSecret / RevealNotes
- Views: Index, Create, Edit, Details, Delete
- Partial: _PasswordGenerator.cshtml
- wwwroot/js/password-generator.js (crypto.getRandomValues, rejection sampling)

### Database State:
- Migrations: InitialIdentitySchema, AddVaultItems
- Tables: AspNetUsers..., VaultItems
- Indexes: IX_VaultItems_UserId, IX_VaultItems_UserId_Category
- FK: VaultItems.UserId -> AspNetUsers.Id (Cascade)
- Seed data: none (test data created manually)

### Encryption Contract (DO NOT CHANGE):
- Secret purpose : "CipherVault.Secret"
- Notes purpose  : "CipherVault.Notes"
- Both exposed as public const on VaultItemService
- Key ring: App_Data/keys (gitignored, DataProtection:KeyDirectory override)

### Known Issues:
- None blocking.
- Reveal decryption failure throws InvalidOperationException -> 500.
  Global exception middleware is a Phase 4 deliverable.
- No pagination on the Index list (Phase 4 polish item).

### Configuration:
- DI: AddScoped<IVaultItemRepository, VaultItemRepository>()
      AddScoped<IVaultItemService, VaultItemService>()
- Unchanged from Phase 1: cookie name "CipherVault.Auth",
  SecurePolicy = Always, no __Host- prefix, HTTPS-only launch profiles.

### Next Phase Requirements (Phase 3):
- Extend IVaultItemService with a search/filter method
  (Title/Username/Url contains + Category equality), server-side LINQ,
  case-insensitive, always scoped to userId.
- Add Dashboard aggregate queries (total count, count per category,
  last 5 items). These are trivial read-only queries — inject
  ApplicationDbContext directly into a DashboardService
  (do NOT create a repository for them).
- Add Controllers/Api/VaultApiController with JWT scheme:
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
  Reuse IVaultItemService. Never return decrypted values.
  ?decrypt=true must be ignored or 400.
- Register a named "Bearer" authentication scheme.
  MVC controllers stay on the cookie scheme.
- Do NOT use WithOpenApi (deprecated in ASP.NET Core 10).

### Git Commit/Tag:
- Tag: phase-2-complete 