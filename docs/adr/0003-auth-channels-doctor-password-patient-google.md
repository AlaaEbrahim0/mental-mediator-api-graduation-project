# ADR 0003 — Password-only sign-in for both portals (Google sign-in removed)

**Status:** superseded · **Date recorded:** 2026-11-01 · **Superseded:** 2026-11-01 — external login removed from API and patient-ui

> **Superseded.** This decision no longer describes the running system.
> Google sign-in was removed end to end on 2026-11-01. The API dropped
> `/api/auth/external-login`, `/external-login-callback` and `/exchange-token`,
> along with `AddGoogle`, `ExternalLoginProfile` and the `ExternalAuthentication.*`
> error codes. The `patient-ui` portal dropped `ExternalLoginGate`,
> `externalSession.js` and the Google button.
> Both portals are password-only. The reasoning below is kept as the
> historical record of why the flow existed and what it cost.

**Code:** `Infrastructure/Services/AuthService.cs`, `Infrastructure/Utilities/AuthHandoff.cs`,
`Domain/Errors/ExternalAuthenticationErrors.cs`, `API/Controllers/AuthController.cs`,
`API/Configurations/DependencyInjection.cs` (`ConfigureAuthentication`, `ConfigureRateLimiting`)

**Context**

Two portals, one identity store (`IdentityDbContext<BaseUser>` with TPT `User` / `Doctor` /
`Admin`), and two different trust profiles:

- A **patient** account is self-service: anyone may create one, and the fastest honest way to
  prove control of an email address is Google.
- A **doctor** or **admin** account is staff. It only means something once a clinic profile,
  certificates, qualifications, specialisation, fees, weekly availability periods and
  scheduling settings exist. Nothing an external identity provider returns can supply that, and
  nobody outside the platform should be able to self-promote into it.

The Google path is also structurally constrained:

- The patient shell mounts the notifications provider (and a SignalR connection) for
  **whatever token it holds**, and `NotificationController` has no role filter. Minting a
  clinician token on that path would open a patient UI over clinician data.
- The handoff is a browser navigation chain, so failures must be communicated by a code, not by
  a body: `GET /api/auth/external-login` → Google → the OAuth handler's own
  `GET /signin-google` → `GET /api/auth/external-login-callback` →
  `GET /api/auth/exchange-token`. The one-time `temporaryToken` is kept in `IMemoryCache`
  (process-local), which is why the deployment is single-instance.
- Only the fixed production origins are in the CORS allowlist, so a Vercel **preview**
  deployment cannot complete the exchange at all.

**Decision**

- **Google sign-in is wired end to end for the patient portal only.** `doctor-ui` is
  email/password only and stays that way; the handoff gate (`ExternalLoginGate`,
  `externalSession.js`) must not be ported there.
- **External sign-up can provision exactly one role.** `AuthService` holds
  `private const string PatientRole = "User";` and refuses anything else before a token is
  minted, returning `ExternalAuthentication.RoleUnsupported` — mapped across the redirect to
  `?authError=role_unsupported` — with the message "This account is registered as a clinician or
  administrator and must sign in with a password." Staff accounts are created through
  registration/admin paths and seed scripts, never through an external provider.
- **Failure copy is a short allow-listed code.** `AuthHandoff` maps internal error codes to
  `role_unsupported`, `account_incomplete`, `identity_taken`, `expired`, `external_failed` and
  redirects to the allow-listed portal base URL (`ClientUI` / `DoctorUI` settings).
  Descriptions never carry an email address or identity detail, because a query string ends up in
  browser history.
- **Degraded states are answered, not thrown.** A database that has never run
  `scripts/seed.sql` has no `User` role, and Identity *throws* for an unknown role name, so
  sign-up checks for the role first, deletes the half-created account and answers
  `?authError=account_incomplete`. Because `UserName` is derived from the email local part and
  `NormalizedUserName` is unique, a colliding local part answers `?authError=identity_taken`
  instead of silently attaching Google to somebody else's account.
- **Google is optional configuration.** `AddGoogle` is registered only when both
  `GoogleAuthentication:Id` and `:Secret` are present; `OnRemoteFailure` logs a warning and
  redirects with the generic code rather than surfacing a raw 500 in a top-level navigation.
- **Operator-side requirements are part of the decision**, not implementation details: the
  authorized redirect URI must be `https://<api-host>/signin-google` (the handler path, never a
  controller route), and the OAuth consent screen must stay **Published**.

**Consequences**

*Positive*

- No privilege-escalation surface through an external provider: a Google account can only ever
  reach the patient role, so staff identity remains an administrative act.
- Patients get one-tap sign-in with a provider-verified email; the password path with email
  confirmation stays available for everyone.
- Local and CI runs work without Google credentials — the authentication scheme simply is not
  registered.
- Errors are readable in the browser and safe to log.

*Negative / accepted costs*

- **Asymmetric sign-in UX**: a clinician who tries "Sign in with Google" on the patient portal is
  refused. That is the intended behaviour and is worded, not hidden.
- Two handoff routes need their own rate-limit policy (`oauth-handoff`, 60/min per IP) because
  the credential budget (`auth`, 10/min) would 429 a legitimate three-navigation sign-in mid
  redirect. The handler path `/signin-google` runs before `UseRateLimiter` and is not limited.
- The process-local handoff token pins the deployment to a single instance, and a release (which
  drops `IMemoryCache` via `AppOffline`) kills an in-flight handoff — recoverable by pressing the
  button again, reported as `?authError=expired`.
- Preview deployments cannot use Google sign-in at all (CORS), so this flow can only be verified
  against a production alias or by temporarily adding an origin.
- The account model keeps a real wart: email local part as `UserName` means collisions are
  possible and the external identity is not the key. Making it so is a data migration, not a
  freeze-window change.

*Revisit when*

Staff need external sign-in: that requires an explicit link-to-existing-account flow (verified
email matched to an existing `Doctor` row, plus MFA) rather than a self-service role assignment,
and a distributed cache before scaling out.
