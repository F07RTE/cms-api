# Architecture (SOLID)

**Single Responsibility** — one class, one reason to change. Services fetch and map data. Components render and handle user intent.

**Open/Closed** — new behavior arrives as a new service or component. Existing services are not widened to absorb unrelated features.

**Liskov Substitution** — every implementation of a service interface is substitutable without altering behavior; tests substitute them with NSubstitute and must not need special cases.

**Interface Segregation** — interfaces are small and focused.

**Dependency Inversion** — components depend on interfaces.

## Layer Rules

- Dependencies point inward: `CmsApi → Core ← Data`, `CmsApi.Worker → Core ← Data`
- The hosts (`CmsApi`, `CmsApi.Worker`) never reference each other. The Inbox is their only coupling
- `CmsApi.Core` holds the domain, the event rules, the use cases and the repository interfaces (`IInboxRepository`, …). It has no EF Core or ASP.NET Core dependency
- `CmsApi.Data` implements Core's repository interfaces: EF contexts, one repository per table, migrations
- Controllers stay thin: auth policy, binding, calling Core/Data, mapping to response DTOs. No event rules in controllers
- Event rules and batch ordering are pure functions in Core, with no I/O
- Shared DI goes through `AddCmsCore()` / `AddCmsData(config)`, not per-host copies

## Folder Layout

- Projects live in `src/`, test projects in `tests/`
- Folders group by purpose, not by kind. A folder says what its files are for (`Events/Validation`, `Events/Rules`, `Inbox`), never what they are (`Models`, `Enums`, `Interfaces`, `Services`)
- Core has two parts:
    - `Domain/`: one folder per concept, holding its types and its repository interface (`Domain/Events/CmsEvent`, `Domain/Inbox/IInboxRepository`); sub-folders hold one purpose each
    - `UseCases/`: one folder per use case that has logic of its own (`ReceiveBatch`, `ProcessInbox`, `ProcessBatch`). A use case that only reads or writes through a repository has no class: its controller calls the repository. Add the folder when logic appears
- Data has one folder per table and one repository per table, mirroring `Domain/` (`Core/Domain/Inbox/IInboxRepository` ↔ `Data/Inbox/InboxRepository`). `content_entities` has two, split by context: `ContentEntityReadRepository` (Reader) and `ContentEntityRepository` (Writer)
- A write that spans tables lives in the repository of the table whose row it locks (`ContentEntityRepository.ApplyGroupAsync` also writes `tombstones` and `event_log`)
- Namespace = folder path
- Exceptions: `Core/Exceptions` holds the shared exception hierarchy; the API host keeps ASP.NET's `Controllers/` and `Dtos/`

## Data Access

- `ReadDbContext` (NoTracking) for GETs and the auth lookup. `WriteDbContext` for ingestion, PATCH and everything the worker does
- The worker registers the Writer only
- Writes to a Content Entity take a row lock (`SELECT … FOR UPDATE`) inside their transaction. No `xmin` token
