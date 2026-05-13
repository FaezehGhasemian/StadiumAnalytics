using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Application.Abstractions.Persistence;
using StadiumAnalytics.Domain.Entities;

namespace StadiumAnalytics.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SensorEvent> SensorEvents => Set<SensorEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
