# Argon2id with a credential cache for Basic Auth

User passwords are hashed with Argon2id (`Isopoh.Cryptography.Argon2`, m=19 MiB, t=2, p=1, PHC string) behind `IPasswordHasher<User>`, instead of the built-in PBKDF2 `PasswordHasher<User>`. Basic Auth verifies the password on every request, and Argon2id is memory-hard, so 100 concurrent requests would need about 1.9 GB. To avoid that, successful verifications are cached in `IMemoryCache` for 60 s, keyed by SHA-256 of the `Authorization` header.

## Considered Options

- **PBKDF2 (`PasswordHasher<User>`)**: built in, no cache needed. Rejected in favour of Argon2id's resistance to GPU cracking.
- **Argon2id without a cache**: memory and CPU cost on every request.

## Consequences

- A changed or removed password stays valid for up to 60 s. That's acceptable while there's no user management.
- Stored hashes are PHC strings, so the settings can change later through rehash-on-verify.
- The CMS Client secret is not hashed: it's a shared secret held in the secret store and compared with `FixedTimeEquals`.
