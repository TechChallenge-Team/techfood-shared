using System;
using System.Linq;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TechFood.Shared.Application.Events;
using TechFood.Shared.Domain.Events;
using TechFood.Shared.Domain.UoW;
using TechFood.Shared.Infra.Events;
using TechFood.Shared.Infra.Extensions;
using TechFood.Shared.Infra.Http;
using TechFood.Shared.Infra.Persistence.Behaviors;
using TechFood.Shared.Infra.Persistence.Contexts;
using TechFood.Shared.Infra.Persistence.UoW;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfra<DbContext>(this IServiceCollection services, InfraOptions? options = null) where DbContext : TechFoodContext
    {
        options ??= new InfraOptions();

        services.TryAddSingleton(Options.Options.Create(options));

        //Context
        services.TryAddScoped<DbContext>();
        services.AddDbContext<DbContext>((serviceProvider, dbOptions) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            options.DbContext?.Invoke(serviceProvider, dbOptions);
        });

        //UoW
        services.TryAddScoped<IUnitOfWorkTransaction, UnitOfWorkTransaction>();
        services.TryAddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<DbContext>());

        //DomainEvents
        services.TryAddScoped<IDomainEventStore>(serviceProvider => serviceProvider.GetRequiredService<DbContext>());

        //MediatR
        services.AddMediatR(options.ApplicationAssembly);

        var mediatR = services.First(s => s.ServiceType == typeof(IMediator));

        services.Replace(ServiceDescriptor.Transient<IMediator, TechFood.Shared.Infra.EventualConsistency.Mediator>());
        services.Add(
            new ServiceDescriptor(
                mediatR.ServiceType,
                TechFood.Shared.Infra.EventualConsistency.Mediator.ServiceKey,
                mediatR.ImplementationType!,
                mediatR.Lifetime));

        // Register SaveChanges handler for all notifications (runs LAST to commit changes)
        // Uses IUnitOfWork abstraction - already registered above as DbContext
        services.AddScoped(typeof(INotificationHandler<>), typeof(SaveChangesNotificationHandler<>));

        //EventBus
        services.TryAddSingleton<IEventBus, RabbitMqEventBus>();

        //ServiceUrlProvider
        services.TryAddSingleton<IServiceUrlProvider, ServiceUrlProvider>();

        //TokenService
        services.AddMemoryCache();
        services.AddHttpClient<ITokenService, TokenService>((services, client) =>
        {
            var serviceUrlProvider = services.GetRequiredService<IServiceUrlProvider>();
            client.BaseAddress = serviceUrlProvider.GetServiceUri("Authentication");
        });

        return services;
    }
}
