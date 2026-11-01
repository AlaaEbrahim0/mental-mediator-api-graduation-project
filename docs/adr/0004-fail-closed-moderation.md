# ADR 0004 — Content moderation fails **closed**

**Status:** accepted (demo decision, reaffirmed under freeze) · **Date recorded:** 2026-11-01
**Code:** `Infrastructure/Clients/MLServiceProbe.cs`, `HateSpeechDetectorClient.cs`,
`ChatContentModerator.cs`, `DepressionDetectorClient.cs`, `Application/Services/PostService.cs`,
`CommentService.cs`, `ReplyService.cs`, `ChatService.cs`,
`API/Configurations/DependencyInjection.cs` (`ConfigureHttpClients`)
**Tests:** `Tests/ModerationClientTests.cs`

**Context**

Every user-authored text — posts, comments, replies, chat messages — is classified by the
hate-speech model at `MLServerAddress` **before** it is persisted. That model is a free-tier
host that sleeps when idle: a cold request measured ~32 s, warm ~0.5 s, and the service exposes
no health or readiness route (`/health`, `/ping`, `/` all 404 or error), so nothing is cheaper to
probe than a real prediction.

Two behaviours were observed before this decision was written down:

- On the community path an unclassified transport failure let `TaskCanceledException` escape, so
  the browser received an HTTP 500 with a real stack trace for what is actually "the safety check
  did not finish".
- Any allow-on-error shortcut would publish unmoderated content into a support community of
  people discussing depression and stress — precisely the content the check exists for, and
  precisely the moment the check is unavailable.

**Decision**

**No answer is never treated as a safe answer.** Specifically:

- `MLServiceProbe` is the only transport. One budget (`MLServer:TimeoutSeconds`, default 45)
  covers a whole attempt sequence and is also the `ml-client` `HttpClient.Timeout`. Retries
  (`MLServer:MaxRetries`, default 2, backoff `MLServer:RetryBackoffSeconds`, default 1, doubling,
  skipped when it would cross the deadline) are spent **only** on failures that answer quickly:
  a transport error or 408/429/500/502/503/504 — what the host returns while a container starts.
  A timed-out attempt is never retried, because the budget is already spent.
- A pass requires an **explicit** `"normal"` prediction. `Completed` with a blank/missing
  prediction, `TimedOut`, `Unavailable` and `InvalidResponse` all map to a controlled failure:
  `Error.ServiceUnavailable("ExternalServices.HateSpeechDetectionServiceUnavailable", …)` for
  community content, `ChatErrors.ModerationUnavailable()` for chat. Status code 503.
- A deterministic rejection is not a failure: a non-`normal` prediction returns
  `Content.Forbidden` / `ChatErrors.ContentRejected()` once and is not retried.
- `MLServerAddress` is **required at startup**: `ConfigureHttpClients` throws
  `MLServerAddress must be configured for content moderation.` rather than booting an
  unmoderated deployment.
- **No keep-alive poller.** `Infrastructure/BackgroundJobs/HostingRefresher.cs` exists and is
  deliberately not registered; a sleeping model is absorbed inside the request budget.
- The probe logs the failure type, status and path — **never the submitted text**.
- The portals are paired to this contract: the request timeout for a moderation-checked submit is
  raised **above** the server budget (60 s vs 45 s, so the browser never gives up first), the
  composer shows the elapsed stage of the check (`src/utils/moderationState.js`), a failed check
  is worded "we could not finish the safety check" rather than as a policy violation, and the
  draft survives so retry is one keypress.

**Consequences**

*Positive*

- Unmoderated abusive content cannot enter the community, the forum or a chat room as a side
  effect of an infrastructure hiccup. For this domain that is the correct default.
- Outages are honest and typed: a 503 with a stable code, not a 500 with a stack trace, and not a
  silent success.
- Cost is bounded and predictable — no always-on timer waking a free-tier service whether or not
  anyone is typing.
- Retry-ability is preserved for the user: nothing is lost, the draft stays, the same submit
  succeeds once the model answers.

*Negative / accepted costs*

- Write availability is coupled to a free third-party host. If it is down, the community is
  read-only. Accepted: read paths, booking, chat history and notifications keep working.
- The first author after an idle period pays up to ~32–45 s and may still fail. Under a demo
  clock this is the single most likely visible defect, so **warming the model is a runbook step**
  (`RUNBOOK.md` §3), not an optimization.
- Moderation latency is user-facing on the write path; there is no async "pending review" queue
  that would decouple them.
- A model that answers but degrades (blank predictions, unreadable JSON) blocks writes too —
  which is the desired bias, but makes a model-side regression look like an API outage.
- No circuit breaker: a down dependency is rediscovered by every submit, one budget at a time.

*Revisit when*

Availability must outrank the safety guarantee. The honest options, all of which are behaviour
changes and therefore out of scope under freeze: (a) an async pending-review queue with
moderator surfacing, (b) a circuit breaker that fails open only for low-risk surfaces while chat
stays fail-closed, (c) self-hosting the model on an instance that does not sleep. Any move to
fail-open must be an explicit, signed-off product decision.
