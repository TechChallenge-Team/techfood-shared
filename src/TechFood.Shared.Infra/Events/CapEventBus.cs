using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using MediatR;
using Microsoft.Extensions.Logging;
using TechFood.Shared.Application.Events;

namespace TechFood.Shared.Infra.Events
{
    public class CapEventBus : IEventBus
    {
        private readonly ICapPublisher _capPublisher;
        private readonly ILogger<CapEventBus> _logger;
        public CapEventBus(
            ICapPublisher capPublisher,
            ILogger<CapEventBus> logger
            )
        {
            _logger = logger;
            _capPublisher = capPublisher;
        }
        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(T eventMessage, string? callbackName = null, CancellationToken cancellationToken = default) where T : IIntegrationEvent
        {
            string topicName = eventMessage.GetType().Name;

            _logger.LogInformation("Publishing event {EventType} to topic {TopicName}", eventMessage.GetType().Name, topicName);

            return _capPublisher.PublishAsync(
                topicName,
                eventMessage,
                callbackName: callbackName,
                cancellationToken: cancellationToken
            );
        }

        public void Subscribe<T, TH>()
            where T : IIntegrationEvent
            where TH : INotificationHandler<T>
        {
            throw new NotImplementedException();
        }
    }
}
