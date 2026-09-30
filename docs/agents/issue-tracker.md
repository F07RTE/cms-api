# Issue tracker: local markdown

Issues and specs for this repo live as markdown files under `.scratch/` (gitignored). One directory per effort, one file per ticket.

## Conventions

- **Layout**: `.scratch/<effort-slug>/issues/<NN>-<slug>.md`, numbered from `01` in dependency order (blockers first). Supporting material goes beside it (`research/`, `map.md`).
- **Ticket header**: `# <title>`, then `Type:`, `Status:` (`open` | `ready-for-agent` | `resolved`), `Assignee:` (once claimed), `Blocked by:` (ticket numbers, or empty).
- **Create**: write a new file with the next free number.
- **Read**: read the file; the answer, if any, is its `## Answer` section.
- **Comment / resolve**: append an `## Answer` section and set `Status: resolved`.
- **Refer by title**, linking the file; never by a bare number.

Current efforts:

- `.scratch/cms-api-spec/` — the wayfinder map (decision tickets, all resolved)
- `.scratch/cms-api-build/` — ordered build tickets
- `.scratch/rabbitmq-delivery/` — tickets for Deliver Batches through RabbitMQ (ADR 0004)

## When a skill says "publish to the issue tracker"

Write a ticket file under the effort's `issues/` directory.

## When a skill says "fetch the relevant ticket"

Read the ticket file.

## Wayfinding operations

Used by `/wayfinder`.

- **Map**: `.scratch/<effort>/map.md`, with `Label: wayfinder:map` under the title and the Destination / Notes / Decisions so far / Not yet specified / Out of scope sections.
- **Child ticket**: a file in `.scratch/<effort>/issues/`. `Type:` holds the wayfinder type (`research`/`prototype`/`grilling`/`task`).
- **Blocking**: the `Blocked by:` line (ticket numbers). A ticket is unblocked when every listed ticket is `resolved`.
- **Frontier query**: tickets with `Status: open`, no `Assignee:`, and every blocker resolved; the lowest number wins.
- **Claim**: add `Assignee: <dev>` under `Status:` — the session's first write.
- **Resolve**: append `## Answer`, set `Status: resolved`, then add a context pointer (gist + link) to the map's Decisions so far.
