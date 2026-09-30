# CMS API

Keeps a private, versioned copy of the content a CMS publishes, and serves it to authenticated users.

- The CMS delivers **Batches** of **CMS Events** (`publish`, `unPublish`, `delete`) to `POST /cms/events`.
- Users read **Content Entities** through `GET /entities` and `GET /entities/{id}`.
- Admins also see Content Entities that aren't Visible, and can mark one Disabled with `PATCH /entities/{id}/disable|enable`.

The domain vocabulary is in [`CONTEXT.md`](CONTEXT.md). The decisions that are hard to reverse are in [`docs/adr/`](docs/adr/).

## Running it

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`)
- Docker Desktop (Mac or Windows) or Docker Engine with the compose plugin

The commands are the same on macOS, Linux and Windows (PowerShell or cmd). They run through the [`run-script`](https://github.com/xt0rted/dotnet-run-script) local tool; the scripts live in `global.json`.

### Setup

```sh
dotnet tool restore        # once after cloning: run-script, dotnet-ef, CSharpier, Husky
dotnet r services:up       # Postgres 17 and RabbitMQ in Docker, waits until they're healthy
dotnet r migrations:up     # creates the schema and seeds the dev users
```

Migrations are never applied at startup; run `migrations:up` after pulling new ones.

The first `services:up` runs `infra/postgres/init.sql`. It creates two databases (`cms_api` for dev, `cms_api_test` for tests) and two roles:

- `cms_writer`, which owns the tables
- `cms_reader`, which can only `SELECT`

### Run

One process: the API also consumes the Batches it queues. The RabbitMQ URI carries credentials, so it comes from user-secrets (once per clone):

```sh
dotnet user-secrets set ConnectionStrings:RabbitMq amqp://cms:cms_local@localhost:5672 --project src/CmsApi
```

Then one command starts Postgres and RabbitMQ, applies migrations and runs the API on http://localhost:5290:

```sh
dotnet r dev
```

### Dev credentials

| Caller     | Username         | Password                               | Can call                          |
| ---------- | ---------------- | -------------------------------------- | --------------------------------- |
| CMS Client | `cms-dev-client` | see `src/CmsApi/appsettings.Development.json` | `POST /cms/events`             |
| Admin      | `admin`          | `admin123`                             | every `/entities` route           |
| User       | `reader`         | `reader123`                            | `GET /entities`, `GET /entities/{id}` |

- `admin` and `reader` are seeded by the migrations, so they exist in every environment the migrations run in. Rotate or delete them before any real deployment.
- Outside Development, the CMS credential and the connection strings come from user-secrets or environment variables (`CmsCredentials__Username`, `CmsCredentials__Password`, `ConnectionStrings__Reader`, `ConnectionStrings__Writer`, `ConnectionStrings__RabbitMq`). The base `appsettings.json` leaves the credential empty, so the API refuses to start without one.

### Try it

```sh
curl -u cms-dev-client:<cms-password> \
  -H "Content-Type: application/json" \
  -d '[{"type":"publish","id":"article-1","version":1,"timestamp":"2026-09-29T10:00:00Z","payload":{"title":"Hello"}}]' \
  http://localhost:5290/cms/events

curl -u reader:reader123 http://localhost:5290/entities
```

On Windows PowerShell, call `curl.exe`, not the `curl` alias.

For more, [`scenarios/`](scenarios/) has `.http` files that walk each event rule.

### API docs

With the API running in Development:

- **Scalar UI:** http://localhost:5290/scalar. It has example calls for every route. Click "Authorize" and enter one of the dev credentials above.
- **OpenAPI document:** http://localhost:5290/openapi/v1.json

Both are served anonymously, and only when `ApiDocs:Enabled` is true. That is set in `appsettings.Development.json`; outside Development they're off.

### Tests and lint

```sh
dotnet r test         # unit + integration; needs services:up
dotnet r lint:run     # format with CSharpier (CI runs lint:check)
```

- Integration tests use the `cms_api_test` database. They migrate it once, and Respawn clears the data before each test.
- The consumer is off in tests. They process Batches through `DrainInboxAsync()`, which drains the real queue, so no test waits on a timer.
- `dotnet r services:down` stops Postgres and RabbitMQ. The data volumes survive it.

## Assumptions

- The CMS never reuses an id after deleting it.
- A `version` only goes up for a given id. When two events carry the same version, the later timestamp wins.
- Timestamps order events **within** one Batch. Across Batches, the version decides; an equal version applies only if its timestamp is newer than the stored `last_event_at`.
- The CMS doesn't need a per-event result in the webhook response. A 202 means "stored, will be processed".
- Payloads are JSON objects with a schema we don't know and don't need to know.
- There is one CMS, so its credential lives in configuration rather than a table.
- Users are created by a person with database access. There is no user management API.
- TLS ends at a reverse proxy in front of the API.

## Design notes

### Code structure

```
src/
  CmsApi/            the only host: controllers, DTOs, auth, errors; runs the Batch consumer
  CmsApi.Core/
    Domain/          one folder per concept: its types, rules and repository interface
    UseCases/        ReceiveBatch, ConsumeBatch, ProcessBatch
  CmsApi.Data/       one folder and one repository per table, EF contexts, migrations
  CmsApi.Messaging/  RabbitMQ: topology, publisher, consumer
