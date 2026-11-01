# ADR 0001 — Vendor `@nexus/shared-ui` into the portals instead of publishing an npm package

**Status:** accepted · **Date recorded:** 2026-11-01 · **Scope:** cross-repo (affects the API
indirectly, recorded here because the API's CORS and auth contracts determine when a portal
build can be verified)

**Context**

The platform has two React 18 + Create React App portals (`patient-ui`, `doctor-ui`) that must
look and behave identically: one design-token source, one set of shared components, one Axios/
SignalR/session layer. The shared code lives in a third repository, `nexus-shared-ui`.

The obvious shape is a versioned npm package (private registry or GitHub packages). Three things
block it:

1. **`react-scripts` transpiles JSX only under the consuming app's own `src/`.** Its Babel rule is
   `include: paths.appSrc`, so a dependency — from `node_modules` or a `github:` specifier —
   that ships raw JSX fails the production build. The workarounds are craco or
   react-app-rewired (config override layers on a toolchain the team had already shipped), or
   ejecting, which rewrites the build config the vendoring tooling and the ESM/Jest constraints
   depend on.
2. **Adding a build/bundle step means owning a build pipeline** (transpile, type/dep resolution,
   sourcemaps, publish permissions, version collision handling) for a graduation project in
   feature freeze, on a toolchain with no bundler configured upstream. `nexus-shared-ui`
   deliberately has no build, no `dist/`, no `files` field and no publish step.
3. **Both portals are deployed to Vercel independently and not connected to GitHub**, so a
   package version bump would be an unpinned, moving artifact in a build the team cannot
   reproduce later. Reproducibility is more valuable here than convenience.

**Decision**

Keep the shared layer **vendored by copy, pinned to a commit SHA**:

- Each portal lists the exact files it consumes in `.shared-ui.json` (`repo`, `ref`, `version`
  as a label only, `files`). Currently `patient-ui` vendors 45 files and `doctor-ui` 51, both
  pinned to `f082cb5d2f2d45de0b38f43fbea2cc9de9f7d1fd`.
- `scripts/shared-ui-sync.mjs` copies the pinned files into the portal at mirrored paths.
  `npm run shared-ui:sync` writes them; `npm run shared-ui:check` exits non-zero on drift or on
  a listed path missing upstream. The tool only ever copies — deleting a path upstream leaves a
  stale copy that must be removed by hand.
- A sibling `../shared-ui` checkout is used only when its `HEAD` equals the pinned `ref`;
  otherwise the pinned ref is cloned from GitHub, so CI and Vercel never depend on a local path.
- Fix order is strictly upstream first: edit in `nexus-shared-ui`, push to `master`, then
  re-pin **both** portals (`npm run shared-ui:sync -- --ref <sha>`), `shared-ui:check`, build,
  test, and commit the re-pinned manifest together with the synced files.
- Portal code imports the vendored copy by relative path — never from `@nexus/shared-ui`.

**Consequences**

*Positive*

- No build pipeline, no registry, no private-package credentials to manage; the pin is a commit
  SHA, so any past state of any portal is reproducible.
- Drift is detectable mechanically (`shared-ui:check`, plus a `shared-ui-drift.yml` CI job),
  and the copies are byte-identical by construction.
- Works with unmodified CRA — no craco/react-app-rewired/eject, so the constraints that other
  decisions depend on (ESM-only `react-markdown` kept out of the primitives barrel, CRA's Jest
  behaviour) stay intact.

*Negative / accepted costs*

- Two vendored copies must be kept on the same SHA. A shared fix landed in one portal only
  guarantees a visual/behavioural divergence between the doctor and patient views.
- The vendored manifests are hand-maintained and have diverged (6 extra files in `doctor-ui`:
  four shared test files plus `CertificateModal.js` / `QualificationModal.js`).
- The sync never deletes, so removing a shared file is a three-repo, manually-verified operation.
- Editing a vendored copy inside a portal is silently possible until `shared-ui:check` turns red
  — which is why the rule "never edit vendored files in a portal" is in `CONTRIBUTING.md` and
  `AGENTS.md`.
- Portal `src/index.css` and `tailwind.config.js` are vendored too: token changes belong
  upstream, never in a portal.

*Revisit when*

The portals move off `react-scripts` (Vite, Next, or an ejected/rspack pipeline) that transpiles
JSX from a dependency — then a real workspace or package becomes cheaper than the copy tooling.
