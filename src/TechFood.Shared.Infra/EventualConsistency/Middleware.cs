using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TechFood.Shared.Application.Events;
using TechFood.Shared.Domain.Events;
using TechFood.Shared.Domain.UoW;

namespace TechFood.Shared.Infra.EventualConsistency;

internal class Middleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IUnitOfWorkTransaction transaction)
    {
        try
        {
            await _next(context);

            var publisher = context.RequestServices.GetRequiredKeyedService<IMediator>(Mediator.ServiceKey);

            // Process domain events (internal processing with MediatR)
            if (
                context.Items.TryGetValue(Mediator.DomainEventsQueueKey, out var value) &&
                value is Queue<INotification> eventsQueue)
            {
                while (eventsQueue!.TryDequeue(out var @event))
                {
                    await publisher.Publish(@event);
                }
            }

            // Get domain events from store and publish them internally
            var events = context.RequestServices.GetRequiredService<IDomainEventStore>();

            foreach (var domainEvent in await events.GetDomainEventsAsync())
            {
                await publisher.Publish(domainEvent);
            }

            await transaction.CommitAsync();

            // Process integration events (publish to message broker)
            if (
                context.Items.TryGetValue(Mediator.IntegrationEventsQueueKey, out var integrationValue) &&
                integrationValue is Queue<IIntegrationEvent> integrationEventsQueue)
            {
                var integrationEventPublisher = context.RequestServices.GetService<IIntegrationEventPublisher>();

                if (integrationEventPublisher != null)
                {
                    while (integrationEventsQueue!.TryDequeue(out var integrationEvent))
                    {
                        await integrationEventPublisher.PublishAsync(integrationEvent);
                    }
                }
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
        }
    }
}
