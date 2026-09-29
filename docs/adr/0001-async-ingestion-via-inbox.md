# Async ingestion via an inbox table

`POST /cms/events` saves the raw Batch to an Inbox table and returns 202. A background worker processes it later. We chose this over processing inside the request, even though that is simpler and gives the CMS immediate per-event feedback, because it keeps the webhook fast whatever the batch size, absorbs spikes, and processes batches in arrival order without parallel requests racing on the same Content Entity.

## Considered Options

- **Synchronous processing:** the CMS waits and gets 200 only after commit. Durability comes from CMS retries. Rejected: large batches risk CMS timeouts, and parallel requests need concurrency control on every write.
- **Async with an in-memory queue:** rejected. Once the CMS gets 202 it won't retry, so a crash loses events. Idempotency protects against duplicates, not loss.
- **Message broker (RabbitMQ, SQS):** rejected. It adds infrastructure to compose, CI and tests without solving anything we need. It needs a consumer and idempotency just like the inbox, redelivery breaks ordering, and the raw Batch is gone after ack. Its push delivery, dead-letter queues and scale only matter at volumes this service won't see. The worker reads through an inbox interface, so a broker can replace the table later without changing processing.

## Consequences

- The CMS gets no per-event outcome. Outcomes exist only in the EventLog and logs.
- We own retries, poison batches and Inbox cleanup; the CMS no longer retries failed processing.
- Integration tests must trigger or await the worker.
