using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

public sealed class WriteDbContext(DbContextOptions<WriteDbContext> options)
    : CmsDbContext(options);
