# Known Limitations — Nexus API

Feature-freeze document. Each item is either **intentional** (a deliberate design
choice), **deferred** (would need new product behavior or an architecture change,
so it is out of scope for stabilization), or **accepted risk** (a decision taken
for the demo). Every item states the reason and the recommended future fix.

Verified against the code on 2026-11-01. Line references are approximate and
drift; trust the symbol names.

---

## Security / authentication

### JWT bearer token stored in `localStorage` (not HttpOnly cookie) — deferred
Both portals keep the session (`nexus_auth_session`, which holds the JWT) in
`localStorage`. Any script running in the page origin can read it, so a successful
XSS yields a usable token until it expires.
- **Why not fixed now:** moving to an HttpOnly, `Secure`, `SameSite` cookie is a
  token-transport redesign that touches the API CORS/CSRF setup and both portals'
  auth flows atomically. Partial deployment breaks sign-in. Out of scope under
  freeze.
- **Recommended fix:** HttpOnly cookie + anti-CSRF token; keep the `Authorization`
  header path for SignalR. Requires coordinated deploy of API + both portals.
- **Mitigations in place:** CSP and the fact that content is rendered as text (no
  `dangerouslySetInnerHTML`) reduce XSS surface; Cloudinary-only media URLs block
  injected external content. ⚠️ Token expiry does **not** meaningfully limit the theft
  window — see the lifetime note below (tokens last ~30 days, not 30 minutes).

### No refresh token; long-lived JWT (≈30 days) — deferred + needs a decision
`JwtSettings:Duration` is `30`, but `JwtTokenGenerator.cs` sets expiry with
`DateTime.Now.AddDays(Duration)` — so tokens live **~30 days**, not the "30 minutes"
stated in older docs and the audit. This is a likely **unit bug** (`AddMinutes` was
probably intended); a 30-day bearer token in `localStorage` materially widens the XSS
theft window.
- **Why not fixed now:** changing the unit is a user-visible behavior change (sessions
  drop from ~30 days to ~30 minutes, and there is no refresh token to renew them) — it
  needs a product decision, not a silent freeze-time edit.
- **Recommended fix:** decide the intended lifetime; if short-lived, use
  `AddMinutes(Duration)` (or `AddSeconds`) and add a refresh-token flow so users are not
  logged out constantly. Also switch `DateTime.Now` → `DateTime.UtcNow` for expiry.
- **Demo impact:** with the current ~30-day lifetime, sessions do **not** expire
  mid-demo; the "re-login before presenting" note in `DEMO_GUIDE.md` is precautionary,
  not required.

### SignalR passes the JWT via `?access_token=` query string — accepted risk
The chat/notification hubs authenticate with a query-string token (the standard
SignalR WebSocket pattern). Query strings can land in server/proxy logs and
browser history.
- **Why not fixed now:** header-based auth is unavailable on the WebSocket
  transport; the alternative is a message-level token handshake (behavior change).
- **Recommended fix:** `accessTokenFactory` over an SSE/long-polling fallback, or a
  post-connect auth message.

### Email-confirmation and password-reset tokens travel in the URL — accepted risk
Identity's confirmation/reset links carry the token as a query parameter, so it is
visible in history/logs.
- **Why not fixed now:** changing to a POST/fragment flow alters the email link
  contract (behavior users see).
- **Mitigations:** tokens are single-use and expire; HTTPS is enforced outside
  Development.

### Swagger UI is publicly reachable in production — intentional (demo decision)
`/swagger` and `/swagger/v1/swagger.json` are enabled unconditionally. The team
chose to keep it public for the graduation demo so reviewers can explore the API.
- **Risk:** the full endpoint surface is discoverable, aiding targeted probing.
  Auth endpoints are rate-limited; data endpoints still require a valid token.
- **Recommended fix (post-demo):** gate `UseSwagger`/`UseSwaggerUI` behind
  `app.Environment.IsDevelopment()`, or protect `/swagger` with the same auth.

### CORS allowlist — hardened, verify before adding origins
`ConfigureCors` compiles in only the two fixed production portal origins plus
localhost dev origins, and accepts `CORS_ALLOWED_ORIGINS` / `Cors:AllowedOrigins`
at deploy time. Stale/unverified third-party origins were removed during
stabilization.
- **Note:** Vercel *preview* URLs are intentionally not listed, so a preview build
  cannot call the API. Add a preview origin via the env var only
  temporarily.

