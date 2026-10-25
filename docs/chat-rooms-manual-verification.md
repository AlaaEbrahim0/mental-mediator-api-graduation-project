# Chat Rooms — M3/M4 Manual Verification and API Contract

Scope: REST API, application services, and SignalR delivery. The doctor UI,
patient UI, seed data, migration application, commits, and deployment are **not**
part of M3/M4.

> **Do not run this matrix against a production database.** The API's startup
> initializer applies pending migrations when the database is reachable. Use a
> disposable, non-production database for every check below.

The migration `20261024001703_AddChatRooms` is additive and remains **unapplied**.

---

## 1. Build check

```bash
cd api
dotnet build MentalMediator.sln --no-restore
```

Expected: 0 errors. The four known pre-existing warnings are acceptable:
AutoMapper `NU1903` (twice), MailKit `NU1902`, and `CS8604` in
`AppointmentRepository.cs:285`.

---

## 2. Roles and visibility rules

| Rule | Enforcement |
| --- | --- |
| Only authenticated, non-deleted users can use chat | `[Authorize]` plus `UserManager.FindByIdAsync` in `ChatService` |
| Only Doctor or Admin accounts create rooms | Controller role filter plus DB-role check in `ChatService` |
| Patients cannot create rooms | Both checks above |
| Public rooms only in v1 | Repository filters `Visibility == Public`; private rooms return 404 |
| Any active member can read messages and member lists | Membership check; non-members get 404 |
| Patients see a stable random per-room alias | Stored on `ChatRoomMember.DisplayName`; never derived from user id |
| Doctors see real name plus Doctor badge | `ChatRoomMember.DisplayName` + `IsDoctor` |
| Only the owner and Doctor moderators remove messages | Membership role plus current DB Doctor role |
| Patients can report messages, not moderate them | Report endpoint is member-only; moderator endpoints require Owner/Doctor |
| Owner cannot leave; archive instead | `LeaveRoomAsync` returns 409 |
| Messages are immutable and only soft-deleted | No edit endpoint; delete sets `IsDeleted`, `DeletedAtUtc`, `DeletedById` |

The database role is authoritative. A room membership's `IsDoctor` is a display
snapshot; moderator authorization re-checks the account's current `Doctor` role.
Removing the `Doctor` role revokes doctor-moderator privileges immediately, but it
does not rewrite historical message display snapshots. Room ownership is a
room-level role and is not revoked or transferred by a global role change (see
known limits). Coordinate role changes with a data cleanup if a room must also
stop showing a former doctor's real name on old messages.

---

## 3. Endpoint inventory

All routes require a bearer token.

| Method | Route | Who | Success |
| --- | --- | --- | --- |
| `GET` | `/api/chat/rooms?beforeRoomId=&limit=` | Any authenticated user | 200 `ChatRoomPageResponse` |
| `POST` | `/api/chat/rooms` | Doctor/Admin | 201 `ChatRoomResponse` |
| `GET` | `/api/chat/rooms/{roomId}` | Any authenticated user (archived rooms: active members only) | 200 `ChatRoomResponse` |
| `POST` | `/api/chat/rooms/{roomId}/join` | Any authenticated user | 200 `ChatRoomMemberResponse` |
| `POST` | `/api/chat/rooms/{roomId}/leave` | Active member (owner blocked) | 204 |
| `GET` | `/api/chat/rooms/{roomId}/members` | Active member | 200 `ChatRoomMemberResponse[]` |
| `GET` | `/api/chat/rooms/{roomId}/messages?beforeId=&afterId=&limit=` | Active member | 200 `ChatMessagePageResponse` |
| `POST` | `/api/chat/rooms/{roomId}/messages` | Active member | 200 `ChatMessageResponse` (idempotent) |
| `DELETE` | `/api/chat/messages/{messageId}` | Sender, owner, or Doctor moderator | 200 `ChatMessageResponse` |
| `POST` | `/api/chat/messages/{messageId}/reports` | Active member, not the sender | 200 `ChatMessageReportResponse` |
| `GET` | `/api/chat/rooms/{roomId}/reports?status=&limit=` | Owner or Doctor moderator | 200 `ChatMessageReportResponse[]` |
| `PATCH` | `/api/chat/rooms/{roomId}/reports/{reportId}` | Owner or Doctor moderator | 200 `ChatMessageReportResponse` |
| `PATCH` | `/api/chat/rooms/{roomId}/members/{memberId}/role` | Owner | 200 `ChatRoomMemberResponse` |
| `POST` | `/api/chat/rooms/{roomId}/archive` | Owner | 200 `ChatRoomResponse` |

