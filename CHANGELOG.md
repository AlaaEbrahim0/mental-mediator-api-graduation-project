# Changelog — Nexus API

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), the header convention follows
Semantic Versioning, and **this repository is in FEATURE FREEZE**: the entries below are
stabilization work (fixes, guards, demo data, documentation), not new behaviour. There are no
release tags; entries are dated by the day the work landed on `master`.

## [Unreleased]

Nothing queued. Under the freeze, anything that is not a defect fix is recorded as a deferred
item in [`docs/KNOWN_LIMITATIONS.md`](docs/KNOWN_LIMITATIONS.md) instead of being scheduled here.

## [2026-11-03] — Community reactions, comment/reply attachments, chat voice notes

Requested by the project owner as a deliberate exception to the feature freeze, together with
the matching portal work in `../shared-ui`, `../patient-ui` and `../doctor-ui`. Baseline:
`dotnet build -c Release` → 0 errors, 6 warnings; `dotnet test -c Release` → **271 passed**
(was 244 before the 27 reaction/attachment tests). **API-first rollout: this deploy must land
before the portals, and every change below is additive** — `/like`, JSON comment bodies and
all existing response shapes keep working.

### Added

- **Reactions** (`like`, `love`, `haha`, `wow`, `sad`) on posts, comments, replies and chat
  messages. `Domain/Enums/ReactionKinds.cs` is the vocabulary; `Domain/Value_Objects/ReactionState.cs`
  aggregates one query per page. New `PUT`/`DELETE …/reactions` routes on
  `/api/posts/{id}`, `/api/posts/{postId}/comments/{commentId}`, `…/replies/{replyId}` and
  `/api/chat/messages/{messageId}`. Responses gained `reactions` + `myReaction`.
- `PostLike.Reaction` (migration `AddCommunityReactionsAndAttachments`, `PostLikes` keeps its
  name and every existing row stays a valid `like`) plus new `CommentReactions`,
  `ReplyReactions` and `ChatMessageReactions` tables, each with a unique `(UserId, TargetId)`
  index — one reaction per member per target, enforced by the database.
- **One image or video attachment per comment/reply.** `Comment`/`Reply` gained `MediaUrl` +
  `MediaType`; `PostMediaAsset` gained nullable `CommentId`/`ReplyId` so the existing upload
  ledger also owns comment and reply media (`NoAction` FKs; the services delete the rows and
  destroy the Cloudinary asset, because SQL Server refuses two cascade paths into one table).
  `Application/Community/AttachmentMediaPolicy.cs` accepts a URL only when the ledger says the
  submitting account owns it and it is attached to nothing else.
- **Voice notes in chat rooms.** `CloudinaryStorageService` now classifies and uploads audio
  (`.mp3 .m4a .wav .ogg .oga .aac .opus .weba .flac`, 10 MB cap) and deletes it through the
  video resource type. `SendChatMessageRequest` accepts `mediaType: "audio"`, a voice note is a
  valid media-only message, and `LastMessagePreview` renders “Sent a voice message”.
- `MessageReactionsUpdated` SignalR event: one neutral payload per room carrying the counts and
  `actorMemberId`/`actorReaction`, so each viewer resolves its own `myReaction` client-side.
- `Tests/ChatReactionTests.cs` (11) and `Tests/CommunityReactionTests.cs` (16): membership gating
  on message reactions, replace-not-stack per member, deleted-message refusal, the like/reaction
  equivalence, media-only comment and reply creation, ownership and reuse rejection, and
  comment-thread attachment cleanup.

### Changed

- `PUT/DELETE /posts/{id}/like` are now aliases for setting/removing the `like` reaction, and
  `LikesCount`/`IsLikedByCurrentUser` are computed from the `like` token only. A `love` reaction
  does not raise the like count. Notifying the author reads “reacted to your post” for any token
  other than `like`.
- `CreateReplyRequest.Content` became `string` defaulting to `string.Empty`, matching
  `CreateCommentRequest`. Media-only replies previously mapped to a `null` content and failed at
  `SaveChanges`; this is the defect fix that makes a reply-with-video actually persist.
- Hate-speech moderation on a comment or reply now runs only when text is present, so a
  media-only post is not sent to the model as an empty string.

### Deferred

- `docs/openapi.json` is now eight routes behind the live surface: the four
  `PUT/DELETE …/reactions` pairs are missing from the snapshot (94 paths exported at commit
  `2bdc6a2`). Refreshing it means running the API and saving `/swagger/v1/swagger.json`, and
  that export path is not scripted in this repo, so it was not regenerated as part of this
  change. The Swagger UI served at `/swagger` is generated from the running app and is correct.

