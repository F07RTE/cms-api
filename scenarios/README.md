# Scenarios

`.http` files that walk the event rules against a running API. Each file is one scenario; run its requests top to bottom.

## Setup

1. Start everything: `dotnet r dev`.
2. Create `scenarios/http-client.private.env.json` (gitignored) with the dev CMS password from `src/CmsApi/appsettings.Development.json`:

   ```json
   { "dev": { "cmsPassword": "<CmsCredentials:Password>" } }
   ```

3. Pick the `dev` environment in your client:
   - **Rider / Visual Studio:** reads `http-client.env.json` and the private file natively.
   - **VS Code:** use [httpYac](https://marketplace.visualstudio.com/items?itemName=anweber.vscode-httpyac), which reads the same files. REST Client does not.

## Running

- A `POST` is applied asynchronously by the consumer. Wait a moment before the next `GET`.
- Ids are never reused after a delete, so each file has `@run = 1` at the top. Bump it before running a file again.

## Scenarios

| File                     | Shows                                                             |
| ------------------------ | ----------------------------------------------------------------- |
| `01-happy-path.http`     | publish, update, read, list                                       |
| `02-out-of-order.http`   | timestamp orders within a Batch; version decides across Batches   |
| `03-unpublish.http`      | equal-version unPublish applies; a late retry is a Duplicate      |
| `04-deletes.http`        | Tombstones are final; delete of an unknown id                     |
| `05-duplicates.http`     | replayed Batch; same version twice in one Batch                   |
| `06-invalid-events.http` | Failed events don't fail the Batch; 400 / 401 / 403 on the body   |
| `07-admin-disable.http`  | Disabled survives CMS Events; only an Admin clears it             |

## Seeing the Event Outcomes

The webhook answers 202 only. Outcomes are in the API logs and the `event_log` table:

```sh
docker compose -f infra/compose.yaml exec postgres psql -U postgres -d cms_api -c \
  "select batch_id, entity_id, event_type, version, outcome, reason from event_log order by id desc limit 20;"
```
