# Stack

- .NET 10 LTS: ASP.NET Core Web API (controllers) + a Worker Service host
- EF Core 10 + Npgsql on Postgres; snake_case via `EFCore.NamingConventions`; payload stored as `jsonb`
- Postgres runs in Docker compose (`infra/compose.yaml`) — no Testcontainers
- Config: `appsettings*.json` + user-secrets/env vars, typed options with `ValidateOnStart`; connection strings `Reader` / `Writer`
- Errors: `IExceptionHandler` → `ProblemDetails` + a tabnews-style `action`
- Auth: a hand-written Basic `AuthenticationHandler`; Argon2id via `Isopoh.Cryptography.Argon2`
- NUnit + FluentAssertions 7 + `WebApplicationFactory` + Respawn for tests
- CSharpier for formatting, Husky.Net pre-commit, `commit-linter` for Conventional Commits
- GitHub Actions runs tests and lint

## Layout

- `src/CmsApi` — HTTP host: controllers, DTOs, auth, error handling
- `src/CmsApi.Worker` — Worker Service host: the Inbox `BackgroundService` only; own process, own appsettings
- `src/CmsApi.Core` — `Domain/` (concepts, event rules, validation, repository interfaces) and `UseCases/`
- `src/CmsApi.Data` — EF Read/Write contexts, one repository per table, migrations
- `tests/CmsApi.Core.Tests`, `tests/CmsApi.IntegrationTests`
- `infra/` — compose and the Postgres init script

## Conventions

- Services are interface-first where there's a seam (`IInboxRepository`, `IBatchProcessor`, …); no `new` of services inside other services
- Response DTOs are separate records from EF entities; controllers never return EF entities
- Routes exactly per spec: `POST /cms/events`, `GET /entities`, `GET /entities/{id}`, `PATCH /entities/{id}/disable|enable`
- JSON is camelCase; timestamps are ISO-8601 UTC (`DateTimeOffset`)

## Sources of truth

- Glossary: `CONTEXT.md`. Decisions: `docs/adr/` and the resolved tickets in `.scratch/cms-api-spec/issues/`
- Build tickets: `.scratch/cms-api-build/issues/`
