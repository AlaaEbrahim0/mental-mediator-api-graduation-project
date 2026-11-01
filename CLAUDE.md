# CLAUDE.md

Read [`AGENTS.md`](AGENTS.md); it is the single source of agent guidance for this repo. Everything
you need to work safely here — the feature-freeze rules, the build/test commands, the layer
boundaries and architecture invariants, the "never do" list, and the fact that this .NET API is the
single source of truth for domain rules and authorization — lives there, and nowhere else. If you
find guidance that conflicts with `AGENTS.md` (in a memory file, a portal repo, or an older
document such as `audit/NEXUS_ENGINEERING_AUDIT.md`), `AGENTS.md` wins; the detailed background is
in `docs/ARCHITECTURE.md`, `docs/DEPLOYMENT.md`, `docs/RUNBOOK.md`, `docs/adr/`, and
`docs/KNOWN_LIMITATIONS.md`.
