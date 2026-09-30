using CmsApi.Core.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsApi.Data.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private const string TableName = "users";

    // Dev seeds, applied with the migrations. Passwords are in the README; these are their hashes.
    private static readonly User Admin = new()
    {
        Id = new Guid("5b1f0c9e-2d4a-4e7b-8c3f-1a6d9e2b7c40"),
        Username = "admin",
        PasswordHash =
            "$argon2id$v=19$m=19456,t=2,p=1$Fv52v2hZpdZghTdBrt+4dg$7ecPyqGcRQUZKWrABnd8aECTIKVWlkjUBU3FQSZAdpY",
        Role = UserRole.Admin,
    };

    private static readonly User Reader = new()
    {
        Id = new Guid("8e3a6d21-7c5b-4f90-a1e4-3d2c8b6f5a19"),
        Username = "reader",
        PasswordHash =
            "$argon2id$v=19$m=19456,t=2,p=1$skZWs2Vzjq8tXNNLb9nqiA$rWK4wkmROwm4dyYDeYdm/CqARAXhmJcIfao5cBrOfao",
        Role = UserRole.User,
    };

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(TableName);
        // Lowercased on every write, so the unique index and the login lookup ignore case.
        builder
            .Property(user => user.Username)
            .HasConversion(username => username.ToLowerInvariant(), username => username);
        builder.HasIndex(user => user.Username).IsUnique();
        builder.Property(user => user.Role).HasConversion<string>();
        builder.HasData(Admin, Reader);
    }
}
