using System;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TechFood.Shared.Domain.Common.Interfaces;
using TechFood.Shared.Domain.UoW;
using TechFood.Shared.Infra.Persistence.Contexts;
using TechFood.Shared.Infra.Persistence.UoW;

namespace TechFood.Shared.Infra.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfra<DbContext>(this IServiceCollection services, InfraOptions? options = null) where DbContext : TechFoodContext
    {
        //Context
        services.AddScoped<DbContext>();
        services.AddDbContext<DbContext>((serviceProvider, dbOptions) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            options?.DbContext?.Invoke(dbOptions);
        });

        //UoW
        services.AddScoped<IUnitOfWorkTransaction, UnitOfWorkTransaction>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<DbContext>());

        //DomainEvents
        services.AddScoped<IDomainEventStore>(serviceProvider => serviceProvider.GetRequiredService<DbContext>());

        //MediatR
        services.AddMediatR(options?.AssemblyLoad);

        var mediatR = services.First(s => s.ServiceType == typeof(IMediator));

        services.Replace(ServiceDescriptor.Transient<IMediator, EventualConsistency.Mediator>());
        services.Add(
            new ServiceDescriptor(
                mediatR.ServiceType,
                EventualConsistency.Mediator.ServiceKey,
                mediatR.ImplementationType!,
                mediatR.Lifetime));

        return services;
    }
}
