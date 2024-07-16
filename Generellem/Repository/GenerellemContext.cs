using Generellem.Services;

using Microsoft.EntityFrameworkCore;

namespace Generellem.Repository;

public class GenerellemContext(IGenerellemFiles gemFiles) : DbContext
{
    public DbSet<DocumentHash> DocumentHashes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = gemFiles.GetAppDataPath("generellem.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath};Default Timeout=5");
    }
}