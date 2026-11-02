# Runbook — Nexus API

Operational procedures for the graduation demo and for anyone hosting the API: check it is
alive, read its logs, warm the moderation model, recover a session, reset demo data, and the
failure modes we have actually hit with the fix for each.

Verified 2026-11-01. Deployment mechanics are in [`DEPLOYMENT.md`](DEPLOYMENT.md); deliberate
gaps are in [`KNOWN_LIMITATIONS.md`](KNOWN_LIMITATIONS.md); behaviour and invariants are in
[`ARCHITECTURE.md`](ARCHITECTURE.md).

Production API: `https://nexus-api.runasp.net` (site `site101103`, database `db75421`).

---

## 1. Check health

### Live site

```bash
curl -sS https://nexus-api.runasp.net/health
```

Expected 200 (observed 2026-11-01):

```json
{ "status": "healthy", "service": "mental-mediator-api", "time": "2026-11-01T15:33:10.8305473Z" }
```

`/health` needs no token, opens no database connection and calls no external service, so a 200
means the application process is running. The payload is intentionally just the service name
and time — do not add dependency checks to it, that turns a real outage into a false one.

Then confirm the pipeline that matters for the demo:

```bash
curl -sS -o /dev/null -w "%{http_code}\n" https://nexus-api.runasp.net/swagger/v1/swagger.json
curl -sS -o /dev/null -w "%{http_code}\n" https://nexus-api.runasp.net/api/articles
curl -sS -o /dev/null -w "%{http_code}\n" https://nexus-api.runasp.net/api/doctors
```

Expected `200`, `200`, `401`. A `401` on an `[Authorize]` endpoint is proof that
authentication middleware ran — it is a good sign, not a failure.

### Which layer failed (when it is not 200)

Read the `Server` header; it tells you whether the application was ever involved:

```bash
curl -sSI https://nexus-api.runasp.net/health | grep -i '^server:'
```

| `Server` | Layer | Next step |
|---|---|---|
| `Microsoft-HTTPAPI/2.0` | `http.sys` — the app pool never started | §5 (hosting); application logs will be **empty**, and *every* path 503s |
| `Microsoft-IIS/10.0` | IIS worker process | With in-process hosting this is what a **healthy** production site shows (observed 2026-11-01), so pair it with the `/health` body: IIS header + the app's JSON = the app is running; IIS header + a 500.3x page = check `web.config`, the pool bitness and the hosting bundle |
| `Kestrel` / none | the application, out of process | The app produced the response — read its own log |

Cross-check with a static file that ships with the app (`API/wwwroot/confirm-email.html`):
if `http.sys` is answering, everything 503s including static content.

```bash
curl -sS -o /dev/null -w "%{http_code}\n" https://nexus-api.runasp.net/confirm-email.html
```

### Locally

```bash
dotnet run --project API --launch-profile https   # https://localhost:7235 + http://localhost:5184
curl -sS http://localhost:5184/health
```

`launchSettings.json` defines `http` (`http://localhost:5184`) and `https`
(`https://localhost:7235;http://localhost:5184`), both with `ASPNETCORE_ENVIRONMENT=Development`
and `launchUrl: swagger`. Use the `https` profile when checking anything that depends on secure cookies — notably the authenticated flows and the SignalR hubs — because cookies are issued `secure; samesite=none`, so over plain HTTP the browser silently drops them.

The Docker stack (API + SQL Server + one portal) is `./start-stack.sh up | down | logs`, with
the API on `http://localhost:5184`, SQL on `localhost:1433`, and the web container on `3000`.
Note that `docker-compose.yml` builds from `../` with hard-coded sibling repo directory names,
so it only works in a checkout laid out that way.

---

## 2. Read logs

| Environment | Where | Notes |
|---|---|---|
| Local (`dotnet run`) | console | Development also turns on `EnableDetailedErrors`, `EnableSensitiveDataLogging` and EF `LogTo(Console.WriteLine)` |
| Docker stack | `./start-stack.sh logs` (`docker compose logs -f`) | service names `nexus-api-dev`, `nexus-db-dev`, `nexus-web-dev` |
| MonsterASP production | Control Panel → **Application & Access logs** | ASP.NET Core startup output lands here. If the app pool never started, these logs are **empty** — that absence is itself the diagnosis (see the `Server` header above) |

Log levels shipped in production settings: `Default=Information`, `Microsoft=Warning`,
`Microsoft.Hosting.Lifetime=Information`. Request logging stays at `Warning` so that a
SignalR `?access_token=` query string is not written to logs.

Things worth grepping for in a startup incident:

