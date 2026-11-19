# CMS Enterprise Template: agent start sheet

A reusable .NET 10 CMS template (Clean Architecture). BaseCamp was derived from it, and the two have since diverged, so a
change here does not reach BaseCamp. It is not deployed anywhere. The workspace rules in `C:\HIGHVELDLABS\AGENTS.md` come
first: Workspace project "HighveldLabs Site & CMS", AI-Memory ID `cms-template`. Details live in [README.md](README.md) and
[docs/](docs/).

## Layout

- `src/CMS.{Domain,Shared,Application,Infrastructure,Contracts,Web}` (`CMS.slnx`): ASP.NET Core MVC, Razor, SignalR,
  EF Core 10 on SQL Server, MediatR, Hangfire.
- `tests/CMS.{UnitTests,IntegrationTests,UiTests}`. Integration tests need SQL Server; UI tests need Playwright.
- `Dockerfile` and `docker-compose.yml` are for local runs only.

## Verify a change (what CI runs)

`dotnet build CMS.slnx` and `dotnet test tests/CMS.UnitTests/CMS.UnitTests.csproj`. `dotnet test CMS.slnx` also runs
the integration and UI tests.

## Don't

- Reuse the template's committed development credentials (default admin, SA password, JWT and signing keys) in a real
  project. Every client project needs its own.
- Deploy it without the owner's OK and a read of `C:\HIGHVELDLABS\SERVER\`.
- Start a BugFixLog in a hurry: there isn't one yet. Create `docs/BugFixLog.md` with the first real fix.