tests/
  CmsApi.Core.Tests/        mirror Core
  CmsApi.IntegrationTests/  mirror the routes; UseCases/ for the consumer and the end-to-end flow
```

Dependencies point inward: the host depends on Core, Data implements Core's repository interfaces, and Messaging implements Core's publisher and runs the consumer.

| Use case | Core | Repository |
| --- | --- | --- |
| Receive Batch (`POST /cms/events`) | `BatchReceiver` | `InboxRepository` |
| Consume Batch (skip, retry or Dead) | `BatchConsumer` | `InboxRepository` |
| Process Batch (validate, decide, store) | `BatchProcessor` | `ContentEntityRepository`, `EventLogRepository` |
| List / Get Content Entities | none | `ContentEntityReadRepository` (Reader) |
| Disable / Enable a Content Entity | none | `ContentEntityRepository` (Writer) |

Only use cases with logic of their own get a class in `UseCases/`. List, Get, Disable and Enable are a single repository call, so their controllers call the repository directly; a class that only forwards the call would add a layer and no behaviour.

### Async ingestion

`POST /cms/events` checks only the body's shape: at most 10 MB, a JSON array of 1–1000 events. It stores the raw Batch in the **Inbox** table, publishes `{ batchId }` to RabbitMQ and answers 202 once the broker confirms. A failed publish answers 500, and the CMS retries. A consumer inside the API host applies the events later ([ADR 0001](docs/adr/0001-async-ingestion-via-inbox.md), [ADR 0004](docs/adr/0004-deliver-batches-through-rabbitmq.md)).

**Why not process inside the request:** it's simpler, and the CMS would get each event's outcome. But:

- a large Batch could outlast the CMS's timeout
- parallel webhook calls would race on the same Content Entity
- a traffic spike would land straight on the database

**Order doesn't matter.** Replicas consume in parallel, so arrival order between Batches is lost. The stored state doesn't depend on it:

- the version decides whether an event is stale
- equal versions tie-break on the stored `last_event_at`
- a delete leaves a **Tombstone** that's final whatever the timestamp ([ADR 0002](docs/adr/0002-deletes-are-final.md))

Only the Event Log outcomes differ. So a crash mid-Batch is harmless: the broker redelivers it, and the groups already committed come back as `SkippedDuplicate` or `SkippedDeleted`. A Batch no longer `Pending` is acked and skipped.

**Retries and Dead Batches.** The CMS won't retry after a 202, so we do. Outcomes are in the `event_log` table and the logs.

- An invalid event never fails its Batch. It's logged as `Failed`, stored in full, and the rest of the Batch applies.
- A Batch whose processing throws is rejected into `cms.batches.retry`, which routes it back after `Messaging:RetryDelay`.
- The attempt comes from the broker's `x-death` count. After `Messaging:MaxAttempts`, the Batch becomes a **Dead Batch**: marked with its `last_error`, logged as an error, and left in the Inbox for a person to inspect.
- The retry delay is fixed on the queue when it's declared. Changing `RetryDelay` after `cms.batches.retry` exists fails startup with `PRECONDITION_FAILED`; delete the queue first.

### Payloads are stored verbatim

The payload is stored exactly as the CMS sent it. There is no HTML sanitising or rewriting.

- We don't know the payload's schema, so we can't tell markup from data.
- Sanitising is the renderer's job; it knows the output context (HTML, attribute, JSON…).
- Our responses are JSON, and the serialiser encodes them.

The payload is stored as `jsonb`. `jsonb` keeps the **meaning** of the JSON, not its bytes: key order and whitespace may change, and duplicate keys collapse. So a GET returns an equivalent object, not the same bytes. The Inbox keeps the raw body as `text`, byte-exact, until the consumer validates it. Duplicate keys and `\u0000`, which `jsonb` can't hold, become per-event `Failed` outcomes instead of errors.

### Auth and TLS

- Basic Auth, verified on every request. There are three roles: CMS Client, User and Admin. A wrong or missing credential gets 401. A valid caller with the wrong role gets 403.
- **TLS ends at a proxy.** The API speaks plain HTTP and doesn't redirect to HTTPS: by the time a redirect happens, the credentials have already crossed in clear, and webhook clients don't follow POST redirects reliably. HSTS belongs on that proxy too: the app only sees plain HTTP.
- **User passwords** are hashed with Argon2id ([ADR 0003](docs/adr/0003-argon2id-with-credential-cache.md)). An unknown username is still checked against a dummy hash, so timing doesn't reveal which usernames exist.
- **Credential cache:** Argon2id is deliberately expensive, and Basic Auth verifies on every request. So a successful check is cached for 60 s, keyed by a SHA-256 of the `Authorization` header. The trade-off: a changed or removed password keeps working for up to 60 s.
- **Hardening left for later:**
  - Hash the CMS secret. Today it's a GUID held in plaintext in the secret store, compared in constant time.
  - Shorten or drop the credential cache once user management exists.

### Replica-ready reads

- There are two connection strings, `Reader` and `Writer`. In compose, both point at the same Postgres, as different roles. The reader role can't write, as on a real replica.
- `GET /entities*` and the login lookup use the Reader. Ingestion, PATCH and the whole consumer use the Writer.
- **Moving reads to a replica is a config change:** point `Reader` at the replica.
- **Eventual consistency:** under a replica, a GET may briefly lag a write. The PATCH response is built from the Writer, so an Admin sees their own change straight away.

### Keyset pagination

`GET /entities?limit=&cursor=` pages newest first (`updatedAt` desc, then `id`). `limit` defaults to 50, maximum 200. `nextCursor` is an opaque base64url `(updatedAt, id)`, and `null` on the last page.

- **Why keyset:** every page costs the same index seek, however deep the walk goes. Offset paging gets slower with depth and skips or repeats rows when data shifts.
- **No total count:** a count would scan every Visible row on every call.
- **Cursor drift:** a Content Entity updated during a walk jumps to the front, so the walk can miss it. A client that needs a full sync restarts from the first page.
- A tampered cursor that still parses is harmless: the visibility filter still applies.

### Row lock, not `xmin`

The consumer and an Admin's PATCH can touch the same Content Entity. Each write takes a row lock (`SELECT … FOR UPDATE`) inside its transaction. The two write different columns:

- the consumer writes the CMS data
- PATCH writes the admin override

So the lock only serialises them; neither ever overwrites the other.

**Why not `xmin`:** optimistic concurrency on `xmin` would make every admin disable bump the row version and fail the consumer's concurrent write, a conflict that isn't real. Retrying it buys nothing the lock doesn't already give.

## Out of scope

- **Inbox cleanup.** `Done` Batches accumulate. A scheduled delete, or partitioning by `received_at`, would handle it.
- **Event Log retention and querying.** The table grows forever, and no endpoint reads it.
- **User management.** No endpoints to create, change or disable Users. Admins disable Content Entities, never Users.
- **Status/health endpoint.** There is no way to ask for a Batch's progress. The 202 returns a `batchId` for correlating with logs and the database.

## Future work

- **Azure Service Bus.** It would replace RabbitMQ behind the Messaging project; Core only sees `IBatchPublisher` and the Consume Batch use case.
- **Hosts organised by use case.** Give the API one folder and one controller per use case (`ReceiveBatch/`, `ListContentEntities/`, …), so a use case has the same name in the host, Core and the tests. Today's host keeps ASP.NET's `Controllers/` and `Dtos/`, which is small enough to read at a glance.
