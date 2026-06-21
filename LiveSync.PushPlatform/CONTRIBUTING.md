# Contributing

Thanks for your interest in LiveSync!

## Development setup

1. Fork and clone the repository
2. Start infrastructure: `docker compose up -d` in `LiveSync.PushPlatform/`
3. Run API: `dotnet run --project LiveSync.API`
4. Run Worker: `dotnet run --project LiveSync.Worker`
5. (Optional) Frontend: `cd LiveSync.API/client && npm run dev`

## Code standards

- Follow `.editorconfig` formatting
- Keep domain logic free of infrastructure dependencies
- Add tests for new behavior (unit or integration as appropriate)
- Use conventional commit messages

## Pull requests

1. Create a feature branch from `main`
2. Ensure `dotnet test` passes (unit + integration if Docker available)
3. Update documentation if behavior or architecture changes
4. Keep PRs focused — one concern per PR

## Secrets

Never commit production connection strings, JWT secrets, or API keys. Use User Secrets or environment variables locally.
