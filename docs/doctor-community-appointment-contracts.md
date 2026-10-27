# Doctor Portal API contracts

All routes use the `/api` prefix. Authenticated routes require the bearer token returned by `POST /auth/signin`. The signin response includes `userId`, `roles`, `token`, and `expiresOn`; doctor portal access requires the `Doctor` role. Validation failures use `400 ValidationProblemDetails`; authorization failures use `401` or `403`; domain failures retain the API's problem-details envelope.

On startup, the API creates the `User`, `Doctor`, and `Admin` Identity roles if missing. To bootstrap an administrator, set both `AdminSeed__Email` and `AdminSeed__Password` in the local `.env` before starting the API. Optional profile values use `AdminSeed__FirstName`, `AdminSeed__LastName`, `AdminSeed__BirthDate`, and `AdminSeed__Gender`. The bootstrap account is email-confirmed automatically and seeding is idempotent. Public registration only accepts `User` or `Doctor` roles; do not configure a default admin password in source control.

## Profiles

- `GET /doctors/me` returns `id`, names, email, phone number, birth date (`YYYY-MM-DD`), gender, `photoUrl`, biography, city, location, session fees, specialization, qualifications, and `isAvailable`.
- `PUT /doctors/me` accepts multipart form fields with those editable values and optional `Photo` image upload. Qualifications are limited to 2,000 characters; session fees must be nonnegative.
- Image uploads require `CLOUDINARY_URL=cloudinary://<api_key>:<api_secret>@<cloud_name>`. Without it, API routes that do not upload images remain available; upload requests return a service-unavailable error.
- The user profile contracts (`GET /users/me`, `PUT /users/me`) now carry phone number, city, and biography alongside existing birth date, gender, and photo URL fields. Phone number is the Identity `PhoneNumber` field.
- `GET /doctors` supports `specialization`, `city`, `gender`, `minFees`, `maxFees`, and `isAvailable`, in addition to name and paging. Omitted fee bounds are not applied; an explicit zero is a valid bound.

## Scheduling

Scheduling has exactly two doctor-managed inputs: weekly availability periods and time off. Bookable slots are never stored; they are calculated on every request from:

1. the weekly periods, interpreted in the doctor's configured time zone;
2. minus time-off ranges;
3. minus `Pending` and `Confirmed` appointments.

The doctor also chooses one consultation duration: `30`, `45`, or `60` minutes (default `30`). The backend owns this value. A request may send a duration for compatibility, but it is accepted only when it equals the configured duration; otherwise booking fails with `Scheduling.DurationMismatch`. The stored duration and fee always come from the doctor's configuration, not the request. Existing appointments keep the start, end, and duration they were booked with.

### Weekly availability

- `GET /doctors/{doctorId}/schedule` returns `{ doctorId, timeZoneId, consultationDurationMinutes, periods }`. Any authenticated user can read it.
- `PUT /doctors/{doctorId}/schedule` replaces the whole weekly configuration. It requires the `Doctor` role and the route id must match the signed-in doctor.
- Request body:

```json
{
  "timeZoneId": "Africa/Cairo",
  "consultationDurationMinutes": 45,
  "periods": [
    { "dayOfWeek": "Monday", "startTime": "09:00:00", "endTime": "12:00:00" },
    { "dayOfWeek": "Monday", "startTime": "13:00:00", "endTime": "17:00:00" }
  ]
}
```

- A weekday may have several periods. Two periods on the same weekday create unbookable time between them, so lunch and similar breaks need no separate rule. Periods for the same weekday must not overlap or be duplicated; end must be after start and before midnight. Send an empty `periods` array for a fully unavailable week.
- `timeZoneId` must be an id known to the server; weekly times and time-off requests are interpreted in it, while appointments and slot instants are stored and returned in UTC.
- A schedule change is rejected with `409` when an active (`Pending` or `Confirmed`) appointment that has not ended would fall outside the new periods. The response lists the conflicting appointment ids. Reschedule or cancel them first; the schedule is not partially saved.
- The save is serialized per doctor with booking, so a concurrent booking cannot slip into a gap created by a schedule change.

### Time off

- `GET /doctors/{doctorId}/time-off?from=YYYY-MM-DD&to=YYYY-MM-DD` lists time off in the doctor's local dates. The range is optional and both bounds must be supplied together. Only the owning doctor can read it.
- `POST /doctors/{doctorId}/time-off` creates a range:

```json
{
  "date": "2026-11-03",
  "endDate": "2026-11-05",
  "isFullDay": false,
  "startTime": "12:00:00",
  "endTime": "14:00:00",
  "reason": "Conference"
}
```

- `date` is the first local date and `endDate` is the optional inclusive last local date. Omit `endDate` (or send the same date) for a single day. Responses always include the resolved `endDate`.
- With `isFullDay: true`, `startTime` and `endTime` are ignored and every local calendar date from `date` through `endDate` is blocked. Otherwise both times are required and apply to the first and last date; the whole continuous interval between them is blocked. For example, the request above blocks 12:00 on 3 November through 14:00 on 5 November. The range must exist in the doctor's time zone (a daylight-saving gap is rejected).
- A single-date partial range must end after it starts. A multi-date range may end at an earlier wall-clock time on the last date (for example 17:00 Monday through 09:00 Wednesday).
- Overlapping time off is rejected with `409 Scheduling.TimeOffOverlap`.
- Time off that overlaps an active appointment is rejected with `409 Scheduling.AppointmentConflict` and the conflicting appointment ids. Confirmed and pending appointments are never silently invalidated; the response states that the time off was not added.
- `DELETE /doctors/{doctorId}/time-off/{id}` removes a range, which makes the affected slots bookable again.

### Slots

