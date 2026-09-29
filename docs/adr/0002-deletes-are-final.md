# Deletes are final (tombstone wins across batches)

A `delete` hard-deletes the Content Entity and writes a Tombstone. Any later CMS Event for that id is skipped, whatever its timestamp. A delete older than the entity's last event still applies: deletes are CMS truth. Timestamps only order events inside one Batch. We assume the CMS never reuses ids. With that assumption, a replayed or late `publish` must not resurrect deleted, possibly confidential content; that risk outweighs supporting a CMS restore we have no evidence exists.

## Considered Options

- **Timestamps everywhere:** store `deleted_at`; a newer publish recreates the entity, an older delete is skipped. Rejected: a delayed replay would bring back deleted content.
- **EventLog as the source of deletes:** rejected. Business rules would depend on a log table, and log retention would silently remove the protection.
