# Mental Mediator API

> A production-ready ASP.NET Core 8 backend API for a mental health support platform. Built as a final-year graduation project with enterprise-grade architecture, security hardening, and scalable design patterns.

[![CI/CD Pipeline](https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project/actions/workflows/ci_cd_pipeline.yml/badge.svg)](https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project/actions/workflows/ci_cd_pipeline.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![Commit](https://img.shields.io/github/last-commit/AlaaEbrahim0/mental-mediator-api-graduation-project)](https://github.com/AlaaEbrahim0/mental-mediator-api-graduation-project/commits/master)

---

## 📋 Project Overview

Mental Mediator is a comprehensive full-stack graduation project that connects individuals struggling with depression, stress, and mental health challenges with professional doctors and a supportive community. Users can share thoughts publicly or anonymously, receive advice, book doctor appointments, and access AI-assisted mental health screening tools.

The backend is built with **ASP.NET Core 8**, following **Clean Architecture** principles, **SOLID** design patterns, **Onion Architecture** concepts, and **CQRS**-inspired separation of concerns between controllers, services, and data access layers.

### Key Highlights

| Area | Achievement |
|------|-------------|
| **Architecture** | Clean/Layered architecture with boundary-constrained dependency injection |
| **Security** | JWT + Google OAuth auth, role-based access, input validation, no hardcoded secrets, production exception handling |
| **Performance** | Database indexes on FK/status columns, lazy-loaded repository pattern, structured logging, HTTP client factory |
| **Reliability** | `Result<T>` pattern, global validation filter, configurable exception handler, structured logging |
| **DevOps** | GitHub Actions CI/CD pipeline, Docker multi-stage builds, Render deployment, Somee hosting support |

---

## ✨ Features

### Core Functionalities
- **Community Support** — Users post thoughts, comment, and reply; doctors and other users can advise
- **Anonymity Mode** — Posts and comments can be shared anonymously to protect privacy
- **Role-Based Access Control** — Four roles: `User`, `Doctor`, `Admin`, `Anonymous` (JWT + Google OAuth)
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
- **Email Service** — MailKit with Google SMTP for confirmation, password reset, and appointment notifications
- **Image Storage** — Cloudinary API for secure photo uploads
- **News Feed** — News API integration for relevant articles
- **Template System** — Reusable HTML email templates

---

## 🏗️ Architecture

```
MentalMediator.sln
├── API/                     # Presentation Layer (ASP.NET Core Web API)
│   ├── Controllers/         # REST endpoints (8 controllers) + Validation Filter
│   ├── Configurations/      # DI, CORS, Swagger, Validation Filter, Mapping Profile
│   ├── Program.cs           # Pipeline setup, exception handling, CORS, auth
│   └── appsettings.json     # Configuration (secrets via .env / environment variables)
├── Application/             # Business Logic Layer (Domain services, DTOs, Contracts)
│   ├── Contracts/           # Service interfaces
│   ├── Dtos/                # Request/Response DTOs per domain
│   ├── Services/            # Business logic, orchestration, validation
│   └── Options/             # Configuration options
├── Domain/                  # Domain Layer (Entities, Enums, Errors, Value Objects)
│   ├── Entities/            # Core domain models (User, Doctor, Post, Appointment, etc.)
│   ├── Enums/               # Domain enums (AppointmentStatus, DoctorSpecialization, etc.)
│   ├── Errors/              # Domain error definitions
│   ├── Repositories/        # Repository interfaces
│   └── States/              # State pattern for Appointment lifecycle
├── Infrastructure/          # Data & External Services
│   ├── Data/                # AppDbContext, DbInitializer, Migrations
│   ├── Configurations/      # EF Core entity configurations + indexes
│   ├── Clients/             # External service clients (ML, News, Cloudinary)
│   ├── Repositories/        # EF Core implementations
│   ├── Services/            # AuthService, MailService, StorageService, NotificationSender
│   └── Utilities/           # JWT Token Generator
└── Shared/                  # Shared utilities (Result<T>, Paging, Errors)
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
| **SQL Server** | 2019+ | Local development or somee hosting |
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
| **Auth** | `GET /api/auth/external-login` | Start Google OAuth |
| **Posts** | `GET /api/posts` | List posts (paginated, filterable) |
| **Posts** | `POST /api/posts` | Create post (with optional photo) |
| **Comments** | `GET /api/posts/{postId}/comments` | Get post comments |
| **Comments** | `POST /api/posts/{postId}/comments` | Add comment |
| **Replies** | `GET /api/posts/{postId}/comments/{commentId}/replies` | Get replies |
| **Appointments** | `POST /api/appointments` | Book appointment |
| **Appointments** | `GET /api/appointments/clients/me` | My appointments |
| **Appointments** | `GET /api/appointments/doctors/me` | Doctor's appointments |
| **News** | `GET /api/articles` | Fetch latest articles |
| **Notification** | `GET /api/notifications/users/me` | My notifications |
| **User** | `GET /api/users/me` | Current profile |
| **Doctor** | `GET /api/doctors/me` | My profile |
| **Schedule** | `GET /api/doctors/{doctorId}/schedule` | Weekly schedule |

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

The pipeline runs on every push to `master`:

1. **Build** — `dotnet restore`, `dotnet build --configuration Release --no-restore`
2. **Test** — `dotnet test --configuration Release --no-build --verbosity normal` (no test project yet; the step is a no-op that must keep passing)
3. **Publish** — Build and push `systematicmess/nexus_api:latest` to Docker Hub
3. **Deploy** — Publish the app for IIS and upload it to Somee over FTP

### Recent production fixes

| Commit | Type | Fix |
|---|---|---|
| `9043bf5` | `fix(api)` | `PostRepository`: included the `AppUser` navigation in **all four** post query sites (`GetAllPosts`, `GetConfessionOnly`, `GetPostsByUserId`, `GetPostById`) so projects no longer throw `NullReferenceException` when reading `AppUser.FullName`/`AppUser.PhotoUrl`. This fixed `GET /api/posts → 500` and `POST /api/auth/signin → 500` on the production Somee host. |

Docker Hub publishing is **not** on the deploy critical path: the live host is IIS on
Somee, so a registry failure cannot take the site down.

### Pipeline Steps

| Step | Command / Input |
|------|-----------------|
| Restore | `dotnet restore` |
| Build | `dotnet build --configuration Release --no-restore` |
| Test | `dotnet test --configuration Release --no-build --verbosity normal` |
| Docker Login | `docker/login-action@v3` using `DOCKER_USERNAME` / `DOCKER_PASSWORD` |
| Docker Build & Push | `docker/build-push-action@v5` → `systematicmess/nexus_api:latest` |
| Publish for IIS | `dotnet publish API/API.csproj -c Release --output ./publish --runtime win-x86 --self-contained false -p:UseAppHost=false` |
| Production settings | Generated in the runner from secrets into `publish/appsettings.Production.json` (never committed) |
| FTP Deploy | `SamKirkland/FTP-Deploy-Action@v4.3.5` → `/www.nexus-api-gp.somee.com/` |
| Verify | Polls `http://nexus-api-gp.somee.com/health` for HTTP 200 |

### Why `win-x86` and `-p:UseAppHost=false`

Both are hard requirements on this host, for different reasons:

| Flag | Why |
|---|---|
| `--runtime win-x86` | The Somee application pool is configured for **32-bit**. A framework-dependent app is AnyCPU and only loads in a 64-bit pool, so the build must match the pool as provisioned. |
| `-p:UseAppHost=false` | With a RID specified, `dotnet publish` otherwise emits an apphost `API.exe` and a `web.config` pointing at it. A custom EXE runtime reads as a website *launching a separate program*, which shared-host security watchdogs treat as an abuse signal and suspend the site for. The standard `dotnet API.dll` startup is the supported form in both hosting models. |

The pipeline fails fast if `API.exe` appears in the publish output, so a regression
surfaces as a red build rather than a site that mysteriously stops answering.

### Required GitHub Secrets

| Secret | Purpose |
|--------|---------|
| `DOCKER_USERNAME` | Docker Hub username |
| `DOCKER_PASSWORD` | Docker Hub access token |
| `SOMEE_FTP_SERVER` | Somee FTP host (`ftp://nexus-api-gp.somee.com`) |
| `SOMEE_FTP_USERNAME` | Somee FTP username |
| `SOMEE_FTP_PASSWORD` | Somee FTP password (the control-panel password) |
| `PRODUCTION_CONSTR_SOMEE` | SQL Server connection string for the hosted database |
| `PRODUCTION_JWT_KEY` | JWT signing key (must be ≥32 bytes for HS256) |
| `PRODUCTION_GOOGLE_ID` / `PRODUCTION_GOOGLE_SECRET` | Google OAuth client credentials |
| `PRODUCTION_MAIL_USERNAME` / `PRODUCTION_MAIL_PASSWORD` | Gmail SMTP credentials |
| `PRODUCTION_NEWS_API_KEY` | News API key |

Every secret is passed through `env:` and read with `$env:NAME`. Secrets are never
inlined into a PowerShell script body, and `appsettings.Production.json` is generated in
the runner — it is gitignored and never committed.

### Hosting Constraints (Somee free plan)

| Constraint | Value | Consequence |
|---|---|---|
| Storage | 150 MB | The publish output is ~30 MB. The pipeline fails above 120 MB. |
| Database | MS SQL Express, 30 MB data + 30 MB log | Keep seeded data lean; a full rebuild may not fit. |
| Bandwidth | 5 GB / month | External calls (ML, News) count against it. |
| WebSockets | **Not supported** | SignalR falls back to Server-Sent Events and long polling. The app already uses `AddSignalR()` with no explicit WebSocket transport, so no change was needed. |
| SSL | Free Let's Encrypt, **manual renewal** | The site is served over plain HTTP. `RequireHttps` is set to `false` for this reason — see below. |
| Ads | Injected by the host | The host appends an ad to responses; source files are untouched. |
| Activity | Must be visited at least 5×/month | An untouched free site is deleted. |

### HTTPS is opt-in

`UseHttpsRedirection` is **not** called unconditionally. Somee's free plan does not
auto-issue a certificate for `*.somee.com` subdomains (only paid plans renew
automatically), so an unconditional redirect bounced every request into a loop and the
site never answered. HTTPS is enabled by setting `RequireHttps=true` in
`appsettings.Production.json` — do this only once a certificate is actually installed.

HSTS is likewise not enabled, for the same reason: it would advertise
`Strict-Transport-Security` over plain HTTP and browsers would refuse to connect.

### Deploy Readiness Checklist

If the site stops answering, check these in order:

1. **Did the pipeline actually deploy?** A failed deploy leaves the previous upload in
   place. Confirm the *Deploy to Somee* job is green, not just the build.
2. **Do the FTP secrets exist and are they current?** A missing `SOMEE_FTP_SERVER`
   fails the deploy with `Input required and not supplied: server`. Secrets are read at
   run start, so editing one does **not** retry the in-flight run.
3. **Did the files land in the right folder?** The FTP path is
   `/www.nexus-api-gp.somee.com/` — that subdirectory *is* the site root. Files uploaded
   one level up are outside the site and are never served.
4. **Is the app pool still 32-bit?** The build is `win-x86`; a 64-bit pool will not load
   it (`500.32 ANCM Failed to Load dll`). Control Panel → IIS Application pool →
   Application mode must be `32 bit`.
5. **Did the app pool start?** A missing .NET 8 ASP.NET Core hosting bundle leaves the
   pool stopped and IIS answers before the app runs.
6. **Is `appsettings.Production.json` present** with a populated
   `ConnectionStrings:constr_somee`? A blank value makes the app start but fail on the
   first database call.

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
curl -o /dev/null -w "%{http_code}\n" http://nexus-api-gp.somee.com/robots.txt
```

Note that Somee serves plain HTML error pages with a **200** status for several failure
modes, so a 200 on an arbitrary URL is not proof the app is up. That is why the
pipeline probes `/health` first: it is unauthenticated, performs no database
round-trip and calls no external service, so a 200 means the app is genuinely running.

---

## 📦 Database

### Entity Framework Core

- **Migrations** are stored in `Infrastructure/Migrations/`
- **Reverse engineering** available via `dotnet ef dbcontext scaffold`
- **Seeding** uses the Bogus library for test data insertion

### Key Entity Relationships

```
User 1─────── N Appointment 1───── 1 Doctor
User 1───── N Post 1───── N Comment 1───── N Reply
User 1───── N Notification
Doctor 1───── N Appointment
Doctor 1───── N DoctorScheduleWeekDay
Post 1───── N Comment
Comment 1───── N Reply
```

### Indexes Added for Performance

| Table | Columns |
|-------|---------|
| `Appointments` | `StartTime`, `DoctorId`, `UserId`, `Status`, Composite `(DoctorId, StartTime, Status)` |
| `Posts` | `PostedOn`, `AppUserId`, `IsAnonymous` |
| `Comments` | `PostId`, `AppUserId` |
| `Replies` | `CommentId`, `AppUserId` |
| `Notifications` | `AppUserId`, `IsRead`, Composite `(AppUserId, IsRead)` |
| `Doctors` | `Specialization`, `City` |

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
| Email service | ✅ MailKit with structured logging |

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
- **GitHub Actions** and **Render** for hosting and CI/CD infrastructure
