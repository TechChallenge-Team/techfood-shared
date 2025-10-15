using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechFood.Shared.Domain.Entities;
using TechFood.Shared.Domain.Interfaces;
using TechFood.Shared.Domain.UoW;

namespace TechFood.Shared.Infra.Persistence.Contexts;

public abstract class TechFoodContext(DbContextOptions options) : DbContext(options), IUnitOfWork, IDomainEventStore
{
    public Task<IEnumerable<IDomainEvent>> GetDomainEventsAsync()
    {
        // get hold of all the domain events
        var domainEvents = ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity.PopEvents())
            .SelectMany(events => events);

        return Task.FromResult(domainEvents);
    }

    public async Task<bool> CommitAsync()
    {
        var success = await SaveChangesAsync() > 0;
        return success;
    }

    public Task RollbackAsync()
    {
        return Task.CompletedTask;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker
            .Entries<Entity>()
            .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TechFoodContext).Assembly);

        var properties = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetProperties());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(Entity.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);

                modelBuilder.Entity(entityType.ClrType)
                    .HasKey(nameof(Entity.Id));

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Entity.Id))
                    .IsRequired()
                    .ValueGeneratedNever();
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.LogTo(Console.WriteLine);
#endif
    }
}
