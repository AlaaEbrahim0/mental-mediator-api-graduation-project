# ADR 0002 — One `JwtSettings:Audience` per environment, frozen once deployed

**Status:** accepted · **Date recorded:** 2026-11-01
**Code:** `API/Configurations/DependencyInjection.cs` (`ConfigureAuthentication`),
`Infrastructure/Utilities/JwtTokenGenerator.cs`, `API/appsettings.json`,
`.github/workflows/ci_cd_pipeline.yml` (generated `appsettings.Production.json`)

**Context**

Token validation is configured with `ValidateIssuer = true`, `ValidateAudience = true` and
`ValidateIssuerSigningKey = true`, and `ValidAudience` is read from `JwtSettings:Audience`.
Issuance reads the *same* section: `JwtTokenGenerator` builds the token with
`issuer: _jwtOptions.Issuer, audience: _jwtOptions.Audience`. So a value only has to be
self-consistent inside one deployment — nothing in the platform cross-checks it against the
caller.

There are two production portals (`patient-ui` at `https://mental-health-ochre-xi.vercel.app`,
`doctor-ui` at
`https://mental-mediator-doctor-ui-graduation-project.vercel.app`) talking to one API, and the
configuration model supports exactly **one** audience string. The deployed value is the doctor
origin; the committed `appsettings.json` / `appsettings.Development.json` carry the patient
origin. That asymmetry looks like a typo and is not one.

**Decision**

- Keep a single `JwtSettings:Audience` per environment. Do not introduce per-portal audiences,
  an audience list, or client-id-scoped tokens.
- **Freeze the deployed value.** The production audience is
  `https://mental-mediator-doctor-ui-graduation-project.vercel.app`, written by the pipeline
  into the generated `appsettings.Production.json`. It must not be "corrected", aligned with
  `ClientUI`, or reformatted. Changing it invalidates every already-issued token, because
  validation compares the `aud` claim of tokens that are already in browsers.
- The local value (patient-portal origin) stays as it is: local runs mint and validate with the
  same string, so it works, and no local token can be replayed against production (different
  key, different issuer, different audience).
- Treat a deployed audience or key change as a **coordinated sign-out event**: announce it,
  deploy API + both portals together if the change is part of something larger, and re-check
  sign-in on both portals afterwards. `JwtSettings:Issuer` is frozen for the same reason even
  though it is decorative (`https://nexus-api.runasp.net`).
- Anything that looks like a bug in this area and is not a code change to validation parameters
  is a configuration change and therefore a deployment decision, not a fix.

**Consequences**

*Positive*

- Audience validation stays on, which is the point of the setting: a token minted elsewhere
  (or by a local dev instance with a different audience) is refused by the deployed API.
- Zero coordination cost today: neither portal reads `aud`, they only replay the bearer token,
  so one value serving two clients is harmless.
- The rule is mechanically checkable: compare the `aud` claim of an issued token with the
  deployed `appsettings.Production.json` (see `RUNBOOK.md` §4).

*Negative / accepted costs*

- The production audience names only one of the two portals, which reads as an error to anyone
  skimming the config, and a plausible-looking "fix" causes an immediate full sign-out.
- No way to distinguish the two clients by token: a patient token and a doctor token carry the
  same `aud`. Authorization therefore rests entirely on the `roles` claim plus the per-resource
  checks in services — the audience must never be treated as a tenant boundary.
- Rotating the value requires a re-login for every session; there is no refresh token to soften
  it (`KNOWN_LIMITATIONS.md`).
- Because `MapInboundClaims = false` and `RoleClaimType = "roles"`, the audience/issuer/claim
  trio is effectively frozen as a set: changing any one of them in a deployed environment is the
  same kind of event.

*Revisit when*

Per-client tokens or a proper cookie/refresh-token transport are introduced — that redesign
(`KNOWN_LIMITATIONS.md`, "JWT bearer token stored in `localStorage`") is the right moment to
make `aud` meaningful, and must be deployed as one coordinated change across the API and both
portals.
