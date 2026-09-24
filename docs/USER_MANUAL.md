# CipherVault — User Manual

> A friendly guide for end users of CipherVault. No technical background
> required.

**⚠️ Important:** CipherVault is an educational/student project, not a
production-grade password manager. Read the
[Before You Start](#before-you-start) section carefully.

---

## Table of Contents

- [Before you start](#before-you-start)
- [Getting started](#getting-started)
  - [Creating an account](#creating-an-account)
  - [Password requirements](#password-requirements)
  - [Logging in](#logging-in)
  - [Logging out](#logging-out)
- [The dashboard](#the-dashboard)
- [Managing vault items](#managing-vault-items)
  - [Creating an item](#creating-an-item)
  - [Categories explained](#categories-explained)
  - [The password generator](#the-password-generator)
  - [The strength meter](#the-strength-meter)
- [Viewing and revealing secrets](#viewing-and-revealing-secrets)
  - [Reveal a secret](#reveal-a-secret)
  - [Reveal notes](#reveal-notes)
  - [Copy to clipboard](#copy-to-clipboard)
  - [Auto-hide](#auto-hide)
- [Editing items](#editing-items)
  - [The blank-preserve rule](#the-blank-preserve-rule)
  - [Changing the secret](#changing-the-secret)
  - [Clearing notes](#clearing-notes)
- [Deleting items](#deleting-items)
- [Finding items](#finding-items)
  - [Searching](#searching)
  - [Filtering by category](#filtering-by-category)
  - [Combining search and filter](#combining-search-and-filter)
  - [Pagination](#pagination)
- [Account and security](#account-and-security)
  - [Password policy](#password-policy)
  - [Account lockout](#account-lockout)
  - [Session expiry](#session-expiry)
- [Frequently asked questions](#frequently-asked-questions)
- [Troubleshooting](#troubleshooting)
- [Privacy and limitations](#privacy-and-limitations)
- [Glossary](#glossary)

---

## Before you start

Please read this before creating an account.

### What CipherVault is

- A **learning project** that demonstrates how a password manager works.
- A safe place to store low-value test credentials and practice using a
  vault.
- Encrypted on the server with the ASP.NET Core Data Protection API.

### What CipherVault is **not**

- **Not** a replacement for a commercial password manager.
- **Not** independently audited.
- **Not** recommended for storing:
  - Banking credentials
  - Government IDs (SSN, Aadhaar, PAN, passport)
  - Cryptocurrency seed phrases or private keys
  - Email account passwords (which are usually password-reset backdoors)
  - Any credential you cannot afford to lose or have exposed

### What the server can see

CipherVault encrypts your Secret and Notes fields on the server, but the
**server holds the encryption keys**. In practice this means:

- Your **Title, Username, URL, Category**, and **timestamps** are stored as
  plaintext in the database.
- Your **Secret** and **Notes** are encrypted at rest, but the server can
  decrypt them whenever you ask it to (that's what happens when you click
  Reveal).
- A malicious server operator could theoretically read your secrets.

If any of this is unacceptable for your use case, use a zero-knowledge
password manager instead (Bitwarden, 1Password, KeePassXC).

### Ready to continue?

If you're storing test data or non-critical credentials as part of learning
or evaluating CipherVault — continue. Otherwise, stop here.

---

## Getting started

### Creating an account

1. Open the CipherVault URL (e.g. `https://localhost:7123` in development).
2. Click **Register** in the top-right corner.
3. Fill in the form:
   - **Email** — used to log in. Must be unique across all accounts.
   - **Password** — see [Password requirements](#password-requirements).
   - **Confirm password** — must match.
4. Click **Register**.
5. You are logged in automatically. You'll land on the **Dashboard**.

> **Note:** Email confirmation is not required in this version (no email
> sender is configured). You can register with any email address.

### Password requirements

Your account password must meet **all** of these:

- At least **8 characters** long
- At least one **uppercase** letter (A–Z)
- At least one **lowercase** letter (a–z)
- At least one **digit** (0–9)
- At least one **non-alphanumeric** character (e.g. `!`, `@`, `#`, `$`, `-`,
  `_`, `.`)

**Examples of acceptable passwords:**

- `MyDogEats2Kibble!`
- `Summer2026@Beach`
- `p@ssw0rd-Is-Weak` (weak but passes the policy — do not use this)

**Tip:** use a **passphrase** — four or five random words plus a number and
a symbol. They're easy to remember and hard to crack. Example:
`correct-horse-battery-staple-7!`.

**Important:** Your account password protects access to your vault, but it is
**not** the encryption key for your data. The server holds the encryption
keys independently.

### Logging in

1. Click **Login** in the top-right corner.
2. Enter your email and password.
3. Optionally tick **Remember me** to extend your session.
4. Click **Log in**.

You'll be redirected to the **Dashboard**.

### Logging out

Click **Logout** in the top-right corner of any page.

**Why log out?**

- On a shared or public computer, always log out.
- The session cookie expires after 2 hours of inactivity (sliding window),
  but logging out is instant and safer.

---

## The dashboard

The Dashboard is the first page you see after logging in. It shows:

- **Total item count** — how many items you have in your vault.
- **Per-category breakdown** — a chart showing how many items are in each
  category (Password, Note, API Key, Credit Card, Other).
- **Recently updated items** — the last five items you created or edited,
  with masked secrets.

The Dashboard is a read-only summary. To interact with items, click **Vault**
in the top navigation.

---

## Managing vault items

### Creating an item

1. From anywhere, click **Vault** in the navigation bar.
2. Click **New Item** in the top-right of the Vault list.
3. Fill in the form:

| Field | Required? | What to put |
|-------|-----------|-------------|
| **Title** | ✅ | A name you'll recognize, e.g. `GitHub`, `Bank account`, `Wifi password` |
| **Category** | ✅ | See [Categories explained](#categories-explained) |
| **Username** | No | The username or email you log in with |
| **Secret** | ✅ | The password, API key, or sensitive value itself |
| **Notes** | No | Any extra info — recovery codes, hints, security questions |
| **URL** | No | The website where you use this credential |

4. Click **Create**.

The item appears in your Vault list with a masked secret
(`••••••••`). Your **Secret** and **Notes** are encrypted before they are
stored.

### Categories explained

Use the category that best fits:

| Category | Use for |
|----------|---------|
| **Password** | Website logins, app passwords, PINs |
| **Note** | Free-form sensitive text — recovery phrases, license keys, private notes |
| **API Key** | Developer tokens, API secrets, OAuth client secrets |
| **Credit Card** | Card numbers, CVVs, expiry dates (keep the sensitive parts in the Secret field) |
| **Other** | Anything that doesn't fit the above |

Categories are used for **filtering** on the Vault page and **counting** on
the Dashboard. Choose the one that makes sense to you.

### The password generator

When creating or editing an item, you can generate a random password instead
of typing one.

1. In the **Secret** field, look for a **Generate** button or a dice icon
   (🎲).
2. Click it. A random password appears in the field.
3. If you want a different length or character set, adjust the settings
   first, then click Generate again.

**Under the hood:** the generator uses your browser's cryptographically
secure random number generator (`crypto.getRandomValues()`), not
`Math.random()`. This means the passwords it produces are suitable for
real-world use.

### The strength meter

As you type or generate a password, a strength meter next to the field
updates live. It rates the password roughly as:

- **Weak** (red) — short, or only one character type
- **Fair** (orange) — decent length, some variety
- **Good** (yellow) — long, mixed character types
- **Strong** (green) — long, mixed types, no obvious patterns

Aim for **Strong** whenever you can. The meter is a guide, not a
cryptographic guarantee.

---

## Viewing and revealing secrets

Your vault items are **masked by default**. The list shows a placeholder
string (`••••••••`) instead of the real secret. You must explicitly
**Reveal** to see the value.

### Reveal a secret

1. From the Vault list, click the item's **title** (or the **Details**
   button).
2. On the Details page, find the **Secret** field.
3. Click **Reveal**. The plaintext value appears.
4. The button toggles to **Hide** — click again to mask the value.

**Who can reveal:** only the item's owner (you). If you try to access
another user's item — by guessing the URL — you'll get a **404 Not Found**
page. CipherVault never tells you that the item exists but isn't yours.

### Reveal notes

If the item has notes (the Details page shows a Notes section), you can
reveal them the same way:

1. Click **Reveal** next to the Notes field.
2. The plaintext notes appear.
3. Click **Hide** to mask them again.

### Copy to clipboard

After revealing a secret or note, a **Copy** button becomes available.

1. Click **Copy**.
2. A toast notification appears: *"Copied — clipboard will clear in 10
   seconds"*.
3. Paste anywhere (Ctrl+V) within 10 seconds — the value appears.
4. After 10 seconds, the clipboard is overwritten with an empty string.

**Important caveats about the clipboard:**

- The 10-second timer only runs while the **browser tab is open**. If you
  switch tabs or apps, the clipboard may not clear until you come back to
  the CipherVault tab.
- Some operating systems maintain their own clipboard history (e.g. Windows
  `Win+V`). That history may capture the value **before** CipherVault clears
  it. There is no way for a web app to prevent this.
- Never paste secrets into untrusted applications, chat windows, or search
  bars.

### Auto-hide

A revealed value automatically **hides itself after 30 seconds**. This is a
safety net in case you walk away from your screen. You can always reveal it
again.

---

## Editing items

### Opening the edit form

1. On the Vault list, click the **pencil** icon next to the item.
2. Or on the Details page, click **Edit**.

### The blank-preserve rule

This is the most important behavior to understand:

> **Leaving the Secret or Notes field blank when editing will keep the
> current encrypted value unchanged.**

You do not have to re-enter your secret every time you update the title,
category, username, or URL. Just fill in the fields you want to change.

### Changing the secret

To **replace** the existing secret:

1. Open the edit form.
2. Type or generate the new value in the **Secret** field.
3. Click **Save**.

The old encrypted value is discarded and replaced with the new one.

> **Note:** the previous secret is not recoverable. If you might need it
> later, back it up outside CipherVault before changing it.

### Clearing notes

To **remove notes** entirely:

1. Open the edit form.
2. Clear the Notes field completely (make it empty — not just whitespace).
3. Click **Save**.

> **Note:** leaving Notes **completely blank** clears them. Typing a space
> into Notes and saving will store that space (which is usually not what you
> want).

### What you cannot edit

- **CreatedAt** — set automatically when the item is first created.
- **Id** — internal identifier, immutable.

**UpdatedAt** updates automatically on every successful save.

---

## Deleting items

1. On the Vault list, click the **trash** icon next to the item.
2. Or on the Details page, click **Delete**.
3. A confirmation page appears showing the item's title and category.
4. Click **Delete** to confirm.

> **⚠️ Deletion is permanent.** There is no recycle bin, no undo, and no way
> to recover a deleted item. If you're unsure, back up the secret elsewhere
> first.

---

## Finding items

### Searching

At the top of the Vault page, there is a **search box**.

- Type any part of an item's **Title**, **Username**, or **URL**.
- Press Enter or click **Filter**.
- The list updates to show matching items.

**Search is case-insensitive.** `github`, `GitHub`, and `GITHUB` all match
the same items.

**Search does not look inside Secrets or Notes.** This is by design — those
fields are encrypted, so the server cannot search them.

### Filtering by category

Next to the search box, there's a **category dropdown**.

1. Choose a category (e.g. **Password**).
2. Click **Filter**.

The list shows only items in that category.

### Combining search and filter

You can use both at the same time:

- Search `bank` + Category `Password` → items whose title/username/URL
  contains "bank" AND are in the Password category.

### Pagination

If you have more than 10 items, the Vault list is paginated.

- The bottom of the list shows page numbers.
- Click a page number to go to that page.
- **Your search and filter are preserved** when you switch pages.
- A summary line shows: *"Showing 11–20 of 47 items"*.

To reset and see everything, click **Clear filters** (or just clear the
search box and set the category back to "All Categories").

---

## Account and security

### Password policy

See [Password requirements](#password-requirements). The policy applies
when you register and any time you change your password.

### Account lockout

After **5 failed login attempts**, your account is locked for **15
minutes**.

This applies to:

- The web login form
- The API login endpoint

During lockout, even the correct password will be rejected. Wait 15 minutes
and try again.

**If you don't remember your password:** CipherVault currently has no
password-reset flow (no email sender is configured). A forgotten password
means a permanently inaccessible account. Contact the administrator if you
have one.

### Session expiry

Your login session lasts **2 hours** of activity. The timer resets on every
request (sliding expiration).

If you leave the tab open but idle for 2 hours, you'll be logged out the
next time you try to do something. Just log in again.

**On a shared computer:** always click **Logout** when you're done. Closing
the tab is not enough — the session cookie remains valid until it expires
or you log out.

---

## Frequently asked questions

**Q: Can I recover my account if I forget my password?**

A: No, not currently. There is no password-reset flow. Ask your
administrator.

**Q: Is my data end-to-end encrypted?**

A: No. Secrets and Notes are encrypted at rest on the server, but the server
holds the keys and can decrypt on demand. See
[Before you start](#before-you-start).

**Q: What happens if I forget a Secret?**

A: Once saved, you can view it any time via **Reveal**. Secrets are not
hidden from you — you can always retrieve them (unlike some password
managers that require re-entering a master password).

**Q: Can I share an item with someone else?**

A: No. Every item belongs to exactly one user. There is no sharing feature.

**Q: Can I export my vault?**

A: No. There is no export in this version. You can manually copy items out
one at a time via Reveal + Copy.

**Q: Can I import from another password manager?**

A: No. There is no import feature.

**Q: What if I accidentally delete an item?**

A: It's gone permanently. There is no recycle bin.

**Q: Can I access CipherVault from my phone?**

A: Yes, the interface is responsive and works on mobile browsers.

**Q: Does CipherVault have a browser extension?**

A: No. You must open the CipherVault website to view or copy secrets.

**Q: Is my data backed up?**

A: Depends on the deployment. The administrator is responsible for database
and key-ring backups. Ask your administrator.

**Q: Why can't I search inside my notes?**

A: Notes are encrypted, so the server can't read them to search. Search only
works on plaintext fields (Title, Username, URL).

**Q: Why does the clipboard clear itself?**

A: To limit how long your secret lives in the operating system's clipboard,
where any other app could read it.

**Q: Why does my revealed secret disappear after 30 seconds?**

A: Auto-hide is a safety net for when you walk away from your screen.

---

## Troubleshooting

### "Unable to reveal value" alert appears

**Cause:** the server rejected the reveal request — likely an expired session.

**Fix:** refresh the page. If you're redirected to login, log in again.

### Copy button is disabled

**Cause:** you haven't revealed the value yet.

**Fix:** click **Reveal** first, then **Copy** becomes active.

### Copy button says "HTTPS required"

**Cause:** you're accessing CipherVault over plain HTTP.

**Fix:** use the HTTPS URL (e.g. `https://localhost:7123` instead of
`http://localhost:5123`). The browser's clipboard API only works over HTTPS.

### Paste produces nothing after copying

**Cause:** the 10-second timer expired, and the clipboard was cleared.

**Fix:** reveal and copy again.

### Paste produces nothing even right after copying

**Cause:** the browser tab lost focus before the clipboard could be written.

**Fix:** click inside the CipherVault tab first, then try copy again.

### "Page Not Found" after clicking an item

**Cause:** the item doesn't exist or belongs to another user.

**Fix:** go back to the Vault list and try again. If the item was deleted in
another tab, it may no longer exist.

### "Your account is locked"

**Cause:** 5 failed login attempts.

**Fix:** wait 15 minutes, then try again. If the lockout persists, ask an
administrator to reset the account's failed-attempt counter.

### "Something went wrong" (500 error)

**Cause:** unexpected server error. The error has been logged.

**Fix:** try again in a moment. If it persists, contact the administrator.

### I'm logged out unexpectedly

**Cause:** session expired (2 hours of inactivity).

**Fix:** log in again. This is normal behavior.

### The password generator doesn't appear

**Cause:** the browser blocked the JavaScript file, or you're viewing a
cached version of the page.

**Fix:** hard-refresh (`Ctrl+F5`). If the issue persists, check the browser
console for errors.

---

## Privacy and limitations

### What CipherVault stores about you

- **Email** — used for login.
- **Password hash** — a salted hash, not your actual password.
- **Vault items** — Title, Username, URL, Category, timestamps (plaintext);
  Secret and Notes (encrypted).
- **Audit log entries** — actions like Create / Update / Delete / Reveal,
  with user ID, item ID, and timestamp. **Not** the values.

### What CipherVault does NOT do

- Track you across other websites.
- Use analytics, tracking pixels, or third-party cookies.
- Send marketing emails.
- Share your data with third parties.

### Limitations to be aware of

- **The server can decrypt your secrets** (this is not a zero-knowledge
  vault).
- **Metadata is not encrypted.** Titles and usernames are visible to anyone
  with database access.
- **No two-factor authentication.** A stolen password is enough to access
  your vault.
- **No automated backups.** Ask your administrator.

---

## Glossary

| Term | Meaning |
|------|---------|
| **Vault item** | A single entry in CipherVault (e.g. one website's login) |
| **Secret** | The sensitive value of an item — password, API key, card number, etc. |
| **Notes** | Free-form text attached to an item |
| **Masked** | Displayed as `••••••••` instead of the real value |
| **Reveal** | Temporarily unmask and display a Secret or Notes |
| **Lockout** | A temporary block on login after too many failed attempts |
| **Session** | Your logged-in state; expires after 2 hours of inactivity |
| **Purpose string** | (Technical) A label used by the encryption system to derive keys — not visible to you |

---

## Related documents

- [README.md](../README.md) — project overview
- [SECURITY.md](SECURITY.md) — encryption design and threat model
- [DEPLOYMENT.md](DEPLOYMENT.md) — deployment guide
- [API.md](API.md) — REST API reference

---

> **Reminder:** CipherVault is an educational project. Do not store
> high-value secrets (banking credentials, recovery phrases, government IDs)
> without an independent security review.