## [2026-11-02] — Doctor ratings

Requested by the project owner as a deliberate exception to the feature freeze. Baseline:
`dotnet build -c Release` → 0 errors, 6 warnings; `dotnet test -c Release` → **244 passed**
(was 230 before the 14 rating tests).

### Added

- `DoctorRating` entity + `AddDoctorRatings` migration (table `DoctorRatings`, unique index on
  `(DoctorId, RaterUserId)`; FKs `NoAction` because accounts are soft-deleted, never removed).
- `POST /api/doctors/{doctorId}/ratings` (upsert — a second submission edits the existing vote),
  `GET .../ratings` (recent, max 20), `GET .../ratings/summary`, `GET/DELETE .../ratings/mine`
  (`API/Controllers/DoctorRatingsController.cs`). Writing is `[Authorize(Roles = "User")]`;
  patients only, never the doctor's own account.
- `DoctorInfoResponse.RatingAverage` / `RatingCount`, filled by one grouped query per page in
  `DoctorService` (the `(double)` cast in `DoctorRatingRepository` is load-bearing — SQL
  `AVG(int)` truncates, so 4+5 would report 4).
- Rating comments run through the same hate-speech gate as posts, including the existing
  allow-on-unavailable behaviour (`DoctorRatingService.IsRestrictedCommentAsync`).
- `Tests/DoctorRatingTests.cs` — 14 tests pinning upsert-once, role and self-rating refusal,
  owner-scoped delete, the non-truncating average, and validator bounds.
- `scripts/reset-demo-data.sql` now clears `DoctorRatings` with the other child rows.

## [2026-11-01] — Google OAuth removal

The patient portal's Google sign-in handoff could not establish a session, so the button was a
dead end; per project decision the external-login path was removed entirely.
Baseline: `dotnet build -c Release` → 0 errors, 6 warnings; `dotnet test -c Release` →
**230 passed** (was 265 before the OAuth-flow tests went).

### Removed

- `GET /api/auth/external-login`, `GET /api/auth/external-login-callback` and
  `GET /api/auth/exchange-token` from `AuthController`, plus `AuthService.ExternalLoginAsync`
  and its private helpers, the two `IAuthService` contract members, the `AddGoogle`
  registration and its `OnRemoteFailure` handler, `GoogleAuthenticationOptions`,
  `ExternalAuthenticationErrors`, `ExternalLoginProfile`, and the `oauth-handoff` rate-limit
  policy (commit `b4ef076`).
- `GoogleAuthentication` sections from `appsettings*.json` and `.env.example`
  (`GoogleAuthentication__Id` / `__Secret`).
- The dead OAuth-handoff surface on `AuthHandoff` (`PublicErrorCode`,
  `BuildFailureRedirect`, `BuildHandoffRedirect`, the `ExternalAuthentication.*` code map) and
  its tests; `AuthHandoff` now only resolves portal origins for confirmation/reset links.
- `PRODUCTION_GOOGLE_ID` / `PRODUCTION_GOOGLE_SECRET` from the CI pipeline: the env wiring,
  the fail-if-missing checks, the generated `appsettings.Production.json` block, and the
  publish-output validation. The GitHub secrets themselves can now be deleted from the repo.
- The OAuth endpoints from `docs/openapi.json`.

### Changed

- Sign-in is password-only on both portals. [`docs/adr/0003`](docs/adr/0003-auth-channels-doctor-password-patient-google.md)
  is superseded; `docs/ARCHITECTURE.md`, `DEPLOYMENT.md`, `RUNBOOK.md`, `KNOWN_LIMITATIONS.md`
  and `README.md` were corrected in the same pass.

## [2026-11-01] — Stabilization pass

Baseline before and after: `dotnet build -c Release` → **0 errors, 6 warnings** (the two NuGet
advisories reported per project, deliberately not upgraded); `dotnet test -c Release` →
**265 passed, 0 failed** (was 262 before the regression tests added in this pass).

### Added

- `Tests/AppointmentStatsTests.cs` — regression tests pinning two invariants that had no
  failing-before/passing-after guard: `GetDoctorStats` counts **completed** appointments' fees as
  profit only, and `RepositoryBase.FindAll`/`FindByCondition` honour the `trackChanges` flag
  (the flag was inverted once). Commit `41eb722`.
