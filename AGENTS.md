# AGENTS.md — Nexus API (`api/`)

Read this before touching anything here. Most of what looks like a convention in this repository
is a constraint, not a preference.

**Status: FEATURE FREEZE (graduation project).** No new features, no opportunistic refactors, no
dependency upgrades. Fix real defects with the smallest change that fixes them. If a change is not
required by the task, do not make it. Deferred items go into
[`docs/KNOWN_LIMITATIONS.md`](docs/KNOWN_LIMITATIONS.md), not into a commit.

**A push to `master` deploys to production.** CI triggers on `push: master` and the repo variable
`AUTO_DEPLOY_MONSTERASP` is `true` (verified 2026-11-01), so build → test → publish → WebDeploy →
`/health` verification all run against the live site. Branch first; land on `master` only when the
change is meant to go live.

---

## What this is

ASP.NET Core 8 Web API for Nexus / Mental Mediator — a mental-health platform with a community
forum, doctor appointments, chat rooms, notifications, and ML-assisted screening. Clean/Onion
architecture, EF Core + SQL Server, SignalR, external ML moderation service.

- Production API: `https://nexus-api.runasp.net` (MonsterASP site `site101103`, IIS in-process)
- Solution: `MentalMediator.sln` (SDK pinned in `global.json`: `8.0.426`, `rollForward: latestFeature`)
- Sibling repos (same workspace): `../patient-ui`, `../doctor-ui` (CRA portals), `../shared-ui`
  (design system, **vendored** into the portals), `../docs` (plan/audit/guide documents),
  `../audit/NEXUS_ENGINEERING_AUDIT.md` (the original findings — several are now fixed)

## Where truth lives

> **This .NET API is the single source of truth for domain rules and authorization.**

- Appointment state legality, room access, post audience/visibility, notification ownership,
  patient-history visibility, moderation, fees and pricing: decided here, in `Domain` and
  `Application`.
- The portals' role checks (`ProtectedRoute`, `src/utils/chat.js`) decide **which screen is shown**
  and nothing else. Never weaken, bypass, or "mirror" a server rule in a client to make a UI work;
  never add a client permission the API does not enforce.
- A `403` means the account genuinely lacks the role. A `401` on an `[Authorize]` route from an
  unauthenticated probe is a **healthy** signal, not a bug.
- If a permission looks wrong, the fix is here — not in a portal.
- Documentation is a claim; the code and the tests are the record. When a doc and the tree
  disagree, the tree wins (and fix the doc in the same branch).

## Commands

```bash
dotnet restore
dotnet build -c Release                 # 0 errors, 6 warnings (NU1903 AutoMapper 12.0.1, NU1902 MailKit 4.3.0)
dotnet test  -c Release                 # 265 passed, 0 failed
dotnet run --project API --launch-profile https   # https://localhost:7235 + http://localhost:5184, /swagger
```

There is **no lint or format step** in this repo: no `.editorconfig`, no analyzers configured, no
`TreatWarningsAsErrors`. `dotnet build -c Release` + `dotnet test -c Release` are exactly what CI
runs — that is the gate. Nullable reference types are `enable` in all six projects. Any **new**
build warning is a regression; the 6 advisory warnings are accepted and must not be "cleaned up"
by upgrading packages under the freeze.

EF tooling (pinned in `dotnet-tools.json`, `dotnet-ef` 8.0.32):

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project Infrastructure --startup-project API
dotnet ef database update        --project Infrastructure --startup-project API
```

Local config: `cp .env.example .env` (gitignored; loaded by `dotenv.net` **only** under
`Development`). Template with every key: `API/appsettings.Example.json`. Docker stack:
`./start-stack.sh up|down|logs` (API `5184`, SQL `1433`, web `3000`) — note `docker-compose.yml`
builds from `../` with hard-coded sibling directory names.

## Repo map

```
API/                Program.cs (pipeline, /health, /error, hub mapping, startup migration)
  Configurations/   DependencyInjection.cs = composition root (all Add* extensions), Swagger,
                    CORS allowlist, MappingProfile, RateLimitPolicies, ValidateModelStateFilter,
                    CloudinaryUrlJsonConverter
  Controllers/      18 controllers, thin: bind → service → Result → HTTP
  ResultExtensions.cs  ErrorType → RFC 7807 ProblemDetails (Validation/Failure→400, NotFound→404,
                       Conflict→409, Forbidden→403, ServiceUnavailable→503, else 500)
  wwwroot/          static email-confirmation pages + email templates
Application/        Contracts/ (service + client interfaces, IRepositoryManager), Services/,
                    Dtos/ (+FluentValidation validators), Community/ (PostTaxonomy, PostAccessPolicy),
                    Options/
Domain/             Entities/, Enums/, Errors/, Repositories/ (+ IAppointmentBookingLock),
                    States/AppointmentStates/, Value Objects/
