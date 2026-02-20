using Microsoft.EntityFrameworkCore;
using PublicSafetyLab.Infrastructure.Persistence.Configurations;
using PublicSafetyLab.Infrastructure.Persistence.Entities;

namespace PublicSafetyLab.Infrastructure.Persistence;

public sealed class PublicSafetyDbContext(DbContextOptions<PublicSafetyDbContext> options) : DbContext(options)
{
    public DbSet<IncidentEntity> Incidents => Set<IncidentEntity>();

    public DbSet<EvidenceItemEntity> EvidenceItems => Set<EvidenceItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new IncidentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EvidenceItemEntityConfiguration());
    }

    public static PublicSafetyDbContext CreateForPostgreSql(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PublicSafetyDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PublicSafetyDbContext(options);
    }
}