- `scripts/reset-demo-data.sql` — guarded demo reset: arms only with
  `@Confirm = 'RESET_NEXUS_DEMO_DATA'`, runs only against `db75421` / `MentalMediator` /
  `nexus_demo`, deletes application data in one `XACT_ABORT` transaction, and **preserves**
  `[AspNetRoles]` and `[__EFMigrationsHistory]` so the startup migration check and the seeded
  Identity roles survive. Prints verification counts. Commit `35bc8e4`.
- Seeded PBKDF2-SHA256 passphrase for the private demo chat room, so the private-room path is
  demonstrable without a hand-set secret. Commit `f725cee`.
- Seeded abstract, Cloudinary-hosted demo avatars for the fictional accounts, replacing empty
  profile images in the demo. Commit `3e24e01`.
- `docs/openapi.json` — exported OpenAPI snapshot of the current surface (94 paths). Commit
  `2bdc6a2`.
- `docs/KNOWN_LIMITATIONS.md` — the honest freeze record: what is intentional, what is deferred
  and why, what is accepted risk. Commit `b901e57`.
- Documentation set for this pass: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md),
  [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md), [`docs/RUNBOOK.md`](docs/RUNBOOK.md),
  `docs/adr/0001`–`0004`, [`CONTRIBUTING.md`](CONTRIBUTING.md),
  [`AGENTS.md`](AGENTS.md), [`CLAUDE.md`](CLAUDE.md).

### Changed

- **CORS allowlist reduced to verified origins** — dropped the stale doctor preview origin
  `https://mental-mediator-doctor-ui-graduation-project-lms6panp1.vercel.app`. The compiled list is
  now exactly the two production portal aliases
  (`https://mental-health-ochre-xi.vercel.app`,
  `https://mental-mediator-doctor-ui-graduation-project.vercel.app`) plus localhost dev origins;
  any other origin must be supplied at deploy time through `CORS_ALLOWED_ORIGINS` or
  `Cors:AllowedOrigins`. Vercel preview origins remain excluded by design. Commit `28b79cb`.
- `README.md` and `docs/` claims reconciled with the code: test count corrected to the actual
  suite, stale security statements replaced by pointers to `KNOWN_LIMITATIONS.md`. Commits
  `b901e57`, `2bdc6a2`.
- Monthly appointment statistics are ordered by the **numeric month key** instead of re-parsing
  the abbreviated, culture-dependent month name — removes a fragile round-trip and the `CS8604`
  warning it produced. Commit `41eb722`.

### Fixed

- **Email delivery honours cancellation.** `ResendMailService` passed `CancellationToken.None`, so
  an aborted request could not cancel an in-flight provider call. `IMailService.SendEmailAsync`
  now takes an optional token (default preserves existing callers) that is forwarded to both the
  Resend client and the MailKit SMTP fallback. The same commit bound the already-validated
  `MailSettings` once and cleared the `MailService` null-reference warnings. Commit `799883e`.
- **No accidental hard-delete path for accounts.** `UserRepository.DeleteUser` removed the entity
  with a real SQL delete, contradicting the `IsDeleted` soft-delete query filter on `BaseUser`; it
  was never called (`UserService.DeleteUser` sets `IsDeleted`). The method and its interface
  member were deleted so the only account delete is a soft delete. Commits `77ae07c`, and the
  split is documented in `KNOWN_LIMITATIONS.md`.
- **Doctor report profit filter pinned.** `GetDoctorStats` returns `totalAppointments` and
  `totalProfit` scoped to `Status == Completed`; that is now asserted by
  `Tests/AppointmentStatsTests.cs` rather than only reviewed. Commit `41eb722`.
- **Repository tracking flag pinned.** `trackChanges ? tracked : AsNoTracking()` is now guarded by
  a test, so it cannot silently re-invert. Commit `41eb722`.

### Removed

- `IUserRepository.DeleteUser` / `UserRepository.DeleteUser` — dead hard-delete path (see *Fixed*).
  Commit `77ae07c`. (Earlier in the same window: the tracked debug directory listing, commit
  `cc83b16`.)

### Notes for presenters

- The "total appointments" figure on the doctor report means **completed sessions**, not bookings.
- Warm the ML moderation service before a demo
  ([`docs/RUNBOOK.md`](docs/RUNBOOK.md) §3): moderation is fail-closed and the hosted model
  cold-starts in ~32 s.
- Re-login before a demo; there is no refresh token ([`docs/KNOWN_LIMITATIONS.md`](docs/KNOWN_LIMITATIONS.md)).
- Demo accounts are fictional `@example.com` logins; `scripts/seed.sql` refuses any database that
  already holds a real account.
