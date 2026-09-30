using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

/// <summary>Logs in as the owner role. Owns the migrations.</summary>
public sealed class WriteDbContext(DbContextOptions<WriteDbContext> options)
    : CmsDbContext(options);
