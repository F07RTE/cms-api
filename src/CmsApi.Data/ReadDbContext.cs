using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

/// <summary>Logs in as the SELECT-only reader role. Owns no migrations.</summary>
public sealed class ReadDbContext(DbContextOptions<ReadDbContext> options) : CmsDbContext(options);
