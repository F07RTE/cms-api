using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

// Logs in as the SELECT-only reader role. Owns no migrations.
public sealed class ReadDbContext(DbContextOptions<ReadDbContext> options) : CmsDbContext(options);
