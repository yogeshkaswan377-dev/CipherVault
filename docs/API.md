# CipherVault — REST API Reference

> **Base URL (development):** `https://localhost:7123`  
> **Base URL (production):** `https://<your-domain>`  
> **API version:** `v1`  
> **Auth:** JWT Bearer

The CipherVault REST API provides programmatic access to vault items.
**Secrets and notes are never returned in plaintext by any API endpoint.**
This is a deliberate design decision, not a missing feature — see
[Why secrets are never returned](#why-secrets-are-never-returned).

---

## Table of Contents

- [Quick start](#quick-start)
- [Authentication](#authentication)
- [Conventions](#conventions)
- [Endpoints](#endpoints)
  - [POST /api/auth/login](#post-apiauthlogin)
  - [GET /api/v1/vault](#get-apiv1vault)
  - [GET /api/v1/vault/{id}](#get-apiv1vaultid)
  - [POST /api/v1/vault](#post-apiv1vault)
  - [PUT /api/v1/vault/{id}](#put-apiv1vaultid)
  - [DELETE /api/v1/vault/{id}](#delete-apiv1vaultid)
- [Error responses](#error-responses)
- [The `?decrypt=true` parameter](#the-decrypttrue-parameter)
- [Postman collection](#postman-collection)
- [Rate limiting](#rate-limiting)
- [Security notes](#security-notes)

---

## Quick start

```bash
# 1. Log in and capture the token
TOKEN=$(curl -s -X POST https://localhost:7123/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@test.local","password":"YourPassword123!"}' \
  | jq -r .token)

# 2. List vault items
curl -s https://localhost:7123/api/v1/vault \
  -H "Authorization: Bearer $TOKEN" | jq

# 3. Create an item
curl -s -X POST https://localhost:7123/api/v1/vault \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "GitHub",
    "category": "Password",
    "username": "alice",
    "secret": "ghp_exampletoken123",
    "notes": "Personal access token",
    "url": "https://github.com"
  }' | jq
```

`jq` is optional — it just pretty-prints the JSON output.

---

## Authentication

### Obtaining a token

`POST /api/auth/login` with `email` and `password`. On success, the response
contains a JWT Bearer token.

### Using the token

Include the token in the `Authorization` header of every subsequent request:

```text
Authorization: Bearer <token>
```

### Token lifetime

Default: **60 minutes** (configurable via `Jwt:ExpiryMinutes`).

### What happens when the token expires

Any authenticated request returns:

```text
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{"error":"unauthorized"}
```

The API **never** redirects to a login page. This is intentional — an API
client expects JSON, not an HTML login form. See
[SECURITY.md §14](SECURITY.md#14-api-authentication-separation).

### Obtaining a new token

Call `POST /api/auth/login` again. There is no separate refresh-token
endpoint in this version.

---

## Conventions

### Content type

All requests with a body must send:

```text
Content-Type: application/json
```

All responses (success or error) have:

```text
Content-Type: application/json; charset=utf-8
```

### Date format

All timestamps are ISO 8601 UTC:

```text
2026-09-24T17:11:16.123Z
```

### Identifiers

`VaultItem.Id` is an integer. The API treats any non-integer ID as
`404 Not Found` (never `400`) to avoid leaking implementation details.

### Masking

Every response that describes a vault item includes:

```json
{
  "maskedSecret": "••••••••",
  "hasNotes": true
}
```

`maskedSecret` is a fixed placeholder string. `hasNotes` is a boolean. The
actual `secret` and `notes` values are never included in API responses.

### Ownership

Every endpoint is scoped to the authenticated user. Attempting to access
another user's item returns `404 Not Found` — never `403 Forbidden`. See
[SECURITY.md §11](SECURITY.md#11-ownership-and-access-control-idor).

### Pagination

`GET /api/v1/vault` supports pagination via query string. See
[GET /api/v1/vault](#get-apiv1vault).

---

## Endpoints

### POST /api/auth/login

Authenticates a user and returns a JWT Bearer token.

#### Request

```text
POST /api/auth/login
Content-Type: application/json
```

Body:

| Field      | Type   | Required | Notes                    |
| ---------- | ------ | -------- | ------------------------ |
| `email`    | string | ✅       | Registered email address |
| `password` | string | ✅       | Account password         |

Example:

```json
{
  "email": "alice@test.local",
  "password": "YourPassword123!"
}
```

#### Success response

```text
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-09-24T18:11:16Z"
}
```

#### Error responses

Invalid credentials (wrong email or wrong password — the response is
identical for both, to avoid user enumeration):

```text
HTTP/1.1 401 Unauthorized
```

```json
{ "error": "invalid_credentials" }
```

Account locked (5 failed attempts within the lockout window):

```text
HTTP/1.1 423 Locked
```

```json
{ "error": "account_locked" }
```

Validation error:

```text
HTTP/1.1 400 Bad Request
```

```json
{ "error": "invalid_request", "details": ["The email field is required."] }
```

#### Notes

- The lockout policy mirrors the MVC login form: **5 failed attempts →
  15-minute lockout**.
- A locked account returns `423 Locked` even if the correct password is
  supplied, until the lockout window expires.
- Anonymous endpoint — no `Authorization` header required.

---

### GET /api/v1/vault

Lists the current user's vault items (paginated, filterable).

#### Request

```text
GET /api/v1/vault?search=<term>&category=<cat>&page=<n>&pageSize=<n>
Authorization: Bearer <token>
```

Query parameters:

| Parameter  | Type   | Default | Constraints                   | Notes                                                     |
| ---------- | ------ | ------- | ----------------------------- | --------------------------------------------------------- |
| `search`   | string | (none)  | max 100 chars                 | Case-insensitive match against `title`, `username`, `url` |
| `category` | string | (none)  | one of the allowed categories | Exact match                                               |
| `page`     | int    | 1       | ≥ 1                           | 1-based page number                                       |
| `pageSize` | int    | 10      | 1–100                         | Clamped to 100 if larger                                  |

Allowed categories: `Password`, `Note`, `API Key`, `Credit Card`,
`Other`. Any other value is treated as "no filter".

#### Success response

```text
HTTP/1.1 200 OK
```

```json
{
  "items": [
    {
      "id": 42,
      "title": "GitHub",
      "category": "Password",
      "username": "alice",
      "url": "https://github.com",
      "createdAt": "2026-09-01T10:00:00Z",
      "updatedAt": "2026-09-15T14:30:00Z",
      "maskedSecret": "••••••••",
      "hasNotes": true
    },
    {
      "id": 41,
      "title": "AWS Root",
      "category": "API Key",
      "username": null,
      "url": null,
      "createdAt": "2026-08-20T08:15:00Z",
      "updatedAt": "2026-08-20T08:15:00Z",
      "maskedSecret": "••••••••",
      "hasNotes": false
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 2,
  "totalPages": 1
}
```

#### Error responses

| Status | Body                                   | Cause                             |
| ------ | -------------------------------------- | --------------------------------- |
| 400    | `{"error":"decryption_not_supported"}` | `?decrypt=true` was supplied      |
| 401    | `{"error":"unauthorized"}`             | Missing / invalid / expired token |

#### Notes

- Search is server-side and indexed (`IX_VaultItems_UserId_Category`).
- Results are ordered by `updatedAt` descending, then by `id` descending for
  stable pagination.
- If the `category` value is not one of the allowed ones, it is silently
  ignored (treated as no filter). This prevents filter injection without
  returning a confusing error.

---

### GET /api/v1/vault/{id}

Retrieves a single vault item by ID.

#### Request

```text
GET /api/v1/vault/{id}
Authorization: Bearer <token>
```

Path parameters:

| Parameter | Type | Required | Notes           |
| --------- | ---- | -------- | --------------- |
| `id`      | int  | ✅       | The item's `Id` |

#### Success response

```text
HTTP/1.1 200 OK
```

```json
{
  "id": 42,
  "title": "GitHub",
  "category": "Password",
  "username": "alice",
  "url": "https://github.com",
  "createdAt": "2026-09-01T10:00:00Z",
  "updatedAt": "2026-09-15T14:30:00Z",
  "maskedSecret": "••••••••",
  "hasNotes": true
}
```

#### Error responses

| Status | Body                                   | Cause                                         |
| ------ | -------------------------------------- | --------------------------------------------- |
| 400    | `{"error":"decryption_not_supported"}` | `?decrypt=true` was supplied                  |
| 401    | `{"error":"unauthorized"}`             | Missing / invalid / expired token             |
| 404    | (empty)                                | Item doesn't exist or belongs to another user |

> **Note:** the API cannot distinguish "not found" from "not yours" — this is
> deliberate. See [SECURITY.md §11](SECURITY.md#11-ownership-and-access-control-idor).

---

### POST /api/v1/vault

Creates a new vault item.

#### Request

```text
POST /api/v1/vault
Authorization: Bearer <token>
Content-Type: application/json
```

Body:

| Field      | Type   | Required | Constraints                                  | Notes                            |
| ---------- | ------ | -------- | -------------------------------------------- | -------------------------------- |
| `title`    | string | ✅       | 1–100 chars                                  |                                  |
| `category` | string | ✅       | one of allowed categories                    |                                  |
| `secret`   | string | ✅       | 1–500 chars                                  | Encrypted at rest before storage |
| `username` | string | ❌       | max 100 chars                                |                                  |
| `url`      | string | ❌       | max 500 chars, must be valid URL if provided |                                  |
| `notes`    | string | ❌       | max 2000 chars                               | Encrypted at rest before storage |

Example:

```json
{
  "title": "GitHub",
  "category": "Password",
  "username": "alice",
  "secret": "ghp_exampletoken123",
  "notes": "Personal access token — read/write repo scope",
  "url": "https://github.com"
}
```

#### Success response

```text
HTTP/1.1 201 Created
Location: /api/v1/vault/42
```

```json
{
  "id": 42,
  "title": "GitHub",
  "category": "Password",
  "username": "alice",
  "url": "https://github.com",
  "createdAt": "2026-09-24T17:11:16Z",
  "updatedAt": "2026-09-24T17:11:16Z",
  "maskedSecret": "••••••••",
  "hasNotes": true
}
```

#### Error responses

| Status | Body                                            | Cause                                                   |
| ------ | ----------------------------------------------- | ------------------------------------------------------- |
| 400    | `{"error":"validation_failed","details":[...]}` | Missing required field, invalid category, malformed URL |
| 401    | `{"error":"unauthorized"}`                      | Missing / invalid / expired token                       |

#### Notes

- The response body describes the created item but **never** contains the
  plaintext secret or notes. Clients that need the plaintext must go
  through the MVC UI (which uses a different, ownership-checked reveal
  endpoint).
- A caller cannot read back what they wrote via the API. This is intentional
  — see [Why secrets are never returned](#why-secrets-are-never-returned).

---

### PUT /api/v1/vault/{id}

Updates an existing vault item. Partial updates are supported via a
blank-preserve rule.

#### Request

```text
PUT /api/v1/vault/{id}
Authorization: Bearer <token>
Content-Type: application/json
```

Body:

| Field      | Type   | Required | Blank-preserve behavior                           |
| ---------- | ------ | -------- | ------------------------------------------------- |
| `title`    | string | ✅       | Replaces existing title                           |
| `category` | string | ✅       | Replaces existing category                        |
| `username` | string | ❌       | `null` = preserve; `""` = clear                   |
| `url`      | string | ❌       | `null` = preserve; `""` = clear                   |
| `secret`   | string | ❌       | `null`/`""` = preserve existing encrypted secret  |
| `notes`    | string | ❌       | `null` = preserve; `""` = clear (set to no notes) |

Example — update only the title, keep the secret and notes:

```json
{
  "title": "GitHub (work account)",
  "category": "Password"
}
```

Example — update the secret, preserve notes:

```json
{
  "title": "GitHub",
  "category": "Password",
  "secret": "new-token-here"
}
```

Example — explicitly clear notes:

```json
{
  "title": "GitHub",
  "category": "Password",
  "notes": ""
}
```

#### Success response

```text
HTTP/1.1 200 OK
```

Same shape as `GET /api/v1/vault/{id}`.

#### Error responses

| Status | Body                                            | Cause                                                   |
| ------ | ----------------------------------------------- | ------------------------------------------------------- |
| 400    | `{"error":"validation_failed","details":[...]}` | Missing required field, invalid category, malformed URL |
| 401    | `{"error":"unauthorized"}`                      | Missing / invalid / expired token                       |
| 404    | (empty)                                         | Item doesn't exist or not owned by caller               |

#### Notes

- The blank-preserve rule is the reason `PUT` and `PATCH` semantics are
  merged here. There is no separate `PATCH` endpoint. `PUT` with an empty
  body for `secret` preserves the previous value.
- The request `UpdatedAt` timestamp is set server-side. Any client-supplied
  value is ignored.

---

### DELETE /api/v1/vault/{id}

Permanently deletes a vault item.

#### Request

```text
DELETE /api/v1/vault/{id}
Authorization: Bearer <token>
```

#### Success response

```text
HTTP/1.1 204 No Content
```

No response body.

#### Error responses

| Status | Body                       | Cause                                     |
| ------ | -------------------------- | ----------------------------------------- |
| 401    | `{"error":"unauthorized"}` | Missing / invalid / expired token         |
| 404    | (empty)                    | Item doesn't exist or not owned by caller |

#### Notes

- Deletion is **permanent**. There is no soft delete, no undo, no recycle
  bin in this version.
- The response is `204` even if the item was already deleted by a previous
  call and is not present. To avoid IDOR, `404` is only returned for items
  that were never visible to the caller.

---

## Error responses

All error responses follow the same JSON shape:

```json
{ "error": "<machine_readable_code>" }
```

Errors with additional context include a `details` array:

```json
{
  "error": "validation_failed",
  "details": [
    "Title is required.",
    "Category must be one of: Password, Note, API Key, Credit Card, Other."
  ]
}
```

### Status codes used by this API

| Code                      | Meaning                                   | Typical cause                                                            |
| ------------------------- | ----------------------------------------- | ------------------------------------------------------------------------ |
| 200 OK                    | Success with body                         | GET, PUT                                                                 |
| 201 Created               | Resource created                          | POST                                                                     |
| 204 No Content            | Success, no body                          | DELETE                                                                   |
| 400 Bad Request           | Validation failure or rejected parameter  | Missing field, bad category, `?decrypt=true`                             |
| 401 Unauthorized          | Missing / invalid / expired token         | No `Authorization` header, malformed JWT                                 |
| 404 Not Found             | Resource not found or not owned by caller | ID doesn't exist, or belongs to another user                             |
| 423 Locked                | Account lockout                           | 5 failed login attempts within the lockout window                        |
| 500 Internal Server Error | Unhandled exception server-side           | Always returns `{"error":"internal_server_error"}` — never a stack trace |

### Error codes

| Code                       | HTTP | Meaning                                 |
| -------------------------- | ---- | --------------------------------------- |
| `invalid_request`          | 400  | Malformed JSON or missing field         |
| `validation_failed`        | 400  | Body deserialized but failed validation |
| `decryption_not_supported` | 400  | `?decrypt=true` was supplied            |
| `unauthorized`             | 401  | Auth failed                             |
| `invalid_credentials`      | 401  | Wrong email or password                 |
| `account_locked`           | 423  | Account is locked out                   |
| `internal_server_error`    | 500  | Unhandled server error                  |

---

## The `?decrypt=true` parameter

CipherVault explicitly rejects `?decrypt=true` on all endpoints that return
vault item data.

```text
GET /api/v1/vault?decrypt=true
GET /api/v1/vault/42?decrypt=true
```

Both return:

```text
HTTP/1.1 400 Bad Request
```

```json
{ "error": "decryption_not_supported" }
```

**Why this parameter exists**

It exists so the API can return a clear, specific error rather than
silently ignoring the parameter. A client that sends `?decrypt=true`
expecting plaintext and gets masked data instead would otherwise be
confused.

**Why decryption is not supported via the API**

See [Why secrets are never returned](#why-secrets-are-never-returned).

---

## Why secrets are never returned

The API does **not** return plaintext secrets or notes, by design. This is
deliberate and applies even to authenticated, ownership-checked requests.

Reasons:

1. **Smaller attack surface.** A JSON API is easy to scrape, log, or cache
   accidentally. Every byte the API returns is one more chance for a secret
   to leak into a proxy log, a browser cache, a monitoring tool, or a
   developer's debug console.

2. **Reduces blast radius of token theft.** If an attacker steals a JWT
   (via XSS on a different app on the same machine, a compromised CI
   system, or a leaked Postman export), they get item **metadata** — titles,
   categories, usernames — but not the secrets themselves.

3. **Encourages proper separation.** Any workflow that needs plaintext
   should go through an authenticated browser session, where the secret is
   displayed briefly and cleared. Automated workflows (CI, scripts,
   integrations) should not be pulling passwords out of a vault via HTTP
   anyway — that pattern is a common source of credential sprawl.

4. **Aligns with password-manager expectations.** Commercial password
   managers (1Password, Bitwarden) all gate plaintext behind an interactive
   session with additional verification. CipherVault's API follows the same
   principle.

If your use case requires programmatic access to plaintext secrets, this
API is not the right tool. Consider a dedicated secrets manager (HashiCorp
Vault, AWS Secrets Manager, Azure Key Vault).

---

## Postman collection

A ready-to-import Postman collection is included at:

```text
postman/CipherVault.postman_collection.json
```

### Importing

1. Open Postman → **File → Import**
2. Select `postman/CipherVault.postman_collection.json`
3. In the collection, open the **Variables** tab
4. Set `baseUrl` to your deployment URL (e.g. `https://localhost:7123`)

### Using

The collection has the following requests in order:

| #   | Request         | Purpose                                                     |
| --- | --------------- | ----------------------------------------------------------- |
| 1   | Login           | Obtains a JWT; stores it in the `token` collection variable |
| 2   | List            | `GET /api/v1/vault`                                         |
| 3   | Create          | `POST /api/v1/vault` with a sample body                     |
| 4   | Get by id       | `GET /api/v1/vault/{{lastItemId}}`                          |
| 5   | Update          | `PUT /api/v1/vault/{{lastItemId}}`                          |
| 6   | Delete          | `DELETE /api/v1/vault/{{lastItemId}}`                       |
| 7   | Unauthorized    | Confirms 401 JSON, not redirect                             |
| 8   | `?decrypt=true` | Confirms 400 rejection                                      |

Run them in order. The **Login** request's test script sets the `token`
variable; the **Create** request's test script sets `lastItemId`.

---

## Rate limiting

**Not implemented.** There is no per-IP, per-token, or per-endpoint rate
limiting in this version.

The only throttling mechanism is the account lockout policy on
`POST /api/auth/login` (5 failed attempts → 15-minute lockout).

See [SECURITY.md §16](SECURITY.md#16-known-limitations-and-non-goals) for
the full list of operational non-goals.

---

## Security notes

### Transport

The API requires HTTPS. HTTP requests are redirected to HTTPS. Do not
disable HTTPS redirection in production.

### Token handling (client-side)

- Store tokens in memory, not in `localStorage` or cookies.
- Do not log tokens. Do not include them in error reports.
- Do not share tokens across users or devices.

### Token scope

A JWT grants access to all endpoints for the issuing user. There are no
scope restrictions in this version. A compromised token is equivalent to a
compromised session.

### Token expiration

Tokens expire after 60 minutes by default. There is no revocation list —
an issued token is valid until it expires. If you suspect compromise, rotate
`Jwt:Key` on the server, which invalidates all outstanding tokens
immediately.

### Reporting vulnerabilities

See [SECURITY.md §18](SECURITY.md#18-security-disclaimer) and the reporting
section at the bottom of that document.

---

## Related documents

- [SECURITY.md](SECURITY.md) — encryption design and threat model
- [DEPLOYMENT.md](DEPLOYMENT.md) — deployment guide
- [USER_MANUAL.md](USER_MANUAL.md) — end-user guide
- [README.md](../README.md) — project overview
