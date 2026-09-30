using ApiMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiMonitor.Infrastructure.Persistence.PostgreSQL;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MonitoredEndpoint> Endpoints => Set<MonitoredEndpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonitoredEndpoint>(e =>
        {
            e.ToTable("monitored_endpoints");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.Property(x => x.Method).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.LastStatus).HasConversion<string>().HasMaxLength(10);
            // O worker filtra por habilitados; o dashboard, por status.
            e.HasIndex(x => x.Enabled);
            e.HasIndex(x => x.LastStatus);
        });
    }
}
