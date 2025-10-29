using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TechFood.Shared.Application.Events;

namespace TechFood.Shared.Infra.EventualConsistency
{
    public class Mediator(
        IServiceProvider serviceProvider,
        [FromKeyedServices(Mediator.ServiceKey)] IMediator mediator,
        IEventBus eventBus) : IMediator
    {
        public const string ServiceKey = "mediatR";
        public const string DomainEventsQueueKey = "DomainEventsQueue";
        public const string IntegrationEventsQueueKey = "IntegrationEventsQueue";

        private readonly IMediator _mediator = mediator;
        private readonly IEventBus _eventBus = eventBus;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public async Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);

            if (notification is not INotification instance)
            {
                throw new ArgumentException($"{nameof(notification)} does not implement ${nameof(INotification)}");
            }

            if (IsUserWaitingOnline())
            {
                var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();

                // Check if it's an integration event
                if (instance is IIntegrationEvent integrationEvent)
                {
                    // Store integration events in a separate queue
                    var integrationEventsQueue = httpContextAccessor.HttpContext!.Items
                        .TryGetValue(IntegrationEventsQueueKey, out var value) && value is Queue<IIntegrationEvent> existingIntegrationEvents
                            ? existingIntegrationEvents
                            : new Queue<IIntegrationEvent>();

                    integrationEventsQueue.Enqueue(integrationEvent);
                    httpContextAccessor.HttpContext!.Items[IntegrationEventsQueueKey] = integrationEventsQueue;
                }
                else
                {
                    // Store domain events in the regular queue (to be processed internally)
                    var eventsQueue = httpContextAccessor.HttpContext!.Items
                        .TryGetValue(DomainEventsQueueKey, out var value) && value is Queue<INotification> existingEvents
                            ? existingEvents
                            : new Queue<INotification>();

                    eventsQueue.Enqueue(instance);
                    httpContextAccessor.HttpContext!.Items[DomainEventsQueueKey] = eventsQueue;
                }
            }
            else
            {
                // If the user is not waiting online, handle events immediately
                if (instance is IIntegrationEvent integrationEvent)
                {
                    await _eventBus.PublishAsync(integrationEvent, null, cancellationToken);
                }
                else
                {
                    // Process domain events immediately
                    await _mediator.Publish(instance, cancellationToken);
                }
            }
        }

        private bool IsUserWaitingOnline() => _serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext is not null;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Publish(notification as object, cancellationToken);

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            _mediator.Send(request, cancellationToken);

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            _mediator.Send(request, cancellationToken);
    }
}
