# TDD — Required for All Changes

1. Failing test is written first
2. Developer reviews the test before implementation begins
3. Implementation makes the test pass — no more, no less
4. No implementation code is written without a corresponding test

## Keep Tests Simple

Cover what the spec asks for and each rule once. No exotic scenarios, no timing-based tests. Offer extra edge cases only if asked.

## Test Conventions

- Unit tests (`CmsApi.UnitTests`) cover pure Core logic: event rules, ordering, validation, cursor, Basic header parsing, backoff
- Integration tests (`CmsApi.IntegrationTests`) go through HTTP with `WebApplicationFactory` against the compose Postgres test database. They run serially; Respawn resets the data before each test
- Worker processing is triggered through the Orchestrator's `DrainInboxAsync()`, never by sleeping
- Test data goes through Orchestrator helpers (`CreateUserAsync`, `PostBatchAsync`, `SeedEntityAsync`, …) — reuse them rather than writing a second pattern
- Assertions use FluentAssertions
- An integration test asserts on the response **and** on what was stored (Content Entity, Tombstone, Event Log) when the endpoint writes. Answering 202 while storing the wrong thing is the bug that reaches production

## Test Naming

Names describe the **actor and the input condition, never the outcome**.

- `<Actor>_<Condition>`: `CmsClient_WithValidBatch`, `AnonymousUser_WithInvalidCredentials`,
  `DefaultUser_WithDisabledContentEntity`
- The fixture class names the subject, so the method does not repeat it

Never `..._Returns404`, `..._ReturnsUnauthorized`, or any name stating the result.
The assertions state the outcome; the name states the case. A name carrying the expected status
has to be edited whenever the status changes, and it duplicates the assertion below it.

## Test File Structure

- **Tests mirror the source tree.** Integration tests mirror the routes
  (`Api/Cms/Events/PostTests.cs`, `Api/Entities/Id/GetTests.cs`, …); unit tests mirror the Core namespaces.
  Do not create cross-cutting fixtures that gather one concern across many classes — someone checking
  whether a class is covered looks in that class's file, and a matrix elsewhere is invisible to them.
- **Helpers and builders go below the tests**, after the last `[Test]`. The tests are the subject
  of the file; fixture-building is detail you read only when a test confuses you.
