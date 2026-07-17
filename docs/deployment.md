# Deployment guide

## Configuration

All settings can be supplied via `appsettings.json`, environment variables, or a
secrets store. Environment variables use `__` to separate sections, e.g.
`ConnectionStrings__DefaultConnection`.

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:SigningKey` | **Must be overridden** in production (≥ 32 chars) |
| `Jwt:Issuer` / `Jwt:Audience` | JWT validation values |
| `Email:Smtp:*` | SMTP host/port/credentials; set `Enabled=true` to send |
| `MultiTenancy:Enabled` | Turn on per-tenant resolution + filtering |

The app **migrates and seeds the database on startup** (`DatabaseInitializer`).
For controlled rollouts, run `dotnet ef database update` in a release step and
gate the startup initializer behind configuration.

## Run with Docker

```bash
docker compose up --build
# → http://localhost:8080   (admin@cms.local / Admin#12345)
```

`docker-compose.yml` starts SQL Server 2022 and the app. Kestrel listens on port
8080 inside the container; **terminate TLS at a reverse proxy** (nginx, Caddy,
cloud load balancer) in front of it. The auth cookie uses `SameAsRequest`, so it
is marked `Secure` automatically once traffic arrives over HTTPS.

## Run from source

```bash
dotnet run --project src/CMS.Web            # http://localhost:5243
dotnet run --project src/CMS.Web --launch-profile https   # https://localhost:7111
```

Prerequisites: .NET 10 SDK and SQL Server (LocalDB/Express/full). Trust the dev
cert once with `dotnet dev-certs https --trust`.

## Production checklist

- [ ] Override `Jwt:SigningKey` and the seeded admin password.
- [ ] Point `ConnectionStrings:DefaultConnection` at your SQL Server.
- [ ] Terminate TLS (HSTS is enabled outside Development).
- [ ] Configure SMTP (`Email:Smtp:Enabled=true`) for real email delivery.
- [ ] Persist Data Protection keys to a shared store for multi-instance
      deployments (affects encrypted settings and antiforgery).
- [ ] Point Serilog file logs / the database sink at durable storage; ship logs
      to Seq/Elasticsearch if desired.
- [ ] Restrict the Hangfire dashboard (already permission-gated) and scale
      workers as needed.

## CI/CD

`.github/workflows/ci.yml` restores, builds, runs the unit tests, and builds the
Docker image on every push/PR to `main`. Extend it with a publish/deploy job for
your target (Azure App Service, container registry, Kubernetes, …).