Rate limits, partitioned by authenticated user id:

| Policy | Endpoint | Limit |
| --- | --- | --- |
| `chat-messages` | `POST /rooms/{roomId}/messages` | 30 per minute |
| `chat-room-creation` | `POST /rooms` | 5 per hour |
| `chat-reports` | `POST /messages/{messageId}/reports` | 10 per hour |

A rejected request returns `429` with `Retry-After` and a `ProblemDetails` body.

---

## 4. Client contract and privacy rules

### Request bodies

```json
// POST /api/chat/rooms
{ "name": "Anxiety support", "description": "A calm, moderated space." }

// POST /api/chat/rooms/{roomId}/messages
{ "content": "Hello", "clientMessageId": "5f0c2d5e-5f4a-4d33-9d1f-0e6c8c1d2a11" }

// POST /api/chat/messages/{messageId}/reports
{ "reason": "This message is harassing another member." }

// PATCH /api/chat/rooms/{roomId}/members/{memberId}/role
{ "role": "Moderator" }

// PATCH /api/chat/rooms/{roomId}/reports/{reportId}
{ "status": "Reviewed" }
```

Enums are serialized as strings: `Public`, `Member`, `Moderator`, `Owner`, `Open`,
`Reviewed`, `Dismissed`, `Actioned`.

Room names reject all control characters. Descriptions and message content may
contain line breaks and tabs, but no other control characters.

### Representative response shapes

```json
// ChatRoomResponse
{
  "id": 12,
  "name": "Anxiety support",
  "description": "A calm, moderated space.",
  "visibility": "Public",
  "createdAtUtc": "2026-10-24T00:17:03Z",
  "lastMessageAtUtc": "2026-10-24T00:20:11Z",
  "isArchived": false,
  "memberCount": 8,
  "isMember": true,
  "myRole": "Member",
  "myMemberId": 44,
  "lastMessagePreview": "Welcome everyone"
}
```

```json
// ChatRoomPageResponse
{
  "rooms": [ /* ChatRoomResponse[] */ ],
  "hasMore": true,
  "nextBeforeRoomId": 9
}
```

```json
// ChatRoomMemberResponse
{
  "memberId": 44,
  "displayName": "Patient 7A19C2B4E6F0",
  "isDoctor": false,
  "role": "Member",
  "joinedAtUtc": "2026-10-24T00:19:02Z"
}
```

```json
// ChatMessageResponse
{
  "id": 901,
  "roomId": 12,
  "senderMemberId": 44,
  "clientMessageId": "5f0c2d5e-5f4a-4d33-9d1f-0e6c8c1d2a11",
  "senderDisplayName": "Patient 7A19C2B4E6F0",
  "senderIsDoctor": false,
  "content": "Hello",
  "sentAtUtc": "2026-10-24T00:20:11Z",
  "isDeleted": false
}
```

```json
// ChatMessagePageResponse
{
  "messages": [ /* ChatMessageResponse[] */ ],
  "hasMore": true,
  "nextBeforeId": 881,
  "nextAfterId": null
}
```

```json
// ChatMessageReportResponse (moderator view)
{
  "id": 7,
  "messageId": 901,
  "reason": "This message is harassing another member.",
  "status": "Open",
  "createdAtUtc": "2026-10-24T00:21:00Z",
  "reviewedAtUtc": null,
  "message": { /* ChatMessageResponse */ }
}
```

### Response rules

- Chat DTOs **never expose global user ids**. `SenderMemberId` and `MemberId` are
  per-room integer ids. `CreatedById`, `SenderId`, `ReporterId`, and `ReviewedById`
  are not returned.
- `ChatRoomResponse.LastMessagePreview` is `null` for non-members and for left
  members. Room metadata is browsable, but conversation content is not.
- `ChatMessageResponse.Content` is empty when `IsDeleted` is true. Render a neutral
  “message removed” placeholder; do not attempt to recover deleted content.
