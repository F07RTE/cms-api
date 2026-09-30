# Clean Code

- No magic strings or numbers — use constants or configuration values
- Method names describe what they do: `FindPendingAsync()` not `GetData()`
- Methods are short (~20 lines max) — extract when longer
- No commented-out code left in the codebase
- No summary comments (`///`): names say what the code does. A `//` comment only explains why, when the code can't
- One level of abstraction per method
- Nullable reference types are enabled — express nullability instead of suppressing it with `!`
- `async void` is never used; every async method returns `Task`/`ValueTask`, and `CancellationToken` flows through
- Analyzer warnings are fixed, not suppressed, unless the suppression carries a comment explaining why
- Formatting is CSharpier's job — run `dotnet csharpier .` instead of hand-formatting
- Use the `CONTEXT.md` vocabulary in names: `ContentEntity` (never `Entity`/`Item`), `CmsEvent`, `Batch`, `Inbox`, `Tombstone`, `EventOutcome`
- Logging is structured `ILogger` with message templates, never string interpolation. Never log credentials, the `Authorization` header or request bodies

## Commands

Run via the `run-script` local tool (`dotnet tool restore` once after cloning, before the Husky hooks work):

- `docker compose -f infra/compose.yaml up -d` — start Postgres
- `dotnet r migrations:up` — apply EF migrations (never applied at startup)
- `dotnet r test` — run the test suite
- `dotnet r lint:run` — format; CI runs `dotnet r lint:check`
