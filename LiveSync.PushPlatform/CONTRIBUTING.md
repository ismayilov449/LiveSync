# Contributing

Thanks for your interest in LiveSync!

## Development setup

1. Fork and clone the repository
2. Read **[docs/demo-walkthrough.md](docs/demo-walkthrough.md)** for the full demo script
3. Start infrastructure: `docker compose up -d` in `LiveSync.PushPlatform/`
4. Build client: `cd LiveSync.API/client && npm ci && npm run build`
5. Run API: `dotnet run --project LiveSync.API`
6. Run Worker: `dotnet run --project LiveSync.Worker`
7. (Optional) Frontend dev: `cd LiveSync.API/client && npm run dev`
8. (Optional) Observability: `cd observability && docker compose -f docker-compose.observability.yml --profile observability up -d`

## Code standards

- Follow `.editorconfig` formatting
- Keep domain logic free of infrastructure dependencies
- Add tests for new behavior (unit or integration as appropriate)
- Use conventional commit messages

## Pull requests

1. Create a feature branch from `main`
2. Ensure `dotnet test` passes (unit + integration if Docker available)
3. **Update documentation** if behavior, API, UI, or architecture changes — see `docs/` and root `README.md`
4. Keep PRs focused — one concern per PR

## Documentation map

When you change… update these files:

| Area | Files |
|------|-------|
| API endpoints | `README.md` API reference, `docs/architecture.md` |
| Tenancy / lifecycle | `docs/tenancy.md`, `docs/solution-architecture.md` |
| Real-time / queue | `docs/real-time-sync.md`, ADR 003 |
| Demo steps | `docs/demo-walkthrough.md` |
| UI routes | `README.md` Admin UI section, `docs/demo-walkthrough.md` |
| Observability | `README.md` Observability section, `docs/solution-architecture.md` |
| CV bullets | `docs/resume-bullets.md` |

## Secrets

Never commit production connection strings, JWT secrets, or API keys. Use User Secrets or environment variables locally.
