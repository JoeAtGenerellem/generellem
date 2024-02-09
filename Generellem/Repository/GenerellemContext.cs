using Microsoft.EntityFrameworkCore;

namespace Generellem.Repository;

public class GenerellemContext : DbContext
{
    public DbSet<DocumentHash> DocumentHashes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=generellem.db;Default Timeout=5");
    }
}