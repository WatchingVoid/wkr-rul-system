using Microsoft.EntityFrameworkCore;
using Backend.Api.Entities;

namespace Backend.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TelemetrySpindleEntity> TelemetrySpindle => Set<TelemetrySpindleEntity>();
    public DbSet<RulPredictionEntity> RulPredictions => Set<RulPredictionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetrySpindleEntity>()
            .HasIndex(x => new { x.MachineId, x.ToolId, x.Ts });

        modelBuilder.Entity<RulPredictionEntity>()
            .HasIndex(x => new { x.MachineId, x.ToolId, x.Ts });
    }
}