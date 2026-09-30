using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data;

public sealed class ReadDbContext(DbContextOptions<ReadDbContext> options) : CmsDbContext(options);
