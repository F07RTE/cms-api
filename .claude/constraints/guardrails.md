# Change Constraints

- NuGet packages are not added without my approval, beyond those the spec names
- Route shapes, status codes and response DTOs follow the REST API contract. They are not renamed or changed without re-opening that decision — they are the contract with the CMS and consumers
- Event processing rules follow the decision in `.scratch/cms-api-spec/` and ADRs 0001–0003. If a change contradicts one, stop and flag it instead of silently overriding
- Migrations are never edited once applied — add a new one
- Secrets never go in `appsettings.json`; only the dev CMS credential lives in `appsettings.Development.json`. Everything else comes from user-secrets or env vars
- Credentials, password hashes and the `Authorization` header are never logged or returned
- Read the relevant files before proposing any change
- Understand existing patterns before introducing new ones
- No commits I will commit everything at the end. This is about _my_ history: do not commit in my
  checkout, and never merge or push to a shared branch.

## Review Checkpoints

After completing each task, stop, give a overview of what was done and wait for explicit approval before
proceeding to the next task. Do not continue automatically.