- `GET /doctors/{doctorId}/slots?date=YYYY-MM-DD` returns one local date. Use `?from=YYYY-MM-DD&to=YYYY-MM-DD` for a range; do not combine `date` with `from`/`to`. A range is limited to 62 days, and the request is authenticated.
- Response:

```json
{
  "doctorId": "doctor-1",
  "timeZoneId": "Africa/Cairo",
  "consultationDurationMinutes": 45,
  "days": [
    {
      "date": "2026-11-03",
      "slots": [
        {
          "date": "2026-11-03",
          "startTime": "2026-11-03T07:00:00Z",
          "endTime": "2026-11-03T07:45:00Z",
          "durationMinutes": 45
        }
      ]
    }
  ]
}
```

- `startTime` and `endTime` are UTC instants. Render them in the returned `timeZoneId` for the doctor's wall-clock times. `days` is always present, including for a day with no slots (`slots: []`). Slots outside the weekly periods, in a time-off range, in the past, or overlapping an active appointment are absent.
- Slots are half-open intervals: `[start, end)`. They never cross a period boundary. Ties are broken by starting each period at its configured start and stepping by the consultation duration.
- The slot list and the booking gate use the same calculation. Booking a time that is not offered fails rather than creating an out-of-schedule appointment.

### Booking a slot (patient contract)

There is no patient-facing booking application in this repository. A patient client should implement this flow:

1. Authenticate and obtain a bearer token.
2. Call `GET /doctors/{id}/slots?from=...&to=...` and render each slot in the doctor's `timeZoneId`. Do not recalculate slots on the client.
3. On selection, call `POST /appointments?doctorId={id}` with the slot's UTC `startTime` and a `location`:

```json
{
  "startTime": "2026-11-03T07:00:00Z",
  "location": "Online",
  "reason": "First consultation"
}
```

4. Do not send `fees`; it is ignored. A `duration` is optional and, if sent, must match `consultationDurationMinutes` exactly.
5. A successful response is a `Pending` appointment. The doctor still confirms or rejects it through the existing workflow.
6. On `409`, refresh the slot list before asking the patient to choose again. Another patient may have taken the slot.

`POST /appointments?doctorId={id}` requires authentication and a `location` (max 200 chars). The appointment is persisted only after the doctor's current availability has been revalidated while holding a per-doctor lock, so concurrent requests cannot double-book the same slot. The fee is copied from the doctor profile. Cancelling or rejecting an appointment frees its slot; completing it does not reserve future time.

## Appointments

- `POST /appointments?doctorId={id}` accepts `{ startTime, duration?, location, reason?, fees? }`. Start time must be in the future, location is required (max 200 chars), and reason is optional (max 1,000 chars). `duration` is optional and must equal the doctor's configured consultation duration when supplied; the server otherwise overrides it. `fees` is ignored and the fee comes from the doctor profile.
- Appointment responses contain ids, start/end time, duration, location, reason, fees, status, participant names/emails/photo URLs, and cancellation/rejection reasons.
- Doctors can `PUT /appointments/{id}/confirm`, `PUT /appointments/{id}/reject` with `{ rejectionReason }`, and `PUT /appointments/{id}/complete`. These actions are limited to the assigned doctor and domain-valid transitions. A client may cancel with `PUT /appointments/{id}/cancel` using the existing string request body.
- Queue reads use `GET /appointments/doctors/me` with paging, date, client name, and status filters.

## Community

- Posts are created as multipart form data at `POST /posts`, with `title`, `content` (Markdown text supported by the Portal renderer), optional `category`, comma-separated optional `tags`, `isAnonymous`, and optional `photoPost`. Category is limited to 100 characters and tags to 1,000 characters. Post responses include author association, timestamp, category/tags, comment count, `likesCount`, and `isLikedByCurrentUser`.
- `GET /posts`, `GET /posts/{id}`, and `GET /posts/user/{userId}` return the same post shape. Existing nested comment/reply routes remain `GET/POST /posts/{postId}/comments`, `PUT/DELETE /posts/{postId}/comments/{commentId}`, and corresponding `/replies` routes.
- `PUT /posts/{id}/like` adds the authenticated user's like; `DELETE /posts/{id}/like` removes it. Both are idempotent and return the updated post with count and current user's liked state. The database enforces one like per user/post pair.

## Features without an API

Patient medical history, consultation notes, chat, prescriptions, and video sessions remain marked as preview in the Doctor Portal because no corresponding backend contracts exist yet.

## Scheduling migration notes

The `SimplifyDoctorScheduling` migration moves data from the previous scheduling model. Review these limitations before applying it to an existing database:

- Legacy `ScheduleWeekDays` rows become weekly `DoctorAvailabilityPeriods`. Active recurring breaks are subtracted at minute granularity, so a 09:00–17:00 day with a 12:00–13:00 break becomes 09:00–12:00 and 13:00–17:00. Inactive breaks are ignored.
- Legacy `DoctorTimeBlocks` that are active and have a positive range become `DoctorTimeOff`. Cancelled blocks and empty ranges are not migrated; block type and audit metadata are dropped.
- Legacy per-date `DoctorAvailabilityOverrides` have no equivalent in the two-input model and are **dropped**, not guessed at. Recreate exceptional dates as time off or by editing the weekly periods before relying on availability after the migration.
- Legacy arbitrary `SessionDuration` values are not migrated. `ConsultationDurationMinutes` is added with a default of `30`; doctors who offered 45- or 60-minute sessions must select the new duration after migrating.
- The migration does not cancel or reschedule any appointment. If an existing appointment falls outside the migrated periods, the schedule editor will report it as a conflict the next time the doctor saves.
- A database backup is required before applying this migration. The rollback is best-effort: multiple periods collapse back to one row per weekday and gaps between periods cannot be represented in the old schema.
