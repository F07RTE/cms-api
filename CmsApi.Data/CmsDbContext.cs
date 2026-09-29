using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

public abstract class CmsDbContext : DbContext
{
    protected CmsDbContext(DbContextOptions options)
        : base(options) { }
}