- `SenderDisplayName`, `SenderIsDoctor`, and `SenderMemberId` are stored on the
  message when it is sent. Later room-role changes do not rewrite the identity
  shown on older messages.
- `ClientMessageId` is the sender's random idempotency key. It is not a user id
  and is safe to use for optimistic-message reconciliation and deduplication.
- Moderator report responses may include the original content of a deleted message
  when `IsDeleted` is true. This is intentional for report review and must only be
  shown in moderator views.
- `ChatMessageReportResponse` does not expose the reporter's identity.
- A sender who has left the room may still soft-delete their own message. This is
  the only message operation available to a former member; attempts to read or
  delete anyone else's message return 404.
- `ChatMessagePageResponse.NextAfterId` supports reconnect recovery. Request
  `?afterId=<lastSeenId>` to fetch messages newer than the last one the client saw.
- All timestamps are UTC.
- Render message content as text, not HTML. The API validates length and runs the
  moderation model, but it does not sanitize HTML.

### Idempotent send

`clientMessageId` is required and unique per sender. Repeating the same id with the
same room and content returns the existing message without calling the moderation
model again. Reusing it with different content or a different room returns `409`.

### Real-time contract (M4)

Hub URL: `/chat-hub`. Authentication uses the same JWT in the `access_token` query
string. Query tokens are accepted only on `/chat-hub` and `/notification-hub`; REST
continues to require `Authorization: Bearer`.

The hub is **receive-only**. Send, delete, report, join, leave, role, and archive
commands stay on the REST endpoints. This keeps validation, moderation, and the
`chat-messages` rate limit on one path.

Client methods:

| Method | Returns | Behavior |
| --- | --- | --- |
| `SubscribeToRoom(roomId)` | `ChatHubResult<ChatRoomResponse>` | Requires an active membership; adds this connection to `chat-room-{roomId}` and returns the current room snapshot. Non-members get `Chat.RoomNotFound`. |
| `UnsubscribeFromRoom(roomId)` | `ChatHubResult` | Stops live delivery on this connection; membership is unchanged. |

Server events:

| Event | Payload | When |
| --- | --- | --- |
| `MessageCreated` | `ChatMessageResponse` | A message was persisted. |
| `MessageDeleted` | `ChatMessageResponse` | A message was soft-deleted; `isDeleted=true`, content empty. |
| `MemberUpdated` | `ChatRoomMemberResponse` | A member joined, rejoined, or had a role/display change. |
| `MemberLeft` | per-room `memberId` (integer) | A member left; the leaver's connections are removed from the group first. |
| `RoomArchived` | `ChatRoomResponse` | The owner archived the room. |

Example subscribe result:

```json
{
  "success": true,
  "data": { /* ChatRoomResponse */ },
  "code": null,
  "message": null
}
```

Example failure:

```json
{
  "success": false,
  "data": null,
  "code": "Chat.RoomNotFound",
  "message": "Chat room was not found."
}
```

Delivery and reconnect:

1. Connect with a valid token.
2. For each open room, call `SubscribeToRoom`.
3. Load or recover history from REST.
4. On reconnect, repeat step 2 for every open room, then recover with
   `GET /api/chat/rooms/{roomId}/messages?afterId=<lastSeenId>&limit=100` until
   `hasMore=false`.
5. Deduplicate by `id`; use `clientMessageId` to reconcile the sender's optimistic
   message with the REST response or broadcast. Subscribe before recovering so a
   message that arrives during recovery is delivered live and then deduplicated
   from the recovery page.

`POST /leave` removes all of that user's live connections from the room group
before broadcasting `MemberLeft`. `UnsubscribeFromRoom` only stops live delivery;
it does not leave the membership.

Broadcast failures are logged without content and never fail the REST command.
The persisted change is recoverable through the history endpoints.

---

## 5. Manual verification matrix

Prepare at least:

- `doctorA` — Doctor account, room owner
- `doctorB` — Doctor account, regular member
- `patientA` — User account
- `patientB` — User account
- `adminA` — Admin account
- A disposable non-production database with the chat migration applied **only** there

Use `Authorization: Bearer <token>` on every request.

### 5.1 Authentication and room creation

