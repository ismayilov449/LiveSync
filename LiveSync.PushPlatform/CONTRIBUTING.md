# Contributing

Thanks for your interest in LiveSync!

## Development setup

1. Fork and clone the repository (parent repo: [LiveSync](https://github.com/ismayilov449/LiveSync); solution in `LiveSync.PushPlatform/`)
2. Read **[docs/demo-walkthrough.md](docs/demo-walkthrough.md)** for the full demo script
3. **Quick start script:** `.\scripts\dev.ps1` (Windows) or `./scripts/dev.sh` (Linux/macOS)
4. Or manually:
   - `docker compose up -d` in `LiveSync.PushPlatform/`
   - `cd LiveSync.API/client && npm ci && npm run build`
   - `dotnet run --project LiveSync.API`
   - `dotnet run --project LiveSync.Worker`
5. (Optional) Frontend dev: `cd LiveSync.API/client && npm run dev`
6. (Optional) Observability: `cd observability && docker compose -f docker-compose.observability.yml --profile observability up -d`

**Configuration:** Docker SQL auth is in `appsettings.json`. For Docker + JWT, copy `LiveSync.API/appsettings.Development.docker.example.json` → `appsettings.Development.json` or set `ASPNETCORE_ENVIRONMENT=Development`.

**Troubleshooting:** [docs/troubleshooting.md](docs/troubleshooting.md)  
**Client patterns:** [docs/client-development.md](docs/client-development.md)  
**Adding features:** [docs/extending-the-platform.md](docs/extending-the-platform.md)  
**Demo assets:** `python scripts/capture-demo-assets.py` (regenerates GIFs/screenshots in `docs/assets/`)

## Code standards

- Follow `.editorconfig` formatting
- Keep domain logic free of infrastructure dependencies
- Add tests for new behavior (unit, integration, and client Vitest as appropriate)
- Use conventional commit messages
- Client: `npm run lint` and `npm test` before PR

## Pull requests

1. Create a feature branch from `main` (e.g. `feature/`, `fix/`)
2. Ensure tests pass:
   - `dotnet test LiveSync.Tests`
   - `dotnet test LiveSync.IntegrationTests` (**requires Docker Desktop** for Testcontainers)
   - `cd LiveSync.API/client && npm test`
3. **Update documentation** if behavior, API, UI, or architecture changes — see doc map below
4. Keep PRs focused — one concern per PR

CI runs in the parent repo: `.github/workflows/ci.yml` (client build + test, .NET build + tests, Docker images).

## Documentation map

When you change… update these files:

| Area | Files |
|------|-------|
| API endpoints | `README.md` API reference, `docs/architecture.md` |
| Support Desk / new aggregates | `docs/extending-the-platform.md`, `docs/adr/006-support-desk-aggregates.md`, `docs/architecture.md`, `docs/glossary.md`, `README.md` |
| Tenancy / lifecycle | `docs/tenancy.md`, `docs/solution-architecture.md` |
| Real-time / queue / buckets | `docs/real-time-sync.md`, ADR 003, ADR 004/005 |
| Client / SPA / push UX | `docs/client-development.md`, `docs/demo-walkthrough.md` |
| Demo steps | `docs/demo-walkthrough.md` |
| UI routes | `README.md` Admin UI section, `docs/architecture.md`, `docs/demo-walkthrough.md` |
| Observability | `README.md` Observability section, `docs/solution-architecture.md` |
| Setup / local dev pain | `docs/troubleshooting.md`, `scripts/dev.ps1` |
| CV bullets | `docs/resume-bullets.md` |
| New architecture decision | `docs/adr/NNN-title.md` + README design decisions table |

## Secrets

Never commit production connection strings, JWT secrets, or API keys. Use User Secrets or environment variables locally:

```bash
dotnet user-secrets set "Auth:Jwt:SecretKey" "your-local-secret-min-32-chars" --project LiveSync.API
```
