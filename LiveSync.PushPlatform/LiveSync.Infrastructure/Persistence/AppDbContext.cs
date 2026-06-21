using LiveSync.Domain.Common;
using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ChangeQueueEntry> ChangeQueue => Set<ChangeQueueEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}