| # | Action | Expected |
| --- | --- | --- |
| 1 | `GET /api/chat/rooms` with no token | 401 |
| 2 | `POST /api/chat/rooms` as `patientA` | 403 |
| 3 | `POST /api/chat/rooms` as `doctorA` | 201; `myRole=Owner`, `isMember=true`, `memberCount=1`; owner display is the doctor's real name and `isDoctor=true` |
| 4 | `POST /api/chat/rooms` as `adminA` | 201; owner display is `Staff`, `isDoctor=false`, `myRole=Owner` |
| 5 | `GET /api/chat/rooms` as `patientA` | 200; created rooms appear; `isMember=false`, `myMemberId=null`, `lastMessagePreview=null` |
| 6 | `GET /api/chat/rooms/{roomId}` as `patientA` before joining | 200 room metadata; no preview |
| 7 | `GET /api/chat/rooms/{roomId}/messages` as `patientA` before joining | 404 (non-member) |

### 5.2 Membership and aliases

| # | Action | Expected |
| --- | --- | --- |
| 8 | `POST /api/chat/rooms/{roomId}/join` as `patientA` | 200; `role=Member`, `isDoctor=false`, `displayName` starts with `Patient ` |
| 9 | Repeat join as `patientA` | 200; same `memberId` and same alias |
| 10 | `POST /leave` as `patientA`, then `GET /messages` | 204, then 404 |
| 11 | Rejoin as `patientA` | 200; same `memberId` and same alias |
| 12 | Join as `doctorB` | 200; real name, `isDoctor=true`, `role=Member` |
| 13 | Join as `adminA` | 200; display `Staff`, `isDoctor=false` |
| 14 | `GET /members` as `patientA` | 200; integer member ids only; no global user ids |
| 15 | `GET /members` as a non-member | 404 |
| 16 | `POST /leave` as the owner | 409 |

### 5.3 Sending, validation, and idempotency

| # | Action | Expected |
| --- | --- | --- |
| 17 | Send as `patientA` | 200; `senderMemberId` equals own member id; alias; no global ids |
| 18 | Send as `doctorB` | 200; real name and `senderIsDoctor=true` |
| 19 | Send empty/whitespace content | 400 validation |
| 20 | Send 2001 characters | 400 validation |
| 21 | Send without `clientMessageId` | 400 validation |
| 22 | Send same `clientMessageId` and content twice | Both 200; same message id; no duplicate row |
| 23 | Reuse the same `clientMessageId` with different content | 409 |
| 24 | Send as a non-member | 404 |
| 25 | Send to an archived room | 409 |

### 5.4 Moderation

| # | Action | Expected |
| --- | --- | --- |
| 26 | Send content the model classifies as hate speech | 400 `Chat.ContentRejected`; no message row |
| 27 | Start API with `MLServerAddress` pointing at an unreachable host, then send | 503 `Chat.ModerationUnavailable`; no message row |
| 28 | Restore the ML address and send | 200 |
| 29 | Inspect API logs for message content | No message content is logged |

For #27, use an environment override on a disposable instance, for example:

```bash
ASPNETCORE_ENVIRONMENT=Development \
MLServerAddress=http://127.0.0.1:1 \
dotnet run --project API --no-launch-profile
```

> Development note: the existing `appsettings.Development.json` enables EF Core
> sensitive-data logging and SQL logging. That can write SQL parameter values,
> including message content, to the console. It is pre-existing and must not be
> enabled in production. The chat service and moderation client do not log
> content. Run #29 and #72 in Production, or disable sensitive-data logging for
> the test instance.

### 5.5 Soft delete

The sender delete path is ownership-based and still works after the sender leaves
the room. A non-member who is not the sender gets 404 for any message operation.

| # | Action | Expected |
| --- | --- | --- |
| 30 | Sender deletes own message | 200; `isDeleted=true`, `content=""` |
| 31 | Repeat sender delete | 200; still deleted; no error |
| 32 | `patientB` tries to delete `patientA`'s message | 403 |
| 33 | Non-member tries to delete another member's message | 404 |
| 34 | Owner deletes another member's message | 200; soft-deleted |
| 35 | Promote `doctorB` to `Moderator`, then delete as `doctorB` | 200 |
| 36 | Fetch history after deletion | Deleted rows remain in order with empty content |
| 37 | Remove `Doctor` role from a moderator in the database, keep the room membership, then delete as that user | 403; current DB role is authoritative; older messages keep their send-time display snapshot |

