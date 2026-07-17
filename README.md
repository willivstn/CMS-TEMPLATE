# CMS Enterprise Template

A production-ready, reusable Content Management System template for building enterprise
ASP.NET Core applications. Built on **.NET 10 (LTS)** following **Clean Architecture**,
**SOLID**, and permission-based security.

> **Status:** feature-complete across all delivery phases (0–8). The solution builds
> clean, the test suite is green, and the app runs with an admin UI, REST API, and a
> sample module. See the [Delivery Roadmap](#delivery-roadmap) and the guides in
> [`docs/`](docs/): [database schema](docs/database-schema.md),
> [extending / adding a module](docs/extending.md), [deployment](docs/deployment.md).

## Technology Stack

| Area | Choice |
|------|--------|
| Runtime | .NET 10 (LTS) |
| Web | ASP.NET Core MVC, Razor, SignalR |
| Data | EF Core 10, SQL Server |
| Identity | ASP.NET Core Identity + permission-based authorization |
| Application | MediatR (CQRS), FluentValidation, Mapperly (source-gen mapping) |
| Background jobs | Hangfire |
| Logging | Serilog (structured) |
| Front-end | Bootstrap 5, vanilla JS |
| Testing | xUnit, integration tests, Playwright |

## Solution Structure

```
CMS.slnx
├── src/
│   ├── CMS.Domain          # Entities, value objects, domain events. No outward dependencies.
│   ├── CMS.Shared          # Result<T>, Error, Guard, cross-cutting primitives.
│   ├── CMS.Application      # CQRS handlers, interfaces, DTOs, validators, mapping. Depends on Domain + Shared.
│   ├── CMS.Infrastructure   # EF Core, Identity, email, storage, caching, jobs. Implements Application interfaces.
│   ├── CMS.Contracts        # Versioned public API request/response models.
│   └── CMS.Web              # MVC + Razor + SignalR + REST API. Composition root.
└── tests/
    ├── CMS.UnitTests
    ├── CMS.IntegrationTests
    └── CMS.UiTests          # Playwright
```

**Dependency rule:** dependencies point inward only. `Domain` depends on nothing;
`Application` on `Domain`/`Shared`; `Infrastructure` and `Web` on the layers below.
Concrete implementations are bound to abstractions in the Web composition root
(`Program.cs` → `AddApplication()` + `AddInfrastructure()`).

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB, Express, or full) — *required from Phase 1 onward*

### Build & run
```bash
dotnet build CMS.slnx
dotnet run --project src/CMS.Web
```

### Run tests
```bash
dotnet test CMS.slnx
```

## Conventions

- **Central Package Management** — all NuGet versions live in `Directory.Packages.props`.
- **Shared build settings** — `Directory.Build.props` (nullable, analyzers, XML docs on).
- **Code style** — enforced by `.editorconfig` (file-scoped namespaces, `_camelCase` fields).
- **Error handling** — services return `Result` / `Result<T>` instead of throwing for
  expected failures.

## Delivery Roadmap

- [x] **Phase 0** — Solution skeleton, base primitives, DI wiring, green build.
- [x] **Phase 1** — Domain model, EF Core config, migrations, seed data.
- [x] **Phase 2** — Identity authentication + permission-based authorization.
- [x] **Phase 3** — Admin shell UI, dynamic menu, breadcrumbs, reusable components.
- [x] **Phase 4** — User / Role / Permission management.
- [x] **Phase 5** — CMS content (pages, posts, versioning, workflow) + Media Library.
- [x] **Phase 6** — SignalR, background jobs, email, settings, caching, logging.
- [x] **Phase 7** — REST API (JWT, Swagger, versioning, rate limiting) + security hardening.
- [x] **Phase 8** — Sample module (Announcements), Docker/CI, documentation, tests.

## REST API

A versioned REST API lives under `/api/v1` with Swagger UI at `/swagger` (Development).
Authenticate with a JWT (`POST /api/v1/auth/login`) or an `X-Api-Key` header. See the
in-app Swagger docs for the full surface.