- `Database is not reachable; skipping migrations` — `DbInitializer` warning; the app runs but
  every data endpoint will fail. Check `ConnectionStrings:constr_somee`.
- `Applying N pending migration(s): …` — startup migration ran; expected after a schema change.
- `ML request to /predict_HS did not answer within 45s` — cold or down model, not a code fault.
- `{Setting} did not provide a safe portal origin; handoff falls back to request origin` —
  `ClientUI` / `DoctorUI` missing or not an absolute http(s) URL, so the confirmation
  link points at the API host. Set the setting rather than relying on the fallback.

There is no file or centralized log sink: stdout plus the host's own log viewer.

---

## 3. Warm the ML moderation service

Moderation is **fail-closed**: no decision from the model means the post/comment/reply/chat
message is **not** saved and the client gets a 503
(`ExternalServices.HateSpeechDetectionServiceUnavailable`), and chat gets
`Chat.ModerationUnavailable`. The model host sleeps when idle; a cold request takes ~32 s
(warm ≈0.5 s), which the 45 s budget absorbs. Nothing polls it to keep it awake — by design.

**Do this immediately before a demo.** The service exposes no health or readiness route
(`/health`, `/ping`, `/` all 404 or error), so warming means issuing a real prediction. The
hate-speech endpoint is `POST /predict_HS` with a JSON body `{ "text": "<content>" }`:

```bash
curl -sS -w "\ncold-call seconds: %{time_total}\n" \
  -X POST https://sentiment-nja5.onrender.com/predict_HS \
  -H 'Content-Type: application/json' \
  -d '{"text":"hello"}'
```

Expected body: `{"prediction":"Normal"}` (the API compares it case-insensitively; anything that is
not `normal` is a rejection, a blank value is treated as "no decision"). Re-run until
`time_total` drops well under a second. Measured on 2026-11-01: the first call after an idle
period took **41.6 s** and still returned 200 — inside the 45 s budget, which is why the request
succeeds slowly instead of failing. Depression screening uses the same host on `POST /predict_DP`.

The address is configuration (`MLServerAddress`), currently
`https://sentiment-nja5.onrender.com/`; a blank value makes the API refuse to start
(`MLServerAddress must be configured for content moderation.`).

Symptoms of an un-warmed or down model: a composer submit that takes ~45 s and then says the
safety check could not be finished, with the draft preserved. That is correct behaviour —
retry once the model answers, and never "fix" it by allowing unmoderated content.

---

## 4. Sessions and expired JWTs

Both portals persist the session in `localStorage` under `nexus_auth_session` as
`{ token, userId, roles, expiresOn }`. There is **no refresh token**: when the token expires,
requests 401 and the user must sign in again.

**Check whether the token is really expired** (browser DevTools console):

```js
const s = JSON.parse(localStorage.getItem("nexus_auth_session"));
const p = JSON.parse(atob(s.token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
console.log({ iss: p.iss, aud: p.aud, exp: new Date(p.exp * 1000).toISOString(), roles: p.roles });
```

**Re-login:** sign out, or clear the session and reload:

```js
localStorage.removeItem("nexus_auth_session"); location.reload();
```

Then sign in again with the account's own credentials (do not share one demo login across
roles — the audit trail and message authorship depend on the identity being honest).
Demo logins and the shared demo password are in the header of
[`../scripts/seed.sql`](../scripts/seed.sql); `Password1!` for every seeded account, only
`admin@example.com` holds the Admin role.

**If a previously working token suddenly 401s for everyone:** the deployed
`JwtSettings:Audience` or `:Key` changed. Issuance and validation read the same settings, so a
change signs every session out at once. Treat it as a coordinated sign-out event, never as a
typo fix — [`adr/0002`](adr/0002-jwt-audience-per-environment.md).

**Verify the audience value itself** (`401` with `IDX10237`/audience-mismatch style failures in
the host log) by comparing the `aud` claim from the snippet above with
`appsettings.Production.json` on the host. Production issues tokens for
`https://mental-mediator-doctor-ui-graduation-project.vercel.app`; the committed
`appsettings.json` carries the patient-portal origin for local runs.

Note before a demo: `JwtSettings:Duration` is configured as `30` and documented as minutes,
but `Infrastructure/Utilities/JwtTokenGenerator.cs` applies it with
`DateTime.Now.AddDays(...)`, so the issued `exp` may be ~30 days out. Read the `exp` claim with
the snippet above rather than assuming the lifetime; confirm the intended behaviour before
changing it (see [`KNOWN_LIMITATIONS.md`](KNOWN_LIMITATIONS.md) "No refresh token").

---

