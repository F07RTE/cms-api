# Deliver Batches through RabbitMQ

Status: accepted. Supersedes the broker rejection and the single-worker design of ADR 0001.

`POST /cms/events` still stores the raw Batch in the Inbox table, then publishes `{ batchId }` to RabbitMQ and returns 202 only after the publisher confirm. A consumer inside the API host processes the Batch and sets its final status (`Done` or `Dead`). We moved to a broker because the table-as-queue made us hand-build what a broker already gives: a leader lock, orphan recovery, claiming, retry scheduling and a polling loop. The Inbox table stays as the record of what arrived. It keeps the `long` `batchId` of the 202 contract, the Event Log's foreign key, Dead Batches a person can inspect, and bodies too large for a message.

## Considered Options

- **Keep the table as the queue, scale with `FOR UPDATE SKIP LOCKED` + leases:** rejected. Leases, heartbeats and lease-based recovery are more hand-written machinery, not less.
- **Broker only, no Inbox table:** rejected. We'd lose the `batchId` source, the Event Log foreign key, Dead Batch inspection and the raw body after ack, and large Batches would hit message size limits.
- **Outbox + relay to publish:** rejected. The CMS retries every delivery that didn't get a 202, so a crash between commit and publish loses nothing. It leaves a `Pending` row that was never published, which is harmless.
- **Azure Service Bus:** rejected for now because of cloud lock-in and a heavier local emulator. Messaging is its own project, so it can replace RabbitMQ later.
- **Wolverine or MassTransit:** rejected. Wolverine is a whole framework for a handful of moving parts, and MassTransit v9 is commercial. We use `RabbitMQ.Client` directly.

## Consequences

- There is no Worker host, no Leader and no Orphaned Batch. Any number of API replicas consume, and arrival order between Batches is lost. The version rules don't depend on that order (ADR 0001, Async ingestion pipeline).
- Retries go through the broker: a failed Batch is dead-lettered to a retry queue with a fixed delay that routes it back. The consumer reads the attempt count from `x-death`, and after `MaxAttempts` it marks the row `Dead` and acks. Exponential backoff is gone.
- A crash mid-Batch means redelivery. Replay is safe because committed groups replay as `SkippedDuplicate` / `SkippedDeleted`, and a message whose row is no longer `Pending` is acked and skipped.
- Processing shares the API's CPU and connections and can't be scaled on its own. Moving the consumer back into its own host means moving one `BackgroundService`.
- RabbitMQ joins compose and CI. Integration tests drain the real queue with `BasicGet` instead of sleeping.
