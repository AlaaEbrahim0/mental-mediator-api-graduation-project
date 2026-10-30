# Mental Mediator API

> A production-ready ASP.NET Core 8 backend API for a mental health support platform. Built as a final-year graduation project with enterprise-grade architecture, security hardening, and scalable design patterns.

[![CI/CD Pipeline](https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project/actions/workflows/ci_cd_pipeline.yml/badge.svg)](https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project/actions/workflows/ci_cd_pipeline.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![Commit](https://img.shields.io/github/last-commit/AlaaEbrahim0/mental-mediator-api-graduation-project)](https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project/commits/master)

**Production API:** https://nexus-api.runasp.net/  
**Swagger UI:** https://nexus-api.runasp.net/swagger/index.html ⚠️ (currently exposed without an `IsDevelopment()` gate — see [SEC audit finding](audit/NEXUS_ENGINEERING_AUDIT.md))  

| Frontend | URL | Repository |
|----------|-----|-----------|
| Doctor portal | https://mental-mediator-doctor-ui-graduation-project.vercel.app | [mental_mediator_doctor_ui_graduation_project](https://github.com/AlaaEbrahim0/mental_mediator_doctor_ui_graduation_project) |
| Patient portal | https://mental-health-ochre-xi.vercel.app | [mental_mediator_patient_ui_graduation_project-](https://github.com/AlaaEbrahim0/mental_mediator_patient_ui_graduation_project-) |
| Shared UI | — | [nexus-shared-ui](https://github.com/AlaaEbrahim0/nexus-shared-ui) |

---

## 📋 Project Overview

Mental Mediator is a comprehensive full-stack graduation project that connects individuals struggling with depression, stress, and mental health challenges with professional doctors and a supportive community. Users can share thoughts publicly or anonymously, receive advice, book doctor appointments, and access AI-assisted mental health screening tools.

The backend is built with **ASP.NET Core 8**, following **Clean Architecture** principles, **SOLID** design patterns, **Onion Architecture** concepts, and **CQRS**-inspired separation of concerns between controllers, services, and data access layers.

### Key Highlights

| Area | Achievement |
|------|-------------|
| **Architecture** | Clean/Layered architecture with boundary-constrained dependency injection |
| **Security** | JWT + Google OAuth auth, role-based access, input validation, no hardcoded secrets, production exception handling, PBKDF2-SHA256 room passphrases (150k iterations), per-IP rate limiting on auth endpoints, Cloudinary URL allowlist preventing external media embedding |
| **Performance** | Database indexes on FK/status/search columns, projection-based repository reads, structured logging, HTTP client factory, ML service cold-start probe (`MLServiceProbe`), fixed-window rate limiters on chat/auth endpoints |
| **Reliability** | `Result<T>` pattern, global validation filter, configurable exception handler, structured logging |
| **DevOps** | GitHub Actions CI/CD pipeline, Docker multi-stage builds, MonsterASP (WebDeploy) hosting |

---

## ✨ Features

### Core Functionalities
- **Community Support** — Users post thoughts, comment, and reply; doctors and other users can advise
- **Anonymity Mode** — Posts and comments can be shared anonymously to protect privacy
- **Role-Based Access Control** — Four roles: `User`, `Doctor`, `Admin`, `Anonymous` (JWT + Google OAuth)
- **Rich Profiles** — Public doctor and user profiles with certificates, structured education, languages, interests, social links, and optional public contact details
- **Appointment System** — Doctors set weekly schedules; users book available time slots; confirm/reject/cancel with notifications
- **Real-Time Notifications** — SignalR hub for instant in-app notifications

### Machine Learning Integration
- **Hate Speech / Offensive Language Detection** — External ML service filters toxic content before publishing
- **Depression Detection Screening** — AI-powered depression risk assessment tool
- **Secure External Client Architecture** — `IHttpClientFactory` with typed clients, circuit breaker patterns, and graceful failure handling

### Admin & Insights
- **System Summary** — Dashboard with appointment stats, profit tracking, depression test result distributions
- **User & Doctor Management** — CRUD operations with audit trails
- **Reporting** — Doctors and admins can view appointment/profiling reports

### Communication & Media
- **Email Service** — Resend (primary) with MailKit SMTP fallback for confirmation, password reset, and appointment notifications
- **Image Storage** — Cloudinary API for secure photo uploads
- **News Feed** — News API integration for relevant articles
- **Template System** — Reusable HTML email templates

---

## 🏗️ Architecture

```
MentalMediator.sln
├── API/                     # Presentation Layer (ASP.NET Core Web API)
│   ├── Controllers/         # REST endpoints (16 controllers) + Validation Filter
│   ├── Configurations/      # DI, CORS, Swagger, Validation Filter, Mapping Profile
│   ├── Program.cs           # Pipeline setup, exception handling, CORS, auth
│   └── appsettings.json     # Configuration (secrets via .env / environment variables)
├── Application/             # Business Logic Layer (Domain services, DTOs, Contracts)
│   ├── Contracts/           # Service interfaces
│   ├── Community/           # Post taxonomy, visibility rules
│   ├── Dtos/                # Request/Response DTOs per domain
│   ├── Services/            # Business logic, orchestration, validation
│   └── Options/             # Configuration options (JwtSettings, Resend, etc.)
├── Domain/                  # Domain Layer (Entities, Enums, Errors, Value Objects)
│   ├── Entities/            # Core domain models (BaseUser, User, Doctor, Post, Comment, Reply, Appointment, ChatRoom, ChatMessage, Notification, DoctorSchedulingSettings, etc.)
│   ├── Enums/               # Domain enums (AppointmentStatus, DoctorSpecialization, PostVisibility, etc.)
│   ├── Errors/              # Domain error definitions
│   ├── Repositories/        # Repository interfaces
│   └── States/              # State pattern for Appointment lifecycle
├── Infrastructure/          # Data & External Services
│   ├── Data/                # AppDbContext, DbInitializer, 20 Migrations
│   ├── Configurations/      # EF Core entity configurations + indexes
│   ├── Clients/             # External service clients (ML/HateSpeech, News, Cloudinary, Resend)
│   ├── Repositories/        # EF Core implementations (RepositoryBase + 12 repos)
│   ├── Services/            # AuthService, ProfileService, ChatService, NotificationSender, CloudinaryStorageService, ResendMailService, ChatRealtimeNotifier
│   ├── Hubs/                # SignalR hub implementations
│   ├── Caching/             # InMemoryCacheService, MLServiceProbe (cold-start probe)
│   └── Utilities/           # JWT Token Generator, AuthHandoff, RoomPassphraseHasher, RoomJoinThrottle
└── Shared/                  # Shared utilities (Result<T>, Error, Paging, RequestParameters, Extensions, Enums)
```

### Design Patterns & Principles

| Pattern | Usage |
|---------|-------|
| **Clean Architecture** | Strict layered separation: API → Application → Domain → Infrastructure → Shared |
| **Dependency Injection** | `IServiceCollection` extensions, scoped repositories, lazy-loaded `IRepositoryManager` |
| **Repository Pattern** | `IRepositoryBase<T>` base class, `IRepositoryManager` for unit-of-work |
| **Unit of Work** | `IRepositoryManager.SaveAsync()` consolidates multiple repository writes |
| **State Pattern** | `Appointment` lifecycle (`Pending` → `Confirmed` → `Completed` / `Cancelled` / `Rejected`) |
| **Specification/Query Objects** | `RequestParameters` classes for pagination and filtering |
| **Result Pattern** | `Result<T>` / `Error` for explicit success/failure outcomes (no exceptions for control flow) |
| **AutoMapper** | DTO↔Entity mapping profiles |
| **SignalR** | Real-time push notifications |

---

## 🛠️ Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| **.NET SDK** | 8.0+ | Required for building and running the API |
| **SQL Server** | 2019+ | Local development or MonsterASP MSSQL hosting |
| **Git** | 2.x+ | Version control |
| **Cloudinary Account** | Optional | For media uploads |
| **News API Key** | Optional | For news integration |
| **ML Detection Services** | Optional | For hate speech & depression detection |

---

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project.git
cd mental-mediator-api-graduation-project
```

### 2. Set Up Environment Variables

```bash
# Copy the template and fill in your values
cp API/.env.example API/.env

# Edit API/.env with your actual values:
#   - ConnectionStrings (SQL Server)
#   - JwtSettings (secret key, issuer, audience)
#   - GoogleAuthentication (client ID, secret)
#   - MailSettings (SMTP credentials)
#   - NewsApi (API key)
#   - ML Services (if using external detection endpoints)
```

### 3. Restore NuGet Packages

```bash
dotnet restore
```

### 4. Apply Database Migrations

```bash
dotnet ef database update --project Infrastructure
```

### 5. Run the Application

```bash
dotnet run --project API
```

The API will be available at `https://localhost:7221` (or the port shown in output).

### 6. Verify the Build

```bash
dotnet build --configuration Release
# Should output: Build succeeded, 0 Error(s)
```

---

## 📖 API Documentation

### Swagger UI (Production)
```
https://<your-deploy-url>/swagger
```

### Key Endpoints

| Category | Endpoint | Description |
|----------|----------|-------------|
| **Auth** | `POST /api/auth/register` | Register new user |
| **Auth** | `POST /api/auth/signin` | Authenticate & receive JWT |
| **Auth** | `GET /api/auth/external-login` | Start Google OAuth (patient accounts only) |
| **Auth** | `GET /signin-google` | OAuth handler callback (validates `state`/PKCE, then continues to `external-login-callback`) |
| **Auth** | `GET /api/auth/exchange-token` | Turn a one-time Google handoff token into a JWT |
| **Posts** | `GET /api/posts` | List posts (paginated, filterable; doctors-only posts are hidden from non-doctors) |
| **Posts** | `POST /api/posts` | Create post (optional title, predefined category, tags, audience, optional media) |
| **Posts** | `POST /api/posts/media` | Upload one image or short video for the composer (`multipart/form-data`, field `File`) |
| **Comments** | `GET /api/posts/{postId}/comments` | Get post comments |
| **Comments** | `POST /api/posts/{postId}/comments` | Add comment |
| **Replies** | `GET /api/posts/{postId}/comments/{commentId}/replies` | Get replies |
| **Appointments** | `POST /api/appointments` | Book appointment |
| **Appointments** | `GET /api/appointments/clients/me` | My appointments |
| **Appointments** | `GET /api/appointments/doctors/me` | Doctor's appointments |
| **News** | `GET /api/articles` | Fetch latest articles |
| **Notification** | `GET /api/notifications/users/me` | My notifications |
| **User** | `GET /api/users/me` | Current profile |
| **Profiles** | `GET /api/profiles/{id}` | Public rich profile (anonymous access) |
| **Profiles** | `GET /api/profiles/me` | My rich profile |
| **Profiles** | `PUT /api/profiles/me` | Update profile, social links, contact info, and doctor practice details |
| **Profiles** | `PUT /api/profiles/me/photo` | Upload a profile photo (`multipart/form-data`, field `Photo`) |
| **Profiles** | `POST/PUT/DELETE /api/profiles/me/certificates` | Manage certificates (`multipart/form-data`, optional `Image`) |
| **Profiles** | `POST/PUT/DELETE /api/profiles/me/qualifications` | Manage structured qualifications (JSON) |
| **Doctor** | `GET /api/doctors/me` | My profile |
| **Schedule** | `GET /api/doctors/{doctorId}/schedule` | Weekly schedule (availability periods, time zone, consultation duration) |
| **Schedule** | `PUT /api/doctors/{doctorId}/schedule` | Update weekly schedule (Doctor role only) |
| **Schedule** | `GET /api/doctors/{doctorId}/time-off` | List time-off periods (Doctor role only) |
| **Schedule** | `POST /api/doctors/{doctorId}/time-off` | Create a time-off period blocking slots (Doctor role only) |
| **Schedule** | `DELETE /api/doctors/{doctorId}/time-off/{id}` | Remove a time-off period (Doctor role only) |
| **Schedule** | `GET /api/doctors/{id}/slots?date=...&from=...&to=...` | Available slots derived from availability + time-off + existing appointments |
| **Doctor workspace** | `GET /api/doctors/me/dashboard` / `activity` / `notes` / `follow-ups` | Doctor-specific workspace data (Doctor role only, scoped to caller) |
| **Chat** | `GET /api/chat/rooms/browse` | Search, filter and sort listable rooms |
| **Chat** | `POST /api/chat/rooms` | Create a room (patients and staff; public or private, optional passphrase) |
| **Chat** | `POST /api/chat/rooms/{id}/join` | Join a room (`{"passphrase": "..."}` only when required) |
| **Chat** | `POST /api/chat/rooms/{id}/lock` / `unlock` | Pause or resume posting (owner or doctor moderator) |
| **Chat** | `PUT /api/chat/rooms/{id}/privacy` / `passphrase` / `scheduled` | Manage room access and scheduling |
| **Chat** | `PUT/DELETE /api/chat/rooms/{id}/cover` | Upload or clear the room cover (`multipart/form-data`, field `Image`) |

#### Post composer rules

- A title is optional; the category must be one of `Clinical Case`, `General Discussion`, `Medical News`, `Patient Care` or `Platform Feedback`, and tags must come from the standard vocabulary (max 5).
- Media is uploaded one file at a time through `POST /api/posts/media` (images up to 5 MB, videos up to 20 MB) and then referenced by URL in the post `media` JSON array. Only URLs under the configured Cloudinary cloud are accepted.
- Audiences are `Public` and `DoctorsOnly`. Doctors-only posts are hidden from non-doctors in feeds, detail, likes and comments, and only doctors/admins can publish them. `SpecificGroup` is reserved and rejected until groups exist.
- Anonymous posts appear only under the Confessions filter and never expose `appUserId` or `username`; the author still receives `isOwnPost: true` so the client can offer edit/delete.

#### Profile visibility and contact details

- `GET /api/profiles/{id}` is the only anonymous profile endpoint. It returns `404` for private profiles unless the caller is the owner or an admin.
- Doctor profiles are public by default; patient/user profiles are private until their owner enables visibility.
- Public contact details (`contactEmail`, `contactPhone`, `contactAddress`) are optional, explicitly separate from the private account email and phone number, and are only returned through the profile endpoints.
- Run the profile migrations before deploying: `dotnet ef database update --project Infrastructure --startup-project API`.

### Authentication

All endpoints use **JWT Bearer Token** authentication:

```
Authorization: Bearer <your-jwt-token>
```

Role-based access:
- `User` — Can book appointments, post, comment
- `Doctor` — Can confirm appointments, view schedule, update profile
- `Admin` — Full access to all endpoints

---

## 🐳 Docker Deployment

### Build the Image

```bash
docker build -t mental-mediator-api .
```

### Run the Container

```bash
docker run -d -p 8080:8080 --env-file API/.env mental-mediator-api
```

### Multi-Stage Docker Build (Included)

The `API/Dockerfile` uses multi-stage builds to minimize image size:

1. **Build stage** — Compiles the application
2. **Publish stage** — Publishes Release binaries
3. **Runtime stage** — Runs the published app with minimal dependencies

---

## 🔄 CI/CD Pipeline

### GitHub Actions Workflow

The pipeline runs on every push to `master` and on manual `workflow_dispatch`:

1. **Build** — `dotnet restore`, `dotnet build --configuration Release --no-restore`
2. **Test** — `dotnet test --configuration Release --no-build --verbosity normal`. This runs the
   193 tests in `Tests/MentalMediator.Tests.csproj` covering booking validation, slot calculation,
   schedule mutation, profile validation, content moderation, room passphrase hashing, chat room
   privacy, notification ownership, auth security, OAuth handoff, and post composition. The
   project is registered in `MentalMediator.sln`, so solution-level `dotnet test` discovers and runs
   the full suite.
3. **Publish** — Build and push `systematicmess/nexus_api:latest` to Docker Hub
4. **Deploy** — Publish the app for IIS and upload it to MonsterASP over WebDeploy (manual or on push when `AUTO_DEPLOY_MONSTERASP` is enabled)

### Recent production fixes

| Commit | Type | Fix |
|---|---|---|
| `9043bf5` | `fix(api)` | `PostRepository`: included the `AppUser` navigation in **all four** post query sites (`GetAllPosts`, `GetConfessionOnly`, `GetPostsByUserId`, `GetPostById`) so projects no longer throw `NullReferenceException` when reading `AppUser.FullName`/`AppUser.PhotoUrl`. This fixed `GET /api/posts → 500` and `POST /api/auth/signin → 500` on the production Somee host. |

Docker Hub publishing is **not** on the deploy critical path: the live host is IIS on
MonsterASP, so a registry failure cannot take the site down.

### Pipeline Steps

| Step | Command / Input |
|------|-----------------|
| Restore | `dotnet restore` |
| Build | `dotnet build --configuration Release --no-restore` |
| Test | `dotnet test --configuration Release --no-build --verbosity normal` (193 tests in `Tests/`, registered in `MentalMediator.sln`) |
| Docker Login | `docker/login-action@v3` using `DOCKER_USERNAME` / `DOCKER_PASSWORD` |
| Docker Build & Push | `docker/build-push-action@v5` → `systematicmess/nexus_api:latest` |
| Publish for IIS | `dotnet publish API/API.csproj -c Release --output ./publish --runtime win-x86 --self-contained false -p:UseAppHost=false` |
| Production settings | Generated in the runner from secrets into `publish/appsettings.Production.json` (never committed) |
| web.config injection | `ASPNETCORE_ENVIRONMENT=Production` (+ optional `CLOUDINARY_URL`) written into the generated `publish/web.config` |
| WebDeploy | `msdeploy.exe -verb:sync` → `<publish folder>` → `/wwwroot` of MonsterASP site `site101103` |
| Verify | Polls `https://nexus-api.runasp.net/health` for HTTP 200 |

### Deploy trigger

- **Manual:** `workflow_dispatch` with the `deploy_to_production` input (first deployments are
  initiated deliberately).
- **Automatic on push to `master`:** enabled by setting the repository **variable**
  `AUTO_DEPLOY_MONSTERASP` to `true`. Until then, pushes only build and test.

### Why `win-x86` and `-p:UseAppHost=false`

Both match the host, for different reasons:

| Flag | Why |
|---|---|
| `--runtime win-x86` | MonsterASP's own deploy documentation publishes with `win-x86`, matching the 32-bit application pool provisioned on the free shared host. |
| `-p:UseAppHost=false` | With a RID specified, `dotnet publish` otherwise emits an apphost `API.exe` and a `web.config` pointing at it. A framework-dependent DLL started as `dotnet API.dll` in-process is the form MonsterASP (and shared IIS hosts generally) support. |

The pipeline fails fast if `API.exe` appears in the publish output, so a regression
surfaces as a red build rather than a site that mysteriously stops answering.

### Required GitHub Secrets

| Secret | Purpose |
|--------|---------|
| `DOCKER_USERNAME` | Docker Hub username |
| `DOCKER_PASSWORD` | Docker Hub access token |
| `MONSTERASP_MSDEPLOY_PASSWORD` | WebDeploy password from the MonsterASP **Deploy → Visual Studio access** card (user name is the site id, `site101103`) |
| `PRODUCTION_CONSTR_SOMEE` | SQL Server connection string for the hosted MonsterASP MSSQL database (the `constr_somee` key is what the app reads outside Development) |
| `PRODUCTION_JWT_KEY` | JWT signing key (must be ≥32 bytes for HS256) |
| `PRODUCTION_GOOGLE_ID` / `PRODUCTION_GOOGLE_SECRET` | Google OAuth client credentials |
| `PRODUCTION_MAIL_USERNAME` / `PRODUCTION_MAIL_PASSWORD` | Gmail SMTP credentials — must be an **app password**, not the account password |
| `PRODUCTION_NEWS_API_KEY` | News API key |
| `PRODUCTION_CLOUDINARY_URL` ⚠️ **not configured** | `cloudinary://<key>:<secret>@<cloud>` — injected into the generated `web.config` so image uploads work on the host |

`PRODUCTION_CLOUDINARY_URL` is the only optional secret, and it is **currently absent from the
repository's secrets**, so image uploads on `nexus-api.runasp.net` return the controlled 503 until it
is added (`gh secret set PRODUCTION_CLOUDINARY_URL`). Without it the API starts and everything except
uploads works. The setting is read from the process environment only
(`Infrastructure/Services/StorageService.cs`), so the pipeline writes it, together with
`ASPNETCORE_ENVIRONMENT=Production`, into the generated `publish/web.config`
`<environmentVariables>` block.

Gmail rejects plain account passwords with `534 5.7.9 Please log in with your web browser`.
`MailSettings:SenderName` is optional and non-secret; when set it becomes the display name on the
`From` header.

### Repository variables

| Variable | Purpose |
|----------|---------|
| `PRODUCTION_SITE_URL` | Overrides the default production URL (`https://nexus-api.runasp.net`) used by the deploy verification probe |
| `AUTO_DEPLOY_MONSTERASP` (`true`/`false`) | When `true`, a push to `master` deploys to production; when `false` (or unset), production deploys happen only via `workflow_dispatch` |

Every secret is passed through `env:` and read with `$env:NAME`. Secrets are never
inlined into a PowerShell script body, and `appsettings.Production.json` is generated in
the runner — it is gitignored and never committed.

### Hosting Constraints (MonsterASP free plan)

| Constraint | Value | Consequence |
|---|---|---|
| Storage | 5 GB per website | The framework-dependent publish output is ~30 MB, far below the limit; the pipeline still fails above 120 MB as a bloat check. |
| Database | 1× MSSQL, 1 GB | Managed via WebMSSQL / SSMS. Keep data lean. |
| RAM | 256 MB dedicated | In-process hosting only — with in-process IIS, no separate process is spawned. |
| WebSockets | Supported | SignalR uses its default WebSocket transport with fallbacks. |
| HTTPS | Let's Encrypt on `*.runasp.net` subdomains | Free subdomains are served over TLS automatically, so `RequireHttps=true` is safe (see below). |
| Activity | Free sites need periodic visits | Check the plan terms; an untouched free site is suspended or deleted. |

### HTTPS

MonsterASP serves `*.runasp.net` subdomains over TLS, so production
`appsettings.Production.json` sets `RequireHttps=true`, which enables
`UseHttpsRedirection` for the public site. HSTS is deliberately still not enabled: it is
only safe behind a certificate we control end-to-end (it advertises
`Strict-Transport-Security` at the HTTP layer, and a lapsed certificate would then leave
browsers unable to reach the site at all).

### Secondary hosting (Somee free plan)

The API is also deployed to Somee (`nexus-api-gp.somee.com`) as a free-tier fallback:

| Constraint | Value | Consequence |
|---|---|---|
| Architecture | 32-bit app pool | The API must be published with `win-x86` (matches the WebDeploy pipeline). |
| Storage | 150 MB per site | Total across published files, logs, and DataProtection keys. |
| Database | SQL Server Express, 30 MB data-file limit (7.7 MB used, 19 tables) | Migrations must be schema-only; data must stay lean. |
| HTTPS | Not available on free subdomains | Plain HTTP only; JWT tokens transit in cleartext. Use the primary RunASP endpoint for any sensitive traffic. |
| Memory | 256 MB | SignalR and large query results may cause OOM in high-concurrency scenarios. |

> Somee has 18 of 20 migrations applied. The `/api/posts` endpoint currently returns HTTP 500 due to schema drift (the `Posts` table on Somee is behind the committed migration `AddPostAudienceAndMedia`). Run `dotnet ef database update` or the appropriate ALTER statements to reconcile before relying on Somee as a production endpoint.

### Deploy Readiness Checklist

If the site stops answering, check these in order:

1. **Did the pipeline actually deploy?** A failed deploy leaves the previous upload in
   place. Confirm the *Deploy to MonsterASP.net* job is green, not just the build.
2. **Is the deploy secret current?** A missing or stale `MONSTERASP_MSDEPLOY_PASSWORD`
   fails with an HTTP 401/login-refused from `msdeploy.axd`. The password lives on the
   Control Panel **Deploy → Visual Studio access** card; WebDeploy must be *Enabled* there.
3. **Did the files land in `/wwwroot`?** WebDeploy uploads the entire publish folder into
   `/wwwroot` of site `site101103`; files outside that folder are never served.
4. **Is the app pool still 32-bit?** The build is `win-x86`; a 64-bit pool will not load
   it (`500.32 ANCM Failed to Load dll`). Control Panel → Website → detailed settings.
5. **Did the app pool start?** A missing .NET 8 ASP.NET Core hosting bundle leaves the
   pool stopped and IIS answers before the app runs.
6. **Is `appsettings.Production.json` present** with a populated
   `ConnectionStrings:constr_somee`? A blank value makes the app start but fail on the
   first database call.
7. **Do the runtime environment variables exist in `web.config`?** The pipeline writes
   `ASPNETCORE_ENVIRONMENT=Production` (and optionally `CLOUDINARY_URL`) into the
   generated web.config; a hand-edited web.config that lost them drops the app back into
   Development defaults and breaks image uploads.

### The `Server` header tells you which layer failed

| Header | Layer that answered | Meaning |
|---|---|---|
| `Microsoft-HTTPAPI/2.0` | kernel `http.sys` | The app pool never started. The app's own logs are empty. |
| `Microsoft-IIS/10.0` | IIS worker process | A worker process is alive and processing requests. |
| `Kestrel` / none | the app | The app ran and produced the error — read its logs. |

Because `http.sys` answers before user mode, a site in this state returns 503 for
**every** request, including static files such as `/robots.txt` that IIS serves
without the application. Probe a static file to confirm:

```bash
curl -o /dev/null -w "%{http_code}\n" https://nexus-api.runasp.net/robots.txt
```

IIS error pages carry an accurate status code on MonsterASP, but a 200 on an arbitrary
URL is still not proof the app is up: IIS can serve content before the application ever
runs. That is why the pipeline probes `/health` first: it is unauthenticated, performs no
database round-trip and calls no external service, so a 200 means the app is genuinely
running.

---

## 📦 Database

### Entity Framework Core

- **Migrations** are stored in `Infrastructure/Migrations/`
- **Reverse engineering** available via `dotnet ef dbcontext scaffold`
- **Seeding** runs through `scripts/seed.sql` (run manually from WebMSSQL / SSMS — the application only applies migrations at startup, and only when the database exists)

### Demo dataset (`scripts/seed.sql`)

A curated, fictional dataset for demos and portfolio review: five doctors with
distinct specialties, fees, consultation lengths and weekly availability (one of
them deliberately closed to new patients), five patients, appointments covering
every `AppointmentStatus`, certificates and qualifications, community posts with
comments, replies and likes, four chat rooms (public, private and archived) with
members, messages and one open report, and notifications in both read and unread
states.

- **It refuses to run against production.** The first statement aborts if `AspNetUsers`
  already holds any account outside the reserved `@example.com` domain. Run it only
  against a fresh or dedicated demo database.
- **It is idempotent.** Every insert is guarded with `NOT EXISTS` on a natural key, so
  running it twice never duplicates anything, and it only ever updates rows belonging
  to the demo accounts.
- **It does not go stale.** Appointments are anchored to the weekday the script is run
  in and every relative timestamp is re-derived, so upcoming sessions always sit in the
  coming week. Slot times are placed on each doctor's real availability grid
  (period start + *n* × consultation length), so the booking screens agree with the data.
- Demo logins and the shared demo password are listed in the header of the script.
  They are for demo databases only and must never be reused for a real deployment.
- The script is not executed by CI and is never run automatically by the application.

### Key Entity Relationships

```
User 1─────── N Appointment 1───── 1 Doctor
User 1───── N Post 1───── N Comment 1───── N Reply
User 1───── N Notification
User 1───── N PostLike
Doctor 1───── N Appointment
Doctor 1───── N DoctorAvailabilityPeriod   (weekly working windows)
Doctor 1───── N DoctorTimeOff              (UTC ranges that block slots)
Doctor 1───── 1 DoctorSchedulingSettings   (time zone + consultation length)
Doctor 1───── N PatientNote                (doctor workspace notes)
Doctor 1───── N FollowUp                   (scheduled follow-ups)
Post 1───── N Comment
Comment 1───── N Reply
ChatRoom 1───── N ChatRoomMember 1───── N ChatMessage
ChatRoom 1───── N ChatMessageReport        (moderation reports on messages)
Patient 1─── N ChatRoomMember 1─── N ChatMessage
```

### Indexes Added for Performance

| Table | Columns |
||-------|---------|
|| `Appointments` | `StartTime`, `DoctorId`, `UserId`, `Status`, Composite `(DoctorId, StartTime, Status)` |
|| `Posts` | `PostedOn`, `AppUserId`, `IsAnonymous` |
|| `Comments` | `PostId`, `AppUserId` |
|| `Replies` | `CommentId`, `AppUserId` |
|| `Notifications` | `AppUserId`, `IsRead`, Composite `(AppUserId, IsRead)` |
|| `Doctors` | `Specialization`, `City` |
|| `ChatRooms` | Composite `(Visibility, IsArchived, LastMessageAtUtc)`, `(IsArchived, IsLocked, LastMessageAtUtc)`, `ScheduledStartAtUtc` (filtered) |
|| `ChatRoomMembers` | `UserId`, `RoomId` (foreign keys) |
|| `ChatMessages` | `RoomId`, `SenderId`, `SentAt` |
|| `ChatMessageReports` | `MessageId`, `Status`, `ReportedById` |
|| `PostLikes` | Unique `(UserId, PostId)` |

---

## 🧠 Content moderation and cold starts

Every post, comment, reply and chat message is checked by the hate-speech model
hosted at `MLServerAddress` **before** it is saved. That service sleeps when idle, so
its first request after a quiet period can take tens of seconds — measured at ~32 s on
the deployed instance, versus ~0.5 s once warm.

`Infrastructure/Clients/MLServiceProbe.cs` owns that behaviour for all three callers
(`HateSpeechDetectorClient`, `ChatContentModerator`, `DepressionDetectorClient`):

| Setting | Default | Meaning |
|---------|---------|---------|
| `MLServer__TimeoutSeconds` | 45 | Total budget for one moderation attempt sequence, also applied as the per-request `HttpClient.Timeout`. Wide enough to absorb a cold start. |
| `MLServer__MaxRetries` | 2 | Extra attempts, spent only on failures that answer quickly — a transport error or 502/503/504, which is what the host returns while a container is starting. A timed-out attempt is never retried because the budget is already spent. |
| `MLServer__RetryBackoffSeconds` | 1 | Base backoff, doubling per attempt, and skipped when it would push past the deadline. |

The rules that follow from this:

- **No answer is never treated as a safe answer.** Timeout, unreachable, non-2xx, an
  unreadable body or a blank prediction all return a controlled
  `ServiceUnavailable` result, the content stays unpublished, and the caller retries.
  Before this, the community path let `TaskCanceledException` escape and the browser
  received an HTTP 500 with a real stack trace.
- **Deterministic rejections are not retried.** A validation error returns
  `Content.Forbidden` once, as before.
- The probe logs the failure type and status, never the submitted text.
- There is deliberately **no keep-alive poller** for the model service
  (`Infrastructure/BackgroundJobs/HostingRefresher.cs` is dormant). Waking it on a timer
  spends resources whether or not anyone is typing; the budget above absorbs the wake.
- The model exposes no health or readiness endpoint (`/health`, `/ping` and `/` all
  return 404 or an error), so there is nothing cheaper to probe than the prediction
  itself.

The portals pair with this: the request timeout for a moderation-checked submission is
raised above the server budget, the composer shows the elapsed stage of the check
(`src/utils/moderationState.js`), a failed check is worded as "we could not finish the
safety check" rather than as a policy violation, and the draft survives so retry is one
keypress. See `Tests/ModerationClientTests.cs` for the behaviours covered.

---

## 🔒 Security Notes

| Measure | Status |
|---------|--------|
| Secrets in `appsettings.json` | ❌ Removed — now in `.env` / environment variables |
| Global exception handling | ✅ Added (`UseExceptionHandler` + `/error` endpoint) |
| Production error details | ✅ Disabled (`EnableDetailedErrors` only in Development) |
| Sensitive data logging | ✅ Disabled in production |
| Input validation | ✅ Global `ValidateModelStateFilter` applied |
| Role-based auth | ✅ JWT + Google OAuth + claims extraction |
| Database indexes | ✅ Added on FK/status/search columns |
| Secure HTTP client usage | ✅ `IHttpClientFactory` with `using` disposal |
| Email service | ✅ Resend (primary) with MailKit SMTP fallback; structured logging; no secret fields in errors |

### Known security issues (tracked for remediation)

| Issue | Status | Tracking |
|---|---|---|
| Swagger UI exposed in production (no `IsDevelopment()` gate) | Open | `Program.cs:78-80` |
| JWT bearer token stored in `localStorage` (XSS token theft) | Open | both portals' auth contexts |
| No refresh token; 30-min JWT expiry with no renewal | Open | `JwtTokenGenerator.cs` |
| CORS allowlist includes unverified third-party origins | Open | `DependencyInjection.cs:51-57` |
| `RepositoryBase.FindByCondition`/`FindAll` inverted `trackChanges` logic | Open | `RepositoryBase.cs:20-37` |
| `InitializeDatabaseAsync().Wait()` synchronous startup | Open | `Program.cs:126` |
| `GetDoctorStats` sums fees for all statuses (incl. cancelled/rejected) | Open | `AppointmentRepository.cs:325-326` |
| `GetDoctorStats`/`GetDoctorAppointmentCounts` load full history into memory | Open | `AppointmentRepository.cs:319-329`, `339-365` |
| `PopulateLikeState` N+1: one DB query per post in a listing | Open | `PostService.cs:347-352` |
| `ResendMailService` passes `CancellationToken.None` | Open | `ResendMailService.cs:62` |
| `output.txt` debug artifact tracked in git | Open | Repo root |

A full audit report is available at `audit/NEXUS_ENGINEERING_AUDIT.md`.

### Known integration limitations

- **Room passphrases are hashed with PBKDF2-SHA256 (150k iterations, per-room salt) and never leave
  the server.** `ChatRoomResponse` exposes `requiresPassword` as a boolean and nothing else; a
  reflection test (`ChatRoomPrivacyTests`) fails the build if a response property could carry a hash.
  Five wrong attempts per account and room block further guesses for 15 minutes and return the same
  `Chat.RoomPassphraseIncorrect` error as a wrong guess, so a caller cannot distinguish "wrong" from
  "throttled". Private rooms are deliberately listable — name, cover, member count and owner badge
  are public, the description and conversation are member-only. The seeded `Visibility=1` room in
  `scripts/seed.sql` therefore becomes visible at deploy time.
- **Google sign-in is wired end to end on the patient portal; the doctor portal is password-only by
  design.** The flow is `GET /api/auth/external-login` → Google → the OAuth handler's own
  `GET /signin-google` → `GET /api/auth/external-login-callback` → `GET /api/auth/exchange-token`.
  `patient-ui` starts it from its sign-in screen and consumes `?temporaryToken=` in
  `src/auth/ExternalLoginGate.js`, which writes the session
  before the router's first render. Every account created through Google gets the `User` role, so a
  clinician or administrator is refused with `?authError=role_unsupported` before any token is
  minted: the patient shell mounts the notifications provider (and a SignalR connection) for
  whatever token it holds, and `NotificationController` has no role filter. Two operator-side
  requirements:
  1. The authorized redirect URI in Google Cloud Console must be
     `https://nexus-api.runasp.net/signin-google`. Earlier text here said
     `/api/auth/external-login-callback`; registering that guarantees `redirect_uri_mismatch`,
     because `redirect_uri` is built from the handler's `CallbackPath`, not from our controller
     route. Add the same path per environment (locally `http://localhost:5184/signin-google`, and
     `https://<origin>/signin-google` for any other host that starts the flow).
  2. The consent screen must stay **Published**. In "Testing" Google only signs in allow-listed test
     users; the `email` scope is also sensitive, so an unverified app shows its "Google hasn't
     verified this app" interstitial.
  Two limits of the current account model, both measured rather than assumed:
  * A database that has never run `scripts/seed.sql` has no `User` role. Identity *throws* for an
    unknown role name, so sign-up checks for the role first and answers
    `?authError=account_incomplete` while deleting the half-created account — no 500 in the middle
    of the redirect, no role-less user left behind.
  * `BaseUser` derives `UserName` from the email's local part and `NormalizedUserName` is unique, so
    `ali@gmail.com` cannot be created by Google once `ali@outlook.com` exists: the sign-up answers
    `?authError=identity_taken` instead of silently reusing the other person's account. Making the
    external identity the key (or storing the full address in `UserName`) is the real fix and is not
    part of this flow.
  Two things this rules out. A Vercel **preview** deployment cannot finish the flow: its random
  preview origin is not in `ConfigureCors`, so the exchange call is blocked by the browser — verify
  against the fixed production alias (or add the preview origin temporarily). And locally, the
  correlation cookie is issued `secure; samesite=none` even over plain HTTP, so the callback rejects
  it and the browser sees "Correlation failed"; run the API under its https profile
  (`https://localhost:7235`) when testing sign-in.
- **The Google handoff requires a single API instance.** The one-time `temporaryToken` lives in
  `IMemoryCache`, which is process-local: with more than one instance the browser can be routed to a
  second one that has never heard of the token, and every sign-in fails at the exchange. The
  WebDeploy's `-enableRule:AppOffline` also drops the cache on each release, so a handoff in flight
  across a deploy is dead and the user gets `?authError=expired` — rare, and recoverable by pressing
  the button again. Moving the handoff to `IDistributedCache` (Redis) is the fix if the app ever
  scales out.
- **`JwtSettings:Audience` differs per environment and must not be "corrected".** Production issues
  and validates tokens with `https://mental-mediator-doctor-ui-graduation-project.vercel.app` (the
  pipeline's generated settings); the committed `appsettings.json` now carries the patient portal
  origin for local runs. Because `ValidateAudience = true` and issuance reads the same value, both
  work, but changing a deployed audience invalidates every already-issued token and signs everyone
  out. Treat it as a coordinated sign-out event, not a typo fix.
- **Email delivery depends on a valid Gmail app password.** `MailService` reports a missing
  `MailSettings` value, a message-construction failure, and a provider rejection as three distinct
  errors, and logs the offending setting name server-side only. A successful `SendAsync` means the
  SMTP provider accepted the message, not that it reached an inbox.
- **Image uploads need `CLOUDINARY_URL`.** Cloudinary is the only `IStorageService` implementation;
  no local-filesystem provider exists. Replacing a photo leaves the previous asset in Cloudinary —
  deletion was intentionally not implemented, since removing stored files needs explicit approval and
  the interface exposes no delete operation.

---

## 📫 Contact

For inquiries, feedback, or collaboration opportunities:

- **Email:** [alaaebrahim387@gmail.com](mailto:alaaebrahim387@gmail.com)
- **GitHub:** [@AlaaEbrahim0](https://github.com/AlaaEbrahim0)
- **Portfolio:** [mental-mediator](https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project)

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE.txt](LICENSE.txt) file for details.

---

## 🙏 Acknowledgments

Built as a **final-year undergraduate project** in Computer Science. Special thanks to:
- **Faculty advisors** for guidance and mentorship
- **Open-source communities** — ASP.NET Core, EF Core, SignalR, Cloudinary, MailKit, Bogus, AutoMapper
- **External service providers** — Google OAuth, Google SMTP, News API, Cloudinary, ML detection services
- **GitHub Actions** for CI/CD and **MonsterASP** for production hosting (WebDeploy)
