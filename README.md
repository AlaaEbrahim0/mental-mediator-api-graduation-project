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

1. **Build** — `dotnet build --configuration Release`
2. **Test** — `dotnet test --configuration Release` (not currently wired to a test project)
3. **Publish** — Push Docker image to Docker Hub
4. **Deploy** — Trigger Render deployment via webhook

### Pipeline Steps

| Step | Command |
|------|---------|
| Restore | `dotnet restore` |
| Build | `dotnet build --configuration Release --no-restore` |
| Test | `dotnet test --configuration Release --no-build --verbosity normal` |
| Docker Login | `docker login -u $DOCKER_USERNAME -p $DOCKER_PASSWORD` |
| Docker Build & Push | `docker buildx build --push --tag systematicmess/nexus_api:latest .` |
| Render Deploy | `curl -X POST $RENDER_DEPLOY_HOOK` |

### Required GitHub Secrets

| Secret | Purpose |
|--------|---------|
| `DOCKER_USERNAME` | Docker Hub username |
| `DOCKER_PASSWORD` | Docker Hub access token |
| `RENDER_DEPLOY_HOOK` | Render deployment webhook URL |

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