## 5. Known failure modes and fixes

| Symptom | Cause | Fix |
|---|---|---|
| 503 on every path, `Server: Microsoft-HTTPAPI/2.0`, application logs empty | App pool never started | Hosting Bundle for .NET 8 present? Pool started? Recycle from the Control Panel. Then re-run the deploy job |
| `500.32 — ANCM Failed to Load DLL` | 64-bit pool cannot load the `win-x86` publish | Set the app pool back to 32-bit (Control Panel → Website → detailed settings) |
| `500.30 — app failed to start`, no app logs | Startup threw before middleware | Check `web.config` (`processPath="dotnet"`, `hostingModel="inprocess"`, `ASPNETCORE_ENVIRONMENT=Production`), then `appsettings.Production.json`. Historically: `.env` probing on shared IIS (fixed — Development-only now) |
| Deploy fails with `ERROR_FILE_IN_USE` | In-process IIS holds `API.dll` open | The pipeline's `-enableRule:AppOffline` handles it and retries up to 3× on lock errors; a manual WebDeploy must use the rule too (staging `app_offline.htm` via `filePath` is not authorized on this host) |
| Deploy fails with `ERROR_USER_NOT_AUTHORIZED_FOR_DEPLOYMENTPROVIDER` | Attempted a provider the shared host does not allow (e.g. hand-staged `app_offline.htm`, AppPool/Certificate/Content links) | Keep `-disableLink:AppPoolExtension -disableLink:ContentExtension -disableLink:CertificateExtension` and `-enableRule:AppOffline` in the sync command |
| msdeploy: HTTP 401 / login refused from `msdeploy.axd` | Missing or stale `MONSTERASP_MSDEPLOY_PASSWORD`, or WebDeploy disabled | Rotate the secret from the Control Panel **Deploy → Visual Studio access** card and make sure WebDeploy is Enabled |
| Pipeline fails with "`appsettings.Production.json` field X is empty" | A secret is unset or was read into the wrong env var | Set the named secret (§3 of DEPLOYMENT.md), re-run `workflow_dispatch` |
| Pipeline fails: "Publish output is N MB, unexpectedly large" | Something got vendored into the publish folder | Check for added large assets before retrying; the gate exists to fail loudly |
| Every `[Authorize]` endpoint returns 403 for a valid token | Inbound claim mapping rewrote `roles` | `MapInboundClaims = false` and `RoleClaimType = "roles"` are required in `ConfigureAuthentication`; do not remove them |
| All users signed out at once | `JwtSettings:Audience` or `:Key` changed on a deployed environment | Restore the previous value and redeploy, or accept the sign-out and re-issue tokens; see `adr/0002` |
| Image upload returns 503 `ExternalServices.StorageServiceUnavailable` | `CLOUDINARY_URL` missing from `web.config` `<environmentVariables>` | Add the `PRODUCTION_CLOUDINARY_URL` secret and redeploy; note the interface has no delete operation, so replaced assets stay in Cloudinary |
| A post/comment/chat message fails with "could not finish the safety check" | ML model cold/down/unreachable — fail-closed | §3 warm-up; if it persists, the model host is down. Never switch to allow-on-write |
| `/api/posts` 500 on a fallback host | Schema drift (that host's `Posts` table predates `AddPostAudienceAndMedia`) | Apply pending migrations there or stop using it; the primary host is healthy |
| Chat room join: same error for a wrong and a throttled passphrase | `RoomJoinThrottle`: 5 wrong attempts per account+room blocks guessing for 15 minutes | Intended. Wait, or use a different account |
| `429 Too Many Requests` with `Retry-After` | Fixed-window rate limit (`auth` 10/min/IP, `chat-messages` 30/min, `chat-room-join` 10/5 min, `chat-reports` 10/h, `chat-room-creation` 5/h staff · 2/h others) | Wait for the window. Repeated 429 during a demo usually means two portals are hammering one IP |
| Booked slot accepted twice / double booking | Booking without the per-doctor lock | All booking and schedule-mutation paths must hold `AcquireBookingLockAsync(doctorId)` — see `ARCHITECTURE.md` §4.5 |
| Appointment report "total" looks low | `GetDoctorStats` counts **completed** appointments only | Intended; the report also exposes the full status breakdown (`KNOWN_LIMITATIONS.md`) |
| SignalR cannot connect | Wrong hub path or token | Hubs are `/chat-hub` and `/notification-hub` (no `/hubs` prefix), token via `?access_token=`; `API_BASE_URL` must have no trailing slash or the concatenation yields `//…` and a 404 |
| Emails never arrive but no error | SMTP accepted the message ≠ delivered; Gmail rejects non-app passwords | Check `MailSettings` values (the service logs the offending setting name server-side, never the value) and confirm an app password is in use |

---

## 6. Reset and reseed demo data

Scripts live in [`../scripts/`](../scripts/). They are **never** run by CI or by the
application; the app only applies pending migrations at startup
(`Infrastructure/Data/DbInitializer.cs`) and seeds nothing.

Run them from the MonsterASP Control Panel (WebMSSQL) or from SQL Server Management Studio
against a demo database. Allowed database names for the reset guard:
`db75421`, `MentalMediator`, `nexus_demo`.

**Rules before touching a hosted database:** take a backup and verify it, dry-run the script,
and get explicit confirmation from the owner. `scripts/reset-demo-data.sql` deletes
**application data for the accounts currently on the hosted database**, including the
maintainer's own, and is recoverable only from a backup.

```
1. Backup confirmed            →  2. scripts/reset-demo-data.sql  →  3. scripts/seed.sql  →  4. /health + log in with a demo account
```

1. **`scripts/reset-demo-data.sql`** — deletes all application data rows (accounts,
   appointments, posts/comments/replies/likes/media, chat rooms/members/messages/reports,
   notifications, scheduling rows, workspace notes and follow-ups, Identity link tables).
   Keeps the schema, `[AspNetRoles]` (so the `User`/`Doctor`/`Admin`/`Anonymous` roles
   survive) and `[__EFMigrationsHistory]` (so the startup migration check stays intact).
   Three guards, all of which must hold or it aborts before changing anything:
   `@Confirm` must be set to exactly `RESET_NEXUS_DEMO_DATA`; `DB_NAME()` must be in the
   allowed demo list; everything runs in one transaction with `SET XACT_ABORT ON`.
   Its final `SELECT` prints row counts for verification (`RolesKept` and
   `MigrationsKept` must be non-zero).
2. **`scripts/seed.sql`** — the curated fictional dataset: five doctors with distinct
   specialties, fees, consultation lengths and weekly grids (one closed to new patients),
   five patients covering every `AppointmentStatus`, certificates and qualifications,
   community posts with comments/replies/likes, four chat rooms (public, private, archived)
   with members, messages and one open report, notifications in read and unread states, and
   Cloudinary-hosted abstract avatars. Properties that matter:
   - Section 0 **refuses to run** against a database holding any account outside
     `@example.com`, so a live production database cannot be seeded by accident.
   - Idempotent: every insert is `NOT EXISTS`-guarded on a natural key; only rows belonging
     to the demo accounts are ever updated.
   - Fresh: appointments are re-anchored to the weekday the script runs in and every relative
     timestamp is re-derived, so upcoming sessions always sit in the coming week. Slot times
     are placed on each doctor's real grid (period start + n × consultation length), so the
     booking screens agree with the data. All seeded doctors keep `TimeZoneId = 'UTC'`.
   - Demo logins and `Password1!` are listed in the script header — demo databases only, never
     reused for a real deployment.
3. If the schema also needs updating, run migrations explicitly
   (`dotnet ef database update --project Infrastructure --startup-project API`) rather than
   relying on the startup pass.

---

## 7. Local development quick reference

```bash
cp .env.example .env                      # fill in values; .env is gitignored, never committed
dotnet restore
dotnet build -c Release                   # expect 0 errors, 6 warnings (package advisories)
dotnet test  -c Release                   # expect 244 passed
dotnet run --project API --launch-profile https
```

`global.json` pins the SDK to `8.0.426` with `rollForward: latestFeature`; CI uses
`8.0.x`. EF tooling is pinned in `dotnet-tools.json` (`dotnet-ef` 8.0.32):

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project Infrastructure --startup-project API
dotnet ef database update        --project Infrastructure --startup-project API
```

Configuration the app reads: `ConnectionStrings:constr` (Development) / `constr_somee`
(everywhere else), `JwtSettings:{Issuer,Audience,Key,Duration}`,
`MailSettings`, `Resend` (or `RESEND_API_KEY`), `CLOUDINARY_URL` (process environment only),
`MLServerAddress` + `MLServer:*`, `NewsApi`, `BaseAddress`, `ClientUI`, `DoctorUI`,
`RequireHttps`, and the CORS allowlist via `Cors:AllowedOrigins` or `CORS_ALLOWED_ORIGINS`
(comma-separated). Full templates:
[`../API/appsettings.Example.json`](../API/appsettings.Example.json) and
[`../.env.example`](../.env.example).

Local JWT keys are development-only; never reuse the production key locally or vice versa.
