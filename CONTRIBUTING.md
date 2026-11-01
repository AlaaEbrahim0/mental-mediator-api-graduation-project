# Contributing — Nexus API

## Status: FEATURE FREEZE (graduation project)

No new features, no opportunistic refactors, no dependency upgrades. Fix real defects with the
smallest change that fixes them. If a change is not required by the task, do not make it.
Anything that would alter user-visible behaviour, the auth contract, the moderation policy or
the deployment shape belongs in
[`docs/KNOWN_LIMITATIONS.md`](docs/KNOWN_LIMITATIONS.md) as a deferred item, not in a commit.

**A push to `master` deploys to production.** The CI/CD workflow triggers on `push: master`, and
the repository variable `AUTO_DEPLOY_MONSTERASP` is currently `true` (verified 2026-11-01), so
build → test → publish → WebDeploy → `/health` verification all run against the live site. Work
on a branch; push to `master` only when the change is meant to go live.

---

## 1. Setup, build, test

Requires the .NET 8 SDK (`global.json` pins `8.0.426`, `rollForward: latestFeature`; CI uses
`8.0.x`) and SQL Server 2019+ for a local run.

```bash
cp .env.example .env                 # local dev values only; .env is gitignored
dotnet restore
dotnet build -c Release              # expect: Build succeeded, 0 errors, 6 warnings
dotnet test  -c Release              # expect: Failed: 0, Passed: 265
dotnet run --project API --launch-profile https   # https://localhost:7235 + http://localhost:5184
```

The 6 Release warnings are the two NuGet advisories reported per project in both the restore and
build phases — `NU1903` AutoMapper 12.0.1 and `NU1902` MailKit 4.3.0. They are **intentionally
not upgraded** under freeze (see `docs/KNOWN_LIMITATIONS.md`). Treat any *new* warning as a
regression.

EF tooling is pinned in `dotnet-tools.json`:

```bash
dotnet tool restore                   # dotnet-ef 8.0.32
dotnet ef migrations add <Name> --project Infrastructure --startup-project API
dotnet ef database update        --project Infrastructure --startup-project API
```

### Lint

There is **no separate lint or format step in this repository**: no `.editorconfig`, no analyzer
configuration, no `TreatWarningsAsErrors`. The gate is the compiler and CI —
`dotnet build -c Release` plus `dotnet test -c Release` are exactly what the pipeline runs, so a
local green is a CI green. (`dotnet format` will apply the SDK's defaults if you want to tidy a
file, but it is not a required or enforced step; do not mass-reformat.) Nullable reference types
are `enable` in every project — keep them that way and do not silence a warning with `!` when a
guard is what is needed.

The two portals and the shared UI repo do have their own checks (`CI=true npm run build` fails on
ESLint warnings, `npm run shared-ui:check`); those live in their repositories.

### Tests

`Tests/MentalMediator.Tests` (xUnit + FluentAssertions + `Microsoft.EntityFrameworkCore.InMemory`)
is registered in `MentalMediator.sln`, so solution-level `dotnet test` discovers all 265.
Add a **failing-before / passing-after regression test with every bug fix** — that is how the
stabilization pass pinned the repository tracking flag and the doctor-stats filter
(`Tests/AppointmentStatsTests.cs`).

---

## 2. Branches

Default branch: `master`. Observed naming (keep using it):

```
feat/<kebab-scope>      fix/<kebab-scope>       refactor/<kebab-scope>
docs/<kebab-scope>      test/<kebab-scope>      chore/<kebab-scope>
```

Examples from history: `feat/backend-profile-community-appointments`,
`fix/posts-schema-reconciliation`, `migrate-deployment-to-monsterasp`.

Keep a branch to one concern: a schema change and its migration, one bug fix and its test, one
documentation set. Rebase or merge `master` before review, not after a production deploy.

---

## 3. Commit messages — Conventional Commits

```
<type>(<scope>): <imperative summary, no trailing period>

<why, not what — the body explains the constraint or the symptom>
```

