using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TechFood.Shared.Application.Events;
using TechFood.Shared.Domain.Events;
using TechFood.Shared.Domain.UoW;

namespace TechFood.Shared.Infra.EventualConsistency;

internal class Middleware(
    ILogger<Middleware> logger,
    RequestDelegate next)
{
    private readonly ILogger<Middleware> _logger = logger;
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IUnitOfWorkTransaction transaction)
    {
        try
        {
            await _next(context);

            var mediator = context.RequestServices.GetRequiredKeyedService<IMediator>(Mediator.ServiceKey);

            // Step 1: Get domain events from entities (before saving)
            var domainEventStore = context.RequestServices.GetRequiredService<IDomainEventStore>();
            var domainEventsFromEntities = await domainEventStore.GetDomainEventsAsync();

            // Step 2: Process all domain events from entities (these may generate new events in the queue)
            foreach (var domainEvent in domainEventsFromEntities)
            {
                await mediator.Publish(domainEvent);
            }

            // Step 3: Process any domain events that were added to the queue during handlers execution
            if (context.Items.TryGetValue(Mediator.DomainEventsQueueKey, out var value) &&
                value is Queue<INotification> eventsQueue)
            {
                while (eventsQueue.TryDequeue(out var @event))
                {
                    await mediator.Publish(@event);
                }
            }

            // Step 4: Commit the transaction (persist all changes)
            await transaction.CommitAsync();

            // Step 5: ONLY AFTER successful commit, publish integration events to message broker
            if (context.Items.TryGetValue(Mediator.IntegrationEventsQueueKey, out var integrationValue) &&
                integrationValue is Queue<IIntegrationEvent> integrationEventsQueue)
            {
                var eventBus = context.RequestServices.GetRequiredService<IEventBus>();

                while (integrationEventsQueue.TryDequeue(out var integrationEvent))
                {
                    try
                    {
                        await eventBus.PublishAsync(integrationEvent);
                    }
                    catch (Exception ex)
                    {
                        // Log the error - the transaction is already committed
                        // This is a known limitation: integration events might be lost if broker is down
                        _logger.LogError(ex,
                            "Failed to publish integration event {EventType}. " +
                            "Consider implementing Outbox Pattern for guaranteed delivery.",
                            integrationEvent.GetType().Name);

                        // TODO: Implement Outbox Pattern to store integration events in DB
                        // and retry publishing them asynchronously
                    }
                }
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