Infrastructure/     Data/ (AppDbContext, DbInitializer), Configurations/ (EF entity configs+indexes,
                    TPT, soft-delete filter), Repositories/ (RepositoryBase + RepositoryManager +
                    SqlServerAppointmentBookingLock), Services/, Clients/ (MLServiceProbe,
                    HateSpeech*, ChatContentModerator, News, Resend), Hubs/, Caching/, Utilities/,
                    Migrations/ (37 migrations), BackgroundJobs/ (HostingRefresher — DORMANT)
Shared/             Result.cs, Error.cs, ErrorType.cs, PagingExtensions.cs, RequestParameters/
Tests/              xUnit + FluentAssertions + EF InMemory — 265 tests, registered in the solution
scripts/            reset-demo-data.sql (guarded), seed.sql (guarded, idempotent, fictional data)
docs/               ARCHITECTURE.md, DEPLOYMENT.md, RUNBOOK.md, KNOWN_LIMITATIONS.md,
                    adr/0001–0004, openapi.json, migrations/ (idempotent SQL for hosts without
                    tooling), chat-rooms-manual-verification.md,
                    doctor-community-appointment-contracts.md, deployment-diagnosis-monsterasp.md
.github/workflows/  ci_cd_pipeline.yml (build+test → docker publish → WebDeploy → verify)
```

Dependency direction (inward only): `API → Application → Domain → Shared`;
`Infrastructure → Application/Domain/Shared` and implements the interfaces; `API` is the only
project that references `Infrastructure`, because it is the composition root. Never add an
upward reference, never let `Application` know a DbContext or an HTTP type exists.

## Architecture rules (hard)

- **`Result<T>` / `Error`, not exceptions.** Business failures return `Result.Failure(Error.X(...))`
  with a stable code (`"Posts.DoctorsOnlyForbidden"`, `"Chat.RoomPassphraseIncorrect"`). Exceptions
  are for defects. Services return values; controllers map them to HTTP.
- **Unit of work.** All repositories share one `AppDbContext` via `RepositoryManager`; the commit
  point is `SaveAsync()`. Never call `SaveChanges` inside a repository method, never commit twice
  in one request.
- **`trackChanges` is load-bearing.** `FindAll(bool)` / `FindByCondition(_, bool)` choose tracked
  vs `AsNoTracking()`. Reads that intend to mutate must pass `true`. Pinned by
  `Tests/AppointmentStatsTests.cs`.
- **Soft delete for accounts only.** `BaseUser` has `IsDeleted` + a global query filter; delete =
  set the flag. Child rows with no `IsDeleted` column hard-delete. Chat messages have their own
  `IsDeleted`. There is deliberately **no** hard-delete-account path anymore.
- **TPT, and the accounts table is `AspNetUsers`.** `User` → `Users`, `Doctor` → `Doctors`,
  `Admin` → `Admins`. `AppUserConfiguration.ToTable("BaseUsers")` is overridden because
  `AppDbContext.OnModelCreating` calls `base.OnModelCreating(builder)` *after*
  `ApplyConfigurationsFromAssembly`, so Identity re-asserts `AspNetUsers` — and no `BaseUsers`
  table exists in any migration. Raw SQL (scripts, SSMS, WebMSSQL) must target `AspNetUsers`.
- **Appointment transitions go through the state objects** (`PendingState` … `RejectedState`);
  an illegal transition returns `Error.Conflict`, it does not throw.
- **Booking and schedule mutation hold the per-doctor lock.** Any path that creates an appointment
  or changes availability/time-off must wrap it in
  `_repos.Appointments.AcquireBookingLockAsync(doctorId)` (SQL `sp_getapplock`,
  `doctor-booking:<doctorId>`, transaction-owned) and follow
  acquire → revalidate → mutate → `SaveAsync()` → `CommitAsync()` → dispose.
- **Moderation fails closed.** No decision ⇒ the content is not written, and the caller gets a
  503 / `ChatErrors.ModerationUnavailable()`. Only an explicit `"normal"` prediction passes.
  `MLServiceProbe` owns the budget/retries; do not add a second HTTP path to the model, do not add
  a keep-alive poller, and never let a timeout or an unreadable body be read as "safe".
- **Auth invariants:** `MapInboundClaims = false`, `RoleClaimType = "roles"`,
  `NameClaimType = "name"`, `ValidateAudience = true`. Query-string tokens are accepted **only** on
  `/chat-hub` and `/notification-hub`. Google is registered only when both client id and secret
  exist. The real OAuth callback path is `/signin-google` (handler), not a controller route.
- **External services go through typed clients / interfaces** (`IHttpClientFactory`, named
  `ml-client`), never a raw `new HttpClient`, and credentials come from configuration or the
  process environment (`CLOUDINARY_URL` is environment-only).
- **Side effects are ordered by reversibility.** Commit inside the lock, deliver the in-app
  notification after the commit, and treat the email as best effort (`SendAppointmentEmailAsync`
  logs and swallows a mail failure — telling the client "failed" would invite a retry that
  duplicates the booking). Never let a notification or email problem roll back persisted state.
- **Startup migrations are additive and conditional.** `DbInitializer` applies pending migrations
  only when the database is reachable, never creates a database, and never seeds data.

## Never do

1. **Never change `JwtSettings:Audience`** (nor `Issuer`, nor the deployed `Key`) in a configuration
   that reaches a running environment. Issuance and validation read the same value, so the change
   invalidates every issued token and signs all users out. The production audience naming the
   doctor portal while both portals use the API is **not a typo** — see
   [`docs/adr/0002`](docs/adr/0002-jwt-audience-per-environment.md).
2. **Never run a destructive DB script without a verified backup and explicit confirmation** —
   including `scripts/reset-demo-data.sql` (it deletes the accounts currently on the hosted DB) and
   any `DROP`/`TRUNCATE`/data-rewriting `ALTER`. Never weaken the guards in
   `scripts/seed.sql` / `reset-demo-data.sql` (`@Confirm`, the database-name allowlist, the
   non-`@example.com` refusal) to make a run convenient.
3. **Never commit secrets.** `.env`, `API/.env`, `API/appsettings.Production.json`, `keys/` are
   gitignored and stay that way; production settings are generated in the runner from GitHub
   Actions secrets. Document secret **names** only. Never inline `${{ secrets.* }}` into a
   PowerShell body — pass through `env:` and read `$env:NAME`. Never log a credential, a submitted
   moderation text, or a `?access_token=`.
4. **Never edit a vendored `shared-ui` file inside a portal.** Anything listed in a portal's
   `.shared-ui.json` `files` array is overwritten by the next `npm run shared-ui:sync` and turns the
   other portal's drift check red. Fix upstream in `../shared-ui`, push, then re-pin **both**
   portals. See [`docs/adr/0001`](docs/adr/0001-vendored-shared-ui-instead-of-npm-package.md).
5. **Never make moderation fail open**, never register `HostingRefresher.cs`, never add a second
   ML HTTP client.
6. **Never remove the claim-mapping settings** (item list above) or re-add an inbound-claim
   assumption — role checks silently become 403s.
7. **Never hard-delete an account**, and never book/mutate a schedule outside the booking lock.
8. **Never change the publish/deploy shape**: `--runtime win-x86`, `-p:UseAppHost=false`, in-process
   `hostingModel="inprocess"`, `processPath="dotnet"`, the `publish/API.exe` guard in the pipeline,
   and `-enableRule:AppOffline -enableRule:DoNotDeleteRule` (the latter preserves the DataProtection
   `keys/` folder). Never enable HSTS. Never broaden CORS with an origin you have not verified
   belongs to this project — Vercel preview origins are excluded by design.
9. **Never upgrade AutoMapper 12.0.1 / MailKit 4.3.0** or any other package under the freeze; the
   advisories are documented in `KNOWN_LIMITATIONS.md`.
10. **Never weaken or delete a test** to make the suite pass, and never trust the portals' claims
    about server behaviour without reading the controller/service.
11. **Never rewrite `README.md`'s production claims wholesale** — it is partly a portfolio
    document and partly verified operational record. Correct specific stale lines, and point at
    `docs/KNOWN_LIMITATIONS.md` for the honest state instead of duplicating it.
12. **Never assume the token lifetime from `JwtSettings:Duration`.** The setting is `30` and the
    docs call it minutes, but `JwtTokenGenerator` applies it as `DateTime.Now.AddDays(Duration)`.
    If lifetime matters for a task, read the issued token's `exp` claim and raise it as an explicit
    question rather than "fixing" the unit under freeze.

## Verifying your change

```bash
dotnet build -c Release            # 0 errors, exactly 6 warnings
dotnet test  -c Release            # 265+ passing, no skipped
```

For anything touching auth, CORS, moderation, scheduling/booking, or the pipeline, also re-check
the affected section of [`docs/RUNBOOK.md`](docs/RUNBOOK.md) against reality (it is the operator's
view of the same invariants) and update the docs in the same branch: `docs/ARCHITECTURE.md` for a
pattern, `docs/DEPLOYMENT.md` for config/pipeline, `CHANGELOG.md` for anything user- or
ops-visible, `docs/KNOWN_LIMITATIONS.md` for anything deliberately left unfixed.

Conventions: file-scoped namespaces where the file already uses them, and match the indentation of
the file you are editing — the solution mixes tabs and 4-space indent, so whitespace-only churn in
a diff is not a refactor. One class per file, `I`-prefixed interfaces, `*Errors` static classes per
area, Conventional Commits with scopes `api`, `scripts`, `seed`, `migrations`
(see [`CONTRIBUTING.md`](CONTRIBUTING.md)).