### 5.6 Reports and moderation workflow

| # | Action | Expected |
| --- | --- | --- |
| 38 | `patientB` reports `patientA`'s message | 200; no reporter id in response |
| 39 | Report own message | 400 |
| 40 | Report the same message again as `patientB` | 409 |
| 41 | Report a deleted message | 400 |
| 42 | Non-member reports | 404 |
| 43 | `patientB` lists room reports | 403 |
| 44 | Owner lists reports | 200; reason and message content visible |
| 45 | `doctorB` (after promotion) lists reports | 200 |
| 46 | Owner sets report status to `Reviewed` | 200; `reviewedAtUtc` set |
| 47 | `patientB` tries to set report status | 403 |
| 48 | Set report status to `Open` | 400 validation |
| 49 | Report a message, then sender deletes it, then owner lists reports | Report remains; moderator response still includes original content with `isDeleted=true` |

### 5.7 Roles and archive

| # | Action | Expected |
| --- | --- | --- |
| 50 | Owner promotes `doctorB` to `Moderator` | 200; role updated; real name and Doctor badge in the member list; older messages keep their send-time snapshot |
| 51 | Owner tries to promote `patientA` | 400 `Chat.ModeratorMustBeDoctor` |
| 52 | `patientA` tries to promote anyone | 403 |
| 53 | Owner tries to change own role | 409 |
| 54 | Owner archives the room | 200; `isArchived=true` |
| 55 | `GET /api/chat/rooms` | Archived room is absent |
| 56 | Send to archived room | 409 |
| 57 | Join archived room | 409 |
| 58 | Active member reads archived room history | 200 |
| 59 | Non-owner member archives | 403 |
| 60 | Non-member archives | 404 |
| 61 | Repeat archive as owner | 200; idempotent |

### 5.8 Pagination and recovery

| # | Action | Expected |
| --- | --- | --- |
| 62 | Seed more than `limit` messages | — |
| 63 | `GET /messages?limit=20` | Newest 20, ascending; `hasMore=true`; `nextBeforeId` set |
| 64 | `GET /messages?beforeId={nextBeforeId}&limit=20` | Older page; no overlap; `nextBeforeId` advances |
| 65 | `GET /messages?afterId={lastSeenId}&limit=20` | Messages newer than `lastSeenId`; `nextAfterId` set when more remain |
| 66 | Send both `beforeId` and `afterId` | 400 `Chat.InvalidMessagePagination` |
| 67 | `limit=1000` | Clamped to 100 for messages, 50 for rooms |

### 5.9 Rate limits

These limits use the in-process fixed-window limiter, so they apply per API
instance. That matches the current single-instance SignalR assumption. A
multi-instance deployment needs a distributed limiter.

Use fresh tokens so previous test traffic does not consume the window.

| # | Action | Expected |
| --- | --- | --- |
| 68 | Send 30 messages in one minute as one user, then a 31st | 31st returns 429 with `Retry-After` |
| 69 | Create 5 rooms in one hour as one doctor, then a 6th | 6th returns 429 |
| 70 | Submit 10 reports in one hour as one user, then an 11th | 11th returns 429 |

### 5.10 Deleted accounts and logging

| # | Action | Expected |
| --- | --- | --- |
| 71 | Soft-delete a user in the disposable DB, then use a still-valid token | 403 `Chat.UserNotAvailable` |
| 72 | Search API logs for message bodies, report reasons, and aliases | No content or report text is logged |

### 5.11 Real-time delivery (M4)

Use at least two SignalR clients and the same disposable database. The full matrix
cannot be exercised against an unreachable database.

