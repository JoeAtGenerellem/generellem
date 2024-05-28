using Generellem.Services;
using Microsoft.EntityFrameworkCore;

namespace Generellem.Repository;

public class GenerellemContext : DbContext
{
    public DbSet<DocumentHash> DocumentHashes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = GenerellemFiles.GetAppDataPath("generellem.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath};Default Timeout=5");
    }
}