| Field | Values in use |
|---|---|
| `type` | `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `merge` |
| `scope` | `api`, `scripts`, `seed`, `migrations` (portals/shared-ui use their own scopes) |

Real examples from this repository:

```
fix(api): thread CancellationToken through email delivery
refactor(api): remove dead hard-delete user repository method
test(api): pin doctor profit filter and repository tracking; fix month order
feat(scripts): add guarded reset-demo-data.sql for the demo reseed
docs(api): add KNOWN_LIMITATIONS and correct stale security/test claims
```

Rules: one logical change per commit; the summary says *what*, the body says *why*; no
"fixups"/"wip" commits landing on `master`; never skip or bypass a hook — fix the cause.

---

## 4. Where a change belongs

| Change | Layer | Also touch |
|---|---|---|
| Entity, enum, state rule, domain error | `Domain` | test in `Tests/` if a rule changed |
| Service rule, DTO, validator, taxonomy | `Application` | controller only if the contract changes |
| Query, `Include`, index, migration, external client, hub | `Infrastructure` | `docs/ARCHITECTURE.md` if the pattern changed |
| Route, DI registration, pipeline option, HTTP mapping | `API` | `docs/DEPLOYMENT.md` if it needs config |
| Anything a portal "needs the API to allow" | — | **refuse**: the API is the authorization source of truth |

Migrations must be additive and idempotent-friendly: production applies pending migrations at
startup (`Infrastructure/Data/DbInitializer.cs`) and never recreates a database. Destructive
schema work needs a written plan and a verified backup, and is out of scope under freeze.

Configuration: add new keys to `API/appsettings.Example.json` **and** `.env.example`, with an
empty or placeholder credential value. Never put a real credential in `appsettings.json`,
`appsettings.Development.json`, or a test fixture.

---

## 5. NEVER do

1. **Never change `JwtSettings:Audience`** (or `Issuer`, or the deployed `JwtSettings:Key`) in a
   configuration that reaches a running environment. `ValidateAudience = true` and issuance reads
   the same value, so the change invalidates every token already in browsers and signs everyone
   out. See [`docs/adr/0002`](docs/adr/0002-jwt-audience-per-environment.md). The local value
   *looking* different from the production one is not a bug.
2. **Never run a destructive database script without a verified backup and explicit confirmation.**
   That includes `scripts/reset-demo-data.sql` (which deletes the accounts currently on the hosted
   database), `DROP`/`TRUNCATE`, and hand-written `ALTER`s that rewrite data. Arm-and-verify the
   guards, dry-run first, then run. `scripts/seed.sql` refuses a non-demo database — do not weaken
   that guard.
3. **Never commit a secret.** No connection strings, API keys, tokens, client secrets or real
   credentials in source, tests, fixtures, docs, `.env`, or the deploy log. Production settings are
   generated in the runner from GitHub Actions secrets and are gitignored
   (`API/appsettings.Production.json`, `.env`, `/keys/`). List secret **names** only in
   documentation; pass values through `env:` and read them with `$env:NAME` in the workflow.
4. **Never edit a vendored `shared-ui` file inside a portal.** Files listed in a portal's
   `.shared-ui.json` `files` array are overwritten by the next `shared-ui:sync` and turn the other
   portal's drift check red. Fix upstream in `nexus-shared-ui`, push, then re-pin **both** portals.
   See [`docs/adr/0001`](docs/adr/0001-vendored-shared-ui-instead-of-npm-package.md).
5. **Never make content moderation fail open**, and never add a keep-alive poller for the model
   host. A missing decision is a 503, not a publish.
   See [`docs/adr/0004`](docs/adr/0004-fail-closed-moderation.md).
6. **Never remove `MapInboundClaims = false` / `RoleClaimType = "roles"` / `NameClaimType = "name"`**
   from `ConfigureAuthentication`. With the default claim mapping every `[Authorize(Roles = "Doctor")]`
   endpoint answers 403.
7. **Never hard-delete an account.** `BaseUser` is soft-deleted (`IsDeleted` + global query filter);
   the dead hard-delete repository method was removed deliberately. Child rows with no `IsDeleted`
   column are the only hard deletes.
8. **Never book or mutate a doctor's schedule outside the per-doctor booking lock**
   (`IAppointmentBookingLock` → `sys.sp_getapplock` with `@Resource = "doctor-booking:<doctorId>"`).
9. **Never change the publish shape** (`--runtime win-x86`, `-p:UseAppHost=false`,
   `hostingModel="inprocess"`, `processPath="dotnet"`), and never remove the pipeline's
   self-validation step or the `-enableRule:AppOffline -enableRule:DoNotDeleteRule` pair — the
   second one is what preserves the DataProtection `keys/` folder.
10. **Never enable HSTS**, and never broaden the CORS allowlist with an origin you have not
    verified belongs to this project. Vercel preview origins are excluded by design.
11. **Never make an authorization decision only in a portal.** Frontend role checks decide which
    screen is shown; the API decides whether the request is allowed.
12. **Never upgrade AutoMapper or MailKit** (or any other package) under the freeze; the advisories
    are documented. Never add a package without saying why in the PR.
13. **Never weaken a test to make it pass**, and never delete `Tests/ChatRoomPrivacyTests`-style
    guard tests. If a guard test fails, the code changed the invariant it protects.
14. **Never amend, force-push, or rewrite a commit that has been deployed** — the deploy is
    reproducible from history, and that is worth more than a tidy log.

---

## 6. Before opening a PR / pushing

- [ ] `dotnet build -c Release` → 0 errors, and still exactly the 6 known advisory warnings.
- [ ] `dotnet test -c Release` → 265 passing (or more, with your new regression test).
- [ ] No new warning, no new package, no mass reformat.
- [ ] Any schema change ships as an **additive** migration in `Infrastructure/Migrations`, and the
      app still starts against an empty-but-existing database (`DbInitializer` applies, never creates).
- [ ] New config keys added to `API/appsettings.Example.json` and `.env.example` with placeholders.
- [ ] Docs updated in the same branch: `docs/ARCHITECTURE.md` for a pattern, `docs/DEPLOYMENT.md`
      for pipeline/config, `docs/RUNBOOK.md` for an operational step,
      `docs/KNOWN_LIMITATIONS.md` for anything deliberately *not* fixed, `CHANGELOG.md` for a
      user-visible or ops-visible change.
- [ ] Nothing secret is in the diff (`git diff --cached -U0 | grep -iE 'password|secret|key='` is a
      cheap last check) and no real user data is in a fixture or screenshot.
- [ ] If the change touches auth, CORS, moderation, or the deploy job, say so in the PR body: those
      four are the ones that can take production down or sign everyone out.

## 7. Reviewing

Review against the freeze, not against taste: does this fix a real defect, is it the smallest
change, does it add a test, does it leave the docs honest? A reviewer's most useful comment here
is often "move this to `KNOWN_LIMITATIONS.md` and keep the commit".