---

## Data access / semantics

### `RepositoryBase.Delete` hard-deletes; accounts soft-delete — intentional split
- **Accounts** (`BaseUser`) soft-delete: `UserService.DeleteUser` sets
  `IsDeleted = true`, and a global query filter (`!IsDeleted`) hides them. The dead
  hard-delete `UserRepository.DeleteUser` was removed during stabilization so there
  is no accidental hard-delete path for users.
- **Child rows without an `IsDeleted` column** (comments, replies, certificates,
  qualifications, availability periods, time-off, post likes, post media assets)
  hard-delete — there is no soft-delete column to set.
- **Chat messages** soft-delete via their own path (`ChatMessage.IsDeleted`).
- **Recommended fix (if audit needs it):** add `IsDeleted`/`DeletedAt` + filters to
  the child entities that need retention, rather than a blanket change.

### Doctor report `TotalAppointments` counts only completed appointments — intentional
`GetDoctorStats` returns `totalAppointments` and `totalProfit` both scoped to
`Status == Completed`. The report also shows a full `StatusCounts` breakdown, so
`TotalAppointments` is "completed sessions," not "all bookings."
- **Decision:** confirmed as intended (profit and count both completed-only).
- **Note for presenters:** the "total appointments" figure is completed sessions.

### Appointment denormalizes party name/email/photo at creation — accepted risk
`Appointment` copies `DoctorName`, `ClientEmail`, etc. at booking time; they are not
re-synced if a profile later changes. This is deliberate (a historical snapshot) but
means old appointments keep old PII.
- **Recommended fix (compliance):** a right-to-erasure pass that nulls PII on
  historical rows, if GDPR/CCPA is ever in scope.

---

## Reliability / dependencies

### ML moderation is fail-closed — intentional (demo decision)
If the `HateSpeechDetectorClient` errors or times out, post/comment/chat-text
creation is **blocked** rather than allowed. For a mental-health community,
blocking on an unavailable safety check is the safer default.
- **Risk:** the hosted model cold-starts (~32 s on Render free tier), so the first
  post after idle can fail until warmed.
- **Recommended fix:** a circuit-breaker + warm-up, or an async "pending review"
  queue, if availability must exceed the safety guarantee.
- **Demo action:** warm the ML service before presenting (see `DEMO_GUIDE.md`).

### Vulnerable NuGet packages — accepted risk (no upgrade under freeze)
Release build reports `NU1903` AutoMapper 12.0.1 (high) and `NU1902` MailKit 4.3.0
(moderate). The team chose not to upgrade during the freeze (major-version bumps
risk behavior changes).
- **Recommended fix (post-demo):** AutoMapper 12→13/14 and latest MailKit, behind
  the existing test suite; re-run `dotnet test`.

### Somee fallback database has schema drift — documented
The primary MonsterASP host (`nexus-api.runasp.net`) is healthy (`/api/posts` 200).
The retired Somee free-tier fallback is behind on migrations (e.g. the `Posts`
table predates `AddPostAudienceAndMedia`). Do not rely on Somee as a live endpoint
without reconciling its schema.
- **Recommended fix:** apply pending migrations to Somee, or retire it.

---

## API design

### No API versioning — deferred
Endpoints are unversioned (`/api/...`). Adding `/api/v1` is a cross-cutting change
that would break both portals' call sites; out of scope under freeze.

### Profile update format differs per portal — deferred
Doctor portal posts `FormData`/PascalCase to `/api/doctors/me`; patient portal posts
JSON/camelCase to `/api/profiles/me`. Standardizing on one contract is a coordinated
client+server change; deferred.

### EF Core model-validation warnings (global query filter + required navigation)
Build warns that `BaseUser`'s global query filter interacts with required
navigations to `Post`/`Reply`/`UserCertificate`/`UserQualification`. Non-fatal; the
soft-delete filters and required relationships coexist as configured.
- **Recommended fix:** matching query filters on the dependent entities, or model
  the navigation as optional, if the warnings are ever promoted to errors.
