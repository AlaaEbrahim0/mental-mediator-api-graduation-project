# Deployment diagnosis — MonsterASP migration

Date of audit: 2026-10-23. Baseline: the application works locally (docker-compose stack and
`dotnet run` against a local SQL Server). The goal of this document is to record what was
verified before any change was made, so that the pipeline and hosting changes are evidence-backed.

## Verified environment facts

| Item | Value | Evidence |
| --- | --- | --- |
| Hosting account site id | `site101103` | DNS: `site101103.siteasp.net` resolves and answers HTTPS on 8172; the legacy `site7101` id no longer resolves |
| WebDeploy endpoint | `https://site101103.siteasp.net:8172/msdeploy.axd?site=site101103` | MonsterASP "Deploy from the command line" doc (URL format + `userName = site id` + `AuthType=Basic`) |
| Public API URL | `https://nexus-api.runasp.net/` | Task config; TLS already served by the host (IIS 404 page over HTTPS before first deploy) |
| Authenticated frontends | Vercel: `mental-health-ochre.vercel.app`, `mental-mediator-doctor-ui-graduation-project.vercel.app` (and one active preview URL) | CORS policy + JWT audience in code |
| WebDeploy secret on GitHub | `MONSTERASP_MSDEPLOY_PASSWORD` (present) | Repo secret list; value not printed anywhere |
| Provider deploy semantics | Every file in the msdeploy `contentPath` source is uploaded into `/wwwroot` | Provider command-line deploy doc |

## Stale references found

- `API/MonsterASP.pubxml` (untracked, `*.pubxml` is gitignored): references the dead `site7101`.
  Regenerated to point at `site101103` for anyone publishing from Visual Studio.
- Repo variables `WEBDEPLOY_SERVER` and `WEBSITE_NAME` on GitHub also contain `site7101` —
  updated so the repo configuration cannot mislead a future deploy.

## Confirmed deployment blockers

1. The workflow deploy job targeted the Somee host and the old site id; the uncommitted
   migration switched it to MonsterASP but with `http://` default URLs while the intended
   production URL is HTTPS (the host serves TLS on `*.runasp.net`, so HTTPS is safe).
2. `CLOUDINARY_URL` is read from the process environment only
   (`Infrastructure/Services/StorageService.cs`). Shared IIS hosts only reach environment
   variables through the `web.config` `<environmentVariables>` block (or control-panel app
   settings). The workflow now injects it — and `ASPNETCORE_ENVIRONMENT=Production` — into the
   generated `web.config` when the secret is present. Without it image uploads return a
   controlled 503, the app itself still starts.
3. Frontend `REACT_APP_API_URL` still pointed at the dead Somee URL.

## Known risks / limitations (reported, out of scope to fix here)

- **Credentials exposed in public git history** (removed from tracked files in commit
  `fa6c1e8` but still in the object history): Somee SQL login, earlier JWT signing key,
  Google OAuth client secret, Gmail app password, NewsAPI key. Rotation/revocation in the
  relevant consoles is required; GitHub secrets were rotated after exposure but the
  Google client secret, Gmail app password and NewsAPI key should be re-verified by the
  account owner.
- Google OAuth: the callback `https://nexus-api.runasp.net/signin-google` must be registered
  as an authorized redirect URI in the Google Cloud console (account-side action).
- Swagger is served in production (deliberate: the deploy verification probes it to confirm
  the full middleware pipeline). Restricting it in production is a separate decision.
- Startup migrations (`MigrateAsync` in `DbInitializer`) run on every cold start and are
  additive; a failing migration is logged and does not take the site down. The production
  database already exists and must not be reset.
- No automated test projects exist in the solution, so `dotnet test` currently validates the
  build only.
