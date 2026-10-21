# Doctor Portal API contracts

All routes use the `/api` prefix. Authenticated routes require the bearer token returned by `POST /auth/signin`. The signin response includes `userId`, `roles`, `token`, and `expiresOn`; doctor portal access requires the `Doctor` role. Validation failures use `400 ValidationProblemDetails`; authorization failures use `401` or `403`; domain failures retain the API's problem-details envelope.

## Profiles

- `GET /doctors/me` returns `id`, names, email, phone number, birth date (`YYYY-MM-DD`), gender, `photoUrl`, biography, city, location, session fees, specialization, qualifications, and `isAvailable`.
- `PUT /doctors/me` accepts multipart form fields with those editable values and optional `Photo` image upload. Qualifications are limited to 2,000 characters; session fees must be nonnegative.
- Image uploads require `CLOUDINARY_URL=cloudinary://<api_key>:<api_secret>@<cloud_name>`. Without it, API routes that do not upload images remain available; upload requests return a service-unavailable error.
- The user profile contracts (`GET /users/me`, `PUT /users/me`) now carry phone number, city, and biography alongside existing birth date, gender, and photo URL fields. Phone number is the Identity `PhoneNumber` field.
- `GET /doctors` supports `specialization`, `city`, `gender`, `minFees`, `maxFees`, and `isAvailable`, in addition to name and paging. Omitted fee bounds are not applied; an explicit zero is a valid bound.

## Weekly schedule

- `GET /doctors/{doctorId}/schedule` returns `{ doctorId, weekDays }`.
- `POST /doctors/{doctorId}/schedule`, `DELETE /doctors/{doctorId}/schedule`, and the `/days` POST/PUT/DELETE routes manage weekly availability. Mutations require the Doctor role and the service verifies the route doctor id matches the signed-in doctor.
- Day records contain `dayOfWeek`, `startTime`, `endTime`, and `sessionDuration`.

## Appointments

- `POST /appointments?doctorId={id}` accepts `{ startTime, duration, location, reason, fees }`. Start time must be in the future, duration must be positive and at most eight hours, location is required (max 200 chars), reason is optional (max 1,000 chars), and fees must be nonnegative.
- Appointment responses contain ids, start/end time, duration, location, reason, fees, status, participant names/emails/photo URLs, and cancellation/rejection reasons.
- Doctors can `PUT /appointments/{id}/confirm`, `PUT /appointments/{id}/reject` with `{ rejectionReason }`, and `PUT /appointments/{id}/complete`. These actions are limited to the assigned doctor and domain-valid transitions. A client may cancel with `PUT /appointments/{id}/cancel` using the existing string request body.
- Queue reads use `GET /appointments/doctors/me` with paging, date, client name, and status filters.

## Community

- Posts are created as multipart form data at `POST /posts`, with `title`, `content` (Markdown text supported by the Portal renderer), optional `category`, comma-separated optional `tags`, `isAnonymous`, and optional `photoPost`. Category is limited to 100 characters and tags to 1,000 characters. Post responses include author association, timestamp, category/tags, comment count, `likesCount`, and `isLikedByCurrentUser`.
- `GET /posts`, `GET /posts/{id}`, and `GET /posts/user/{userId}` return the same post shape. Existing nested comment/reply routes remain `GET/POST /posts/{postId}/comments`, `PUT/DELETE /posts/{postId}/comments/{commentId}`, and corresponding `/replies` routes.
- `PUT /posts/{id}/like` adds the authenticated user's like; `DELETE /posts/{id}/like` removes it. Both are idempotent and return the updated post with count and current user's liked state. The database enforces one like per user/post pair.

## Features without an API

Patient medical history, consultation notes, chat, prescriptions, and video sessions remain marked as preview in the Doctor Portal because no corresponding backend contracts exist yet.
