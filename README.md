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
dotnet r services:up       # Postgres 17 in Docker, waits until it's healthy
dotnet r migrations:up     # creates the schema and seeds the dev users
```

Migrations are never applied at startup; run `migrations:up` after pulling new ones.

The first `services:up` runs `infra/postgres/init.sql`. It creates two databases (`cms_api` for dev, `cms_api_test` for tests) and two roles:

- `cms_writer`, which owns the tables
- `cms_reader`, which can only `SELECT`

### Run both hosts

The API and the worker are separate processes. One command starts Postgres, applies migrations, builds, and runs both. Ctrl+C stops both:

```sh
dotnet r dev
```

The script is POSIX `sh`. On Windows, run it from Git Bash with `dotnet r dev --script-shell bash`, or start each host in its own terminal:

```sh
dotnet run --project CmsApi          # API on http://localhost:5290
dotnet run --project CmsApi.Worker   # processes the Inbox
```

Without the worker, `POST /cms/events` still answers 202, but nothing reaches `/entities`.

### Dev credentials

| Caller     | Username         | Password                               | Can call                          |
| ---------- | ---------------- | -------------------------------------- | --------------------------------- |
| CMS Client | `cms-dev-client` | see `CmsApi/appsettings.Development.json` | `POST /cms/events`             |
| Admin      | `admin`          | `admin123`                             | every `/entities` route           |
| User       | `reader`         | `reader123`                            | `GET /entities`, `GET /entities/{id}` |

- `admin` and `reader` are seeded by the migrations, so they exist in every environment the migrations run in. Rotate or delete them before any real deployment.
- Outside Development, the CMS credential and the connection strings come from user-secrets or environment variables (`CmsCredentials__Username`, `CmsCredentials__Password`, `ConnectionStrings__Reader`, `ConnectionStrings__Writer`). The base `appsettings.json` leaves the credential empty, so the API refuses to start without one.

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
- They run the worker through `DrainInboxAsync()`, so no test waits on a timer.
- `dotnet r services:down` stops Postgres. The data volume survives it.

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

### Sync vs async ingestion

`POST /cms/events` checks only the body's overall shape: at most 10 MB, a JSON array of 1–1000 events. It then stores the raw Batch in an **Inbox** table and returns 202. A separate worker process validates and applies each event later ([ADR 0001](docs/adr/0001-async-ingestion-via-inbox.md)).

**The alternative:** process the Batch inside the request and answer 200 after commit. That's simpler, and the CMS would get each event's outcome in the response. We rejected it because:

- a large Batch could outlast the CMS's timeout
- parallel webhook calls would race on the same Content Entity
- a traffic spike would land straight on the database

**Why async is safe: the outcome doesn't depend on order.** Batches are processed roughly in arrival order, but correctness doesn't rely on it:

- the version decides whether an event is stale
- equal versions tie-break on the stored `last_event_at`
- a delete leaves a **Tombstone** that's final whatever the timestamp ([ADR 0002](docs/adr/0002-deletes-are-final.md))

So any processing order leaves the same stored state. Only the Event Log outcomes differ. That's why a Batch waiting for a retry doesn't block the Batches behind it, and why replaying a Batch after a crash is harmless: the groups it already committed come back as `SkippedDuplicate` or `SkippedDeleted`.

**The cost:** the CMS gets no per-event feedback. Outcomes are in the `event_log` table and the logs. Since the CMS won't retry after a 202, we own the retries:

- a Batch whose processing throws is retried with exponential backoff, capped at 5 minutes
- after 5 attempts it becomes a **Dead Batch**, which a person inspects in the Inbox

An invalid event never fails its Batch. It's logged as `Failed`, stored in full, and the rest of the Batch still applies.

**One worker:** a Postgres advisory lock makes one worker the **Leader**. A second replica idles until it can take over. When a worker becomes Leader, it returns every Orphaned Batch (claimed by a worker that crashed or was stopped) to the Inbox.

### Payloads are stored verbatim

The payload is stored exactly as the CMS sent it. There is no HTML sanitising or rewriting.

- We don't know the payload's schema, so we can't tell markup from data.
- Sanitising is the renderer's job; it knows the output context (HTML, attribute, JSON…).
- Our responses are JSON, and the serialiser encodes them.

The payload is stored as `jsonb`. `jsonb` keeps the **meaning** of the JSON, not its bytes: key order and whitespace may change, and duplicate keys collapse. So a GET returns an equivalent object, not the same bytes. The Inbox keeps the raw body as `text`, byte-exact, until the worker validates it. Duplicate keys and `\u0000`, which `jsonb` can't hold, become per-event `Failed` outcomes instead of errors.

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
- `GET /entities*` and the login lookup use the Reader. Ingestion, PATCH and the whole worker use the Writer.
- **Moving reads to a replica is a config change:** point `Reader` at the replica.
- **Eventual consistency:** under a replica, a GET may briefly lag a write. The PATCH response is built from the Writer, so an Admin sees their own change straight away.

### Keyset pagination

`GET /entities?limit=&cursor=` pages newest first (`updatedAt` desc, then `id`). `limit` defaults to 50, maximum 200. `nextCursor` is an opaque base64url `(updatedAt, id)`, and `null` on the last page.

- **Why keyset:** every page costs the same index seek, however deep the walk goes. Offset paging gets slower with depth and skips or repeats rows when data shifts.
- **No total count:** a count would scan every Visible row on every call.
- **Cursor drift:** a Content Entity updated during a walk jumps to the front, so the walk can miss it. A client that needs a full sync restarts from the first page.
- A tampered cursor that still parses is harmless: the visibility filter still applies.

### Row lock, not `xmin`

The worker and an Admin's PATCH can touch the same Content Entity. Each write takes a row lock (`SELECT … FOR UPDATE`) inside its transaction. The two write different columns:

- the worker writes the CMS data
- PATCH writes the admin override

So the lock only serialises them; neither ever overwrites the other.

**Why not `xmin`:** optimistic concurrency on `xmin` would make every admin disable bump the row version and fail the worker's concurrent write, a conflict that isn't real. Retrying it buys nothing the lock doesn't already give.

## Out of scope

- **Inbox cleanup.** `Done` Batches accumulate. A scheduled delete, or partitioning by `received_at`, would handle it.
- **Event Log retention and querying.** The table grows forever, and no endpoint reads it.
- **User management.** No endpoints to create, change or disable Users. Admins disable Content Entities, never Users.
- **Status/health endpoint.** There is no way to ask for a Batch's progress. The 202 returns a `batchId` for correlating with logs and the database.

## Future work

- **More than one worker.** Claim Batches with `FOR UPDATE SKIP LOCKED` instead of a leader lock. The row lock and primary keys already keep concurrent groups correct. What would be lost is arrival order between Batches, which the version rules don't depend on.
