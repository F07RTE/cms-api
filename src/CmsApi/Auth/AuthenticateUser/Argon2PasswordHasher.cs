using CmsApi.Core.Domain.Users;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Identity;

namespace CmsApi.Auth.AuthenticateUser;

public sealed class Argon2PasswordHasher : IPasswordHasher<StoredUser>
{
    private const int TimeCost = 2;
    private const int MemoryCostKib = 19 * 1024;
    private const int Parallelism = 1;
    private const int HashLengthBytes = 32;
    private const Argon2Type Type = Argon2Type.HybridAddressing;
    private const Argon2Version Version = Argon2Version.Nineteen;

    public string HashPassword(StoredUser user, string password) =>
        Argon2.Hash(password, TimeCost, MemoryCostKib, Parallelism, Type, HashLengthBytes);

    public PasswordVerificationResult VerifyHashedPassword(
        StoredUser user,
        string hashedPassword,
        string providedPassword
    )
    {
        if (!Argon2.Verify(hashedPassword, providedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        return HasCurrentSettings(hashedPassword)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.SuccessRehashNeeded;
    }

    private static bool HasCurrentSettings(string hashedPassword)
    {
        var config = new Argon2Config();
        if (!config.DecodeString(hashedPassword, out var hash))
        {
            return false;
        }

        hash?.Dispose();
        return config.Type == Type
            && config.Version == Version
            && config.TimeCost == TimeCost
            && config.MemoryCost == MemoryCostKib
            && config.Lanes == Parallelism
            && config.HashLength == HashLengthBytes;
    }
}
