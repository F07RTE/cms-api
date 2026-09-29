# Architecture (SOLID)

**Single Responsibility** — one class, one reason to change. Services fetch and map data. Components render and handle user intent.

**Open/Closed** — new behavior arrives as a new service or component. Existing services are not widened to absorb unrelated features.

**Liskov Substitution** — every implementation of a service interface is substitutable without altering behavior; tests substitute them with NSubstitute and must not need special cases.

**Interface Segregation** — interfaces are small and focused.

**Dependency Inversion** — components depend on interfaces.

## Layer Rules

- Dependencies point inward: `CmsApi → Core ← Data`, `CmsApi.Worker → Core ← Data`
- The hosts (`CmsApi`, `CmsApi.Worker`) never reference each other. The Inbox is their only coupling
- `CmsApi.Core` holds the domain, event rules, batch processing and interfaces (`IInbox`, …). It has no EF Core or ASP.NET Core dependency
- `CmsApi.Data` implements Core's interfaces: EF contexts, the Inbox, migrations
- Controllers stay thin: auth policy, binding, calling Core/Data, mapping to response DTOs. No event rules in controllers
- Event rules and batch ordering are pure functions in Core, with no I/O
- Shared DI goes through `AddCmsCore()` / `AddCmsData(config)`, not per-host copies

## Data Access

- `ReadDbContext` (NoTracking) for GETs and the auth lookup. `WriteDbContext` for ingestion, PATCH and everything the worker does
- The worker registers the Writer only
- Writes to a Content Entity take a row lock (`SELECT … FOR UPDATE`) inside their transaction. No `xmin` token
