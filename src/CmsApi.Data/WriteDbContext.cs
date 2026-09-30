using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

// Logs in as the owner role. Owns the migrations.
public sealed class WriteDbContext(DbContextOptions<WriteDbContext> options)
    : CmsDbContext(options);
