using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

            if (
                context.Items.TryGetValue(Mediator.EventsQueueKey, out var value) &&
                value is Queue<INotification> eventsQueue)
            {
                while (eventsQueue!.TryDequeue(out var @event))
                {
                    await publisher.Publish(@event);
                }
            }

            var events = context.RequestServices.GetRequiredService<IDomainEventStore>();

            foreach (var domainEvent in await events.GetDomainEventsAsync())
            {
                await publisher.Publish(domainEvent);
            }

            await transaction.CommitAsync();
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
