using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Infrastructure.Data.Configurations;
using StadiumAnalytics.Infrastructure.Data.Entities;

namespace StadiumAnalytics.Infrastructure.Data;

public sealed class StadiumDbContext : DbContext
{
    public DbSet<GateSensorEventEntity> GateSensorEvents => Set<GateSensorEventEntity>();
    public DbSet<FailedEventEntity> FailedEvents => Set<FailedEventEntity>();

    public StadiumDbContext(DbContextOptions<StadiumDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new GateSensorEventConfiguration());
        modelBuilder.ApplyConfiguration(new FailedEventConfiguration());
    }
}
