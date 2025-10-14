using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TechFood.Shared.Infra.EventualConsistency
{
    public class Mediator(
        IServiceProvider serviceProvider,
        [FromKeyedServices(Mediator.ServiceKey)] IMediator mediator) : IMediator
    {
        public const string ServiceKey = "mediatR";
        public const string EventsQueueKey = "EventsQueue";

        private readonly IMediator _mediator = mediator;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);

            if (notification is not INotification instance)
            {
                throw new ArgumentException($"{nameof(notification)} does not implement ${nameof(INotification)}");
            }

            if (IsUserWaitingOnline())
            {
                var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();

                // fetch queue from http context or create a new queue if it doesn't exist
                var eventsQueue = httpContextAccessor.HttpContext!.Items
                    .TryGetValue(EventsQueueKey, out var value) && value is Queue<INotification> existingEvents
                        ? existingEvents
                        : new Queue<INotification>();

                // add the event to the end of the queue
                eventsQueue.Enqueue(instance);

                // store the queue in the http context
                httpContextAccessor.HttpContext!.Items[EventsQueueKey] = eventsQueue;
            }
            else
            {
                // if the user is not waiting online, publish the event immediately
                _mediator.Publish(instance, cancellationToken);
            }

            return Task.CompletedTask;
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
