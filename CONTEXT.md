# CMS API

Keeps a private, versioned copy of the content a CMS publishes, and serves it to authenticated users.

## Language

**Content Entity**:
A single content item owned by the CMS, identified by the CMS's id and carrying the latest version and payload received.
_Avoid_: Entity (clashes with EF Core entities), item, record

**CMS Event**:
A notification from the CMS about one Content Entity: `publish`, `unPublish` or `delete`, with a timestamp and, except for `delete`, a version and payload.
_Avoid_: Message, webhook (the webhook is the delivery, not the event)

**Batch**:
The list of CMS Events delivered in one webhook call. Events in a batch are ordered per Content Entity by timestamp.

**Inbox**:
Where a received Batch waits until it is processed. Receiving a Batch and processing it are separate steps.

**Dead Batch**:
A Batch whose processing kept failing for infrastructure reasons and was given up on. It stays in the Inbox for a person to inspect; it is never retried automatically. Invalid CMS Events never make a Batch dead.
_Avoid_: Poison message, failed batch (Failed is an Event Outcome)

**Leader**:
The one worker allowed to process the Inbox. Other workers wait until they can become Leader. A worker that finds it is no longer Leader steps down and stops taking Batches.
_Avoid_: Master, primary, active worker

**Orphaned Batch**:
A Batch a worker started processing but never finished, because it crashed or was stopped. Whenever a worker becomes Leader, it returns every Orphaned Batch to the Inbox to be processed again.
_Avoid_: Stuck batch, abandoned batch

**Tombstone**:
The record that a Content Entity was deleted. Deletion is final: any later CMS Event for that id is ignored.

**Event Outcome**:
What processing did with one CMS Event: Applied, Skipped (Stale, Duplicate, Deleted, Unknown) or Failed (the CMS Event was invalid).

**Event Log**:
The record of the Event Outcome of every CMS Event processed, kept per Batch. Invalid CMS Events are kept in full, so someone can see what arrived.
_Avoid_: Audit log, history

**Stale**:
A CMS Event whose version is lower than the Content Entity's current version.

**Duplicate**:
A CMS Event with the same version as the Content Entity's current version and no newer timestamp.

**CMS Client**:
The organization caller that delivers Batches. Not a User.
_Avoid_: Organization user, account

**User**:
A person who reads Content Entities.
_Avoid_: Account, consumer

**Admin**:
A User who also sees Content Entities that aren't Visible, and can disable or enable them.

**Disabled**:
A Content Entity an Admin has hidden from Users. It's a local override: it doesn't change CMS data, CMS Events never clear it, and it's independent of whether the CMS has the entity published.
_Avoid_: Deactivated, blocked

**Visible**:
A Content Entity a User can see: published by the CMS and not Disabled. Admins see every Content Entity that isn't deleted.
