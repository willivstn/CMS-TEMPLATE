# CMS Enterprise Template

A production-ready, reusable Content Management System template for building enterprise
ASP.NET Core applications. Built on **.NET 10 (LTS)** following **Clean Architecture**,
**SOLID**, and permission-based security.

> **Status:** under active construction. Phase 0 (solution skeleton) is complete and the
> solution builds clean. See [Delivery Roadmap](#delivery-roadmap) for what's next.

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
- [ ] **Phase 1** — Domain model, EF Core config, migrations, seed data.
- [ ] **Phase 2** — Identity authentication + permission-based authorization.
- [ ] **Phase 3** — Admin shell UI, dynamic menu, breadcrumbs, reusable components.
- [ ] **Phase 4** — User / Role / Permission management.
- [ ] **Phase 5** — CMS content (pages, posts, versioning, workflow) + Media Library.
- [ ] **Phase 6** — SignalR, background jobs, email, settings, caching, logging.
- [ ] **Phase 7** — REST API (JWT, Swagger, versioning, rate limiting) + security hardening.
- [ ] **Phase 8** — Sample plug-in module, Docker/CI, documentation, tests.