| # | Action | Expected |
| --- | --- | --- |
| M4-1 | `POST /chat-hub/negotiate?negotiateVersion=1` with no token | 401 |
| M4-2 | Negotiate with an invalid token | 401 |
| M4-3 | Negotiate with a valid token in `Authorization` | 200 |
| M4-4 | Negotiate with the same token in `access_token` | 200 |
| M4-5 | `GET /api/chat/rooms?access_token=<valid>` | 401; query tokens are not accepted on REST |
| M4-6 | Connect with a valid token that has no `uid` claim | Connection is aborted |
| M4-7 | `SubscribeToRoom` as an active member | Success; room snapshot; connection receives room events |
| M4-8 | `SubscribeToRoom` as a non-member | `Chat.RoomNotFound`; no events |
| M4-9 | `SubscribeToRoom` for a private or nonexistent room | `Chat.RoomNotFound` |
| M4-10 | `SubscribeToRoom` for an archived room as an active member | Success; `isArchived=true`; no new messages accepted |
| M4-11 | `patientA` sends via REST while `patientB` is subscribed | `patientB` receives `MessageCreated` once with the persisted id |
| M4-12 | Sender receives the REST response and the hub event | One message after deduplication by `id`/`clientMessageId` |
| M4-13 | Sender or moderator deletes a message | Subscribed members receive `MessageDeleted`; content is empty |
| M4-14 | `patientB` joins via REST | Subscribed members receive `MemberUpdated` |
| M4-15 | `patientB` leaves via REST | Subscribed members receive `MemberLeft`; `patientB`'s sockets stop receiving |
| M4-16 | Owner promotes `doctorB` | Subscribed members receive `MemberUpdated` |
| M4-17 | Owner archives the room | Subscribed members receive `RoomArchived` with `isArchived=true` |
| M4-18 | Reconnect after being offline | Re-subscribe plus `afterId` recovery; no gap or duplicate |
| M4-19 | Offline while more than 100 messages arrive | Recovery loops until `hasMore=false` |
| M4-20 | A message is deleted while offline | Recovery includes it with `isDeleted=true` |
| M4-21 | Same user has two tabs, then leaves via one | Both tabs are removed from the group |
| M4-22 | `UnsubscribeFromRoom` | No further events on that connection; membership remains |
| M4-23 | `/notification-hub` negotiate with a valid token | Still 200 (regression) |
| M4-24 | Force a broadcast failure, then send via REST | REST still succeeds; message is in history; error is logged without content |

---

## 6. Known limits for v1

- Rate limits are in-process and therefore per API instance. This matches the
  current single-instance SignalR design; a multi-instance deployment needs a
  distributed limiter.
- Soft-deleting an account blocks all chat actions, but does not mark the
  account's room memberships as left. Historical messages remain by design; a
  future account-deletion workflow should decide whether active memberships
  should also be closed.
- There is no ownership transfer or global admin override. If an owner account
  is deleted, doctor moderators can still remove messages and review reports,
  but the room cannot be archived or have roles changed until a future admin
  tool is added.
- Room ownership is room-scoped. Removing the owner's global Doctor/Admin role
  does not transfer or revoke ownership; only doctor-moderator privileges follow
  the current database role. An ownership-transfer/admin tool is a future item.
- Room metadata is restricted to doctor/admin creation and is length and
  control-character validated, but it is not sent through the hate-speech model.
  Only message content is moderated in M3/M4.
- The report list returns the newest `limit` reports (maximum 100) for the
  requested status and has no cursor pagination yet. Extend it before report
  volume can exceed one page.
- SignalR groups and the connection registry are process-local. The current
  design assumes one API instance; a multi-instance deployment needs a
  backplane and a shared registry.
- SignalR authenticates at handshake. An already-connected socket is not
  revalidated when the JWT expires or the account is soft-deleted. The current
  JWT duration is 30 days; shorter access tokens with refresh, or a server-side
  connection-revocation mechanism, are future hardening items.
- The hub uses a query-string access token. Production request logging must stay
  at `Warning` so the token is not written to logs.
- The hub is receive-only. Message sends stay on the REST endpoint so the
  `chat-messages` rate limit and moderation path cannot be bypassed.
- `SubscribeToRoom` and `UnsubscribeFromRoom` are not rate-limited. They require
  an active membership and do not write data, but a per-connection throttle is a
  future hardening item.
- A broadcast failure does not roll back a persisted change. Clients recover
  through the REST history endpoints.
- Report notifications, presence, typing indicators, and unread counts are not
  implemented.
- There is no data export or erasure workflow; retention is indefinite by design.

## 7. Out of scope for M3/M4

- Doctor UI Community tabs (M5).
- Patient UI (blocked until the patient frontend repository is available).
- Hub-based message sending (receive-only by design).
- Report notifications, presence, typing indicators, and unread counts.
- Redis/backplane and multi-instance SignalR.
- Seed data.
- Applying the migration to any shared or production database.
- Commits, pushes, merges, CI/CD changes, and deployment.
