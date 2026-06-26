using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Common;
using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly int? _tenantId;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        if (tenantContext.IsSet)
            _tenantId = tenantContext.TenantId;
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<ChangeQueueEntry> ChangeQueue => Set<ChangeQueueEntry>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
        }

        if (_tenantId.HasValue)
            modelBuilder.Entity<Item>().HasQueryFilter(x => x.TenantId == _tenantId.Value);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateTenantOwnership();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ValidateTenantOwnership();
        return base.SaveChanges();
    }

    private void ValidateTenantOwnership()
    {
        if (!_tenantId.HasValue)
            return;

        foreach (var entry in ChangeTracker.Entries<Item>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            if (entry.Entity.TenantId != _tenantId.Value)
            {
                throw new InvalidOperationException(
                    $"Item tenant id {entry.Entity.TenantId} does not match the active tenant {_tenantId.Value}.");
            }
        }
    }
}
