using Microsoft.EntityFrameworkCore;

namespace Database;

public sealed class DatabaseContext : DbContext
{
    public DbSet<PdfsDbEntity> Pdfs { get; set; }

    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options) { }
}
